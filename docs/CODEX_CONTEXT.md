# Контекст проекта для Codex

## Что это за проект

Проект `CrmSystem` - дипломная CRM-система для малого бизнеса на ASP.NET Core и PostgreSQL.

Целевая архитектура: **Modular Monolith + Clean Architecture + CQRS + Repository + UnitOfWork**.

Поток выполнения:

```text
Controller
 -> MediatR Command/Query
 -> Handler
 -> предметный repository interface
 -> repository implementation
 -> ApplicationDbContext
 -> PostgreSQL
```

## Структура solution

Solution: `CrmSystem.slnx`.

Проекты:

- `BuildingBlocks.Domain` - общие доменные базовые вещи.
- `BuildingBlocks.Application` - общие application abstractions и exceptions.
- `BuildingBlocks.Infrastructure` - общий базовый инфраструктурный слой, включая `AppDbContext`.
- `Infrastructure` - общий инфраструктурный проект приложения: реальный `ApplicationDbContext`, EF migrations, общие runtime-сервисы.
- `Identity.Domain` - доменные сущности Identity.
- `Identity.Application` - MediatR use cases, validators, DTO, repository/security interfaces.
- `Identity.Infrastructure` - EF configurations Identity, repository implementations, security/permission services, seed.
- `Identity.Presentation` - thin API controllers Identity.
- `Clients.Domain` - доменная модель Clients.
- `Clients.Application` - MediatR use cases, validators, DTO и repository interfaces Clients.
- `Clients.Infrastructure` - EF configuration и repository implementation Clients.
- `Clients.Presentation` - thin API controller Clients.
- `Catalog.Domain` - доменная модель Catalog.
- `Catalog.Application` - MediatR use cases, validators, DTO и repository interfaces Catalog.
- `Catalog.Infrastructure` - EF configurations и repository implementations Catalog.
- `Catalog.Presentation` - thin API controllers Catalog.
- `Deals.Domain` - доменная модель Deals.
- `Deals.Application` - MediatR use cases, validators, DTO, calculation/default-stage services и repository/lookup interfaces Deals.
- `Deals.Infrastructure` - EF configurations, repository implementations и lookup services Deals.
- `Deals.Presentation` - thin API controllers Deals.
- `Warehouse.Domain` - доменная модель Warehouse.
- `Warehouse.Application` - MediatR use cases, validators, DTO, repository/service abstractions Warehouse.
- `Warehouse.Infrastructure` - EF configurations, repositories, Catalog lookup и Deals completion integration Warehouse.
- `Warehouse.Presentation` - thin API controllers Warehouse.
- `CrmSystem` - ASP.NET Core Web API, точка входа приложения.

## Ключевые правила

- Не создавать отдельные DbContext на модуль.
- Не создавать `IdentityDbContext` или `ClientsDbContext`.
- `ApplicationDbContext` находится только в `Infrastructure/Persistence`.
- Миграции находятся только в `Infrastructure/Migrations`.
- Используется одна PostgreSQL database.
- Используется один `ApplicationDbContext`.
- Не использовать generic `IRepository<T>`.
- Использовать предметные repositories.
- `SaveChangesAsync` не вызывать внутри repositories.
- Commit делать через общий `IUnitOfWork`.
- Domain не должен содержать EF/DataAnnotations атрибуты.
- EF ограничения описывать через Fluent API.
- Между модулями использовать Guid ID, не жёсткие EF-навигации.
- Все бизнес-данные должны быть tenant-scoped через `OrganizationId`.
- Identity уже реализован, не переписывать его без необходимости.

## Current status

Реализованы и build-tested core-модули:

- **Identity** - implemented and tested.
- **Clients** - implemented and tested.
- **Catalog** - implemented and tested.
- **Deals MVP** - implemented and tested.
- **Warehouse Core** - implemented and tested.

Готов фундамент проекта:

- общий `ApplicationDbContext`;
- миграции `InitialCreate`, `AddIdentityFoundation`, `AddClientsModule`, `AddCatalogModule`, `AddDealsMvpModule`, `AddWarehouseCoreModule`;
- общий `IUnitOfWork`;
- общие abstractions: current user, email sender, time provider;
- common application exceptions;
- global exception middleware;
- JWT authentication;
- permission-based authorization;
- Swagger Bearer token support;
- Identity seed на старте приложения.

## Реализованные модули

### Identity

Полноценно реализован модуль **Identity**:

- системный администратор платформы;
- заявки на подключение организаций;
- approve/reject заявок;
- activation/license key;
- регистрация организации по activation key;
- пользователи организации;
- роли организации;
- системные модули доступа;
- CRUD-права на модули;
- login по `OrganizationEmail + UserEmail + Password`;
- JWT access token;
- refresh token;
- email confirmation;
- password reset;
- change password;
- permission service;
- управление пользователями;
- управление ролями.

### Clients

Полноценно реализован первый бизнес-модуль **Clients**:

- проекты `Clients.Domain`, `Clients.Application`, `Clients.Infrastructure`, `Clients.Presentation`;
- сущность `Client`;
- enums `ClientStatus` и `ClientSource`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- `ClientResponse` DTO;
- `IClientRepository` и `ClientRepository`;
- EF configuration `ClientConfiguration`;
- thin API controller `ClientsController`;
- permissions через существующую Identity permission system;
- migration `AddClientsModule` в `Infrastructure/Migrations`.

Важные правила Clients:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Clients`;
- `Email` не unique;
- `Phone` не unique;
- при create/update обязателен хотя бы один контакт: `Email` или `Phone`;
- `FullName` не хранится в БД, а вычисляется в response DTO;
- `DELETE` выполняет soft delete через `IsActive = false`;
- нет FK на Identity `Organization`;
- нет `ClientsDbContext`;
- используется общий `ApplicationDbContext`.

### Catalog

Полноценно реализован третий модуль проекта и второй бизнес-модуль CRM **Catalog**:

- проекты `Catalog.Domain`, `Catalog.Application`, `Catalog.Infrastructure`, `Catalog.Presentation`;
- сущности `Category`, `Product`, `Service`;
- `Product` и `Service` являются отдельными сущностями;
- `Category` общая для товаров и услуг;
- enums `BonusType` и `DiscountType`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- response DTO: `CategoryResponse`, `ProductResponse`, `ServiceResponse`;
- `ICategoryRepository`, `IProductRepository`, `IServiceRepository` и реализации;
- EF configurations `CategoryConfiguration`, `ProductConfiguration`, `ServiceConfiguration`;
- thin API controllers: `CategoriesController`, `ProductsController`, `ServicesController`;
- permissions через существующую Identity permission system;
- migration `AddCatalogModule` в `Infrastructure/Migrations`.

Важные правила Catalog:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Catalog`;
- `Product.Sku` nullable и не unique;
- `ServiceCode` отсутствует;
- цены считаются в BYN, `Currency` column отсутствует;
- VAT/tax fields отсутствуют;
- `BonusType`/`BonusValue` и `DiscountType`/`DiscountValue` есть в `Category`, `Product`, `Service`;
- полноценный Promotions module не реализован;
- Bonus module не реализован;
- Warehouse Core реализован отдельным модулем и использует Catalog Product через Guid без FK;
- Deals реализован как MVP и интегрирован с Warehouse stock deduction, но не считается финально завершённым до будущих Bonus/Returns/Audit integrations;
- Catalog EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `CatalogDbContext`;
- используется общий `ApplicationDbContext`.

### Deals MVP

Реализован бизнес-модуль **Deals MVP**:

- проекты `Deals.Domain`, `Deals.Application`, `Deals.Infrastructure`, `Deals.Presentation`;
- сущности `Deal`, `DealItem`, `DealStage`, `DealStageHistory`;
- enums `DealItemType` и `DealDiscountType`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- response DTO: `DealResponse`, `DealItemResponse`, `DealStageResponse`, `DealStageHistoryResponse`;
- `CatalogItemSnapshot` для snapshot товара/услуги на момент сделки;
- `DealCalculationService` для расчёта сумм, item discounts и capped bonus usage;
- `IDealStageInitializer` и lazy creation default stages;
- repository interfaces и implementations: `IDealRepository`, `IDealStageRepository`, `IDealStageHistoryRepository`;
- lookup services: `IClientLookupService`, `IUserLookupService`, `ICatalogLookupService`;
- EF configurations `DealConfiguration`, `DealItemConfiguration`, `DealStageConfiguration`, `DealStageHistoryConfiguration`;
- thin API controllers: `DealsController`, `DealStagesController`;
- permissions через существующую Identity permission system;
- migration `AddDealsMvpModule` в `Infrastructure/Migrations`.

