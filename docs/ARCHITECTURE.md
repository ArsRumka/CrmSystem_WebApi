# Архитектура проекта

## Общий стиль

Проект построен как **Modular Monolith + Clean Architecture**.

Это одно ASP.NET Core приложение, одна база данных и один общий `ApplicationDbContext`. Модули разделены логически по проектам и namespace, но не являются отдельными микросервисами.

Основной поток:

```text
HTTP API
 -> Controller
 -> MediatR Command/Query
 -> Handler
 -> Repository interface
 -> Repository implementation
 -> ApplicationDbContext
 -> PostgreSQL
```

Realtime Chat flow:

```text
SignalR client
 -> ChatHub
 -> MediatR Command
 -> Handler
 -> Repository interface
 -> Repository implementation
 -> ApplicationDbContext
 -> PostgreSQL
 -> SignalR group event
```

`ChatHub` является тонким слоем: он проверяет подключение к группам, вызывает application commands и отправляет realtime events. Бизнес-правила остаются в Application handlers.

## Роль BuildingBlocks

`BuildingBlocks.*` содержат общие элементы без конкретной бизнес-логики.

`BuildingBlocks.Application`:

- `IUnitOfWork`;
- `ICurrentUserService`;
- `IEmailSender`;
- `IDateTimeProvider`;
- common exceptions: `NotFoundException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`, `ApplicationValidationException`.

`BuildingBlocks.Infrastructure`:

- базовый `AppDbContext`;
- общая инфраструктурная база для EF Core.

`BuildingBlocks.Domain`:

- место для общих доменных базовых типов, если они нужны нескольким модулям.

## Роль Infrastructure

`Infrastructure` - общий инфраструктурный проект всего приложения.

Здесь находятся:

