# Email Module

## Назначение

Email отвечает за:

- SMTP-настройки почты организации;
- реальную отправку писем через SMTP организации;
- проверку SMTP-настроек;
- шаблоны писем;
- ручные рассылки клиентам;
- автоматические рассылки клиентам, с которыми давно не было сделок;
- историю получателей и статусов отправки;
- соблюдение `AllowMarketingEmails`.

## Архитектура

Email реализован как отдельный модуль:

- `Email.Domain`;
- `Email.Application`;
- `Email.Infrastructure`;
- `Email.Presentation`.

Архитектурные правила:

- используется общий `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `EmailDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations регистрируются через `IEfConfigurationAssemblyProvider` в `AddEmailInfrastructure()`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- связи с другими модулями выполняются только через Guid;
- SMTP-отправка campaign выполняется через настройки конкретной организации;
- глобальный `IEmailSender` не используется для campaign sending.

Внутренние связи Email module допустимы и используются внутри собственных таблиц:

- `EmailCampaigns.TemplateId` -> `EmailTemplates.Id`;
- `EmailCampaignRecipients.CampaignId` -> `EmailCampaigns.Id`;
- `EmailAutomationRules.TemplateId` -> `EmailTemplates.Id`.

## Permission module

Module code:

```text
Email
```

`Email` добавлен в Identity seed как системный module code. Для существующих tenant `Admin` ролей seed выполняет idempotent backfill полного доступа к Email, не создавая дублирующие `ModuleRole` строки. Обычным ролям права автоматически не выдаются.

Будущие организации получают Email permissions для `Admin` роли через существующий registration flow, потому что registration создаёт full permissions на все seeded modules.

## Основные сущности

### EmailSettings

Поля:

- `Id`
- `OrganizationId`
- `SenderName`
- `SenderEmail`
- `SmtpHost`
- `SmtpPort`
- `UseSsl`
- `Username`
- `PasswordEncrypted`
- `IsEnabled`
- `CreatedAt`
- `UpdatedAt`

Правила:

- одна настройка на организацию;
- пароль не хранится plain text;
- пароль шифруется ASP.NET Core Data Protection;
- API response не возвращает `PasswordEncrypted` или plain password;
- `IsEnabled` блокирует campaign sending, но test endpoint может проверять настройки.

### EmailTemplate

Поля:

- `Id`
- `OrganizationId`
- `Name`
- `Subject`
- `Body`
- `IsHtml`
- `IsActive`
- `CreatedAt`
- `CreatedByUserId`
- `UpdatedAt`
- `UpdatedByUserId`

Поддерживаемые placeholders:

- `{{FirstName}}`
- `{{LastName}}`
- `{{MiddleName}}`
- `{{FullName}}`
- `{{OrganizationName}}`
- `{{LastDealDate}}`
- `{{DaysSinceLastDeal}}`

### EmailCampaign

Поля:

- `Id`
- `OrganizationId`
- `TemplateId`
- `Name`
- `Type`
- `Status`
- `CreatedAt`
- `CreatedByUserId`
- `StartedAt`
- `CompletedAt`
- `TotalRecipients`
- `SentCount`
- `FailedCount`
- `SkippedCount`

### EmailCampaignRecipient

Поля:

- `Id`
- `OrganizationId`
- `CampaignId`
- `ClientId`
- `Email`
- `FullNameSnapshot`
- `LastDealDate`
- `DaysSinceLastDeal`
- `Status`
- `ErrorMessage`
- `SentAt`
- `CreatedAt`

### EmailAutomationRule

Поля:

- `Id`
- `OrganizationId`
- `TemplateId`
- `IsEnabled`
- `InactivityDays`
- `RepeatAfterDays`
- `LastRunAt`
- `CreatedAt`
- `UpdatedAt`
- `UpdatedByUserId`

## Enums

`EmailCampaignType`:

- `Manual`
- `AutomaticInactiveClients`

`EmailCampaignStatus`:

- `Draft`
- `Sending`
- `Sent`
- `PartiallyFailed`
- `Failed`
- `Cancelled`

`EmailRecipientStatus`:

- `Pending`
- `Sent`
- `Failed`
- `SkippedNoEmail`
- `SkippedRecentlySent`
- `SkippedMarketingDisabled`

## REST API endpoints

Settings:

- `GET /api/email/settings`
- `PUT /api/email/settings`
- `POST /api/email/settings/test`

Templates:

- `GET /api/email/templates`
- `GET /api/email/templates/{id}`
- `POST /api/email/templates`
- `PUT /api/email/templates/{id}`
- `DELETE /api/email/templates/{id}`

