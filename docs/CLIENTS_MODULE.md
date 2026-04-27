# Clients module

## 1. Назначение модуля

`Clients` - первый бизнес-модуль CRM после Identity.

Он отвечает за хранение и управление клиентами организации:

- карточка клиента;
- контактные данные;
- статус клиента;
- источник клиента;
- согласие на marketing emails;
- заметки;
- поиск и фильтрация;
- soft delete.

Модуль является tenant-scoped: данные одной организации не должны попадать в ответы другой организации.

## 2. Проекты модуля

Модуль состоит из четырёх проектов:

- `Clients.Domain` - entity `Client` и enums.
- `Clients.Application` - CQRS use cases, validators, DTO, repository interface.
- `Clients.Infrastructure` - EF Core configuration, repository implementation, DI.
- `Clients.Presentation` - thin API controller.

WebApi `CrmSystem` подключает:

- `AddClientsApplication()`;
- `AddClientsInfrastructure()`;
- application part для `ClientsController`.

## 3. Сущность Client

`Client` находится в `Clients.Domain/Entities/Client.cs`.

Поля:

- `Id : Guid`
- `OrganizationId : Guid`
- `FirstName : string`
- `LastName : string`
- `MiddleName : string?`
- `Email : string?`
- `Phone : string?`
- `Status : ClientStatus`
- `Source : ClientSource`
- `AllowMarketingEmails : bool`
- `Notes : string?`
- `IsActive : bool`
- `CreatedAt : DateTime`
- `UpdatedAt : DateTime?`

Domain rules:

- `OrganizationId` обязателен.
- `FirstName` обязателен.
- `LastName` обязателен.
- `MiddleName` optional.
- `Email` optional.
- `Phone` optional.
- При создании и обновлении должен быть указан `Email` или `Phone`.
- `Notes` optional.
- Клиент создаётся активным: `IsActive = true`.
- `CreatedAt` задаётся при создании.
- `UpdatedAt` меняется при update/deactivate.

`FullName` не является полем entity и не хранится в БД. Он вычисляется в `ClientResponse`.

## 4. Enums ClientStatus и ClientSource

`ClientStatus`:

- `Lead = 1`
- `Active = 2`
- `Inactive = 3`
- `Blacklisted = 4`

`ClientSource`:

- `Unknown = 0`
- `Website = 1`
- `Phone = 2`
- `Email = 3`
- `Referral = 4`
- `SocialMedia = 5`
- `Other = 6`

Enums хранятся в БД как `int`.

## 5. Бизнес-правила

- `Email` не unique.
- `Phone` не unique.
- Дубликаты клиентов не блокируются на уровне БД.
- Дубликаты должны обрабатываться будущей логикой поиска/предупреждений, если она понадобится.
- `Email` или `Phone` обязательны при create/update.
- `FullName` вычисляется как `LastName + FirstName + MiddleName`, если `MiddleName` указан.
- `DELETE` не удаляет строку из БД, а деактивирует клиента.
- Domain не использует EF/DataAnnotations attributes.

## 6. Tenant scoping

Все Clients use cases получают `OrganizationId` из `ICurrentUserService`.

Правила:

- organization user JWT обязателен;
- system admin JWT не является tenant context;
- list/search/get/update/delete всегда используют `OrganizationId`;
- repository methods принимают `organizationId`;
- нет FK на Identity `Organization`;
- `OrganizationId` хранится как Guid для tenant scoping.

## 7. Permissions

Clients использует существующую Identity permission system.

Endpoints защищены:

- `[Authorize]`
- `[RequirePermission("Clients", PermissionAction.Read)]`
- `[RequirePermission("Clients", PermissionAction.Create)]`
- `[RequirePermission("Clients", PermissionAction.Update)]`
- `[RequirePermission("Clients", PermissionAction.Delete)]`

Permissions:

- `Clients / Read` - list и get by id.
- `Clients / Create` - create.
- `Clients / Update` - update.
- `Clients / Delete` - deactivate.

Module code `Clients` уже seed-ится Identity seed-ом как системный module code.

## 8. Endpoints

Base route:

```http
/api/clients
```

Endpoints:

```http
GET /api/clients
GET /api/clients/{id}
POST /api/clients
PUT /api/clients/{id}
DELETE /api/clients/{id}
```

