# Workflow для новых Codex-сессий

## Как начинать новую сессию

1. Прочитать `docs/CODEX_CONTEXT.md`.
2. Прочитать `docs/ARCHITECTURE.md`.
3. Если задача связана с Identity, прочитать `docs/IDENTITY_MODULE.md`.
4. Если задача связана с Clients, прочитать `docs/CLIENTS_MODULE.md`.
5. Если задача связана с Catalog, прочитать `docs/CATALOG_MODULE.md`.
6. Если задача про дальнейшую разработку, прочитать `docs/DEVELOPMENT_PLAN.md`.
7. Проверить `git status --short`.
8. Быстро осмотреть затрагиваемые проекты и не делать предположений без чтения кода.

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

В документации обязательно фиксировать:

- endpoints;
- migration name и файл migration;
- permissions;
- tenant-scoping правила;
- важные бизнес-решения;
- что модуль намеренно не включает;
- результаты build/database verification, если они важны для дальнейшей работы.

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
- Warehouse, Deals и Bonus modules пока не реализованы.

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
- Реализовывать Promotions, Warehouse, Deals или Bonus внутри Catalog.

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
