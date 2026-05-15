# Deals module

## 1. Статус

`Deals` реализован как MVP/Core-модуль CRM после `Identity`, `Clients` и `Catalog`.

Модуль build-tested и подключён в WebApi через:

- `AddDealsApplication()`;
- `AddDealsInfrastructure()`;
- application part для `Deals.Presentation`.

Для складского списания, бонусной логики и возвратов `Deals.Application` использует abstractions из `Warehouse.Application` и `Bonus.Application`, без зависимости от `Warehouse.Infrastructure`/`Bonus.Infrastructure` и без внешних EF FK между модулями.

Миграции:

```text
AddDealsMvpModule
AddDealsReturnsCore
```

Файлы:

```text
Infrastructure/Migrations/20260427004819_AddDealsMvpModule.cs
Infrastructure/Migrations/20260427004819_AddDealsMvpModule.Designer.cs
Infrastructure/Migrations/20260507121737_AddDealsReturnsCore.cs
```

После реализации Warehouse Core, Bonus Core, Returns Core и Audit Core модуль Deals интегрирован со складским списанием, бонусным списанием/начислением, возвратами по successful final deals и журналированием ключевых действий.

Returns - не отдельный модуль. Это расширение Deals module, которое закрывает базовый жизненный цикл сделки:

- successful final deal;
- Warehouse `Sale` movement;
- Bonus `WriteOff` / `Accrual`;
- `DealReturn`;
- Warehouse `Return` movement;
- Bonus `Refund` / `CorrectionDecrease`.

Audit Core фиксирует create/update/stage change/deactivate событий Deals и create/update/complete/cancel событий DealReturn через manual audit calls.

Email Campaigns Core использует Deals read model для inactive-client automation: клиент считается кандидатом только при наличии хотя бы одной active deal в successful final stage с `ClosedAt`, и latest successful final deal должна быть старше настроенного `InactivityDays`. Внешних FK/navigation между Email и Deals нет.

## 2. Проекты модуля

Модуль состоит из четырёх проектов:

- `Deals.Domain` - entities и enums.
- `Deals.Application` - CQRS use cases, validators, DTO, calculation/default-stage services, repository/lookup abstractions.
- `Deals.Infrastructure` - EF configurations, repositories, lookup services, DI.
- `Deals.Presentation` - thin API controllers.

Собственного DbContext нет. Используется общий:

```text
Infrastructure/Persistence/ApplicationDbContext.cs
```

## 3. Domain model

### Deal

`Deal` - сделка/заказ организации.

Поля:

- `Id`
- `OrganizationId`
- `ClientId`
- `ResponsibleUserId`
- `StageId`
- `TotalAmount`
- `DiscountAmount`
- `BonusPointsUsed`
- `BonusDiscountAmount`
- `FinalAmount`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `ClosedAt`
- `Notes`
- `Items`
- `StageHistory`

Правила:

- все deals tenant-scoped через `OrganizationId`;
- `ClientId` обязателен и проверяется через `IClientLookupService`;
- `ResponsibleUserId` обязателен и проверяется через `IUserLookupService`;
- `StageId` ссылается только на `DealStage` внутри Deals module;
- `IsActive = true` при создании;
- `ClosedAt` устанавливается при переходе на final stage;
- если текущий stage имеет `IsFinal = true`, запрещены `UpdateDeal`, `ChangeDealStage`, `DeactivateDeal`;
- физическое удаление сделки не используется.

### DealItem

`DealItem` - позиция сделки.

Поля:

- `Id`
- `OrganizationId`
- `DealId`
- `ItemType`
- `ItemId`
- `StorageId`
- `NameSnapshot`
- `Quantity`
- `PriceAtMoment`
- `DiscountType`
- `DiscountValue`
- `DiscountAmount`
- `TotalAmount`
- `FinalAmount`

Правила:

- `ItemType = Product` требует `StorageId != null`;
- `ItemType = Service` требует `StorageId == null`;
- `StorageId` используется Warehouse Core при successful final stage;
- stock не резервируется при создании или обновлении сделки;
- stock quantity проверяется при переводе сделки в successful final stage;
- stock movement `Sale` создаётся только при successful final stage;
- `NameSnapshot` и `PriceAtMoment` сохраняют snapshot Catalog item на момент сделки;
- `ItemId` указывает на Product/Service из Catalog через Guid only, без FK.

### DealStage

`DealStage` - tenant-specific редактируемый этап сделки.

Поля:

- `Id`
- `OrganizationId`
- `Name`
- `Order`
- `IsFinal`
- `IsSuccessful`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

В Deals не используется enum `Status`. Вместо него используется таблица `DealStages`.

Default stages создаются lazy при первом использовании Deals module:

1. `New`
2. `InProgress`
3. `Completed`
4. `Cancelled`

Lazy initialization реализован через `IDealStageInitializer` в `Deals.Application`. Он работает через:

- `IDealStageRepository`;
- `IUnitOfWork`;
- `IDateTimeProvider`.

Он не использует EF напрямую.

### DealStageHistory

`DealStageHistory` - история изменения этапов сделки.

Поля:

- `Id`
- `OrganizationId`
- `DealId`
- `OldStageId`
- `NewStageId`
- `ChangedByUserId`
- `ChangedAt`

Запись создаётся:

- при создании сделки с `OldStageId = null`;
- при каждом изменении `Deal.StageId`.

`ChangedByUserId` хранится как Guid пользователя Identity, без FK на Identity.

### DealReturn

`DealReturn` - возврат по успешно завершённой сделке. Возвраты являются частью Deals module, а не отдельным модулем.

Поля:

- `Id`
- `OrganizationId`
- `DealId`
- `Status`
- `Reason`
- `CancellationReason`
- `TotalAmount`
- `BonusPointsReturned`
- `BonusAccrualReversed`
- `MoneyAmount`
- `CreatedAt`
- `CreatedByUserId`
- `CompletedAt`
- `CompletedByUserId`
- `CancelledAt`
- `CancelledByUserId`
- `UpdatedAt`
- `Items`

Статусы `DealReturnStatus`:

- `Draft`
- `Completed`
- `Cancelled`

Правила:

- новый возврат создаётся в `Draft`;
- `Draft` не влияет на Warehouse и Bonus;
- `Draft` можно обновить, завершить или отменить;
- `Completed` применяет Warehouse/Bonus effects;
- `Cancelled` не применяет Warehouse/Bonus effects;
- `Completed` и `Cancelled` immutable;
- возврат можно создать только по active deal в successful final stage;
- unsuccessful final/cancelled deal не допускает возврат;
- Draft returns не резервируют количество;
- remaining quantity считается только по completed returns.

### DealReturnItem

`DealReturnItem` - позиция возврата.

Поля:

- `Id`
- `OrganizationId`
- `DealReturnId`
- `DealId`
- `DealItemId`
- `ItemType`
- `ItemId`
- `StorageId`
- `NameSnapshot`
- `Quantity`
- `ReturnAmount`

Правила:

- возвраты могут включать Product и Service позиции;
- по одной сделке допускаются полный возврат, частичный возврат и несколько частичных возвратов;
- суммарно по каждой `DealItem` нельзя вернуть больше, чем было продано;
- Product return требует resolved `StorageId`: request override `StorageId` или исходный `DealItem.StorageId`;
- Service return хранит `StorageId = null` и не затрагивает Warehouse.

## 4. Calculations

Для позиции:

```text
TotalAmount = Quantity * PriceAtMoment
FinalAmount = TotalAmount - DiscountAmount
```

Discount rules:

- `None` => `0`;
- `Percent` => `TotalAmount * DiscountValue / 100`;
- `Fixed` => `DiscountValue`;
- fixed discount capped до `TotalAmount`.

Для сделки:

```text
TotalAmount = sum(DealItems.TotalAmount)
DiscountAmount = sum(DealItems.DiscountAmount)
amountBeforeBonus = TotalAmount - DiscountAmount
BonusPointsUsed = applied bonus points from Bonus Core
BonusDiscountAmount = monetary bonus discount in BYN from Bonus Core
FinalAmount = TotalAmount - DiscountAmount - BonusDiscountAmount
```

После Bonus Core:

- requested `BonusPointsUsed` трактуется как "использовать до указанного количества";
- в Deal сохраняется applied `BonusPointsUsed`;
- `BonusDiscountAmount` рассчитывается через `BonusSettings.PointValue`;
- `BonusPointsUsed` и `BonusDiscountAmount` не обязаны быть равны;
- `BonusPointsUsed >= 0`;
- `BonusDiscountAmount >= 0`;
- `FinalAmount >= 0`;
- discount capped по requested points, bonus balance, `MaxPaymentPercent` и `amountBeforeBonus`.

Returns Core использует отдельный расчёт возврата:

```text
returnItemAmount = round(DealItem.FinalAmount * returnedQuantity / DealItem.Quantity, 2)
TotalAmount = sum(returnItemAmount)
amountBeforeBonus = Deal.TotalAmount - Deal.DiscountAmount
returnRatio = TotalAmount / amountBeforeBonus
bonusDiscountMoneyShare = round(Deal.BonusDiscountAmount * returnRatio, 2)
MoneyAmount = max(0, round(TotalAmount - bonusDiscountMoneyShare, 2))
```

Если `amountBeforeBonus == 0` и `TotalAmount == 0`, `returnRatio = 0`. Если `amountBeforeBonus == 0` и `TotalAmount > 0`, возвращается `ConflictException`.

Money amounts округляются до 2 знаков. Bonus points округляются до 3 знаков. `Deal.FinalAmount` не используется как база `returnRatio`, потому что он уже уменьшен на `BonusDiscountAmount`.

## 5. Lookups and module boundaries

Deals не создаёт EF navigation/FK на внешние модули.

Внешние связи хранятся как Guid:

- `ClientId` из Clients;
- `ResponsibleUserId` из Identity;
- `ItemId` из Catalog Product/Service;
- `StorageId` из Warehouse.

Application abstractions:

- `IClientLookupService`;
- `IUserLookupService`;
- `ICatalogLookupService`.
- `IWarehouseDealCompletionService` из `Warehouse.Application`.
- `IWarehouseDealReturnService` из `Warehouse.Application`.
- `IBonusDealDiscountService`, `IBonusDealCompletionService` и `IBonusDealReturnService` из `Bonus.Application`.
- `IDealReturnRepository`.

Infrastructure implementations используют общий `ApplicationDbContext`, но не создают EF relationships между Deals и Clients/Catalog/Identity/Warehouse/Bonus.

`ICatalogLookupService` возвращает `CatalogItemSnapshot`:

- `ItemId`;
- `ItemType`;
- `Name`;
- `Price`;
- resolved `DealDiscountType`;
- `DiscountValue`;
- `IsActive`.

Catalog `DiscountType.Inherit` resolves через category chain. Если root остаётся `Inherit`, используется `DealDiscountType.None`.

## 5.1 Warehouse integration

`ChangeDealStage` вызывает `IWarehouseDealCompletionService`, когда новая stage имеет:

- `IsFinal = true`;
- `IsSuccessful = true`.

Warehouse Core выполняет фактическое списание:

- обрабатывает только Product items;
- игнорирует Service items;
- проверяет active Storage по `DealItem.StorageId`;
- проверяет существующий `ProductStock`;
- проверяет достаточность `Quantity`;
- уменьшает остаток;
- создаёт `StockMovement` с `Type = Sale`;
- заполняет `DealId` и `CreatedByUserId`;
- защищает от повторного списания через проверку Sale movement by `DealId`.

Если остатка недостаточно хотя бы по одной Product-позиции, handler получает `ConflictException`: сделка не переходит в successful final stage, а складские изменения не сохраняются.

Если Deal переводится в unsuccessful final stage, например `Cancelled`, Warehouse ничего не списывает.

После Bonus Core общий порядок successful final `ChangeDealStage`:

1. Warehouse completion.
2. Bonus completion.
3. Stage change.
4. `DealStageHistory`.
5. Один общий `IUnitOfWork.SaveChangesAsync`.

Если Bonus completion падает после Warehouse completion, складские изменения не сохраняются, потому что общий `UnitOfWork` ещё не сохранён.

## 5.2 Bonus integration

`CreateDeal` и `UpdateDeal` используют `IBonusDealDiscountService` из `Bonus.Application`.

Правила:

- requested `BonusPointsUsed` означает "использовать до указанного количества";
- сохраняется applied `BonusPointsUsed`, а не raw requested value;
- `BonusDiscountAmount` хранит денежную скидку в BYN;
- `BonusDiscountAmount` рассчитывается через `BonusSettings.PointValue`;
- если бонусы отключены или баланс равен нулю, requested points больше 0 приводят к `ConflictException`;
- если requested points больше разрешённого лимита, применяется cap.

`ChangeDealStage` вызывает `IBonusDealCompletionService` при successful final stage.

Bonus completion:

- списывает бонусы и создаёт `WriteOff`, если `Deal.BonusPointsUsed > 0`;
- начисляет бонусы и создаёт `Accrual`, если Bonus enabled и начисление разрешено;
- если `AccrueOnBonusPayment = false` и сделка использовала бонусы, accrual не создаётся;
- если `AccrueOnBonusPayment = true`, accrual считается от `Deal.FinalAmount`;
- если бонусов недостаточно, сделка не завершается;
- duplicate processing защищён через `ConflictException`, если по `DealId` уже есть `WriteOff` или `Accrual`.

## 5.3 Returns Core

Returns Core реализован внутри Deals module.

Назначение:

- возврат создаётся по успешно завершённой active сделке;
- поддерживаются полный и частичный возвраты;
- по одной сделке допускается несколько частичных возвратов;
- нельзя вернуть больше, чем было продано по каждой `DealItem`;
- возвраты могут включать Product и Service позиции.

Flow:

1. `CreateDealReturn` создаёт `Draft` и не вызывает Warehouse/Bonus.
2. `UpdateDealReturn` заменяет позиции только у `Draft`.
3. `CancelDealReturn` переводит `Draft` в `Cancelled` без Warehouse/Bonus effects.
4. `CompleteDealReturn` повторно валидирует сделку и remaining quantity.
5. Deals вызывает Warehouse return service.
6. Deals вызывает Bonus return service.
7. `DealReturn` переводится в `Completed`.
8. Выполняется один общий `IUnitOfWork.SaveChangesAsync`.

Если Bonus return падает после Warehouse return, складские изменения не сохраняются, потому что integration services не вызывают `SaveChangesAsync`, а общий UnitOfWork ещё не сохранён.

Warehouse behavior:

- Product return увеличивает `ProductStock`;
- Service return не затрагивает Warehouse;
- создаётся `StockMovement` с `Type = Return`;
- `StockMovement.DealId = DealReturn.DealId`;
- `StockMovement.SourceReturnId = DealReturn.Id`;
- `SourceReturnId` - nullable Guid correlation id без FK.

Bonus behavior:

- `Refund` возвращает клиенту ранее списанные бонусы пропорционально возврату;
- `CorrectionDecrease` откатывает ранее начисленные бонусы пропорционально возврату;
- `BonusTransaction.SourceReturnId = DealReturn.Id`;
- `SourceReturnId` - nullable Guid correlation id без FK;
- уже обработанные return-origin операции считаются по `DealId`, `SourceReturnId != null` и `Type = Refund / CorrectionDecrease`;
- `Reason` является информационным полем и не парсится для бизнес-логики;
- если на счёте клиента недостаточно бонусов для отката начисления, completion возврата завершается `ConflictException`.

## 5.4 Audit integration

Deals handlers are integrated with Audit Core through `IAuditLogService`.

Audited Deal events:

- deal created;
- deal updated;
- deal stage changed;
- deal deactivated.

Audited DealReturn events:

- deal return created;
- deal return updated;
- deal return completed;
- deal return cancelled.

Stage change audit stores old/new stage ids and names where available. Completed return audit stores return summary values such as total amount, money amount, bonus points returned and reversed accrual. Free-form notes/reasons are not logged as audit details.

## 6. EF/database details

EF configurations находятся в:

- `Deals.Infrastructure/Configurations/DealConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealItemConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealStageConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealStageHistoryConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealReturnConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealReturnItemConfiguration.cs`

Таблицы:

- `Deals`
- `DealItems`
- `DealStages`
- `DealStageHistories`
- `DealReturns`
- `DealReturnItems`

Внутренние FK внутри Deals module:

- `Deals.StageId` -> `DealStages.Id`
- `DealItems.DealId` -> `Deals.Id`
- `DealStageHistories.DealId` -> `Deals.Id`
- `DealStageHistories.OldStageId` -> `DealStages.Id`
- `DealStageHistories.NewStageId` -> `DealStages.Id`
- `DealReturns.DealId` -> `Deals.Id`
- `DealReturnItems.DealReturnId` -> `DealReturns.Id`
- `DealReturnItems.DealItemId` -> `DealItems.Id`

Внешних FK нет:

- нет FK на Clients;
- нет FK на Catalog;
- нет FK на Identity;
- нет FK на Warehouse.
- нет FK на Bonus.

`DealReturns` indexes:

- `OrganizationId`
- `OrganizationId + DealId`
- `OrganizationId + Status`
- `OrganizationId + CreatedAt`

`DealReturnItems` indexes:

- `OrganizationId`
- `OrganizationId + DealReturnId`
- `OrganizationId + DealId`
- `OrganizationId + DealItemId`
- `OrganizationId + ItemId`

