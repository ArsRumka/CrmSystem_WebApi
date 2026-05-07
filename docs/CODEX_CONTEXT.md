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
- `Deals.Application` - MediatR use cases, validators, DTO, calculation/default-stage/return services и repository/lookup interfaces Deals.
- `Deals.Infrastructure` - EF configurations, repository implementations, lookup services и return repository Deals.
- `Deals.Presentation` - thin API controllers Deals, включая returns endpoints.
- `Warehouse.Domain` - доменная модель Warehouse.
- `Warehouse.Application` - MediatR use cases, validators, DTO, repository/service abstractions Warehouse.
- `Warehouse.Infrastructure` - EF configurations, repositories, Catalog lookup, Deals completion integration и Deals return integration Warehouse.
- `Warehouse.Presentation` - thin API controllers Warehouse.
- `Bonus.Domain` - доменная модель Bonus.
- `Bonus.Application` - MediatR use cases, validators, DTO, repository/service abstractions Bonus.
- `Bonus.Infrastructure` - EF configurations, repositories, Clients/Catalog/Deals lookup, Deals completion integration и Deals return integration Bonus.
- `Bonus.Presentation` - thin API controllers Bonus.
- `Chat.Domain` - доменная модель Chat.
- `Chat.Application` - MediatR use cases, validators, DTO, repositories/lookups abstractions и application-level permission checks Chat.
- `Chat.Infrastructure` - EF configurations, repositories и lookup services Chat.
- `Chat.Presentation` - thin API controllers Chat и SignalR hub.
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
- Chat является особым случаем: inter-organization conversations связаны с двумя организациями, а доступ определяется explicit participant membership.
- Identity уже реализован, не переписывать его без необходимости.

## Current status

Реализованы и build-tested core-модули:

- **Identity** - implemented and tested.
- **Clients** - implemented and tested.
- **Catalog** - implemented and tested.
- **Deals MVP** - implemented and tested.
- **Warehouse Core** - implemented and tested.
- **Bonus Core** - implemented.
- **Returns Core inside Deals** - implemented.
- **Chat Core with SignalR** - implemented.

Готов фундамент проекта:

- общий `ApplicationDbContext`;
- миграции `InitialCreate`, `AddIdentityFoundation`, `AddClientsModule`, `AddCatalogModule`, `AddDealsMvpModule`, `AddWarehouseCoreModule`, `20260506131632_AddBonusCoreModule`, `20260507121737_AddDealsReturnsCore`, `20260507173218_AddChatCoreModule`;
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
- Bonus Core реализован отдельным модулем и использует Catalog bonus rule fields;
- Warehouse Core реализован отдельным модулем и использует Catalog Product через Guid без FK;
- Deals реализован как MVP/Core, интегрирован с Warehouse stock deduction, Bonus completion и Returns Core inside Deals;
- Catalog EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `CatalogDbContext`;
- используется общий `ApplicationDbContext`.

### Deals MVP/Core

Реализован бизнес-модуль **Deals MVP/Core**:

- проекты `Deals.Domain`, `Deals.Application`, `Deals.Infrastructure`, `Deals.Presentation`;
- сущности `Deal`, `DealItem`, `DealStage`, `DealStageHistory`, `DealReturn`, `DealReturnItem`;
- enums `DealItemType`, `DealDiscountType` и `DealReturnStatus`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- response DTO: `DealResponse`, `DealItemResponse`, `DealStageResponse`, `DealStageHistoryResponse`, `DealReturnResponse`, `DealReturnItemResponse`;
- `CatalogItemSnapshot` для snapshot товара/услуги на момент сделки;
- `DealCalculationService` для расчёта сумм, item discounts и capped bonus usage;
- `IDealStageInitializer` и lazy creation default stages;
- repository interfaces и implementations: `IDealRepository`, `IDealStageRepository`, `IDealStageHistoryRepository`, `IDealReturnRepository`;
- lookup services: `IClientLookupService`, `IUserLookupService`, `ICatalogLookupService`;
- EF configurations `DealConfiguration`, `DealItemConfiguration`, `DealStageConfiguration`, `DealStageHistoryConfiguration`, `DealReturnConfiguration`, `DealReturnItemConfiguration`;
- thin API controllers: `DealsController`, `DealStagesController`, `DealReturnsController`;
- permissions через существующую Identity permission system;
- migrations `AddDealsMvpModule` и `20260507121737_AddDealsReturnsCore` в `Infrastructure/Migrations`.

