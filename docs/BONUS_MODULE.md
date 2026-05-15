# Bonus Module

## Назначение

Bonus отвечает за:

- настройки бонусной системы организации;
- бонусные счета клиентов;
- историю бонусных операций;
- ручную корректировку бонусного баланса;
- расчёт бонусной скидки при создании и обновлении сделки;
- списание использованных бонусов при успешном завершении сделки;
- начисление бонусов при успешном завершении сделки;
- возврат списанных бонусов и откат начислений при завершении `DealReturn`.

## Архитектура

Bonus реализован как отдельный модуль:

- `Bonus.Domain`;
- `Bonus.Application`;
- `Bonus.Infrastructure`;
- `Bonus.Presentation`.

Архитектурные правила:

- используется общий `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `BonusDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations регистрируются через `IEfConfigurationAssemblyProvider` в `AddBonusInfrastructure()`;
- `ApplicationDbContext` не ссылается на `Bonus.Infrastructure` напрямую;
- внешних FK/navigation на Clients, Deals, Catalog, Identity и Organization нет;
- связи с другими модулями выполняются только через Guid;
- Bonus интегрирован в Deals через abstractions из `Bonus.Application`.

Межмодульные идентификаторы:

- `ClientId` - Guid клиента из Clients;
- `DealId` - Guid сделки из Deals, nullable;
- `SourceReturnId` - Guid возврата из Deals, nullable, correlation id без FK;
- `CreatedByUserId` - Guid пользователя Identity, nullable;
- `OrganizationId` - Guid организации, без FK на Identity Organization.

## Основные сущности

### BonusSettings

`BonusSettings` - настройки бонусной системы организации.

Поля:

- `Id`
- `OrganizationId`
- `IsEnabled`
- `PointValue`
- `AccrualType`
- `AccrualValue`
- `MaxPaymentPercent`
- `AccrueOnBonusPayment`
- `CreatedAt`
- `UpdatedAt`

Правила:

- одна запись настроек на организацию;
- `PointValue` определяет, сколько BYN даёт 1 бонусный балл;
- `PointValue > 0`;
- `MaxPaymentPercent` ограничивает процент оплаты сделки бонусами;
- `AccrueOnBonusPayment` управляет начислением бонусов по сделкам, где бонусы уже были использованы;
- настройки создаются лениво через `GET /api/bonus/settings`;
- если настройки отсутствуют во время расчёта/закрытия сделки, бонусная система считается выключенной.

Default settings:

- `IsEnabled = false`;
- `PointValue = 1.00`;
- `AccrualType = Percent`;
- `AccrualValue = 0`;
- `MaxPaymentPercent = 0`;
- `AccrueOnBonusPayment = false`.

### BonusAccount

`BonusAccount` - бонусный счёт клиента.

Поля:

- `Id`
- `OrganizationId`
- `ClientId`
- `Balance`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

Правила:

- один счёт на `OrganizationId + ClientId`;
- `Balance` хранится в бонусных баллах;
- `Balance` не может быть отрицательным;
- `ClientId` хранится как Guid без FK на Clients;
- счёт может создаваться лениво при обращении по `clientId`.

### BonusTransaction

`BonusTransaction` - история операций по бонусному счёту.

Поля:

- `Id`
- `OrganizationId`
- `BonusAccountId`
- `ClientId`
- `DealId` nullable
- `SourceReturnId` nullable
- `Type`
- `Points`
- `MonetaryAmount`
- `PointValueAtMoment`
- `BalanceBefore`
- `BalanceAfter`
- `Reason` nullable
- `CreatedAt`
- `CreatedByUserId` nullable

Правила:

- внутренняя FK-связь только `BonusTransaction -> BonusAccount`;
- `ClientId` хранится как Guid без FK на Clients;
- `DealId` хранится как Guid без FK на Deals;
- `SourceReturnId` хранится как Guid correlation id на `DealReturn.Id` без FK;
- `CreatedByUserId` хранится как Guid без FK на Identity;
- ручные корректировки требуют обязательную `reason`;
- автоматические операции по сделке защищены от повторной обработки по `DealId`;
- return-origin операции защищены и считаются по `DealId`, `SourceReturnId != null` и `Type`.

`BonusTransactionResponse` также отдаёт `SourceReturnId`.

## Enums

`BonusAccrualType`:

- `None`
- `Percent`
- `Fixed`

`BonusTransactionType`:

- `Accrual`
- `WriteOff`
- `Refund`
- `CorrectionIncrease`
- `CorrectionDecrease`

`Refund` используется для возврата ранее списанных бонусов по completed `DealReturn`. `CorrectionDecrease` используется для отката ранее начисленных бонусов по completed `DealReturn`.

## Endpoints

Settings:

```http
GET /api/bonus/settings
PUT /api/bonus/settings
```

Accounts:

```http
GET /api/bonus/accounts
GET /api/bonus/accounts/{id}
GET /api/bonus/accounts/by-client/{clientId}
POST /api/bonus/accounts/by-client/{clientId}/adjust
```

Transactions:

```http
GET /api/bonus/transactions
GET /api/bonus/transactions/{id}
```

## Permissions

Module code:

```text
Bonus
```

Mapping:

- `Bonus / Read`:
  - `GET /api/bonus/settings`
  - `GET /api/bonus/accounts`
  - `GET /api/bonus/accounts/{id}`
  - `GET /api/bonus/accounts/by-client/{clientId}`
  - `GET /api/bonus/transactions`
  - `GET /api/bonus/transactions/{id}`
- `Bonus / Update`:
  - `PUT /api/bonus/settings`
  - `POST /api/bonus/accounts/by-client/{clientId}/adjust`

No delete endpoints in Bonus Core.

## Расчёт бонусной скидки в сделке

