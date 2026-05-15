# Audit Module

## Назначение

Audit отвечает за:

- хранение журнала ключевых бизнес-действий;
- просмотр audit logs через read-only API;
- запись audit-событий из других модулей через `IAuditLogService`;
- контроль административных и бизнес-операций;
- повышение прозрачности действий сотрудников организации.

Audit не предназначен для:

- автоматического логирования всех EF-изменений;
- хранения технических токенов;
- хранения паролей;
- хранения текста сообщений чата;
- хранения полного тела email-писем.

## Архитектура

Audit реализован как отдельный модуль:

- `Audit.Domain`;
- `Audit.Application`;
- `Audit.Infrastructure`;
- `Audit.Presentation`.

Архитектурные правила:

- используется общий `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `AuditDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations регистрируются через `IEfConfigurationAssemblyProvider` в `AddAuditInfrastructure()`;
- внешних FK/navigation на Identity, Organization, Clients, Catalog, Deals, Warehouse, Bonus, Chat и Email нет;
- связи с другими модулями хранятся только через Guid/string;
- другие модули зависят только от `Audit.Application`;
- `Audit.Application` не зависит от бизнес-модулей;
- `Audit.Infrastructure` реализует `IAuditLogService`;
- `AuditLogService` не вызывает `SaveChangesAsync`;
- audit logs сохраняются в той же транзакции, что и бизнес-действие, через общий `IUnitOfWork` вызывающего handler.

## Основная сущность

`AuditLog`:

- `Id`
- `OrganizationId`
- `UserId`
- `ModuleCode`
- `Action`
- `EntityName`
- `EntityId`
- `Description`
- `OldValuesJson`
- `NewValuesJson`
- `CreatedAt`
- `IpAddress`
- `UserAgent`
- `CorrelationId`

Правила:

- `OrganizationId` обязателен;
- `UserId` nullable для system/background операций;
- `ModuleCode` хранит код модуля;
- `EntityName` хранит имя сущности;
- `EntityId` хранится как Guid без FK;
- `OldValuesJson` и `NewValuesJson` содержат только небольшие sanitized snapshots;
- полные EF entities не сериализуются;
- чувствительные данные не должны попадать в audit log.

## AuditAction

`AuditAction` хранится как `int` и содержит:

- `Create`
- `Update`
- `Delete`
- `Deactivate`
- `Complete`
- `Cancel`
- `Send`
- `Approve`
- `Reject`
- `PermissionChange`
- `ManualAdjustment`
- `StageChange`
- `Return`
- `Run`

## REST API endpoints

Audit API read-only:

```http
GET /api/audit/logs
GET /api/audit/logs/{id}
```

Query filters:

- `moduleCode`
- `action`
- `entityName`
- `entityId`
- `userId`
- `dateFrom`
- `dateTo`
- `skip`
- `take`

Rules:

- logs are tenant-scoped by `OrganizationId`;
- results ordered by `CreatedAt` descending;
- `take` limited to prevent huge responses.

## Permissions

Module code:

```text
Audit
```

Mapping:

- `Audit / Read`:
  - `GET /api/audit/logs`
  - `GET /api/audit/logs/{id}`

No public create/update/delete endpoints.

Audit writes are internal service calls and are not exposed through public API.

## Integration approach

Audit uses manual audit calls in selected handlers:

- no EF interceptor;
- no automatic logging of all database changes;
- each integrated handler calls `IAuditLogService` before existing `IUnitOfWork.SaveChangesAsync` where safe;
- audit records are part of the same transaction as the business operation;
- no additional `SaveChangesAsync` is called by `AuditLogService`.

## Integrated modules and events

Clients:

- client created;
- client updated;
- client deactivated.

Catalog:

- category created/updated/deactivated;
- product created/updated/deactivated;
- service created/updated/deactivated.

Deals:

- deal created;
- deal updated;
- deal stage changed;
- deal deactivated;
- deal return created;
- deal return updated;
- deal return completed;
- deal return cancelled.

Warehouse:

- storage created;
- storage updated;
- storage set as default;
- storage deactivated;
- manual stock receipt;
- manual stock write-off;
- manual stock correction.

Automatic `Sale` movement from Deal completion is not audited separately. Automatic `Return` movement from `DealReturn` completion is not audited separately.

Bonus:

- bonus settings updated;
- bonus account manually adjusted.

Automatic `WriteOff`, `Accrual`, `Refund` and `CorrectionDecrease` operations from Deals/Returns are not audited separately. They are represented by `BonusTransaction` and Deals audit events.

Chat:

- conversation created;
- conversation deleted;
- participant added;
- participant removed;
- contact request created;
- contact request approved;
- contact request rejected;
- contact request cancelled.

Chat messages are not audited. Typing/read events are not audited. Message edit/delete events are not audited.

Email:

- email settings updated;
- template created;
- template updated;
- template deactivated;
- manual campaign created;
- campaign sent;
- automation rule updated;
- automation run.

Individual recipient sending is not audited separately. Recipient status is stored in `EmailCampaignRecipient`. Email body and rendered body are not logged.

Identity:

- safe user/role/permission events are logged where integrated;
- organization request approval/rejection and public registration may be skipped if unsafe due to activation keys/tokens/system tenant context.

## Sensitive data policy

Audit must not store:

- `Password`
- `PasswordHash`
- `SmtpPassword`
- `PasswordEncrypted`
- `RefreshToken`
- `AccessToken`
- `JWT`
- `Token`
- `KeyHash`
- `ActivationKey`
- `EmailConfirmationToken`
- `PasswordResetToken`
- `Authorization` header
- Chat message `Text`
- Email template `Body`
- Email campaign rendered `Body`

`AuditLogService` defensively removes keys containing:

- `password`
- `token`
- `key`
- `secret`
- `authorization`

Callers still must pass sanitized snapshots. If serialization fails, `AuditLogService` writes `null` JSON instead of failing because of serialization.

Audit integration is covered by API integration tests that verify audit log creation and sensitive data absence.

## Out of scope / Future scope

Не реализовано в Audit Core:

- EF interceptor audit;
- automatic diff of all EF changes;
- audit export;
- immutable append-only storage guarantees;
- advanced compliance reports;
- retention policies;
- anomaly detection;
- frontend UI for audit;
- integration with external SIEM/log systems.
