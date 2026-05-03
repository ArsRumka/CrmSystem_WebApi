# Warehouse module draft

## 0. Статус draft

Этот документ использовался для проектирования Warehouse.

Warehouse Core уже реализован. Актуальное описание фактической реализации находится в:

```text
docs/WAREHOUSE_MODULE.md
```

Draft сохраняется как исторический design note и место для future scope. Advanced ERP-возможности, такие как transfers, suppliers, inventory acts, reservations, batches/lots и full returns flow, остаются будущими итерациями.

## 1. Назначение модуля

`Warehouse` - бизнес-модуль CRM для складского учёта товаров. Core-часть уже реализована.

Warehouse module manages:

- organization storages;
- product stock;
- stock movements;
- sale deductions;
- corrections.

Warehouse нужен для интеграции со сделками: successful final deal stage списывает товары со склада. Возвраты остаются future scope внутри Deals/Returns flow.

## 2. Архитектурные правила

Warehouse должен повторять текущий стиль проекта:

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

Все бизнес-сущности Warehouse должны быть tenant-scoped через `OrganizationId`.

Связи с другими модулями выполнять через Guid ID, без жёстких EF navigation/FK:

- `ProductId` - Id товара из Catalog;
- `DealId` - Id сделки из Deals;
- `CreatedByUserId` - Id пользователя из Identity.

## 3. Domain model

### Storage

`Storage` - склад/точка хранения организации.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `Name : string`
- `Address : string?`
- `IsDefault : bool`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Rules:

- Organization can have multiple storages.
- Only one default storage per organization.
- Storage is tenant-scoped.
- `OrganizationId` обязателен.
- `Name` обязателен.
- `Address` nullable.
- `IsActive = true` при создании.
- No FK to Organization.

### ProductStock

`ProductStock` - остаток товара на конкретном складе.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `StorageId : Guid`
- `ProductId : Guid`
- `Quantity : decimal`
- `UpdatedAt : DateTime?`

Rules:

- One stock row per `StorageId + ProductId`.
- Unique index: `StorageId + ProductId`.
- `OrganizationId` обязателен.
- `StorageId` обязателен.
- `ProductId` references Catalog Product by Guid only, no FK.
- `Quantity >= 0`.
- Negative stock is not allowed.
- Service items do not have stock rows.

### StockMovement

`StockMovement` - история движения товара по складу.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `StorageId : Guid`
- `ProductId : Guid`
- `DealId : Guid?`
- `Type : StockMovementType`
- `Quantity : decimal`
- `QuantityBefore : decimal`
- `QuantityAfter : decimal`
- `Reason : string`
- `CreatedAt : DateTime`
- `CreatedByUserId : Guid?`

Правила:

- `OrganizationId` обязателен.
- `StorageId` обязателен.
- `ProductId` обязателен.
- `DealId` nullable, потому что correction/receipt/write-off могут быть не связаны со сделкой.
- `Quantity > 0`.
- `QuantityBefore >= 0`.
- `QuantityAfter >= 0`.
- `Reason` обязателен.
- `CreatedByUserId` nullable для системных операций.
- FK на Deals, Catalog Product и Identity Users не делать.

## 4. Enums

`StockMovementType`:

- `Receipt = 1`
- `WriteOff = 2`
- `Sale = 3`
- `Return = 4`
- `Correction = 5`

Enums хранятся в БД как `int`.

## 5. Stock rules

Core rules:

- Negative stock is not allowed.
- Product stock is tenant-scoped by `OrganizationId`.
- Stock operations must verify storage belongs to the current organization.
- Product operations must verify product exists in Catalog and belongs to the current organization through lookup service.
- Stock movement should be recorded for every stock-changing operation.

Storage rules:

- Organization may have multiple storages.
- Only one active default storage should exist per organization.
- Deactivating a storage with non-zero product stock should be forbidden or explicitly handled.

ProductStock rules:

- One stock row per `StorageId + ProductId`.
- Stock row can be created when product is first received or corrected.
- Sale deduction requires existing stock row with enough quantity.

## 6. Deals integration

When Deal moves to successful final stage:

- for every Product `DealItem`:
  - use `DealItem.StorageId`;
  - verify Storage exists in same organization;
  - verify `ProductStock` exists;
  - verify `Quantity` is enough;
  - deduct quantity;
  - record `StockMovement` with `Type = Sale`.

For Service `DealItem`:

- no stock movement;
- `StorageId` should be `null`;
- Warehouse is not involved.

Negative stock is not allowed.

On return:

- product quantity is added back to stock;
- `StockMovement` with `Type = Return` is created;
- returned quantity should not exceed quantity that can still be returned for the original deal item.

## 7. Planned endpoints

Core API уже реализован и описан в `docs/WAREHOUSE_MODULE.md`. Исторически ожидавшиеся endpoints:

```http
GET /api/warehouse/storages
GET /api/warehouse/storages/{id}
POST /api/warehouse/storages
PUT /api/warehouse/storages/{id}
DELETE /api/warehouse/storages/{id}
GET /api/warehouse/stocks
GET /api/warehouse/movements
POST /api/warehouse/movements/receipt
POST /api/warehouse/movements/write-off
POST /api/warehouse/movements/correction
```

Sale and return movements should be created through Deals integration, not by direct public sale endpoints.

All endpoints should use organization user JWT and existing Identity permission system.

Module code:

```text
Warehouse
```

Permissions:

- `Warehouse / Read`
- `Warehouse / Create`
- `Warehouse / Update`
- `Warehouse / Delete`

## 8. Что Warehouse module не включает

Warehouse module does not handle:

- service items;
- deal totals;
- bonus points;
- payments;
- invoices;
- catalog editing;
- promotions;
- promo codes;
- chat;
- audit events.

Catalog owns product/service definitions. Deals owns sale flow and returns. Warehouse owns stock and movements.

## 9. Open questions

Часть вопросов закрыта реализацией Warehouse Core:

- default storage назначается при создании первого склада организации;
- reserved quantities в Core не реализованы;
- stock movement не редактируется, изменения остатка выполняются через correction;
- transfer между складами не входит в Core;
- продажа товара требует существующий `ProductStock` row с достаточным остатком.

Исторические вопросы ниже больше не являются blockers для Core, но могут быть полезны при проектировании future scope:

1. Нужны ли reserved quantities после Core?
2. Нужно ли поддерживать transfer между складами отдельным use case?
3. Нужны ли inventory acts поверх correction?
4. Как будет выглядеть full Returns flow вместе с Deals, Bonus и Audit?
