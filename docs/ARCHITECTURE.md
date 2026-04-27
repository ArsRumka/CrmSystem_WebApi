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

Сейчас полноценно реализованы три модуля:

- `Identity` - foundation/runtime и permission system.
- `Clients` - первый бизнес-модуль CRM.
- `Catalog` - второй бизнес-модуль CRM: категории, товары и услуги.

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
- Warehouse, Deals и Bonus modules не реализованы;
- migration `AddCatalogModule` создана в `Infrastructure/Migrations`;
- permissions используют существующую Identity permission system через module code `Catalog`;
- нет FK на Identity `Organization`, tenant scope хранится как `OrganizationId`.

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

## Tenant scoping

Все бизнес-данные CRM должны быть tenant-scoped через `OrganizationId`.

Исключения:

- `SystemAdmin` - системный пользователь платформы, не принадлежит организации.
- `OrganizationRequest` - заявка до создания организации.
- `ActivationKey` - ключ регистрации организации.
- `Module` - системный справочник прав доступа.

Clients полностью tenant-scoped:

- `Client.OrganizationId` обязателен;
- list/search/get/update/delete всегда фильтруются по `OrganizationId`;
- `OrganizationId` берётся из `ICurrentUserService`;
- system admin JWT не является tenant context.

## Что запрещено делать

- Не создавать отдельные DbContext на модуль.
- Не создавать `IdentityDbContext`.
- Не создавать `ClientsDbContext`.
- Не переносить `ApplicationDbContext` из `Infrastructure/Persistence`.
- Не переносить migrations из `Infrastructure/Migrations`.
- Не создавать отдельные базы данных для модулей.
- Не использовать generic `IRepository<T>`.
- Не вызывать `SaveChangesAsync` внутри repositories.
- Не добавлять EF/DataAnnotations атрибуты в Domain.
- Не описывать ограничения БД в Domain.
- Не связывать будущие модули жёсткими EF-навигациями.
- Не переписывать готовый Identity без необходимости.