- `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `Infrastructure/Persistence/UnitOfWork.cs`;
- `Infrastructure/Migrations`;
- общие runtime-сервисы: current user, email, time, authorization;
- `Infrastructure/DependencyInjection.cs`.

`ApplicationDbContext` является единственным реальным EF Core DbContext приложения.

## Роль Modules

Модули размещаются как отдельные проекты:

- `{Module}.Domain`
- `{Module}.Application`
- `{Module}.Infrastructure`
- `{Module}.Presentation`

Сейчас реализованы core-модули:

- `Identity` - foundation/runtime и permission system.
- `Clients` - первый бизнес-модуль CRM.
- `Catalog` - второй бизнес-модуль CRM: категории, товары и услуги.
- `Deals` - MVP/Core продаж, этапов сделок и Returns Core.
- `Warehouse` - Core складского учёта.
- `Bonus` - Core бонусной системы.
- `Chat` - Core коммуникаций с REST API и SignalR.
- `Email` - Core клиентских email-рассылок, SMTP-настроек и automation.
- `Audit` - Core журналирования ключевых бизнес-действий.

Будущие CRM-модули должны повторять этот стиль, но не создавать собственные DbContext и отдельные базы.

У каждого нового модуля должны быть extension methods:

- `Add{Module}Application()`;
- `Add{Module}Infrastructure()`.

WebApi проект `CrmSystem` должен вызвать оба метода в `Program.cs`.

Если у модуля есть отдельный `{Module}.Presentation` project, WebApi должен подключить его controllers через `AddApplicationPart`.

## Clients как пример бизнес-модуля

`Clients` реализован как второй модуль проекта и первый бизнес-модуль после Identity:

- `Clients.Domain` содержит `Client`, `ClientStatus`, `ClientSource`.
- `Clients.Application` содержит MediatR use cases, validators, DTO и `IClientRepository`.
- `Clients.Infrastructure` содержит `ClientConfiguration`, `ClientRepository`, `AddClientsInfrastructure()`.
- `Clients.Presentation` содержит thin `ClientsController`.

Архитектурные детали Clients:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `ClientsDbContext` нет;
- EF configuration подключена в общий `ApplicationDbContext`;
- migration `AddClientsModule` создана в `Infrastructure/Migrations`;
- controller подключён через application part в `CrmSystem/Program.cs`;
- permissions используют существующую Identity permission system через module code `Clients`;
- нет FK на Identity `Organization`, tenant scope хранится как `OrganizationId`.

## Catalog как реализованный бизнес-модуль

`Catalog` реализован как третий модуль проекта после Identity и Clients:

- `Catalog.Domain` содержит `Category`, `Product`, `Service`, `BonusType`, `DiscountType`.
- `Catalog.Application` содержит MediatR use cases, validators, response DTO и repository interfaces.
- `Catalog.Infrastructure` содержит EF configurations, repositories и `AddCatalogInfrastructure()`.
- `Catalog.Presentation` содержит thin controllers для categories, products и services.

Архитектурные детали Catalog:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `CatalogDbContext` нет;
- `Category` общая для товаров и услуг;
- `Product` и `Service` являются отдельными сущностями;
- `Product.Sku` nullable, индексируется, но не unique;
- у `Service` нет SKU и нет `ServiceCode`;
- цены считаются в BYN, `Currency` column не добавлен;
- VAT/tax fields не добавлены;
- `BonusType`/`BonusValue` и `DiscountType`/`DiscountValue` есть в `Category`, `Product`, `Service`;
- полноценный Promotions module не реализован;
- Deals MVP и Warehouse Core реализованы отдельными модулями;
- Bonus Core реализован отдельным модулем и использует Catalog bonus rules;
- migration `AddCatalogModule` создана в `Infrastructure/Migrations`;
- permissions используют существующую Identity permission system через module code `Catalog`;
- нет FK на Identity `Organization`, tenant scope хранится как `OrganizationId`.

## Warehouse как реализованный Core-модуль

`Warehouse` реализован как складской Core после Deals MVP:

- `Warehouse.Domain` содержит `Storage`, `ProductStock`, `StockMovement`, `StockMovementType`.
- `Warehouse.Application` содержит MediatR use cases, validators, DTO, repository abstractions и integration abstractions.
- `Warehouse.Infrastructure` содержит EF configurations, repositories, Catalog product lookup, Deals completion integration service и Deals return integration service.
- `Warehouse.Presentation` содержит thin controllers для storages, stocks и movements.

Архитектурные детали Warehouse:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `WarehouseDbContext` нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider` из `AddWarehouseInfrastructure()`;
- migration `AddWarehouseCoreModule` создана в `Infrastructure/Migrations`;
- permissions используют existing Identity permission system через module code `Warehouse`;
- внешних FK/navigation на Catalog, Deals, Identity и Organization нет;
- `ProductId`, `DealId`, `CreatedByUserId` хранятся как Guid;
- `SourceReturnId` хранится как nullable Guid correlation id на `DealReturn.Id`, без FK;
- internal Warehouse FK есть только от `ProductStocks.StorageId` и `StockMovements.StorageId` к `Storages.Id`;
- Deals `ChangeDealStage` вызывает Warehouse integration service при successful final stage, а сохранение выполняется через общий `IUnitOfWork`.
- Deals return completion вызывает Warehouse return integration service для Product returns; service не вызывает `SaveChangesAsync`.

## Bonus как реализованный Core-модуль

`Bonus` реализован как бонусный Core после Warehouse Core:

- `Bonus.Domain` содержит `BonusSettings`, `BonusAccount`, `BonusTransaction`, `BonusAccrualType`, `BonusTransactionType`.
- `Bonus.Application` содержит MediatR use cases, validators, DTO, repository abstractions и Deals integration abstractions.
- `Bonus.Infrastructure` содержит EF configurations, repositories, Clients/Catalog/Deals lookup, completion integration services и return integration service.
- `Bonus.Presentation` содержит thin controllers для settings, accounts и transactions.