Важные правила Deals MVP:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Deals`;
- `DealStage` используется вместо enum `Status`;
- `DealStage` tenant-specific и редактируемый внутри организации;
- default stages `New`, `InProgress`, `Completed`, `Cancelled` создаются lazy при первом использовании Deals module;
- `DealStageHistory` создаётся при создании сделки и при изменении stage;
- `Deal` хранит `BonusPointsUsed` и `BonusDiscountAmount`, но Bonus module не реализован;
- requested bonus points capped до остатка после item discounts, для MVP `1 bonus point = 1 BYN`;
- `DealItem.StorageId` используется Warehouse Core при successful final stage;
- Product item требует `StorageId`, Service item требует `StorageId = null`;
- при successful final stage Warehouse списывает Product items и создаёт `StockMovement` Type `Sale`;
- Returns не реализованы, но остаются future iteration внутри Deals module;
- если текущий stage сделки `IsFinal = true`, `UpdateDeal`, `ChangeDealStage` и `DeactivateDeal` запрещены;
- внешних FK/navigation на Clients, Catalog, Identity или Warehouse нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `DealsDbContext`;
- используется общий `ApplicationDbContext`.

### Warehouse Core

Реализован бизнес-модуль **Warehouse Core**:

- проекты `Warehouse.Domain`, `Warehouse.Application`, `Warehouse.Infrastructure`, `Warehouse.Presentation`;
- сущности `Storage`, `ProductStock`, `StockMovement`;
- enum `StockMovementType`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- response DTO: `StorageResponse`, `ProductStockResponse`, `StockMovementResponse`;
- repository interfaces и implementations: `IStorageRepository`, `IProductStockRepository`, `IStockMovementRepository`;
- lookup service `IWarehouseProductLookupService`;
- integration service `IWarehouseDealCompletionService`;
- EF configurations `StorageConfiguration`, `ProductStockConfiguration`, `StockMovementConfiguration`;
- thin API controllers: `StoragesController`, `StocksController`, `StockMovementsController`;
- permissions через существующую Identity permission system;
- migration `20260503130507_AddWarehouseCoreModule` в `Infrastructure/Migrations`.

Важные правила Warehouse Core:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Warehouse`;
- первый склад организации становится default автоматически;
- нельзя деактивировать склад с положительными остатками;
- нельзя деактивировать default storage, если есть другие active storages;
- нельзя деактивировать последний active storage организации;
- inactive Catalog Product разрешён для складских операций, если Product существует в той же организации;
- отрицательные остатки запрещены;
- Warehouse EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `WarehouseDbContext`;
- используется общий `ApplicationDbContext`;
- нет внешних FK/navigation на Catalog, Deals, Identity или Organization;
- связи с Catalog, Deals и Identity только через Guid;
- Deals `ChangeDealStage` интегрирован со stock deduction при successful final stage;
- повторное списание сделки защищено через `StockMovement` Type `Sale` + `DealId`.

## Seeded module codes

Коды CRM-модулей seed-ятся как системные `Module` для прав доступа:

- `Users`
- `Roles`
- `Clients`
- `Deals`
- `Catalog`
- `Bonus`
- `Warehouse`
- `Chat`
- `Audit`
- `Settings`

`Clients`, `Catalog`, `Deals` и `Warehouse` уже реализованы как бизнес-модули. `Deals` реализован как MVP и не считается финально завершённым до Bonus/Returns/Audit integrations. `Warehouse` реализован как Core. Остальные CRM-коды пока являются только permission module codes или future modules.

## Endpoints

Public Identity:

- `POST /api/identity/organization-requests`
- `POST /api/identity/register-organization`
- `POST /api/identity/login`
- `POST /api/identity/refresh-token`
- `POST /api/identity/confirm-email`
- `POST /api/identity/forgot-password`
- `POST /api/identity/reset-password`

System admin Identity:

- `POST /api/identity/system-admin/login`
- `GET /api/identity/system-admin/organization-requests`
- `POST /api/identity/system-admin/organization-requests/{id}/approve`
- `POST /api/identity/system-admin/organization-requests/{id}/reject`

Authorized organization user Identity:

- `GET /api/identity/me`
- `POST /api/identity/change-password`
- `GET /api/identity/users`
- `POST /api/identity/users`
- `PUT /api/identity/users/{id}/role`
- `DELETE /api/identity/users/{id}`
- `GET /api/identity/roles`
- `POST /api/identity/roles`
- `PUT /api/identity/roles/{id}/permissions`
- `DELETE /api/identity/roles/{id}`
- `GET /api/identity/modules`

