# Workflow для новых Codex-сессий

## Как начинать новую сессию

1. Прочитать `docs/CODEX_CONTEXT.md`.
2. Прочитать `docs/ARCHITECTURE.md`.
3. Если задача связана с Identity, прочитать `docs/IDENTITY_MODULE.md`.
4. Если задача связана с Clients, прочитать `docs/CLIENTS_MODULE.md`.
5. Если задача связана с Catalog, прочитать `docs/CATALOG_MODULE.md`.
6. Если задача связана с Deals, прочитать `docs/DEALS_MODULE.md`.
7. Если задача связана с Warehouse, прочитать `docs/WAREHOUSE_MODULE.md`.
8. Если задача связана с Bonus, прочитать `docs/BONUS_MODULE.md`.
9. Если задача связана с Chat или SignalR, прочитать `docs/CHAT_MODULE.md`.
10. Если задача связана с Email Campaigns, SMTP или рассылками, прочитать `docs/EMAIL_MODULE.md`.
11. Если задача связана с Audit или журналированием действий, прочитать `docs/AUDIT_MODULE.md`.
12. Если задача про дальнейшую разработку, прочитать `docs/DEVELOPMENT_PLAN.md`.
13. Проверить `git status --short`.
14. Быстро осмотреть затрагиваемые проекты и не делать предположений без чтения кода.

## Какие файлы читать в первую очередь

Общий контекст:

- `CrmSystem.slnx`
- `CrmSystem/Program.cs`
- `CrmSystem/appsettings.json`
- `Infrastructure/DependencyInjection.cs`
- `Infrastructure/Persistence/ApplicationDbContext.cs`
- `Infrastructure/Migrations`

Identity:

- `Identity.Domain/Entities`
- `Identity.Application/DependencyInjection.cs`
- `Identity.Application/Contracts/IdentityResponses.cs`
- `Identity.Infrastructure/DependencyInjection.cs`
- `Identity.Presentation/Controllers`

Clients:

- `Clients.Domain/Entities/Client.cs`
- `Clients.Domain/Enums`
- `Clients.Application/Clients`
- `Clients.Application/Contracts/ClientResponse.cs`
- `Clients.Infrastructure/Configurations/ClientConfiguration.cs`
- `Clients.Infrastructure/Repositories/ClientRepository.cs`
- `Clients.Presentation/Controllers/ClientsController.cs`
- `Infrastructure/Migrations/20260426132325_AddClientsModule.cs`

Catalog:

- `Catalog.Domain/Entities`
- `Catalog.Domain/Enums`
- `Catalog.Application/Categories`
- `Catalog.Application/Products`
- `Catalog.Application/Services`
- `Catalog.Application/Contracts`
- `Catalog.Infrastructure/Configurations`
- `Catalog.Infrastructure/Repositories`
- `Catalog.Presentation/Controllers`
- `Infrastructure/Migrations/20260426152751_AddCatalogModule.cs`

Для нового модуля:

- аналогичные проекты существующих `Identity` и `Clients`;
- текущие conventions repositories, handlers, validators, controllers.

Warehouse:

- `Modules/Warehouse/Warehouse.Domain/Entities`
- `Modules/Warehouse/Warehouse.Application/Storages`
- `Modules/Warehouse/Warehouse.Application/Stocks`
- `Modules/Warehouse/Warehouse.Application/Movements`
- `Modules/Warehouse/Warehouse.Infrastructure/Configurations`
- `Modules/Warehouse/Warehouse.Infrastructure/Repositories`
- `Modules/Warehouse/Warehouse.Infrastructure/Services`
- `Modules/Warehouse/Warehouse.Presentation/Controllers`
- `Infrastructure/Migrations/20260503130507_AddWarehouseCoreModule.cs`

Bonus:

- `Modules/Bonus/Bonus.Domain/Entities`
- `Modules/Bonus/Bonus.Domain/Enums`
- `Modules/Bonus/Bonus.Application/Settings`
- `Modules/Bonus/Bonus.Application/Accounts`
- `Modules/Bonus/Bonus.Application/Transactions`
- `Modules/Bonus/Bonus.Application/Abstractions`
- `Modules/Bonus/Bonus.Infrastructure/Configurations`
- `Modules/Bonus/Bonus.Infrastructure/Repositories`
- `Modules/Bonus/Bonus.Infrastructure/Services`
- `Modules/Bonus/Bonus.Presentation/Controllers`
- `Infrastructure/Migrations/20260506131632_AddBonusCoreModule.cs`

Returns inside Deals:

- `Modules/Deals/Deals.Domain/Entities/DealReturn.cs`
- `Modules/Deals/Deals.Domain/Entities/DealReturnItem.cs`
- `Modules/Deals/Deals.Domain/Enums/DealReturnStatus.cs`
- `Modules/Deals/Deals.Application/Returns`
- `Modules/Deals/Deals.Infrastructure/Configurations/DealReturnConfiguration.cs`
- `Modules/Deals/Deals.Infrastructure/Configurations/DealReturnItemConfiguration.cs`
- `Modules/Deals/Deals.Infrastructure/Repositories/DealReturnRepository.cs`
- `Modules/Deals/Deals.Presentation/Controllers/DealReturnsController.cs`
- `Modules/Warehouse/Warehouse.Infrastructure/Services/WarehouseDealReturnService.cs`
- `Modules/Bonus/Bonus.Infrastructure/Services/BonusDealReturnService.cs`
- `Infrastructure/Migrations/20260507121737_AddDealsReturnsCore.cs`

Chat:

- `Modules/Chat/Chat.Domain/Entities`
- `Modules/Chat/Chat.Domain/Enums`
- `Modules/Chat/Chat.Application/Conversations`
- `Modules/Chat/Chat.Application/Messages`
- `Modules/Chat/Chat.Application/Participants`
- `Modules/Chat/Chat.Application/ContactRequests`
- `Modules/Chat/Chat.Application/Realtime`
- `Modules/Chat/Chat.Application/Common`
- `Modules/Chat/Chat.Infrastructure/Configurations`
- `Modules/Chat/Chat.Infrastructure/Repositories`
- `Modules/Chat/Chat.Infrastructure/Lookups`
- `Modules/Chat/Chat.Presentation/Controllers`
- `Modules/Chat/Chat.Presentation/Hubs/ChatHub.cs`
- `Infrastructure/Migrations/20260507173218_AddChatCoreModule.cs`

Email:

- `Modules/Email/Email.Domain/Entities`
- `Modules/Email/Email.Domain/Enums`
- `Modules/Email/Email.Application/Settings`
- `Modules/Email/Email.Application/Templates`
- `Modules/Email/Email.Application/Campaigns`
- `Modules/Email/Email.Application/Automation`
- `Modules/Email/Email.Application/Abstractions`
- `Modules/Email/Email.Infrastructure/Configurations`
- `Modules/Email/Email.Infrastructure/Repositories`
- `Modules/Email/Email.Infrastructure/Services`
- `Modules/Email/Email.Presentation/Controllers`
- `Infrastructure/Migrations/20260514140401_AddEmailCampaignsCoreModule.cs`

Audit:

- `Modules/Audit/Audit.Domain/Entities`
- `Modules/Audit/Audit.Domain/Enums`
- `Modules/Audit/Audit.Application/Logs`
- `Modules/Audit/Audit.Application/Abstractions`
- `Modules/Audit/Audit.Infrastructure/Configurations`
- `Modules/Audit/Audit.Infrastructure/Repositories`
- `Modules/Audit/Audit.Infrastructure/Services`
- `Modules/Audit/Audit.Presentation/Controllers`
- `Infrastructure/Migrations/20260514193150_AddAuditCoreModule.cs`

## Как составлять план

Перед изменениями:

- определить, какой модуль затрагивается;
- проверить, нужна ли новая модель БД;
- проверить, нужна ли миграция;
- перечислить файлы, которые будут изменены;
- подтвердить, что изменения не нарушают архитектурные правила.

Для крупных задач работать итерациями:

1. Domain + EF configuration + repositories + migration.
2. Application layer: commands/queries/handlers/validators.
3. Infrastructure services + Presentation + WebApi wiring.
4. Проверка сценариев и документация.

Для cross-cutting module работать особенно осторожно:

1. Сначала создать module foundation: domain, abstractions, infrastructure, presentation и DI wiring.
2. Затем добавлять интеграции по приоритету и небольшими группами handlers.
3. Не переписывать бизнес-логику ради интеграции.
4. Не добавлять `SaveChangesAsync` в cross-cutting service.
5. Фиксировать список реально интегрированных handlers в отчёте.

## Как реализовывать новый модуль

Новый модуль должен повторять стиль Identity и Clients:

```text
Modules or root-level module projects
  {Module}.Domain
  {Module}.Application
  {Module}.Infrastructure
  {Module}.Presentation
```

Если проекты модуля ещё не созданы, сначала согласовать структуру с текущим solution style.

Правила:

- Domain entities без EF/DataAnnotations атрибутов.
- Инварианты в конструкторах и методах entity.
- EF constraints только через Fluent API.
- EF configurations нового модуля подключать через `IEfConfigurationAssemblyProvider` в `{Module}.Infrastructure`.
- Не добавлять reference из `Infrastructure` на `{Module}.Infrastructure`.
- Не использовать строковый `Assembly.Load` для module EF configurations.
- Repository interfaces в `{Module}.Application`.
- Repository implementations в `{Module}.Infrastructure`.
- В `{Module}.Application` добавить `Add{Module}Application()`.
- В `{Module}.Infrastructure` добавить `Add{Module}Infrastructure()`.
- WebApi должен вызвать оба DI extension methods в `Program.cs`.
- Если есть отдельный `{Module}.Presentation`, WebApi должен подключить controllers через `AddApplicationPart`.
- Controllers тонкие, логика в handlers.
- Все изменения сохранять через `IUnitOfWork`.
- Все tenant business entities должны иметь `OrganizationId`.
- Permissions проверять через существующий permission mechanism.

## Что обновлять после реализации модуля

После реализации каждого модуля нужно обновлять документацию:

- `docs/CODEX_CONTEXT.md` - текущий статус, список реализованных модулей, endpoints.
- `docs/DEVELOPMENT_PLAN.md` - completed/next modules и Known issues / risks.
- `docs/ARCHITECTURE.md` - архитектурные детали нового модуля, EF/migration/DI integration.
- Документ модуля `docs/{MODULE}_MODULE.md`, если его ещё нет.
- `docs/DATABASE_OVERVIEW.md`, если файл существует и менялась модель БД.

Если новый модуль интегрируется с уже существующим модулем, нужно обновить не только профильный документ нового модуля, но и документы модулей, с которыми появилась связь.

Если feature меняет бизнес-жизненный цикл нескольких модулей, нужно обновить документацию всех затронутых модулей, даже если feature не является отдельным модулем.

Пример: Warehouse Core обновляет:

- `docs/WAREHOUSE_MODULE.md`;
- `docs/DEALS_MODULE.md` или `docs/DEALS_MODULE_DRAFT.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`.

Пример: Bonus Core обновляет:

- `docs/BONUS_MODULE.md`;
- `docs/DEALS_MODULE.md` или `docs/DEALS_MODULE_DRAFT.md`;
- `docs/CATALOG_MODULE.md`;
- `docs/WAREHOUSE_MODULE.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`;
- `docs/DATABASE_OVERVIEW.md`, если файл существует.

Пример: Returns Core inside Deals обновляет:

- `docs/DEALS_MODULE.md`;
- `docs/WAREHOUSE_MODULE.md`;
- `docs/BONUS_MODULE.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`;
- `docs/ARCHITECTURE.md`;
- `docs/DATABASE_OVERVIEW.md`, если файл существует.