Важные правила Deals MVP/Core:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Deals`;
- `DealStage` используется вместо enum `Status`;
- `DealStage` tenant-specific и редактируемый внутри организации;
- default stages `New`, `InProgress`, `Completed`, `Cancelled` создаются lazy при первом использовании Deals module;
- `DealStageHistory` создаётся при создании сделки и при изменении stage;
- `Deal` хранит `BonusPointsUsed` как applied bonus points и `BonusDiscountAmount` как денежную скидку в BYN;
- `CreateDeal` и `UpdateDeal` используют Bonus Core для расчёта bonus discount через `PointValue`;
- `DealItem.StorageId` используется Warehouse Core при successful final stage;
- Product item требует `StorageId`, Service item требует `StorageId = null`;
- при successful final stage Warehouse списывает Product items и создаёт `StockMovement` Type `Sale`;
- при successful final stage Bonus Core списывает использованные бонусы и начисляет новые бонусы, если начисление разрешено;
- Returns не являются отдельным модулем и реализованы внутри Deals module;
- `DealReturnStatus`: `Draft`, `Completed`, `Cancelled`;
- Draft returns не меняют Warehouse/Bonus;
- completed returns создают Warehouse `Return` movement и Bonus `Refund` / `CorrectionDecrease`;
- completed/cancelled returns immutable;
- partial returns разрешены, но нельзя вернуть больше, чем было продано по каждой `DealItem`;
- `SourceReturnId` добавлен в `StockMovement` и `BonusTransaction` как nullable Guid correlation id без FK;
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
- return integration service `IWarehouseDealReturnService`;
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
- Deals `ChangeDealStage` выполняет Warehouse completion и Bonus completion перед stage change/history и одним общим `SaveChangesAsync`;
- Deals return completion создаёт `StockMovement` Type `Return`, заполняет `SourceReturnId` и сохраняется одним общим UnitOfWork;
- повторное списание сделки защищено через `StockMovement` Type `Sale` + `DealId`.

### Bonus Core

Реализован бизнес-модуль **Bonus Core**:

- проекты `Bonus.Domain`, `Bonus.Application`, `Bonus.Infrastructure`, `Bonus.Presentation`;
- сущности `BonusSettings`, `BonusAccount`, `BonusTransaction`;
- enums `BonusAccrualType`, `BonusTransactionType`;
- MediatR commands/queries/handlers;
- FluentValidation validators;
- response DTO: `BonusSettingsResponse`, `BonusAccountResponse`, `BonusTransactionResponse`;
- repository interfaces и implementations: `IBonusSettingsRepository`, `IBonusAccountRepository`, `IBonusTransactionRepository`;
- services: `IBonusDealDiscountService`, `IBonusDealCompletionService`, `IBonusDealReturnService`, `IBonusClientLookupService`;
- Catalog bonus rule resolver для Product/Service -> Category -> parent category chain -> BonusSettings;
- EF configurations `BonusSettingsConfiguration`, `BonusAccountConfiguration`, `BonusTransactionConfiguration`;
- thin API controllers: `BonusSettingsController`, `BonusAccountsController`, `BonusTransactionsController`;
- permissions через существующую Identity permission system;
- migration `20260506131632_AddBonusCoreModule` в `Infrastructure/Migrations`.

Важные правила Bonus Core:

- все операции tenant-scoped через `OrganizationId` из `ICurrentUserService`;
- permissions используют module code `Bonus`;
- `BonusDbContext` не создавался;
- используется общий `ApplicationDbContext`;
- Bonus EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Clients, Deals, Catalog, Identity или Organization нет;
- связи с Clients, Deals, Catalog и Identity только через Guid;
- `PointValue` задаёт, сколько BYN даёт 1 бонусный балл;
- `BonusSettings` создаются лениво через `GET /api/bonus/settings`;
- если settings отсутствуют во время расчёта/закрытия сделки, бонусная система считается выключенной;
- ручная корректировка баланса требует reason;
- duplicate completion защищён проверкой automated BonusTransaction by `DealId`;
- return-origin операции считаются по `DealId`, `SourceReturnId != null` и `Type = Refund / CorrectionDecrease`;
- `AccrueOnBonusPayment` управляет начислением, если сделка использовала бонусы.

### Chat Core with SignalR

Реализован коммуникационный модуль **Chat Core with SignalR**:

- проекты `Chat.Domain`, `Chat.Application`, `Chat.Infrastructure`, `Chat.Presentation`;
- сущности `ChatConversation`, `ChatConversationOrganization`, `ChatParticipant`, `ChatMessage`, `ChatContactRequest`;
- enums `ChatConversationType` и `ChatContactRequestStatus`;
- direct/group chats внутри организации;
- чаты, связанные с `ClientId`;
- чаты, связанные с `DealId`;
- межорганизационные чаты через contact request;
- REST endpoints для conversations, messages, participants и contact requests;
- REST fallback отправки сообщений;
- SignalR hub `/hubs/chat`;
- realtime events `MessageReceived`, `MessageEdited`, `MessageDeleted`, `ConversationRead`, `UserTyping`, `ParticipantAdded`, `ParticipantRemoved`, `ContactRequestReceived`;
- permissions через существующую Identity permission system;
- migration `20260507173218_AddChatCoreModule` в `Infrastructure/Migrations`.

Важные правила Chat:

- используется общий `ApplicationDbContext`;
- `ChatDbContext` не создавался;
- Chat EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- связи с другими модулями только через Guid;
- inter-org chat ограничен двумя организациями;
- inter-org chat создаётся только через approved contact request;
- inter-org conversation видят только explicit participants, а не все сотрудники организации;
- message хранит `SenderOrganizationId` и `SenderUserId`;
- JWT `access_token` из query string принимается только для `/hubs/chat`.

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

`Clients`, `Catalog`, `Deals`, `Warehouse`, `Bonus` и `Chat` уже реализованы как бизнес-модули или MVP/Core-модули. `Deals` реализован как MVP/Core с Returns Core inside Deals. `Warehouse` и `Bonus` реализованы как Core и поддерживают return integration через `SourceReturnId`. `Chat` реализован как Core with SignalR. `Audit` пока является permission module code для будущей бизнес-функции.

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
- `GET /api/deals/{dealId}/returns`
- `GET /api/deals/returns/{id}`
- `POST /api/deals/{dealId}/returns`
- `PUT /api/deals/returns/{id}`
- `POST /api/deals/returns/{id}/complete`
- `POST /api/deals/returns/{id}/cancel`

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

Authorized organization user Bonus:

- `GET /api/bonus/settings`
- `PUT /api/bonus/settings`
- `GET /api/bonus/accounts`
- `GET /api/bonus/accounts/{id}`
- `GET /api/bonus/accounts/by-client/{clientId}`
- `POST /api/bonus/accounts/by-client/{clientId}/adjust`
- `GET /api/bonus/transactions`
- `GET /api/bonus/transactions/{id}`

Authorized organization user Chat:

- `GET /api/chat/conversations`
- `GET /api/chat/conversations/{id}`
- `POST /api/chat/conversations`
- `PUT /api/chat/conversations/{id}`
- `DELETE /api/chat/conversations/{id}`
- `GET /api/chat/conversations/{id}/messages`
- `POST /api/chat/conversations/{id}/messages`
- `POST /api/chat/conversations/{id}/read`
- `POST /api/chat/conversations/{id}/participants`
- `DELETE /api/chat/conversations/{id}/participants/{userId}`
- `PUT /api/chat/messages/{id}`
- `DELETE /api/chat/messages/{id}`
- `POST /api/chat/contact-requests`
- `GET /api/chat/contact-requests/incoming`
- `GET /api/chat/contact-requests/outgoing`
- `POST /api/chat/contact-requests/{id}/approve`
- `POST /api/chat/contact-requests/{id}/reject`
- `POST /api/chat/contact-requests/{id}/cancel`

SignalR Chat:

- `/hubs/chat`

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

Bonus permissions:

- `Bonus / Read`
- `Bonus / Update`

Chat permissions:

- `Chat / Read`
- `Chat / Create`
- `Chat / Update`
- `Chat / Delete`

Важно: JWT системного администратора подходит для system-admin endpoints. Он не является пользователем организации и не должен открывать organization endpoints, включая Clients, Catalog, Deals, Warehouse, Bonus и Chat.

## Текущие настройки WebApi

`CrmSystem/Program.cs` подключает:

- `ApplicationDbContext` через Npgsql;
- `AddIdentityApplication()`;
- `AddChatApplication()`;
- `AddBonusApplication()`;
- `AddClientsApplication()`;
- `AddCatalogApplication()`;
- `AddDealsApplication()`;
- `AddWarehouseApplication()`;
- `AddInfrastructure(builder.Configuration)`;
- `AddIdentityInfrastructure()`;
- `AddChatInfrastructure()`;
- `AddChatPresentation()`;
- `AddBonusInfrastructure()`;
- `AddClientsInfrastructure()`;
- `AddCatalogInfrastructure()`;
- `AddDealsInfrastructure()`;
- `AddWarehouseInfrastructure()`;
- controllers из `Identity.Presentation`;
- controllers из `Chat.Presentation`;
- controllers из `Bonus.Presentation`;
- controllers из `Clients.Presentation`;
- controllers из `Catalog.Presentation`;
- controllers из `Deals.Presentation`;
- controllers из `Warehouse.Presentation`;
- SignalR через `AddSignalR()`;
- ChatHub route `/hubs/chat`;
- JWT Bearer authentication;
- SignalR JWT query token support: `access_token` читается только для path `/hubs/chat`;
- authorization policy `RequireSystemAdmin`;
- dynamic permission policies вида `Permission:{ModuleCode}:{Action}`;
- Swagger Bearer;
- `ExceptionHandlingMiddleware`.

## Следующий модуль

После Chat Core with SignalR следующий рекомендуемый модуль - **Email Campaigns Core**.

Почему Email Campaigns связан с текущим состоянием проекта:

- Deals уже покрывает lifecycle sale -> warehouse deduction -> bonus write-off/accrual -> return -> warehouse return -> bonus refund/reversal;
- Chat уже добавил коммуникационный слой CRM без изменения core business flow;
- Email Campaigns логично расширит коммуникации от диалогов к клиентским рассылкам;
- Audit лучше делать после Email Campaigns, чтобы логировать основные бизнес-события и действия коммуникационных модулей.