Архитектурные детали Bonus:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `BonusDbContext` нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider` из `AddBonusInfrastructure()`;
- migration `AddBonusCoreModule` создана в `Infrastructure/Migrations`;
- permissions используют existing Identity permission system через module code `Bonus`;
- внешних FK/navigation на Clients, Deals, Catalog, Identity и Organization нет;
- `ClientId`, `DealId`, `CreatedByUserId` и `OrganizationId` хранятся как Guid;
- `SourceReturnId` хранится как nullable Guid correlation id на `DealReturn.Id`, без FK;
- internal Bonus FK есть только от `BonusTransactions.BonusAccountId` к `BonusAccounts.Id`;
- Deals `CreateDeal` и `UpdateDeal` используют `IBonusDealDiscountService` из `Bonus.Application`;
- Deals `ChangeDealStage` вызывает Warehouse completion, затем Bonus completion, затем меняет stage/history и сохраняет через общий `IUnitOfWork`.
- Deals return completion вызывает Warehouse return service, затем Bonus return service, затем переводит `DealReturn` в `Completed` и сохраняет все изменения через один общий `IUnitOfWork`.

## Returns inside Deals

Returns Core живёт внутри Deals module. Отдельного Returns module, `ReturnsDbContext` или отдельной базы нет.

Архитектурные правила Returns Core:

- `DealReturn` и `DealReturnItem` находятся в `Deals.Domain`;
- EF configurations находятся в `Deals.Infrastructure`;
- migrations остаются в `Infrastructure/Migrations`;
- Warehouse и Bonus подключаются через Application abstractions;
- `SourceReturnId` в `StockMovement` и `BonusTransaction` является nullable Guid correlation id без FK;
- integration services не вызывают `SaveChangesAsync`;
- completion возврата сохраняет DealReturn, Warehouse return movement и Bonus refund/reversal через один shared UnitOfWork.

Так сохраняются modular boundaries и transaction consistency: если Bonus return падает после Warehouse return, складские изменения не попадают в БД.

## Chat как модуль с особым tenant scoping

`Chat` реализован как отдельный core-модуль коммуникаций:

- `Chat.Domain` содержит `ChatConversation`, `ChatConversationOrganization`, `ChatParticipant`, `ChatMessage`, `ChatContactRequest`;
- `Chat.Application` содержит MediatR use cases, validators, DTO, repository/lookup abstractions и application-level permission checks;
- `Chat.Infrastructure` содержит EF configurations, repositories и lookup services;
- `Chat.Presentation` содержит thin REST controllers и `ChatHub`.

Обычные direct/group/client/deal conversations остаются tenant-scoped внутри одной организации. Inter-organization conversations отличаются:

- conversation связан с двумя organizations через `ChatConversationOrganization`;
- `OwnerOrganizationId` хранит организацию-владельца conversation;
- `ChatMessage` хранит `SenderOrganizationId` и `SenderUserId`;
- доступ определяется explicit participant membership, а не простым фильтром по `OrganizationId`;
- пользователи организации, которые не являются explicit participants, не видят inter-org conversation;
- inter-org conversation создаётся только через approved `ChatContactRequest`;
- Core-реализация ограничивает inter-org conversation двумя организациями.

Связи с Identity User, Identity Organization, Clients и Deals всё равно выполняются только через Guid. Внешних EF FK/navigation на эти модули нет.

## Email как реализованный Core-модуль

`Email` реализован как отдельный core-модуль коммуникаций:

- `Email.Domain` содержит `EmailSettings`, `EmailTemplate`, `EmailCampaign`, `EmailCampaignRecipient`, `EmailAutomationRule`;
- `Email.Application` содержит MediatR use cases, validators, DTO, repository/service abstractions;
- `Email.Infrastructure` содержит EF configurations, repositories, Clients/Deals/Identity lookups, SMTP sender, Data Protection password protector, campaign sender, automation runner и hosted service;
- `Email.Presentation` содержит thin REST controllers.

Архитектурные детали Email:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `EmailDbContext` нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider` из `AddEmailInfrastructure()`;
- migration `AddEmailCampaignsCoreModule` создана в `Infrastructure/Migrations`;
- permissions используют existing Identity permission system через module code `Email`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- `OrganizationId`, `CreatedByUserId` и `ClientId` хранятся как Guid references;
- internal Email FK есть только между `EmailCampaigns`, `EmailCampaignRecipients`, `EmailTemplates` и `EmailAutomationRules`;
- SMTP credentials хранятся encrypted через ASP.NET Core Data Protection;
- API не возвращает plain password или `PasswordEncrypted`;
- campaign sending использует только SMTP settings конкретной организации и не использует global `IEmailSender`;
- `EmailAutomationHostedService` запускает inactive-client automation по расписанию из `EmailAutomation` options.