Пример: Chat Core with SignalR обновляет:

- `docs/CHAT_MODULE.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`;
- `docs/ARCHITECTURE.md`;
- `docs/CODEX_WORKFLOW.md`;
- `docs/DATABASE_OVERVIEW.md`, если файл существует.

Пример: Email Campaigns Core обновляет:

- `docs/EMAIL_MODULE.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`;
- `docs/ARCHITECTURE.md`;
- `docs/CODEX_WORKFLOW.md`;
- `docs/CHAT_MODULE.md`, если там есть roadmap после Chat или email notifications notes;
- `docs/DATABASE_OVERVIEW.md`, если файл существует.

Пример: Audit Core обновляет:

- `docs/AUDIT_MODULE.md`;
- `docs/DEVELOPMENT_PLAN.md`;
- `docs/CODEX_CONTEXT.md`;
- `docs/ARCHITECTURE.md`;
- `docs/CODEX_WORKFLOW.md`;
- профильные документы модулей, handlers которых получили audit calls;
- `docs/DATABASE_OVERVIEW.md`, если файл существует.

В документации обязательно фиксировать:

- endpoints;
- migration name и файл migration;
- permissions;
- tenant-scoping правила;
- важные бизнес-решения;
- что модуль намеренно не включает;
- результаты build/database verification, если они важны для дальнейшей работы.

Если модуль использует SignalR, дополнительно фиксировать:

- hub route;
- hub methods;
- realtime events;
- groups, если они являются частью контракта;
- JWT query token behavior;
- permission checks in hub/application handlers;
- REST fallback endpoints.

Если модуль использует `HostedService` или внешние side effects, дополнительно фиксировать:

- appsettings options;
- external side effects;
- failure behavior;
- security handling for credentials;
- test strategy.

После реализации Audit дополнительно проверить и зафиксировать:

- sensitive data не попадает в audit logs;
- chat message text не логируется;
- email body и rendered body не логируются;
- passwords, tokens, SMTP password и authorization headers не логируются;
- `AuditLogService` не вызывает `SaveChangesAsync`;
- audit integrations сохраняются через существующий UnitOfWork вызывающего handler.

Testing Layer tasks:

- Testing Layer changes должны идти отдельной итерацией.
- Не смешивать tests с business module changes.
- Не менять production business logic ради прохождения тестов без отдельного решения.
- Для API integration tests использовать PostgreSQL/Testcontainers, а не EF InMemory/SQLite.
- API integration tests должны ходить в реальные HTTP endpoints через `HttpClient`.
- В тестах запрещена реальная SMTP-отправка.
- Hosted services с external side effects должны отключаться или заменяться fake implementations.
- Если Docker недоступен, фиксировать это в отчёте.
- После реализации tests обязательно запускать `dotnet build CrmSystem.slnx`.
- После реализации tests обязательно запускать `dotnet test tests/CrmSystem.ApiTests/CrmSystem.ApiTests.csproj`.

Deployment tasks:

- Docker/Nginx изменения должны идти отдельной итерацией;
- не смешивать deployment changes с business module changes.

## Clients как реализованный пример

`Clients` уже реализован как business module.

Полезные ориентиры:

- `Client` хранит `OrganizationId`, контакты, статус, источник, marketing flag, notes, activity timestamps.
- `FullName` не хранится в БД, а вычисляется в `ClientResponse`.
- `Email` и `Phone` не unique.
- `Email` или `Phone` обязательны при create/update.
- `DELETE /api/clients/{id}` делает soft delete через `IsActive = false`.
- Permissions используют module code `Clients`.
- Migration: `AddClientsModule`, файл `20260426132325_AddClientsModule.cs`.

## Catalog как реализованный пример

`Catalog` уже реализован как второй business module.

Полезные ориентиры:

- `Category` общая для товаров и услуг.
- `Product` и `Service` являются отдельными сущностями.
- `Product.Sku` nullable и не unique.
- `ServiceCode` отсутствует.
- Цены считаются в BYN.
- `Currency` column отсутствует.
- VAT/tax fields отсутствуют.
- `BonusType`/`BonusValue` и `DiscountType`/`DiscountValue` есть в `Category`, `Product`, `Service`.
- Permissions используют module code `Catalog`.
- EF configurations подключены через `IEfConfigurationAssemblyProvider`.
- Migration: `AddCatalogModule`, файл `20260426152751_AddCatalogModule.cs`.
- Полноценный Promotions module не реализован.
- Deals MVP и Warehouse Core реализованы отдельными модулями.
- Bonus Core реализован и использует Catalog bonus rule fields.

Endpoints:

```http
GET /api/catalog/categories
GET /api/catalog/categories/{id}
POST /api/catalog/categories
PUT /api/catalog/categories/{id}
DELETE /api/catalog/categories/{id}
GET /api/catalog/products
GET /api/catalog/products/{id}
POST /api/catalog/products
PUT /api/catalog/products/{id}
DELETE /api/catalog/products/{id}
GET /api/catalog/services
GET /api/catalog/services/{id}
POST /api/catalog/services
PUT /api/catalog/services/{id}
DELETE /api/catalog/services/{id}
```

## Команды после изменений

Build:

```bash
dotnet build CrmSystem.slnx
```

Список migrations:

```bash
dotnet ef migrations list --project Infrastructure --startup-project CrmSystem
```

Добавление migration только при изменении EF model:

```bash
dotnet ef migrations add MigrationName --project Infrastructure --startup-project CrmSystem
```

Применение migrations:

```bash
dotnet ef database update --project Infrastructure --startup-project CrmSystem
```

## Как проверять build

1. Убедиться, что WebApi не запущен и не блокирует DLL.
2. Выполнить `dotnet build CrmSystem.slnx`.
3. Если build упал, сначала прочитать ошибку.
4. Не чинить unrelated errors без разрешения пользователя.
5. В отчёте указать warnings/errors.

## Как проверять migrations

Если модель БД не менялась:

- migrations не создавать;
- можно выполнить только `dotnet ef migrations list`.

Если модель БД менялась:

- migration создавать только в `Infrastructure`;
- не редактировать вручную старые migrations;
- `ApplicationDbContextModelSnapshot` может обновляться EF автоматически;
- после migration выполнить build.

## Типичные ошибки, которые запрещены

- Создать отдельный DbContext на модуль.
- Создать `IdentityDbContext`.
- Создать `ClientsDbContext`.
- Перенести `ApplicationDbContext` из `Infrastructure/Persistence`.
- Положить migrations в `{Module}.Infrastructure`.
- Создать отдельную database для модуля.
- Добавить generic `IRepository<T>`.
- Вызвать `SaveChangesAsync` внутри repository.
- Добавить EF/DataAnnotations атрибуты в Domain.
- Завязать один бизнес-модуль на EF navigation другого модуля.
- Реализовать CRM-модули внутри Identity.
- Использовать system admin token как organization user token.
- Переписать Identity без явной необходимости.
- Добавить Catalog-specific `CatalogDbContext`.
- Делать `Product.Sku` unique.
- Добавлять `ServiceCode`.
- Добавлять Currency/VAT/tax fields в Catalog.
- Реализовывать Promotions, Warehouse, Deals или Bonus accounts/transactions/settings внутри Catalog.

## Особенности Identity при тестировании

System admin:

- логинится через `POST /api/identity/system-admin/login`;
- может смотреть, approve и reject organization requests;
- не является пользователем организации.

Organization user:

- появляется после registration organization;
- должен подтвердить email;
- логинится через `OrganizationEmail + UserEmail + Password`;
- использует permissions role/module для organization endpoints.

Если endpoint `/api/identity/roles` возвращает 403 с system admin JWT, это ожидаемо. Нужно войти как пользователь организации.
