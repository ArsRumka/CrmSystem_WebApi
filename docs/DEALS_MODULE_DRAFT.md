# Deals module draft

## 1. Статус draft

Этот документ был исходным draft для Deals MVP. Фактический MVP уже реализован.

Актуальная документация реализованного модуля:

```text
docs/DEALS_MODULE.md
```

Реализовано:

- `Deals.Domain`;
- `Deals.Application`;
- `Deals.Infrastructure`;
- `Deals.Presentation`;
- migration `AddDealsMvpModule`.

После последующих итераций Deals уже интегрирован с Warehouse Core, Bonus Core и Returns Core inside Deals. Актуальный статус описан в `docs/DEALS_MODULE.md`.

Deals MVP/Core уже покрывает возвраты внутри Deals module. Будущей Deals-related итерацией остаётся:

- Audit.

## 2. Реализованный MVP scope

Deals MVP реализует:

- сделки;
- позиции сделки;
- tenant-specific stages;
- историю изменения stages;
- расчёт сумм сделки;
- ручные и catalog-resolved скидки;
- snapshot товара/услуги из Catalog;
- planned bonus fields;
- planned warehouse field `DealItem.StorageId`;
- thin API controllers;
- permissions через module code `Deals`.

Deals MVP не реализует:

- Bonus module;
- Warehouse module;
- Audit module;
- stock checks;
- stock movements;
- bonus account validation;
- BonusSettings validation;
- BonusTransactions;
- bonus accrual/write-off;
- payments;
- invoices;
- refunds;
- promo codes;
- analytics;
- chat.

## 3. Domain model фактически реализованного MVP

### Deal

`Deal` хранит:

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

Important:

- `ClientId` - Guid из Clients, без FK/navigation.
- `ResponsibleUserId` - Guid из Identity, без FK/navigation.
- `StageId` - FK только на `DealStage` внутри Deals module.
- Если текущий stage имеет `IsFinal = true`, запрещены `UpdateDeal`, `ChangeDealStage`, `DeactivateDeal`.
- `BonusPointsUsed` и `BonusDiscountAmount` только рассчитываются и сохраняются.

### DealItem

`DealItem` хранит:

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

Important:

- `ItemId` указывает на Catalog Product/Service через Guid only, без FK/navigation.
- `NameSnapshot` и `PriceAtMoment` фиксируют snapshot на момент сделки.
- Product item требует `StorageId != null`.
- Service item требует `StorageId == null`.
- `StorageId` хранится для будущего Warehouse, без FK/navigation.
- Существование `StorageId`, stock quantity и stock movements в MVP не проверяются/не создаются.

### DealStage

`DealStage` реализован как tenant-specific editable stage.

В Deals не используется enum `Status`.

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

Default stages создаются lazy при первом использовании:

1. `New`
2. `InProgress`
3. `Completed`
4. `Cancelled`

Lazy initialization выполняет `IDealStageInitializer` в `Deals.Application` через:

- `IDealStageRepository`;
- `IUnitOfWork`;
- `IDateTimeProvider`.

EF напрямую initializer не использует.

### DealStageHistory

`DealStageHistory` хранит:

- `Id`
- `OrganizationId`
- `DealId`
- `OldStageId`
- `NewStageId`
- `ChangedByUserId`
- `ChangedAt`

История создаётся:

- при создании сделки с `OldStageId = null`;
- при каждом изменении `StageId`.

`ChangedByUserId` - Guid пользователя Identity, без FK/navigation.

## 4. Calculations

Для `DealItem`:

```text
ItemTotal = Quantity * PriceAtMoment
ItemFinalAmount = ItemTotal - DiscountAmount
```

Discount:

- `None` => `0`;
- `Percent` => `ItemTotal * DiscountValue / 100`;
- `Fixed` => `DiscountValue`;
- fixed discount capped до `ItemTotal`.

Для `Deal`:

```text
TotalAmount = sum(DealItems.TotalAmount)
DiscountAmount = sum(DealItems.DiscountAmount)
appliedBonusPoints = min(requestedBonusPoints, TotalAmount - DiscountAmount)
BonusPointsUsed = appliedBonusPoints
BonusDiscountAmount = appliedBonusPoints
FinalAmount = TotalAmount - DiscountAmount - BonusDiscountAmount
```

Исторический MVP rule до Bonus Core:

- `1 bonus point = 1 BYN`.

Фактическая реализация после Bonus Core описана в `docs/DEALS_MODULE.md`: бонусная скидка рассчитывается через `PointValue`, balance и BonusSettings.

## 5. Architecture boundaries

Deals follows the current architecture:

- Modular Monolith;
- Clean Architecture;
- CQRS with MediatR;
- Repository + UnitOfWork;
- EF Core;
- PostgreSQL;
- one shared `ApplicationDbContext`;
- migrations only in `Infrastructure/Migrations`;
- no module-specific DbContext;
- no generic repository.

No external EF FK/navigation:

- no FK/navigation to Clients;
- no FK/navigation to Catalog;
- no FK/navigation to Identity;
- no FK/navigation to Warehouse.

Deals EF configurations are registered through:

```csharp
services.AddSingleton<IEfConfigurationAssemblyProvider>(
    new EfConfigurationAssemblyProvider(typeof(DealConfiguration).Assembly));
```

`ApplicationDbContext` does not reference `Deals.Infrastructure` directly.

## 6. Implemented endpoints

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

## 7. Permissions

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

## 8. Future iterations

Future Deals-related work:

- add Audit events for important Deals actions.

Cancellation vs return:

- `Cancelled` stage is for active/non-successful deals.
- Return is for completed successful deals and now reverses/corrects Bonus/Warehouse effects through Returns Core inside Deals.