Email является примером модуля с внешними side effects: отправка писем выполняется через реальный SMTP. Поэтому настройки, failure behavior и credential security нужно документировать явно.

## Audit как cross-cutting module

`Audit` реализован как отдельный core-модуль для журнала ключевых бизнес-действий:

- `Audit.Domain` содержит `AuditLog` и `AuditAction`;
- `Audit.Application` содержит read-only MediatR queries, DTO, `IAuditLogRepository` и `IAuditLogService`;
- `Audit.Infrastructure` содержит EF configuration, repository и реализацию `IAuditLogService`;
- `Audit.Presentation` содержит thin REST controller для read-only API.

Архитектурные детали Audit:

- модуль использует общий `Infrastructure.Persistence.ApplicationDbContext`;
- собственного `AuditDbContext` нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider` из `AddAuditInfrastructure()`;
- migration `AddAuditCoreModule` создана в `Infrastructure/Migrations`;
- permissions используют existing Identity permission system через module code `Audit`;
- public API только read-only: `GET /api/audit/logs` и `GET /api/audit/logs/{id}`;
- business modules reference only `Audit.Application`;
- `Audit.Application` не зависит от business modules;
- `Audit.Infrastructure` записывает `AuditLogs` через shared `ApplicationDbContext`;
- `AuditLogService` не вызывает `SaveChangesAsync`;
- audit entries сохраняются в transaction вызывающего handler через общий `IUnitOfWork`;
- внешних FK/navigation на Identity, Organization, Clients, Catalog, Deals, Warehouse, Bonus, Chat или Email нет;
- связи с business modules хранятся через Guid/string: `OrganizationId`, `UserId`, `EntityId`, `ModuleCode`, `EntityName`;
- используются manual audit calls in selected handlers вместо EF interceptor;
- automatic audit всех EF changes намеренно не реализован.

Audit является cross-cutting module, но не нарушает direction dependencies: другие модули видят только application abstraction `IAuditLogService`, а запись в БД остаётся в инфраструктурном слое Audit.

## SignalR

REST API по-прежнему обрабатывается через controllers -> MediatR -> handlers. Realtime communication для Chat идёт через:

```text
Chat.Presentation/Hubs/ChatHub.cs
```

Hub route:

```text
/hubs/chat
```

Архитектурные правила SignalR:

- `ChatHub` не содержит бизнес-логики;
- `JoinConversation`, `SendMessage`, `MarkAsRead` и `Typing` вызывают application commands;
- `SendMessage` использует тот же `SendMessageCommand`, что и REST fallback;
- permission checks для Hub выполняются на application level, потому что dynamic permission attributes недостаточно для hub methods;
- JWT `access_token` из query string поддерживается только для path `/hubs/chat`;
- обычный Bearer JWT flow для REST endpoints не меняется.

## DbContext и migrations

Единственный DbContext:

```text
Infrastructure/Persistence/ApplicationDbContext.cs
```

Он наследуется от:

```text
BuildingBlocks.Infrastructure/Persistence/AppDbContext.cs
```

EF configurations подключаются через `IEfConfigurationAssemblyProvider`.

Каждый `{Module}.Infrastructure` регистрирует assembly со своими EF configurations:

```csharp
services.AddSingleton<IEfConfigurationAssemblyProvider>(
    new EfConfigurationAssemblyProvider(typeof(SomeConfiguration).Assembly));
