# Bonus module draft

## 0. Статус draft

Этот документ использовался для проектирования Bonus Core. Фактический Bonus Core уже реализован.

Актуальное описание реализации находится в:

```text
docs/BONUS_MODULE.md
```

Draft сохраняется как исторический design note и место для future scope. Advanced features вроде promotions, promo codes, loyalty levels, bonus expiration, birthday bonuses, marketing campaigns, returns processing и audit integration остаются будущими итерациями.

## 1. Назначение модуля

`Bonus` - бизнес-модуль CRM для loyalty/bonus-сценариев. Core-часть уже реализована.

Bonus module manages:

- bonus settings per organization;
- client bonus accounts;
- bonus balance;
- bonus accrual;
- bonus write-off;
- refunds/corrections;
- transaction history.

Bonus нужен для интеграции со сделками: успешная сделка может списывать бонусы клиента и начислять новые бонусы, а возврат должен корректировать ранее выполненные операции.

Текущий статус проекта: Warehouse Core и Bonus Core уже реализованы и подключены к событию successful final deal stage. Актуальный порядок интеграции описан в `docs/BONUS_MODULE.md`, `docs/DEALS_MODULE.md` и `docs/WAREHOUSE_MODULE.md`.

## 2. Архитектурные правила

Bonus должен повторять текущий стиль проекта:

- Modular Monolith;
- Clean Architecture;
- CQRS with MediatR;
- Repository + UnitOfWork;
- EF Core;
- PostgreSQL;
- один общий `ApplicationDbContext`;
- migrations только в `Infrastructure/Migrations`;
- без module-specific DbContext;
- без generic `IRepository<T>`.

Все бизнес-сущности Bonus должны быть tenant-scoped через `OrganizationId`.

Связи с другими модулями выполнять через Guid ID, без жёстких EF navigation/FK:

- `ClientId` - Id клиента из Clients;
- `DealId` - Id сделки из Deals;
- `CreatedByUserId` - Id пользователя из Identity.

## 3. Domain model

### BonusSettings

`BonusSettings` - настройки бонусной программы организации.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `IsEnabled : bool`
- `DefaultBonusType : BonusAccrualType`
- `DefaultBonusValue : decimal?`
- `MaxPaymentPercent : decimal`
- `AccrueOnBonusPayment : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Правила:

- `OrganizationId` обязателен.
- Исторический draft предполагал `1 bonus point = 1 BYN`; фактический Bonus Core использует `PointValue` в `BonusSettings`.
- `IsEnabled = false` отключает bonus write-off и accrual.
- `MaxPaymentPercent` ограничивает максимальную часть сделки, которую можно оплатить бонусами.
- `AccrueOnBonusPayment` определяет, начисляются ли новые бонусы, если сделка использовала бонусы.
- Если `AccrueOnBonusPayment = false` и `BonusPointsUsed > 0`, новые бонусы по сделке не начисляются.

### BonusAccount

`BonusAccount` - бонусный счёт клиента в организации.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `ClientId : Guid`
- `Balance : decimal`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Rules:

- One `BonusAccount` per `ClientId` inside `OrganizationId`.
- Unique index: `OrganizationId + ClientId`.
- `Balance >= 0`.
- `IsActive = true` при создании.
- No FK to Clients.
- `ClientId` проверяется через application-level lookup при необходимости.

### BonusTransaction

`BonusTransaction` - запись изменения бонусного баланса.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `BonusAccountId : Guid`
- `DealId : Guid?`
- `Type : BonusTransactionType`
- `Amount : decimal`
- `BalanceAfter : decimal`
- `Reason : string`
- `CreatedAt : DateTime`
- `CreatedByUserId : Guid?`

Правила:

- `OrganizationId` обязателен.
- `BonusAccountId` обязателен.
- `DealId` nullable, потому что correction может быть ручной или не связанной напрямую со сделкой.
- `Amount > 0`.
- `BalanceAfter >= 0`.
- `Reason` обязателен.
- `CreatedByUserId` nullable для системных операций.
- FK на Deals и Identity Users не делать.

## 4. Enums

`BonusAccrualType`:

- `None = 0`
- `Percent = 1`
- `Fixed = 2`

`BonusTransactionType`:

- `Accrual = 1`
- `WriteOff = 2`
- `Refund = 3`
- `Correction = 4`

Enums хранятся в БД как `int`.

## 5. Bonus rules

Core rules:

- `PointValue` задаёт, сколько BYN даёт 1 bonus point.
- Bonus write-off allowed only if `BonusSettings.IsEnabled = true`.
- `MaxPaymentPercent` limits maximum deal amount covered by bonuses.
- `AccrueOnBonusPayment` controls whether bonuses accrue if deal used bonuses.
- Bonus balance cannot become negative.

Bonus accrual source:

```text
Product/Service bonus rule
 -> Category chain
 -> BonusSettings default rule
```

Catalog rules are used but not modified by Bonus.

Important:

- Catalog уже содержит `BonusType`/`BonusValue` в `Category`, `Product`, `Service`.
- Bonus module не должен редактировать Catalog.
- Resolution правила начисления может быть реализован через Catalog lookup service.

## 6. Deals integration

Bonus module is triggered when Deal moves to successful final stage.

Сейчас на этом событии уже выполняется Warehouse stock deduction для Product items. В будущей финальной цепочке successful final deal stage должны выполняться:

- Warehouse stock deduction;
- Bonus write-off/accrual;
- Audit logging.

On successful deal:

- validate `BonusPointsUsed`;
- check `BonusSettings.IsEnabled`;
- check `MaxPaymentPercent`;
- check `BonusAccount.Balance`;
- write off bonuses if requested;
- accrue new bonuses if allowed;
- create `BonusTransaction` records.

On return:

- return used bonuses;
- revert accrued bonuses;
- create `BonusTransaction` correction/refund records.

If Bonus module is disabled:

- bonus usage does not happen;
- bonus accrual does not happen;
- Deals should be completed without bonus effects.

## 7. Planned endpoints

Exact API can be decided during implementation. Expected MVP endpoints:

```http
GET /api/bonus/settings
PUT /api/bonus/settings
GET /api/bonus/accounts
GET /api/bonus/accounts/{clientId}
GET /api/bonus/accounts/{clientId}/transactions
POST /api/bonus/accounts/{clientId}/corrections
```

All endpoints should use organization user JWT and existing Identity permission system.

Module code:

```text
Bonus
```

Permissions:

- `Bonus / Read`
- `Bonus / Create`
- `Bonus / Update`
- `Bonus / Delete`

## 8. Что Bonus module не включает

Bonus module does not handle:

- deals themselves;
- warehouse stock;
- payments;
- invoices;
- catalog editing;
- promotions;
- promo codes;
- date-based promotions;
- chat;
- audit events.

Deals owns the sale flow. Bonus only owns bonus settings, accounts, balances and transactions.

## 9. Open questions

1. Должен ли `BonusAccount` создаваться lazy при первой сделке или при создании клиента?
2. Нужны ли manual corrections в MVP?
3. Нужно ли ограничивать срок действия бонусов?
4. Должны ли бонусы начисляться на сумму до скидки или после скидки?
5. Какой default `MaxPaymentPercent` использовать при создании организации?
