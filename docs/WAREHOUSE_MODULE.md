# Warehouse Module

## Назначение

Warehouse отвечает за:

- склады организации;
- остатки товаров;
- движения товаров;
- ручное поступление товара;
- ручное списание товара;
- корректировку остатка;
- списание товара при успешном завершении сделки.

Warehouse Core реализован как базовый складской модуль для CRM-сценариев. Advanced ERP-функции остаются future scope.

## Архитектура

Warehouse реализован как отдельный модуль:

- `Warehouse.Domain`;
- `Warehouse.Application`;
- `Warehouse.Infrastructure`;
- `Warehouse.Presentation`.

Архитектурные правила:

- используется общий `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `WarehouseDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations регистрируются через `IEfConfigurationAssemblyProvider` в `AddWarehouseInfrastructure()`;
- `ApplicationDbContext` не ссылается на `Warehouse.Infrastructure` напрямую;
- внешних FK/navigation на Catalog, Deals, Identity и Organization нет;
- связи с другими модулями выполняются только через Guid.

Межмодульные идентификаторы:

- `ProductId` - Guid товара из Catalog;
- `DealId` - Guid сделки из Deals, nullable;
- `CreatedByUserId` - Guid пользователя Identity, nullable;
- `OrganizationId` - Guid организации, без FK на Identity Organization.

## Основные сущности

### Storage

`Storage` - склад организации.

Поля:

- `Id`
- `OrganizationId`
- `Name`
- `Address`
- `IsDefault`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

Правила:

- организация может иметь несколько складов;
- первый склад организации становится default автоматически;
- только один default storage в организации поддерживается application logic;
- склад деактивируется мягко через `IsActive = false`;
- нельзя деактивировать склад с положительными остатками;
- нельзя деактивировать default storage, если есть другие active storages;
- нельзя деактивировать последний active storage организации;
- физическое удаление склада не используется.

### ProductStock

`ProductStock` - остаток товара на конкретном складе.

Поля:

- `Id`
- `OrganizationId`
- `StorageId`
- `ProductId`
- `Quantity`
- `CreatedAt`
- `UpdatedAt`

Правила:

- остаток ведётся только для Catalog Product;
- Service не участвует в складском учёте;
- `ProductId` хранится как Guid без FK на Catalog;
- один stock row на `StorageId + ProductId`;
- `Quantity >= 0`;
- отрицательные остатки запрещены;
- inactive Catalog Product разрешён для складских операций, если Product существует в той же организации.

### StockMovement

`StockMovement` - история движения товара.

Поля:

- `Id`
- `OrganizationId`
- `StorageId`
- `ProductId`
- `DealId` nullable
- `Type`
- `Quantity`
- `QuantityBefore`
- `QuantityAfter`
- `Reason` nullable
- `CreatedAt`
- `CreatedByUserId` nullable

Правила:

- `Quantity` всегда положительное;
- направление движения определяется `Type`;
- `QuantityBefore` и `QuantityAfter` сохраняются для истории;
- `DealId` nullable и хранится как Guid без FK на Deals;
- `CreatedByUserId` nullable и хранится как Guid без FK на Identity;
- `ProductId` хранится как Guid без FK на Catalog.

## StockMovementType

`StockMovementType` хранится в БД как `int`:

- `Receipt = 1` - ручное поступление товара;
- `WriteOff = 2` - ручное списание товара;
- `Sale = 3` - автоматическое списание при successful final deal stage;
- `Return = 4` - future usage для будущих возвратов;
- `Correction = 5` - ручная корректировка остатка.

## Endpoints

Storages:

```http
GET /api/warehouse/storages
GET /api/warehouse/storages/{id}
POST /api/warehouse/storages
PUT /api/warehouse/storages/{id}
PUT /api/warehouse/storages/{id}/make-default
DELETE /api/warehouse/storages/{id}
```

Stocks:

```http
GET /api/warehouse/stocks
GET /api/warehouse/stocks/{id}
POST /api/warehouse/stocks/receipt
POST /api/warehouse/stocks/write-off
POST /api/warehouse/stocks/correction
```

Movements:

```http
GET /api/warehouse/movements
GET /api/warehouse/movements/{id}
```

Controllers are thin:

- принимают route/body/query параметры;
- создают MediatR command/query;
- не содержат бизнес-логики.

## Permissions

Module code:

```text
Warehouse
```

Mapping:

- `Warehouse / Read`:
  - `GET /api/warehouse/storages`
  - `GET /api/warehouse/storages/{id}`
  - `GET /api/warehouse/stocks`
  - `GET /api/warehouse/stocks/{id}`
  - `GET /api/warehouse/movements`
  - `GET /api/warehouse/movements/{id}`
- `Warehouse / Create`:
  - `POST /api/warehouse/storages`
  - `POST /api/warehouse/stocks/receipt`
- `Warehouse / Update`:
  - `PUT /api/warehouse/storages/{id}`
  - `PUT /api/warehouse/storages/{id}/make-default`
  - `POST /api/warehouse/stocks/write-off`
  - `POST /api/warehouse/stocks/correction`
- `Warehouse / Delete`:
  - `DELETE /api/warehouse/storages/{id}`

Ручное списание товара считается `Update`, а не `Delete`.

## Интеграция с Deals

Warehouse интегрирован в `ChangeDealStage`.

Когда Deal переводится в stage, где:

- `IsFinal = true`;
- `IsSuccessful = true`;

Warehouse:

- обрабатывает только Product items;
- игнорирует Service items;
- проверяет active Storage;
- проверяет ProductStock;
- проверяет достаточность `Quantity`;
- уменьшает остаток;
- создаёт `StockMovement` с `Type = Sale`;
- заполняет `DealId`;
- заполняет `CreatedByUserId`;
- запрещает завершение сделки, если хотя бы по одной Product-позиции недостаточно остатка;
- защищает от повторного списания через проверку существующего Sale movement by `DealId`.

Если Deal переводится в unsuccessful final stage, например `Cancelled`, Warehouse ничего не списывает.

Важно:

- stock не резервируется при создании сделки;
- списание происходит только при successful final stage;
- Warehouse integration service не вызывает `SaveChangesAsync`;
- изменения сохраняются через общий `IUnitOfWork` в `ChangeDealStage` handler;
- списание склада и перевод сделки в successful final stage выполняются атомарно в одном сохранении.

## EF/database details

Таблицы:

- `Storages`;
- `ProductStocks`;
- `StockMovements`.

Внутренние Warehouse FK:

- `ProductStocks.StorageId -> Storages.Id`;
- `StockMovements.StorageId -> Storages.Id`.

Внешних FK нет:

- `ProductStocks.ProductId` не имеет FK на Catalog;
- `StockMovements.ProductId` не имеет FK на Catalog;
- `StockMovements.DealId` не имеет FK на Deals;
- `StockMovements.CreatedByUserId` не имеет FK на Identity;
- `OrganizationId` не имеет FK на Identity Organization.

Миграция:

```text
20260503130507_AddWarehouseCoreModule
```

## Out of scope / Future scope

Не реализовано в Warehouse Core:

- suppliers;
- purchase orders;
- transfers between storages;
- inventory acts;
- barcode scanning;
- reservations;
- batches/lots;
- cost price;
- supplier returns;
- full Returns flow;
- Bonus integration;
- Audit integration.
