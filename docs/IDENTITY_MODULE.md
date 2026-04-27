# Identity-модуль

## Назначение

Identity отвечает за платформенную и tenant-аутентификацию CRM:

- системный администратор платформы;
- заявки на подключение организаций;
- approve/reject заявок;
- activation/license key;
- регистрация организации;
- пользователи организации;
- роли и permissions;
- JWT access token;
- refresh token;
- email confirmation;
- password reset.

Identity уже реализован как полноценный модуль. Его не нужно переписывать без необходимости.

## Основные проекты

- `Identity.Domain` — сущности и enums.
- `Identity.Application` — MediatR commands/queries/handlers, validators, DTO, interfaces.
- `Identity.Infrastructure` — EF configurations, repositories, security, permission service, seed.
- `Identity.Presentation` — thin controllers.

`ApplicationDbContext` не находится в Identity. Он один и лежит в `Infrastructure/Persistence`.

## Основные сущности

`Organization`:

- организация tenant;
- имеет `Name`, `Email`, `LicenseKeyHash`, `IsActive`;
- организация не логинится как пользователь.

`User`:

- пользователь организации;
- имеет `OrganizationId`, `RoleId`, `Name`, `Email`, `PasswordHash`, `IsActive`, `IsEmailConfirmed`;
- email уникален внутри организации.

`Role`:

- tenant-specific роль организации;
- имеет `OrganizationId`, `Name`;
- связана с permissions через `ModuleRole`.

`Module`:

- системный справочник модулей доступа;
- имеет `Code`, `Name`;
- не содержит `OrganizationId`.

`ModuleRole`:

- связь роли и системного module;
- хранит `CanRead`, `CanCreate`, `CanUpdate`, `CanDelete`.

`SystemAdmin`:

- системный администратор платформы;
- не имеет `OrganizationId`;
- используется для обработки заявок.

`OrganizationRequest`:

- заявка на подключение организации;
- статусы: `Pending`, `Approved`, `Rejected`.

`ActivationKey`:

- ключ активации организации;
- plain key не хранится, только hash.

`EmailConfirmationToken`, `PasswordResetToken`, `RefreshToken`:

- хранят только hash токенов;
- имеют expiry и состояние использования/revoke.

## Заявка на организацию

Public пользователь вызывает:

```http
POST /api/identity/organization-requests
```

Тело:

```json
{
  "companyName": "Company LLC",
  "contactName": "Ivan Ivanov",
  "contactEmail": "owner@example.com",
  "contactPhone": "+375291112233",
  "comment": "Хочу подключить CRM"
}
```

Система создаёт `OrganizationRequest` со статусом `Pending`.

## Approval системным админом

Системный администратор сначала логинится:

```http
POST /api/identity/system-admin/login
```

Тело:

```json
{
  "email": "admin@crm.local",
  "password": "Admin123!"
}
```

Полученный JWT подходит для endpoints с policy `RequireSystemAdmin`.

Просмотр заявок:

```http
GET /api/identity/system-admin/organization-requests
GET /api/identity/system-admin/organization-requests?status=Pending
```

Approve:

```http
POST /api/identity/system-admin/organization-requests/{id}/approve
```

Handler:

- проверяет, что current user является system admin;
- проверяет, что заявка существует и имеет статус `Pending`;
- переводит заявку в `Approved`;
- генерирует plain activation key;
- сохраняет только hash activation key;
- отправляет email заявителю;
- возвращает plain key в ответе один раз.

Reject:

```http
POST /api/identity/system-admin/organization-requests/{id}/reject
```

Тело опционально:

```json
{
  "reason": "Недостаточно данных"
}
```

## Activation/license key flow

Activation key создаётся только при approve заявки.

Правила:

- plain key не хранится в БД;
- в БД хранится `KeyHash`;
- plain key показывается системному администратору только один раз в response approve endpoint;
- тот же plain key отправляется заявителю email-сообщением;
- при регистрации organization пользователь передаёт plain key, система hash-ит его и ищет `ActivationKey` по hash.

## Registration organization flow

Public endpoint:

```http
POST /api/identity/register-organization
```

Тело:

```json
{
  "activationKey": "CRM-XXXX-XXXX-XXXX-XXXX",
  "organizationName": "Company LLC",
  "organizationEmail": "company@example.com",
  "adminName": "Admin User",
  "adminEmail": "admin@company.example",
  "adminPassword": "Admin123!"
}
```

Handler:

- hash-ит activation key;
- проверяет, что key существует, не used и не expired;
- проверяет уникальность organization email;
- создаёт `Organization`;
- создаёт роль `Admin`;
- берёт все seeded system modules;
- создаёт для Admin роли full permissions на все modules;
- создаёт admin `User`;
- создаёт email confirmation token;
- отправляет email confirmation;
- помечает activation key как used;
- сохраняет через `IUnitOfWork`.

