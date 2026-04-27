# Deals module

## 1. Статус

`Deals` реализован как MVP-модуль CRM после `Identity`, `Clients` и `Catalog`.

Модуль build-tested и подключён в WebApi через:

- `AddDealsApplication()`;
- `AddDealsInfrastructure()`;
- application part для `Deals.Presentation`.

Миграция:

```text
AddDealsMvpModule
```

Файлы:

```text
Infrastructure/Migrations/20260427004819_AddDealsMvpModule.cs
Infrastructure/Migrations/20260427004819_AddDealsMvpModule.Designer.cs
```

Deals MVP не считается полностью финальным CRM-модулем до будущих интеграций:

- Bonus;
- Warehouse;
- Returns;
- Audit.

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
- существование `StorageId` не проверяется, потому что Warehouse module ещё не реализован;
- stock quantity не проверяется;
- stock movements не создаются;
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
appliedBonusPoints = min(requestedBonusPoints, TotalAmount - DiscountAmount)
BonusPointsUsed = appliedBonusPoints
BonusDiscountAmount = appliedBonusPoints
FinalAmount = TotalAmount - DiscountAmount - BonusDiscountAmount
```

Для MVP:

- `1 bonus point = 1 BYN`;
- bonus balance не проверяется;
- BonusSettings не проверяются;
- BonusTransactions не создаются.

## 5. Lookups and module boundaries

Deals не создаёт EF navigation/FK на внешние модули.

Внешние связи хранятся как Guid:

- `ClientId` из Clients;
- `ResponsibleUserId` из Identity;
- `ItemId` из Catalog Product/Service;
- `StorageId` из будущего Warehouse.

Application abstractions:

- `IClientLookupService`;
- `IUserLookupService`;
- `ICatalogLookupService`.

Infrastructure implementations используют общий `ApplicationDbContext`, но не создают EF relationships между Deals и Clients/Catalog/Identity/Warehouse.

`ICatalogLookupService` возвращает `CatalogItemSnapshot`:

- `ItemId`;
- `ItemType`;
- `Name`;
- `Price`;
- resolved `DealDiscountType`;
- `DiscountValue`;
- `IsActive`.

Catalog `DiscountType.Inherit` resolves через category chain. Если root остаётся `Inherit`, используется `DealDiscountType.None`.

## 6. EF/database details

EF configurations находятся в:

- `Deals.Infrastructure/Configurations/DealConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealItemConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealStageConfiguration.cs`
- `Deals.Infrastructure/Configurations/DealStageHistoryConfiguration.cs`

Таблицы:

- `Deals`
- `DealItems`
- `DealStages`
- `DealStageHistories`

Внутренние FK внутри Deals module:

- `Deals.StageId` -> `DealStages.Id`
- `DealItems.DealId` -> `Deals.Id`
- `DealStageHistories.DealId` -> `Deals.Id`
- `DealStageHistories.OldStageId` -> `DealStages.Id`
- `DealStageHistories.NewStageId` -> `DealStages.Id`

Внешних FK нет:

- нет FK на Clients;
- нет FK на Catalog;
- нет FK на Identity;
- нет FK на Warehouse.

Deals EF configurations подключены через DI-based mechanism:

```csharp
services.AddSingleton<IEfConfigurationAssemblyProvider>(
    new EfConfigurationAssemblyProvider(typeof(DealConfiguration).Assembly));
```

`ApplicationDbContext` не ссылается на `Deals.Infrastructure` напрямую.

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

## 10. What Deals MVP intentionally does not include

Deals MVP intentionally does not include:

- Bonus module;
- bonus account validation;
- BonusSettings validation;
- BonusTransactions;
- bonus accrual/write-off;
- Warehouse module;
- storage existence validation;
- stock quantity checks;
- stock movements;
- Returns entities/endpoints;
- payments;
- invoices;
- refunds;
- promo codes;
- analytics;
- chat;
- audit events.

Returns belong to Deals module, but are future iteration after Bonus/Warehouse integrations.

## 11. Verification

Implementation verification completed:

```bash
dotnet build CrmSystem.slnx
dotnet ef migrations add AddDealsMvpModule --project Infrastructure --startup-project CrmSystem
dotnet ef database update --project Infrastructure --startup-project CrmSystem
```

Observed result during implementation:

- build succeeded;
- migration was created in `Infrastructure/Migrations`;
- database update succeeded;
- warnings: 0;
- errors: 0.
