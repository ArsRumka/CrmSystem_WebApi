# Testing Layer

## Назначение

Testing Layer предназначен для:

- автоматической проверки основных API-сценариев CRM-системы;
- проверки реальных HTTP endpoints;
- проверки работы auth/permissions;
- проверки межмодульных бизнес-сценариев;
- проверки EF migrations на реальной PostgreSQL БД;
- предотвращения регрессий в ключевых модулях.

## Архитектура тестов

API integration tests находятся в `tests/CrmSystem.ApiTests`.

Тестовый проект использует:

- xUnit;
- `Microsoft.AspNetCore.Mvc.Testing`;
- `WebApplicationFactory<Program>`;
- реальные HTTP-запросы через `HttpClient`;
- PostgreSQL из Testcontainers;
- реальные EF migrations через `ApplicationDbContext.Database.MigrateAsync()`;
- Respawn для сброса БД между тестами;
- fake email services вместо реальной SMTP-отправки;
- `TestClock` для time-dependent scenarios.

Для поддержки `WebApplicationFactory<Program>` в `CrmSystem/Program.cs` добавлен технический `public partial class Program`. Это не меняет production business logic.

Тесты не используют EF InMemory, не используют SQLite и не зависят от локальной development database `localhost:5434`.

## Test infrastructure

`CrmWebApplicationFactory`:

- настраивает environment `Testing`;
- подставляет connection string PostgreSQL Testcontainer в `ConnectionStrings:DefaultConnection`;
- применяет EF migrations перед `IdentitySeedHostedService`;
- заменяет внешние email side effects fake-сервисами;
- удаляет `EmailAutomationHostedService` из test DI;
- выставляет `EmailAutomation:IsEnabled=false`;
- задаёт `BaseAddress = https://localhost` для тестовых `HttpClient`.

`PostgresTestContainerFixture`:

- поднимает `postgres:16-alpine`;
- использует database `crm_tests`;
- использует один container на xUnit test collection;
- останавливает и dispose-ит container после тестов.

`Respawn`:

- используется для сброса БД между тестами;
- работает с PostgreSQL adapter;
- сохраняет необходимые seed/system tables: `__EFMigrationsHistory`, `Modules`, `SystemAdmins`;
- позволяет тестам быть изолированными без пересоздания container на каждый test class.

`AuthTestClient`:

- создаёт тестовые организации, роли и пользователей через `ApplicationDbContext`;
- хеширует пароль через настоящий `IPasswordHasher`;
- подтверждает email тестового пользователя;
- seed-ит admin permissions на системные modules;
- выполняет login через реальный endpoint `POST /api/identity/login`;
- добавляет `Authorization: Bearer {token}` в `HttpClient`;
- умеет создавать limited user для 403-сценариев.

`FakeOrganizationSmtpEmailSender`:

- заменяет реальный organization SMTP sender;
- не открывает SMTP-соединение;
- сохраняет отправленные письма в memory collection;
- позволяет проверять email campaigns без реальной отправки;
- может симулировать SMTP failure для email, содержащих `fail`.

`FakeEmailSender`:

- заменяет общий `IEmailSender`;
- предотвращает реальные письма из Identity flows;
- сохраняет сообщения в memory collection.

`TestClock`:

- заменяет `IDateTimeProvider`;
- позволяет стабилизировать даты;
- поддерживает `SetUtcNow(...)` и `Reset()`;
- используется для email automation scenarios.

## Covered scenarios

Реализовано 16 API integration tests.

Identity/Auth:

- `401` для unauthenticated request;
- успешный доступ admin user;
- `403` для user без permission.

Clients:

- создание клиента;
- validation для клиента без email/phone;
- обновление клиента;
- деактивация клиента;
- проверка `AllowMarketingEmails`.

Catalog:

- создание категории;
- создание товара;
- создание услуги;
- проверка list endpoints.

Warehouse:

- создание склада;
- поступление товара;
- списание товара;
- conflict при избыточном списании.

Deals + Warehouse + Bonus:

- создание клиента/товара/склада;
- поступление товара на склад;
- настройка бонусов;
- ручная корректировка бонусного счёта;
- создание сделки с product item и `BonusPointsUsed`;
- перевод сделки в successful final stage;
- проверка stock deduction;
- проверка `BonusTransaction` WriteOff/Accrual;
- проверка `FinalAmount` с bonus discount;
- проверка audit log для ключевого события.

Returns:

- создание draft return;
- проверка отсутствия warehouse/bonus effects в Draft;
- completion возврата;
- проверка stock increase;
- проверка Bonus refund/correction;
- over-return conflict.

Chat:

- создание group conversation;
- отправка сообщения через REST fallback;
- получение сообщений;
- inter-org contact request;
- approve request;
- проверка inter-org conversation;
- проверка, что non-participant не видит conversation.

Email:

- обновление SMTP settings;
- проверка, что password/`PasswordEncrypted` не возвращаются;
- создание template;
- manual campaign;
- `Sent` recipient;
- `SkippedNoEmail`;
- `SkippedMarketingDisabled`;
- fake SMTP captures exactly one outgoing email;
- automation rule;
- manual automation run.

Audit:

- audited action creates audit log;
- audit logs доступны через API;
- sensitive values вроде `PasswordEncrypted`, `SmtpPassword`, `password`, `token` не попадают в JSON.

## What is intentionally not covered yet

- full SignalR realtime suite не покрыт в первой итерации;
- реальные SMTP подключения не используются;
- frontend tests не реализованы;
- performance/load tests не реализованы;
- exhaustive validation matrix не реализована;
- unit tests for every handler не реализованы.

## How to run tests

```bash
dotnet build CrmSystem.slnx
dotnet test tests/CrmSystem.ApiTests/CrmSystem.ApiTests.csproj --logger "console;verbosity=minimal"
```

Docker должен быть запущен, потому что Testcontainers требует Docker daemon.

Тесты не используют локальную PostgreSQL БД проекта. Тесты сами поднимают PostgreSQL container.

## Latest verification result

Последний зафиксированный результат:

```bash
dotnet test tests/CrmSystem.ApiTests/CrmSystem.ApiTests.csproj --logger "console;verbosity=minimal"
```

Result: Passed 16/16, Failed 0, Skipped 0, duration about 21s.

```bash
dotnet build CrmSystem.slnx --no-restore
```

Result: Succeeded, 0 warnings, 0 errors.

## Future improvements

- добавить полноценные SignalR realtime tests;
- добавить больше permission matrix tests;
- добавить больше validation tests;
- добавить frontend e2e tests после React UI;
- добавить CI pipeline;
- добавить coverage report;
- добавить Docker/Nginx deployment smoke tests.