Важно: handler ожидает, что системные `Module` уже созданы seed-ом на старте приложения.

## Email confirmation flow

После регистрации организации или создания пользователя система создаёт `EmailConfirmationToken` и отправляет email.

Подтверждение:

```http
POST /api/identity/confirm-email
```

Тело:

```json
{
  "token": "plain-token-from-email"
}
```

Handler:

- hash-ит token;
- проверяет, что token существует, не used и не expired;
- подтверждает email пользователя;
- помечает token как used.

Login organization user запрещён, если email не подтверждён.

## Login organization user flow

Public endpoint:

```http
POST /api/identity/login
```

Тело:

```json
{
  "organizationEmail": "company@example.com",
  "userEmail": "admin@company.example",
  "password": "Admin123!"
}
```

Handler:

- ищет organization по `OrganizationEmail`;
- проверяет `Organization.IsActive`;
- ищет user по `OrganizationId + UserEmail`;
- проверяет `User.IsActive`;
- проверяет `User.IsEmailConfirmed`;
- проверяет password через BCrypt;
- генерирует JWT access token;
- генерирует refresh token;
- сохраняет hash refresh token;
- возвращает `accessToken`, `refreshToken`, `expiresAt`.

JWT пользователя организации содержит claims:

- `UserId`
- `OrganizationId`
- `RoleId`
- `Email`
- `OrganizationEmail`

## Refresh token flow

Endpoint:

```http
POST /api/identity/refresh-token
```

Тело:

```json
{
  "refreshToken": "plain-refresh-token"
}
```

Handler:

- hash-ит refresh token;
- ищет stored token;
- проверяет expiry и revoke;
- находит user, role, organization;
- revoke-ит старый refresh token;
- создаёт новый refresh token;
- возвращает новую пару access + refresh.

## Password reset flow

Forgot password:

```http
POST /api/identity/forgot-password
```

Тело:

```json
{
  "organizationEmail": "company@example.com",
  "userEmail": "admin@company.example"
}
```

Endpoint не раскрывает наружу, существует ли organization/user.

Reset password:

```http
POST /api/identity/reset-password
```

Тело:

```json
{
  "token": "plain-reset-token-from-email",
  "newPassword": "NewAdmin123!"
}
```

## Change password

Authorized organization user:

```http
POST /api/identity/change-password
```

Тело:

```json
{
  "currentPassword": "Admin123!",
  "newPassword": "NewAdmin123!"
}
```

## Users management

Endpoints требуют organization user JWT.

```http
GET /api/identity/users
POST /api/identity/users
PUT /api/identity/users/{id}/role
DELETE /api/identity/users/{id}
```

Permissions:

- `Users/Read`
- `Users/Create`
- `Users/Update`
- `Users/Delete`

Удаление пользователя реализовано как deactivate. Самого себя деактивировать запрещено.

## Roles management

Endpoints требуют organization user JWT.

```http
GET /api/identity/roles
POST /api/identity/roles
PUT /api/identity/roles/{id}/permissions
DELETE /api/identity/roles/{id}
```

Permissions:

- `Roles/Read`
- `Roles/Create`
- `Roles/Update`
- `Roles/Delete`

Роль `Admin` защищена: её нельзя удалить, и permissions Admin роли безопаснее не менять.

## Permissions management

Permissions задаются списком:

```json
[
  {
    "moduleCode": "Clients",
    "canRead": true,
    "canCreate": true,
    "canUpdate": true,
    "canDelete": false
  }
]
```

Dynamic authorization policy имеет формат:

```text
Permission:{ModuleCode}:{Action}
```

Пример attribute:

```csharp
[RequirePermission("Users", PermissionAction.Create)]
```

## Modules

Endpoint:

```http
GET /api/identity/modules
```

Seeded modules:

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

`Clients` уже реализован как отдельный бизнес-модуль CRM. `Deals`, `Catalog`, `Bonus`, `Warehouse`, `Chat`, `Audit` пока являются только seeded module codes для будущих бизнес-функций.

## Как тестировать через Swagger

1. Запустить WebApi.
2. Открыть Swagger.
3. Вызвать `POST /api/identity/system-admin/login`.
4. Нажать `Authorize` и вставить JWT system admin.
5. Создать organization request через public endpoint.
6. Посмотреть заявки через system-admin endpoint.
7. Approve заявку и получить activation key.
8. Зарегистрировать organization по activation key.
9. Взять email confirmation token из console/log или SMTP-письма.
10. Подтвердить email admin user.
11. Вызвать `POST /api/identity/login` с `OrganizationEmail + UserEmail + Password`.
12. Заменить JWT в Swagger на organization user JWT.
13. Проверить `/api/identity/me`, `/roles`, `/users`, `/modules`.

Важно: system admin JWT не открывает organization endpoints с role permissions. Для `/api/identity/roles` нужен JWT пользователя организации.