Authorized organization user Clients:

- `GET /api/clients`
- `GET /api/clients/{id}`
- `POST /api/clients`
- `PUT /api/clients/{id}`
- `DELETE /api/clients/{id}`

Authorized organization user Catalog:

- `GET /api/catalog/categories`
- `GET /api/catalog/categories/{id}`
- `POST /api/catalog/categories`
- `PUT /api/catalog/categories/{id}`
- `DELETE /api/catalog/categories/{id}`
- `GET /api/catalog/products`
- `GET /api/catalog/products/{id}`
- `POST /api/catalog/products`
- `PUT /api/catalog/products/{id}`
- `DELETE /api/catalog/products/{id}`
- `GET /api/catalog/services`
- `GET /api/catalog/services/{id}`
- `POST /api/catalog/services`
- `PUT /api/catalog/services/{id}`
- `DELETE /api/catalog/services/{id}`

Authorized organization user Deals:

- `GET /api/deals/stages`
- `POST /api/deals/stages`
- `PUT /api/deals/stages/{id}`
- `DELETE /api/deals/stages/{id}`
- `GET /api/deals`
- `GET /api/deals/{id}`
- `POST /api/deals`
- `PUT /api/deals/{id}`
- `PUT /api/deals/{id}/stage`
- `DELETE /api/deals/{id}`

Authorized organization user Warehouse:

- `GET /api/warehouse/storages`
- `GET /api/warehouse/storages/{id}`
- `POST /api/warehouse/storages`
- `PUT /api/warehouse/storages/{id}`
- `PUT /api/warehouse/storages/{id}/make-default`
- `DELETE /api/warehouse/storages/{id}`
- `GET /api/warehouse/stocks`
- `GET /api/warehouse/stocks/{id}`
- `POST /api/warehouse/stocks/receipt`
- `POST /api/warehouse/stocks/write-off`
- `POST /api/warehouse/stocks/correction`
- `GET /api/warehouse/movements`
- `GET /api/warehouse/movements/{id}`

Clients permissions:

- `Clients / Read`
- `Clients / Create`
- `Clients / Update`
- `Clients / Delete`

Catalog permissions:

- `Catalog / Read`
- `Catalog / Create`
- `Catalog / Update`
- `Catalog / Delete`

Deals permissions:

- `Deals / Read`
- `Deals / Create`
- `Deals / Update`
- `Deals / Delete`

Warehouse permissions:

- `Warehouse / Read`
- `Warehouse / Create`
- `Warehouse / Update`
- `Warehouse / Delete`

Важно: JWT системного администратора подходит для system-admin endpoints. Он не является пользователем организации и не должен открывать organization endpoints, включая Clients, Catalog, Deals и Warehouse.

## Текущие настройки WebApi

`CrmSystem/Program.cs` подключает:

- `ApplicationDbContext` через Npgsql;
- `AddIdentityApplication()`;
- `AddClientsApplication()`;
- `AddCatalogApplication()`;
- `AddDealsApplication()`;
- `AddWarehouseApplication()`;
- `AddInfrastructure(builder.Configuration)`;
- `AddIdentityInfrastructure()`;
- `AddClientsInfrastructure()`;
- `AddCatalogInfrastructure()`;
- `AddDealsInfrastructure()`;
- `AddWarehouseInfrastructure()`;
- controllers из `Identity.Presentation`;
- controllers из `Clients.Presentation`;
- controllers из `Catalog.Presentation`;
- controllers из `Deals.Presentation`;
- controllers из `Warehouse.Presentation`;
- JWT Bearer authentication;
- authorization policy `RequireSystemAdmin`;
- dynamic permission policies вида `Permission:{ModuleCode}:{Action}`;
- Swagger Bearer;
- `ExceptionHandlingMiddleware`.

## Следующий модуль

После Warehouse Core следующий рекомендуемый модуль - **Bonus Core**.

Почему Bonus связан с текущим состоянием Deals:

- Deals уже хранит `BonusPointsUsed` и `BonusDiscountAmount`, но не проверяет bonus balance/settings и не создаёт bonus transactions;
- Warehouse Core уже списывает Product items со склада при successful final stage;
- Returns принадлежат Deals module, но должны появиться после Bonus Core, чтобы корректно возвращать бонусы и складские остатки;
- Audit лучше делать после основных бизнес-операций Bonus/Warehouse/Returns.