```

`ApplicationDbContext` получает `IEnumerable<IEfConfigurationAssemblyProvider>` и вызывает `ApplyConfigurationsFromAssembly(provider.Assembly)` для каждого provider.

Сейчас таким способом подключены configurations:

- `Identity.Infrastructure.Configurations`;
- `Clients.Infrastructure.Configurations`;
- `Catalog.Infrastructure.Configurations`.
- `Deals.Infrastructure.Configurations`;
- `Warehouse.Infrastructure.Configurations`.
- `Bonus.Infrastructure.Configurations`.
- `Chat.Infrastructure.Configurations`.
- `Email.Infrastructure.Configurations`.
- `Audit.Infrastructure.Configurations`.

При добавлении нового модуля его EF configurations нужно регистрировать в DI внутри `{Module}.Infrastructure`. Не добавлять reference из `Infrastructure` на `{Module}.Infrastructure`, не использовать строковый `Assembly.Load` и не создавать отдельный DbContext.

Миграции лежат только здесь:

```text
Infrastructure/Migrations
```

Текущие миграции:

- `InitialCreate`
- `AddIdentityFoundation`
- `AddClientsModule`
- `AddCatalogModule`
- `AddDealsMvpModule`
- `AddWarehouseCoreModule`
- `AddBonusCoreModule`
- `AddDealsReturnsCore`
- `AddChatCoreModule`
- `AddEmailCampaignsCoreModule`
- `AddAuditCoreModule`

Команды:

```bash
dotnet ef migrations add MigrationName --project Infrastructure --startup-project CrmSystem
dotnet ef database update --project Infrastructure --startup-project CrmSystem
dotnet ef migrations list --project Infrastructure --startup-project CrmSystem
```

## Почему один DbContext

Проект является modular monolith, а не набором микросервисов. Поэтому:

- приложение одно;
- база данных одна;
- транзакции могут охватывать несколько модулей;
- migrations общие;
- проще поддерживать дипломный проект и демонстрационные сценарии.

Создание отдельных DbContext на модуль нарушит текущую архитектуру и усложнит migrations.

## Repository + UnitOfWork

В проекте используются предметные repositories. Generic repository запрещён.

Правильно:

- `IUserRepository`
- `IRoleRepository`
- `IOrganizationRepository`
- `IClientRepository`
- `ICategoryRepository`
- `IProductRepository`
- `IServiceRepository`
- `IStorageRepository`
- `IProductStockRepository`
- `IStockMovementRepository`
- `IDealReturnRepository`
- `IBonusSettingsRepository`
- `IBonusAccountRepository`
- `IBonusTransactionRepository`
- `IChatConversationRepository`
- `IChatParticipantRepository`
- `IChatMessageRepository`
- `IChatContactRequestRepository`
- `IEmailSettingsRepository`
- `IEmailTemplateRepository`
- `IEmailCampaignRepository`
- `IEmailCampaignRecipientRepository`
- `IEmailAutomationRuleRepository`
- `IAuditLogRepository`

Неправильно:

- `IRepository<T>`
- `GenericRepository<T>`

Repositories:

- читают и изменяют tracked entities через `ApplicationDbContext`;
- не вызывают `SaveChangesAsync`;
- не управляют транзакцией самостоятельно.

Commit выполняется через:

```text
BuildingBlocks.Application.Abstractions.Persistence.IUnitOfWork
```

Реализация:

```text
Infrastructure/Persistence/UnitOfWork.cs
```

## Permissions

Permissions построены вокруг системной сущности `Module`.

`Module` - это справочник кодов доступа:

- `Id`
- `Code`
- `Name`

`Module` не tenant-specific и не содержит `OrganizationId`.

`Role` tenant-specific и принадлежит организации через `OrganizationId`.

`ModuleRole` связывает роль и module, а также хранит CRUD-флаги:

- `CanRead`
- `CanCreate`
- `CanUpdate`
- `CanDelete`

Проверка прав:

```text
RequirePermissionAttribute
 -> dynamic policy Permission:{ModuleCode}:{Action}
 -> PermissionAuthorizationHandler
 -> IPermissionService
 -> User + Module + ModuleRole
