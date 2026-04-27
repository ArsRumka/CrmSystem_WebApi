# Identity flow

Краткое описание основного сценария работы Identity-модуля CRM.

## 1. Вход системного администратора

Системный администратор не принадлежит организации и используется для управления подключением компаний.

Endpoint:

```http
POST /api/identity/system-admin/login
```

Тело запроса:

```json
{
  "email": "admin@crm.local",
  "password": "Admin123!"
}
```

В ответе возвращается JWT access token системного администратора. Этот token подходит только для system-admin endpoints, например для просмотра и обработки заявок. Он не подходит для endpoints пользователей организации, таких как `/api/identity/roles` или `/api/identity/users`.

## 2. Создание заявки на подключение организации

Заявку может создать любой пользователь без авторизации.

Endpoint:

```http
POST /api/identity/organization-requests
```

Тело запроса:

```json
{
  "companyName": "Demo Company",
  "contactName": "Ivan Ivanov",
  "contactEmail": "owner@demo.local",
  "contactPhone": "+375291234567",
  "comment": "Need CRM access"
}
```

Система создаёт заявку со статусом `Pending` и возвращает `requestId`.

## 3. Одобрение заявки

Одобрять заявки может только системный администратор.

Endpoint:

```http
POST /api/identity/system-admin/organization-requests/{id}/approve
```

Требуется Bearer token системного администратора.

При одобрении система:

- переводит заявку в `Approved`;
- генерирует activation/license key;
- сохраняет в БД только hash ключа;
- отправляет ключ на email заявителя;
- возвращает plain activation key в ответе один раз.

## 4. Регистрация организации по activation key

После получения activation key владелец регистрирует организацию и первого администратора организации.

Endpoint:

```http
POST /api/identity/register-organization
```

Тело запроса:

```json
{
  "activationKey": "CRM-XXXX-XXXX-XXXX-XXXX",
  "organizationName": "Demo Company",
  "organizationEmail": "org@demo.local",
  "adminName": "Admin User",
  "adminEmail": "admin@demo.local",
  "adminPassword": "Admin123!"
}
```

Система:

- проверяет activation key;
- создаёт организацию;
- создаёт роль `Admin`;
- выдаёт роли `Admin` права на все системные модули;
- создаёт первого пользователя организации;
- создаёт token подтверждения email;
- отправляет email confirmation token;
- помечает activation key как использованный.

## 5. Подтверждение email

Пользователь организации должен подтвердить email перед входом.

Endpoint:

```http
POST /api/identity/confirm-email
```

Тело запроса:

```json
{
  "token": "email-confirmation-token"
}
```

После успешного подтверждения пользователь может выполнять login.

## 6. Вход пользователя организации

Пользователь организации входит по трём значениям: email организации, email пользователя и пароль.

Endpoint:

```http
POST /api/identity/login
```

Тело запроса:

```json
{
  "organizationEmail": "org@demo.local",
  "userEmail": "admin@demo.local",
  "password": "Admin123!"
}
```

В ответе возвращаются:

- `accessToken`;
- `refreshToken`;
- `expiresAt`.

Этот JWT используется для endpoints организации: пользователи, роли, права, модули.

## 7. Управление пользователями, ролями и правами

Все endpoints ниже требуют Bearer token пользователя организации.

Текущий пользователь:

```http
GET /api/identity/me
POST /api/identity/change-password
```

Пользователи:

```http
GET    /api/identity/users
POST   /api/identity/users
PUT    /api/identity/users/{id}/role
DELETE /api/identity/users/{id}
```

Роли:

```http
GET    /api/identity/roles
POST   /api/identity/roles
PUT    /api/identity/roles/{id}/permissions
DELETE /api/identity/roles/{id}
```

Модули:

```http
GET /api/identity/modules
```

Права проверяются через `IPermissionService` и permission policies:

- `Users/Read`, `Users/Create`, `Users/Update`, `Users/Delete`;
- `Roles/Read`, `Roles/Create`, `Roles/Update`, `Roles/Delete`.

Системный администратор не имеет прав внутри организации. Для управления пользователями и ролями нужен token пользователя организации.