Deals EF configurations подключены через DI-based mechanism:

```csharp
services.AddSingleton<IEfConfigurationAssemblyProvider>(
    new EfConfigurationAssemblyProvider(typeof(DealConfiguration).Assembly));
```

`ApplicationDbContext` не ссылается на `Deals.Infrastructure` напрямую.

Миграция `20260506131632_AddBonusCoreModule` меняет precision `Deals.BonusPointsUsed` на `decimal(18,3)`. `Deals.BonusDiscountAmount` остаётся `decimal(18,2)`.

Миграция `20260507121737_AddDealsReturnsCore` добавляет `DealReturns`, `DealReturnItems`, `StockMovements.SourceReturnId` и `BonusTransactions.SourceReturnId`.

## 7. Application layer

Use cases:

Deals:

- `CreateDeal`
- `GetDeals`
- `GetDealById`
- `UpdateDeal`
- `ChangeDealStage`
- `DeactivateDeal`

Stages:

- `GetDealStages`
- `CreateDealStage`
- `UpdateDealStage`
- `DeactivateDealStage`

Returns:

- `GetDealReturns`
- `GetDealReturnById`
- `CreateDealReturn`
- `UpdateDealReturn`
- `CompleteDealReturn`
- `CancelDealReturn`

Handlers:

- берут `OrganizationId` и `UserId` из `ICurrentUserService`;
- отклоняют отсутствие tenant context через `UnauthorizedException`;
- используют repositories/lookups;
- сохраняют через `IUnitOfWork`;
- используют `IDateTimeProvider`;
- не содержат controller logic.

## 8. Endpoints

Deal stages:

```http
GET /api/deals/stages
POST /api/deals/stages
PUT /api/deals/stages/{id}
DELETE /api/deals/stages/{id}
```

Deals:

```http
GET /api/deals
GET /api/deals/{id}
POST /api/deals
PUT /api/deals/{id}
PUT /api/deals/{id}/stage
DELETE /api/deals/{id}
```

Returns:

```http
GET /api/deals/{dealId}/returns
GET /api/deals/returns/{id}
POST /api/deals/{dealId}/returns
PUT /api/deals/returns/{id}
POST /api/deals/returns/{id}/complete
POST /api/deals/returns/{id}/cancel
```

Controllers are thin:

- accept route/body/query params;
- create command/query where route id is needed;
- call `IMediator`;
- return `IActionResult`.

## 9. Permissions

Deals uses existing Identity permission system.

Module code:

```text
Deals
```

Permissions:

- `Deals / Read`
- `Deals / Create`
- `Deals / Update`
- `Deals / Delete`

Mapping:

- `GET /api/deals/stages` => `Read`
- `POST /api/deals/stages` => `Create`
- `PUT /api/deals/stages/{id}` => `Update`
- `DELETE /api/deals/stages/{id}` => `Delete`
- `GET /api/deals` => `Read`
- `GET /api/deals/{id}` => `Read`
- `POST /api/deals` => `Create`
- `PUT /api/deals/{id}` => `Update`
- `PUT /api/deals/{id}/stage` => `Update`
- `DELETE /api/deals/{id}` => `Delete`
- `GET /api/deals/{dealId}/returns` => `Read`
- `GET /api/deals/returns/{id}` => `Read`
- `POST /api/deals/{dealId}/returns` => `Update`
- `PUT /api/deals/returns/{id}` => `Update`
- `POST /api/deals/returns/{id}/complete` => `Update`
- `POST /api/deals/returns/{id}/cancel` => `Update`

## 10. What Deals Core intentionally does not include

Deals Core intentionally does not include:

- stock reservations;
- payments;
- invoices;
- payment refunds;
- promo codes;
- analytics;
- chat, который реализован отдельным `Chat` module через `DealId`.

Returns Core реализован внутри Deals. Deals не занимается payment refunds, invoices или supplier returns.

## 11. Verification

Implementation verification history:

```bash
dotnet build CrmSystem.slnx
dotnet ef migrations add AddDealsMvpModule --project Infrastructure --startup-project CrmSystem
dotnet ef database update --project Infrastructure --startup-project CrmSystem
```

Observed MVP result during implementation:

- build succeeded;
- migration was created in `Infrastructure/Migrations`;
- database update succeeded;
- warnings: 0;
- errors: 0.

Returns Core implementation adds migration `20260507121737_AddDealsReturnsCore` in `Infrastructure/Migrations`. This documentation update did not run build, EF migrations or database update.