Deals хранит:

- `BonusPointsUsed` - количество применённых бонусных баллов;
- `BonusDiscountAmount` - денежная скидка в BYN.

`BonusPointsUsed` и `BonusDiscountAmount` больше не обязаны быть равны. `BonusDiscountAmount` рассчитывается через `BonusSettings.PointValue`.

При создании и обновлении сделки requested `BonusPointsUsed` трактуется как "использовать до указанного количества". В `Deal` сохраняются applied points, рассчитанные Bonus Core.

Скидка ограничивается:

- балансом бонусного счёта;
- `MaxPaymentPercent`;
- суммой сделки до бонусов;
- запрошенным количеством бонусов.

Итоговая формула Deals остаётся:

```text
FinalAmount = TotalAmount - DiscountAmount - BonusDiscountAmount
```

Если бонусы отключены, `MaxPaymentPercent = 0` или бонусный баланс равен нулю, requested points больше 0 приводят к `ConflictException`. Если requested points больше допустимого лимита, Bonus Core применяет cap и сохраняет applied points.

## Начисление бонусов

Начисление происходит только при successful final stage сделки:

- `DealStage.IsFinal = true`;
- `DealStage.IsSuccessful = true`.

База начисления - `Deal.FinalAmount`.

Если `AccrueOnBonusPayment = false` и в сделке использовались бонусы, Accrual transaction не создаётся. Если `AccrueOnBonusPayment = true`, начисление выполняется от `Deal.FinalAmount`.

Rule resolution:

1. Product/Service direct bonus rule.
2. Если direct rule = `Inherit`, используется Category rule.
3. Если Category тоже `Inherit`, Bonus Core идёт вверх по `ParentCategoryId`.
4. Если цепочка заканчивается или запись не найдена, используется fallback на `BonusSettings`.
5. Category chain защищён от циклов через visited set / max depth.
6. Inactive Catalog records могут использоваться для rule resolution, если существуют.

`Percent` начисляет бонусы от денежной базы позиции, `Fixed` начисляет фиксированные points за quantity, `None` не начисляет бонусы.

## Интеграция с Deals

Фактическая интеграция:

- `CreateDeal` и `UpdateDeal` используют `IBonusDealDiscountService`;
- `ChangeDealStage` вызывает Bonus completion service при successful final stage;
- Bonus completion выполняет `WriteOff`, если `Deal.BonusPointsUsed > 0`;
- Bonus completion выполняет `Accrual`, если бонусная система включена и начисление разрешено;
- Bonus service не вызывает `SaveChangesAsync`;
- всё сохраняется через общий `IUnitOfWork` в `ChangeDealStage`;
- если бонусов недостаточно, сделка не завершается;
- если бонусная операция по `DealId` уже была выполнена, выбрасывается `ConflictException`.

### Возвраты сделок

Bonus поддерживает бонусную часть Returns Core внутри Deals.

При завершении `DealReturn`:

- `Refund` возвращает клиенту ранее списанные бонусы пропорционально возврату;
- `CorrectionDecrease` откатывает ранее начисленные бонусы пропорционально возврату;
- `BonusTransaction.DealId` заполняется id исходной сделки;
- `BonusTransaction.SourceReturnId` заполняется `DealReturn.Id`;
- `SourceReturnId` является nullable Guid correlation id без FK;
- `Reason` является информационным полем и не парсится для бизнес-логики.

Уже обработанные return-origin операции считаются по:

- `DealId`;
- `SourceReturnId != null`;
- `Type = Refund / CorrectionDecrease`.

Если на счёте клиента недостаточно бонусов для отката начисления, completion возврата завершается `ConflictException`.

Bonus return integration service не вызывает `SaveChangesAsync`. Всё сохраняется атомарно через общий `IUnitOfWork` в Deals return completion вместе с Warehouse return movement и переводом `DealReturn` в `Completed`.

## Интеграция с Warehouse

При successful final deal stage общий порядок в `ChangeDealStage`:

1. Warehouse completion списывает товары со склада.
2. Bonus completion списывает/начисляет бонусы.
3. Deal меняет stage.
4. Добавляется `DealStageHistory`.
5. Выполняется один общий `SaveChangesAsync`.

Если Bonus падает после Warehouse, данные склада не сохраняются, потому что общий `UnitOfWork` ещё не сохранён.

## Audit integration

Bonus settings update and manual account adjustment handlers are integrated with Audit Core.

Audited events:

- bonus settings updated;
- bonus account manually adjusted.

Automatic `WriteOff`, `Accrual`, `Refund` and `CorrectionDecrease` operations created by Deals/Returns are not audited separately in Bonus. They remain represented by `BonusTransaction` records and Deals audit events.

## EF/database details

Таблицы:

- `BonusSettings`;
- `BonusAccounts`;
- `BonusTransactions`.

Precision:

- `BonusSettings.PointValue` - `decimal(18,2)`;
- `BonusAccount.Balance` - `decimal(18,3)`;
- `BonusTransaction.Points` - `decimal(18,3)`;
- `BonusTransaction.MonetaryAmount` - `decimal(18,2)`;
- `BonusTransaction.PointValueAtMoment` - `decimal(18,2)`.

Миграция:

```text
20260506131632_AddBonusCoreModule
20260507121737_AddDealsReturnsCore
```

Миграция также меняет `Deals.BonusPointsUsed` на `decimal(18,3)`.

`20260507121737_AddDealsReturnsCore` добавляет nullable column `BonusTransactions.SourceReturnId` и index `OrganizationId + DealId + SourceReturnId + Type`.

## Out of scope / Future scope

Не реализовано в Bonus Core:

- promotions;
- promo codes;
- loyalty levels;
- bonus expiration;
- advanced analytics.