Campaigns:

- `GET /api/email/campaigns`
- `GET /api/email/campaigns/{id}`
- `GET /api/email/campaigns/{id}/recipients`
- `POST /api/email/campaigns/manual`
- `POST /api/email/campaigns/{id}/send`

Automation:

- `GET /api/email/automation`
- `PUT /api/email/automation`
- `POST /api/email/automation/run`

## Permissions

Module code:

```text
Email
```

Mapping:

- `Email / Read`:
  - `GET /api/email/settings`
  - `GET /api/email/templates`
  - `GET /api/email/templates/{id}`
  - `GET /api/email/campaigns`
  - `GET /api/email/campaigns/{id}`
  - `GET /api/email/campaigns/{id}/recipients`
  - `GET /api/email/automation`
- `Email / Create`:
  - `POST /api/email/templates`
  - `POST /api/email/campaigns/manual`
  - `POST /api/email/campaigns/{id}/send`
  - `POST /api/email/automation/run`
  - `POST /api/email/settings/test`
- `Email / Update`:
  - `PUT /api/email/settings`
  - `PUT /api/email/templates/{id}`
  - `PUT /api/email/automation`
- `Email / Delete`:
  - `DELETE /api/email/templates/{id}` soft-deactivates only.

## Manual campaign flow

1. Пользователь выбирает template.
2. Пользователь выбирает клиентов.
3. Система создаёт campaign `Type=Manual`, `Status=Draft`.
4. Для каждого клиента создаётся recipient:
   - `Pending`, если есть email и `AllowMarketingEmails=true`;
   - `SkippedNoEmail`, если email отсутствует;
   - `SkippedMarketingDisabled`, если `AllowMarketingEmails=false`.
5. При отправке campaign переходит в `Sending`.
6. SMTP-отправка выполняется через `EmailSettings` организации.
7. По каждому recipient сохраняется `Sent`, `Failed` или skipped status.
8. Campaign получает итоговый статус `Sent`, `PartiallyFailed` или `Failed`.

Если отдельный recipient падает на SMTP-отправке, ошибка сохраняется в `ErrorMessage`, а отправка остальных recipients продолжается.

## Automatic inactive clients flow

Automation rule хранит:

- `InactivityDays`;
- `RepeatAfterDays`.

Rule по умолчанию создаётся disabled:

- `IsEnabled=false`;
- `InactivityDays=60`;
- `RepeatAfterDays=30`.

Автоматическая рассылка выбирает клиентов:

- `Client.IsActive=true`;
- `AllowMarketingEmails=true` для отправки;
- у клиента есть хотя бы одна successful final deal;
- latest successful final deal старше `InactivityDays`;
- клиент не получал successful `AutomaticInactiveClients` email в последние `RepeatAfterDays`.

Дополнительные правила:

- `Client.Status` не используется как обязательный фильтр;
- если `AllowMarketingEmails=false`, создаётся `SkippedMarketingDisabled`;
- если email отсутствует, создаётся `SkippedNoEmail`;
- если кандидатов нет, campaign не создаётся;
- каждый запуск automation создаёт campaign `Type=AutomaticInactiveClients`, если есть кандидаты.

## Hosted service

`EmailAutomationHostedService` запускает автоматизацию по расписанию.

Настройки берутся из `appsettings.json`:

```json
{
  "EmailAutomation": {
    "IsEnabled": true,
    "IntervalHours": 24
  }
}
```

Правила:

- `IsEnabled` может отключить hosted automation;
- `IntervalHours` задаёт периодичность;
- ошибки логируются и не роняют приложение;
- `POST /api/email/automation/run` нужен для Swagger/demo проверки automation вручную.

## SMTP and password security

SMTP password принимается plain только в request при создании или обновлении настроек.

В БД хранится только:

```text
PasswordEncrypted
```

Используется ASP.NET Core Data Protection с purpose:

```text
EmailSettings.SmtpPassword
```

Security rules:

- API не возвращает пароль;
- API не возвращает `PasswordEncrypted`;
- при update пустой `SmtpPassword` сохраняет существующий encrypted password;
- если Data Protection keys потеряны, сохранённые SMTP-пароли нельзя расшифровать;
- campaigns используют только organization SMTP settings;
- console fallback не используется как замена SMTP.

## Out of scope / Future scope

Не реализовано в Email Core:

- unsubscribe links;
- bounce tracking;
- open tracking;
- click tracking;
- attachments;
- drag-and-drop email builder;
- A/B testing;
- arbitrary scheduled campaigns;
- external provider APIs;
- frontend UI;
- audit integration.
