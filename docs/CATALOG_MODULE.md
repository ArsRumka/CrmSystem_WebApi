# Catalog module

## 1. Назначение модуля

`Catalog` - третий реализованный модуль CRM после `Identity` и `Clients`.

Модуль отвечает за каталог товаров и услуг организации:

- категории каталога;
- товары;
- услуги;
- цены;
- базовые правила бонусов;
- базовые правила скидок;
- поиск и фильтрацию;
- soft delete через `IsActive = false`.

Модуль является tenant-scoped: все данные каталога хранят `OrganizationId` и доступны только в контексте текущей организации.

## 2. Проекты модуля

Модуль состоит из четырёх проектов:

- `Catalog.Domain` - сущности `Category`, `Product`, `Service` и enums.
- `Catalog.Application` - CQRS use cases, validators, response DTO и repository interfaces.
- `Catalog.Infrastructure` - EF Core configurations, repository implementations и DI.
- `Catalog.Presentation` - thin API controllers.

WebApi `CrmSystem` подключает:

- `AddCatalogApplication()`;
- `AddCatalogInfrastructure()`;
- application part для `CategoriesController`.

## 3. Domain model

### Category

`Category` - общая категория для товаров и услуг.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `Name : string`
- `ParentCategoryId : Guid?`
- `BonusType : BonusType`
- `BonusValue : decimal?`
- `DiscountType : DiscountType`
- `DiscountValue : decimal?`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Правила:

- категория создаётся активной;
- parent category optional;
- parent category проверяется на принадлежность той же организации в application layer;
- update запрещает parent = self и циклы категорий;
- deactivate не деактивирует и не отвязывает товары/услуги.

### Product

`Product` - отдельная сущность товара.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `CategoryId : Guid?`
- `Name : string`
- `Sku : string?`
- `Description : string?`
- `Price : decimal`
- `BonusType : BonusType`
- `BonusValue : decimal?`
- `DiscountType : DiscountType`
- `DiscountValue : decimal?`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Важные решения:

- `Sku` nullable;
- `Sku` не unique;
- `Sku` индексируется вместе с `OrganizationId`;
- duplicate SKU не блокируется на уровне БД;
- товаров без отдельной складской логики достаточно для Catalog;
- `Currency` column отсутствует;
- VAT/tax fields отсутствуют.

### Service

`Service` - отдельная сущность услуги.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `CategoryId : Guid?`
- `Name : string`
- `Description : string?`
- `Price : decimal`
- `BonusType : BonusType`
- `BonusValue : decimal?`
- `DiscountType : DiscountType`
- `DiscountValue : decimal?`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Важные решения:

- услуги не используют SKU;
- `ServiceCode` не добавлен;
- услуги и товары являются разными сущностями и разными таблицами;
- общая категория хранится через nullable `CategoryId`.

## 4. Enums

`BonusType`:

- `Inherit = 0`
- `Percent = 1`
- `Fixed = 2`
- `None = 3`

`DiscountType`:

- `Inherit = 0`
- `Percent = 1`
- `Fixed = 2`
- `None = 3`

Правила consistency для `BonusValue` и `DiscountValue`:

- `Percent` требует значение `> 0` и `<= 100`;
- `Fixed` требует значение `> 0`;
- `None` и `Inherit` допускают только `null` или `0`.

Fixed discount может быть больше цены товара/услуги на уровне Catalog. Ограничение итоговой цены относится к логике Deals/pricing.

Bonus fields в `Product`, `Service` и `Category` используются Bonus Core для начисления бонусов при successful final deal stage.

Rule resolution:

1. Product/Service direct bonus rule.
2. Если direct rule = `Inherit`, используется Category rule.
3. Если Category тоже `Inherit`, Bonus Core идёт вверх по `ParentCategoryId`.
4. Если category chain заканчивается, запись не найдена или rule остаётся `Inherit`, используется fallback на `BonusSettings` организации.

`Inherit` означает наследование правила от категории, родительской категории или настроек организации. Inactive Catalog records могут использоваться для bonus rule resolution, если запись существует.

## 5. Currency

Все цены считаются ценами в BYN.

Catalog намеренно не добавляет:

- `Currency` column;
- VAT fields;
- tax fields;
- cost price;
- discount periods;
- promo codes.

## 6. Application layer

Use cases реализованы через MediatR.

Categories:

- `CreateCategory`
- `GetCategories`
- `GetCategoryById`
- `UpdateCategory`
- `DeactivateCategory`

Products:

- `CreateProduct`
- `GetProducts`
- `GetProductById`
- `UpdateProduct`
- `DeactivateProduct`

Services:

- `CreateService`
- `GetServices`
- `GetServiceById`
- `UpdateService`
- `DeactivateService`

Handlers:

- берут `OrganizationId` из `ICurrentUserService`;
- отклоняют отсутствие tenant context через `UnauthorizedException`;
- проверяют связанные категории по `OrganizationId`;
- сохраняют изменения через общий `IUnitOfWork`;
- используют `IDateTimeProvider` для `CreatedAt` и `UpdatedAt`;
- не содержат controller logic.

Validation реализована через FluentValidation.

## 7. Repositories

Catalog использует предметные repositories:

- `ICategoryRepository` / `CategoryRepository`
- `IProductRepository` / `ProductRepository`
- `IServiceRepository` / `ServiceRepository`

Repositories используют общий:

```text
Infrastructure.Persistence.ApplicationDbContext
```

Repositories не вызывают `SaveChangesAsync`. Commit выполняется через `IUnitOfWork`.

Search всегда tenant-scoped по `OrganizationId`. Для PostgreSQL case-insensitive поиска используется `EF.Functions.ILike`.

## 8. EF/database details

EF configurations находятся в `Catalog.Infrastructure/Configurations`:

- `CategoryConfiguration`
- `ProductConfiguration`
- `ServiceConfiguration`

Таблицы:

- `Categories`
- `Products`
- `Services`

Важные EF-решения:

- `Category.ParentCategoryId` - self-reference на `Categories.Id`;
- `Product.CategoryId` - FK на `Categories.Id`;
- `Service.CategoryId` - FK на `Categories.Id`;
- delete behavior для связей - `Restrict`;
- enums хранятся как `int`;
- `Price`, `BonusValue`, `DiscountValue` имеют precision `18,2`;
- `Product.Sku` индексируется, но не unique;
- нет FK на Identity `Organization`;
- нет FK на Warehouse или Deals.

EF configurations подключены к общему `ApplicationDbContext` через DI-based механизм:

```csharp
services.AddSingleton<IEfConfigurationAssemblyProvider>(
    new EfConfigurationAssemblyProvider(typeof(CategoryConfiguration).Assembly));
```

`ApplicationDbContext` не ссылается на `Catalog.Infrastructure` напрямую и не использует `Assembly.Load` для Catalog.

## 9. Migration

Migration:

```text
AddCatalogModule
```

File:

```text
Infrastructure/Migrations/20260426152751_AddCatalogModule.cs
```

Миграция создана в общем проекте `Infrastructure`, как и остальные migrations приложения.

## 10. Permissions

Catalog использует существующую Identity permission system.

Все endpoints защищены:

- `[Authorize]`
- `[RequirePermission("Catalog", PermissionAction.Read)]`
- `[RequirePermission("Catalog", PermissionAction.Create)]`
- `[RequirePermission("Catalog", PermissionAction.Update)]`
- `[RequirePermission("Catalog", PermissionAction.Delete)]`

Permissions:

- `Catalog / Read` - list и get by id.
- `Catalog / Create` - create.
- `Catalog / Update` - update.
- `Catalog / Delete` - deactivate.

Module code `Catalog` уже seed-ится Identity seed-ом как системный module code.

## 11. Endpoints

Categories:

```http
GET /api/catalog/categories
GET /api/catalog/categories/{id}
POST /api/catalog/categories
PUT /api/catalog/categories/{id}
DELETE /api/catalog/categories/{id}
```

Products:

```http
GET /api/catalog/products
GET /api/catalog/products/{id}
POST /api/catalog/products
PUT /api/catalog/products/{id}
DELETE /api/catalog/products/{id}
```

Services:

```http
GET /api/catalog/services
GET /api/catalog/services/{id}
POST /api/catalog/services
PUT /api/catalog/services/{id}
DELETE /api/catalog/services/{id}
```

`DELETE` endpoints выполняют soft delete и возвращают `204 No Content`.

## 12. Что Catalog намеренно не включает

Catalog module намеренно не реализует:

- Deals;
- bonus transactions;
- organization bonus settings;
- organization discount settings;
- stock/warehouse logic inside Catalog;
- Chat;
- Audit;
- полноценный Promotions module;
- promo codes;
- date-based promotions;
- frontend UI.

## 13. Связанные модули и текущий статус

После Catalog уже реализованы:

- `Deals MVP/Core`;
- `Warehouse Core`;
- `Bonus Core`;
- `Returns Core inside Deals`.

Связи:

- Deals использует `Product` и `Service` из Catalog через Guid и snapshot;
- Returns Core использует уже сохранённые `DealItem` snapshots; прямой связи Catalog -> Returns нет;
- Warehouse Core ведёт остатки только для Catalog Product;
- inactive Catalog Product разрешён для складских операций, если Product существует в той же организации;
- Bonus Core использует `BonusType`/`BonusValue` из Product, Service и Category для rule resolution;
- Bonus Core наследует rule по цепочке Product/Service -> Category -> parent categories -> BonusSettings.