```

Clients endpoints используют:

- `Clients / Read`
- `Clients / Create`
- `Clients / Update`
- `Clients / Delete`

Catalog endpoints используют:

- `Catalog / Read`
- `Catalog / Create`
- `Catalog / Update`
- `Catalog / Delete`

Warehouse endpoints используют:

- `Warehouse / Read`
- `Warehouse / Create`
- `Warehouse / Update`
- `Warehouse / Delete`

Bonus endpoints используют:

- `Bonus / Read`
- `Bonus / Update`

Chat REST endpoints используют:

- `Chat / Read`
- `Chat / Create`
- `Chat / Update`
- `Chat / Delete`

ChatHub проверяет динамические permissions через application handlers/services: `SendMessage` требует `Chat/Create`, `MarkAsRead` требует `Chat/Update`, а `JoinConversation` и `Typing` требуют active participant membership.

Email endpoints используют:

- `Email / Read`
- `Email / Create`
- `Email / Update`
- `Email / Delete`

Audit endpoints используют:

- `Audit / Read`

Системный администратор платформы не является пользователем организации и не получает tenant permissions.

## Правила зависимостей

Общие правила:

- Domain не зависит от EF Core, ASP.NET Core, Infrastructure.
- Domain не содержит DataAnnotations и EF атрибуты.
- Application зависит от Domain и BuildingBlocks abstractions.
- Infrastructure зависит от Application и Domain.
- Presentation зависит от Application и ASP.NET Core.
- WebApi собирает все зависимости через DI.

Между бизнес-модулями не нужно строить жёсткие EF-навигации. Для связей между модулями использовать Guid ID.

Bonus является примером межмодульной интеграции без внешних FK: он хранит `ClientId`, `DealId` и `CreatedByUserId` как Guid, интегрируется с Deals через `Bonus.Application` abstractions, а `Bonus.Infrastructure` реализует сервисы через общий `ApplicationDbContext`.

Returns внутри Deals является примером cross-module lifecycle без нового модуля: Deals хранит `DealReturn`, Warehouse и Bonus получают `SourceReturnId` как correlation Guid без FK, а все эффекты сохраняются через общий UnitOfWork.

Chat является примером модуля с межорганизационным доступом без внешних FK: он хранит `OwnerOrganizationId`, `OrganizationId`, `UserId`, `ClientId`, `DealId`, `SenderOrganizationId` и `SenderUserId` как Guid, а межорганизационный доступ ограничивает через `ChatConversationOrganization` и `ChatParticipant`.

Email является примером модуля с внешними integration lookups и side effects без внешних FK: он хранит `OrganizationId`, `CreatedByUserId`, `ClientId` и `TemplateId` как Guid references, выбирает inactive clients через Clients/Deals tables в `Email.Infrastructure`, а письма отправляет через SMTP settings организации.

Audit является примером cross-cutting module без внешних FK: он хранит `OrganizationId`, nullable `UserId`, nullable `EntityId`, `ModuleCode` и `EntityName`, принимает записи через `IAuditLogService` из `Audit.Application`, а `Audit.Infrastructure` добавляет `AuditLog` в shared `ApplicationDbContext` без собственного save.

## Tenant scoping

Все бизнес-данные CRM должны быть tenant-scoped через `OrganizationId`.

Исключения:

- `SystemAdmin` - системный пользователь платформы, не принадлежит организации.
- `OrganizationRequest` - заявка до создания организации.
- `ActivationKey` - ключ регистрации организации.
- `Module` - системный справочник прав доступа.
- `Chat InterOrganization conversation` - связан с двумя организациями, но доступ всё равно определяется explicit participant membership.

Clients полностью tenant-scoped:

- `Client.OrganizationId` обязателен;
- list/search/get/update/delete всегда фильтруются по `OrganizationId`;
- `OrganizationId` берётся из `ICurrentUserService`;
- system admin JWT не является tenant context.

Chat tenant scoping:

- direct/group/client/deal conversations фильтруются по participant membership внутри организации;
- inter-organization conversations связаны с двумя организациями;
- участник видит conversation только если есть `ChatParticipant`;
- активная отправка сообщения требует active participant;
- organization membership хранится в `ChatConversationOrganization`, без FK на Identity Organization.

Email tenant scoping:

- `EmailSettings`, `EmailTemplate`, `EmailCampaign`, `EmailCampaignRecipient` и `EmailAutomationRule` содержат `OrganizationId`;
- list/get/update/send operations фильтруются по current organization;
- `ClientId`, `CreatedByUserId` и automation lookups являются Guid references без внешних FK;
- inactive-client automation не использует `Client.Status` как обязательный фильтр и требует successful final deal.

Audit tenant scoping:

- `AuditLog.OrganizationId` обязателен;
- read-only API всегда фильтрует logs по current organization;
- `UserId` nullable для system/background операций;
- `EntityId` хранится как Guid reference only, без FK на business entity.

## Testing Layer

Testing Layer реализован как отдельный архитектурный слой в `tests/CrmSystem.ApiTests`. Он не входит в production runtime и не меняет production business logic.

Архитектурная роль слоя:

- проверять application boundary через HTTP API;
- запускать WebApi через `WebApplicationFactory<Program>`;
- выполнять реальные HTTP-запросы через `HttpClient`;
- использовать PostgreSQL Testcontainers вместо local dev/prod database;
- применять реальные EF migrations через `ApplicationDbContext.Database.MigrateAsync()`;
- сбрасывать данные между тестами через Respawn;
- сохранять seed/system tables, необходимые для Identity modules/system admin;
- заменять внешние side effects fake-сервисами.

Email side effects в Testing Layer:

- `IOrganizationSmtpEmailSender` заменяется на fake implementation;
- общий `IEmailSender` заменяется на fake implementation;
- `EmailAutomationHostedService` удаляется из test DI;
- `EmailAutomation:IsEnabled=false`;
- real SMTP не используется в API integration tests.

Time-dependent scenarios используют `TestClock`, который заменяет `IDateTimeProvider`.

Фактический coverage первого Testing Layer:

- Identity/Auth permissions;
- Clients;
- Catalog;
- Warehouse;
- Deals + Warehouse + Bonus;
- Returns Core inside Deals;
- Bonus;
- Chat REST и inter-organization contact request flow;
- Email campaigns и manual automation run;
- Audit log creation и sensitive data absence.

Full SignalR realtime suite пока остаётся future work. В первой итерации Chat проверяется через REST/API-focused scenarios.

## Deployment planned

Dockerization и Nginx reverse proxy запланированы отдельной итерацией.

Ожидаемые правила:

- deployment changes не смешивать с business module changes;
- Docker должен поднимать API и PostgreSQL для локальной/демо среды;
- Nginx должен проксировать HTTP API;
- Nginx должен корректно проксировать SignalR route `/hubs/chat`.

## Что запрещено делать

- Не создавать отдельные DbContext на модуль.
- Не создавать `IdentityDbContext`.
- Не создавать `ClientsDbContext`.
- Не создавать `BonusDbContext`.
- Не создавать `ChatDbContext`.
- Не создавать `EmailDbContext`.
- Не создавать `AuditDbContext`.
- Не переносить `ApplicationDbContext` из `Infrastructure/Persistence`.
- Не переносить migrations из `Infrastructure/Migrations`.
- Не создавать отдельные базы данных для модулей.
- Не использовать generic `IRepository<T>`.
- Не вызывать `SaveChangesAsync` внутри repositories.
- Не добавлять EF/DataAnnotations атрибуты в Domain.
- Не описывать ограничения БД в Domain.
- Не связывать будущие модули жёсткими EF-навигациями.
- Не переписывать готовый Identity без необходимости.