Create body:

```json
{
  "firstName": "Ivan",
  "lastName": "Ivanov",
  "middleName": "Ivanovich",
  "email": "ivan@example.com",
  "phone": "+375291112233",
  "status": 2,
  "source": 4,
  "allowMarketingEmails": true,
  "notes": "VIP client"
}
```

Update body has the same editable fields as create, without `id`; route id is used.

Delete returns `204 No Content` when deactivation succeeds.

## 9. Search/filter behavior

`GET /api/clients` supports query params:

- `search`
- `status`
- `source`
- `isActive`

Search is tenant-scoped and checks:

- `FirstName`
- `LastName`
- `MiddleName`
- `Email`
- `Phone`
- `Notes`

Implementation uses PostgreSQL `ILIKE` through EF Core for case-insensitive contains matching.

Ordering:

- `LastName`
- `FirstName`

## 10. Soft delete behavior

`DELETE /api/clients/{id}` executes `DeactivateClientCommand`.

Behavior:

- loads client by `OrganizationId + Id`;
- returns not found if the client does not exist in current tenant;
- calls `client.Deactivate(now)`;
- sets `IsActive = false`;
- sets `UpdatedAt`;
- saves through `IUnitOfWork`;
- does not remove the row from database.

`Status` is not automatically changed during deactivate.

## 11. EF/database details

Configuration file:

```text
Clients.Infrastructure/Configurations/ClientConfiguration.cs
```

Table:

```text
Clients
```

Columns:

- `Id uuid not null`
- `OrganizationId uuid not null`
- `FirstName character varying(100) not null`
- `LastName character varying(100) not null`
- `MiddleName character varying(100) null`
- `Email character varying(256) null`
- `Phone character varying(30) null`
- `Status integer not null`
- `Source integer not null`
- `AllowMarketingEmails boolean not null`
- `Notes character varying(1000) null`
- `IsActive boolean not null`
- `CreatedAt timestamp with time zone not null`
- `UpdatedAt timestamp with time zone null`

Indexes:

- `OrganizationId`
- `OrganizationId + LastName`
- `OrganizationId + Email`
- `OrganizationId + Phone`
- `OrganizationId + Status`

Important:

- `Email` index is not unique.
- `Phone` index is not unique.
- No FK to Identity `Organization`.
- No `ClientsDbContext`.
- EF configuration is applied through shared `ApplicationDbContext`.

## 12. Migration

Migration:

```text
AddClientsModule
```

File:

```text
Infrastructure/Migrations/20260426132325_AddClientsModule.cs
```

Commands used for module implementation:

```bash
dotnet build CrmSystem.slnx
dotnet ef migrations add AddClientsModule --project Infrastructure --startup-project CrmSystem
dotnet ef database update --project Infrastructure --startup-project CrmSystem
```

Build succeeded with 0 warnings and 0 errors during implementation.

Known local environment note: `database update` requires PostgreSQL reachable at the configured connection string, currently `localhost:5434`.

## 13. How to test via Swagger

1. Start PostgreSQL and apply migrations.
2. Start WebApi.
3. Open Swagger.
4. Login as organization user through `POST /api/identity/login`.
5. Use `Authorize` with organization user JWT.
6. Ensure the user role has `Clients` permissions.
7. Create a client with `POST /api/clients`.
8. Verify the client appears in `GET /api/clients`.
9. Verify `GET /api/clients/{id}` returns the same client.
10. Update the client with `PUT /api/clients/{id}`.
11. Search/filter with `search`, `status`, `source`, `isActive`.
12. Deactivate with `DELETE /api/clients/{id}`.
13. Verify the client still exists when querying with `isActive=false`.

Useful validation checks:

- create with only `email` succeeds;
- create with only `phone` succeeds;
- create/update with neither `email` nor `phone` fails;
- invalid enum values fail validation;
- overlong strings fail validation.

## 14. What Clients module intentionally does not include

Clients module intentionally does not include:

- deals;
- bonus accounts;
- email confirmation for clients;
- client mailing campaigns;
- chat;
- audit;
- catalog;
- client bonus balances;
- LastPurchaseAt;
- frontend UI;
- RabbitMQ/Redis/GraphQL integrations.

Those concerns belong to future modules or future iterations.
