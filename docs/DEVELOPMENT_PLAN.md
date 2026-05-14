# План дальнейшей разработки

## Текущий статус

Проект находится на стадии готового Identity foundation/runtime, реализованных бизнес-модулей Clients и Catalog, а также MVP/Core модулей Deals MVP, Warehouse Core, Bonus Core, Returns Core inside Deals, Chat Core with SignalR и Email Campaigns Core.

Сделано:

- solution и проектная структура;
- общий `ApplicationDbContext`;
- EF migrations в `Infrastructure/Migrations`;
- общий `IUnitOfWork`;
- Identity domain/application/infrastructure/presentation;
- Clients domain/application/infrastructure/presentation;
- Catalog domain/application/infrastructure/presentation;
- Deals domain/application/infrastructure/presentation;
- Warehouse domain/application/infrastructure/presentation;
- Bonus domain/application/infrastructure/presentation;
- Returns Core внутри Deals;
- Chat domain/application/infrastructure/presentation;
- Email domain/application/infrastructure/presentation;
- SignalR hub `/hubs/chat`;
- JWT authentication;
- permission authorization;
- system admin seed;
- system modules seed;
- Swagger Bearer;
- exception middleware.

Готовыми считаются:

- **Identity** - completed.
- **Clients** - completed.
- **Catalog** - completed.
- **Deals MVP** - completed.
- **Warehouse Core** - core implemented.
- **Bonus Core** - core implemented.
- **Returns Core inside Deals** - core implemented.
- **Chat Core with SignalR** - core implemented.
- **Email Campaigns Core** - core implemented.

Planned next:

- **Audit Core**.
- **API integration tests**.
- **Minimal React frontend**.
- **Dockerization**.
- **Nginx reverse proxy**.
- **Final diploma documentation**.

## Реализованные модули

### Identity

Identity считается завершённым, потому что покрывает:

- system admin login;
- organization request;
- approve/reject request;
- activation/license key;
- organization registration;
- email confirmation;
- organization user login;
- refresh token;
- password reset/change password;
- users management;
- roles management;
- module permissions.

Перед разработкой новых модулей Identity не переписывать без необходимости.

### Clients

Clients считается завершённым как первый бизнес-модуль CRM.

Реализовано:

- карточка клиента;
- контактные данные;
- статус клиента;
- источник клиента;
- marketing emails flag;
- заметки;
- поиск и фильтрация;
- tenant scoping через `OrganizationId`;
- permissions через module code `Clients`;
- soft delete через `IsActive = false`;
- migration `AddClientsModule`.

Ключевые бизнес-решения:

- `Email` не unique;
- `Phone` не unique;
- при create/update нужен `Email` или `Phone`;
- `FullName` вычисляется в response DTO и не хранится в БД;
- нет FK на Identity `Organization`;
- нет `ClientsDbContext`.

### Catalog

Catalog считается завершённым как второй бизнес-модуль CRM.

Реализовано:

- категории каталога;
- товары;
- услуги;
- поиск и фильтрация;
- tenant scoping через `OrganizationId`;
- permissions через module code `Catalog`;
- soft delete через `IsActive = false`;
- migration `AddCatalogModule`.

Ключевые бизнес-решения:

- `Category` общая для товаров и услуг;
- `Product` и `Service` являются отдельными сущностями;
- `Product.Sku` nullable и не unique;
- `ServiceCode` отсутствует;
- цены считаются в BYN;
- `Currency` column отсутствует;
- VAT/tax fields отсутствуют;
- `BonusType`/`BonusValue` и `DiscountType`/`DiscountValue` есть в `Category`, `Product`, `Service`;
- полноценный Promotions module не реализован;
- Bonus Core реализован отдельным модулем и использует Catalog bonus rules;
- Warehouse Core реализован отдельным модулем и использует Catalog Product через Guid без FK;
- Deals реализован как MVP и использует Catalog snapshot для позиций сделки;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет FK на Identity `Organization`;
- нет `CatalogDbContext`.

### Deals MVP/Core

Deals MVP/Core реализован как третий бизнес-модуль CRM после Clients и Catalog.

Реализовано:

- сделки;
- позиции сделки;
- tenant-specific editable stages;
- история изменения stages;
- расчёт сумм, скидок и capped bonus usage;
- snapshot товара/услуги из Catalog на момент сделки;
- связь с Client через `ClientId`;
- связь с ответственным пользователем через `ResponsibleUserId`;
- поля для Bonus usage и `DealItem.StorageId`;
- интеграция с Warehouse Core для списания Product items при successful final stage;
- интеграция с Bonus Core для расчёта бонусной скидки при create/update и write-off/accrual при successful final stage;
- Returns Core inside Deals: `Draft`, `Completed`, `Cancelled`;
- частичные и полные возвраты по successful final deals;
- Warehouse return movement при completed `DealReturn`;
- Bonus refund/reversal при completed `DealReturn`;
- permissions через module code `Deals`;
- migrations `AddDealsMvpModule`, `AddDealsReturnsCore`.

Ключевые бизнес-решения:

- `DealStage` используется вместо enum `Status`;
- `DealStage` tenant-specific;
- default stages `New`, `InProgress`, `Completed`, `Cancelled` создаются lazy при первом использовании Deals module;
- `Deal` хранит `BonusPointsUsed` и `BonusDiscountAmount`;
- requested bonus points capped до остатка сделки после item discounts;
- `DealItem.StorageId` используется Warehouse Core при successful final stage;
- Product item требует `StorageId`, Service item требует `StorageId = null`;
- Returns не являются отдельным модулем;
- Draft returns не влияют на Warehouse/Bonus;
- Completed returns применяют Warehouse/Bonus effects;
- Completed/Cancelled returns immutable;
- внешних FK/navigation на Clients, Catalog, Identity, Warehouse нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `DealsDbContext`;
- `SourceReturnId` в Warehouse/Bonus является nullable Guid correlation id без FK.

Returns Core завершает базовый жизненный цикл сделки:

- sale;
- warehouse deduction;
- bonus write-off/accrual;
- return;
- warehouse return;
- bonus refund/reversal.

После Returns Core в Deals остаётся Audit integration и расширенная аналитика.

### Warehouse Core

Warehouse Core реализован как базовый складской модуль после Deals MVP.

Реализовано:

- склады организации;
- default storage;
- остатки товаров;
- движения склада;
- ручное поступление товара;
- ручное списание товара;
- корректировка остатка;
- списание Product items при successful final deal stage;
- return movement при completed `DealReturn`;
- migration `AddWarehouseCoreModule`.

Ключевые бизнес-решения:

- используется общий `ApplicationDbContext`;
- `WarehouseDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Catalog, Deals, Identity и Organization нет;
- `ProductId`, `DealId`, `CreatedByUserId` хранятся как Guid;
- `SourceReturnId` хранится как nullable Guid correlation id без FK;
- inactive Catalog Product разрешён для складских операций, если Product существует в той же организации;
- отрицательные остатки запрещены;
- повторное списание сделки защищено через Sale movement by `DealId`.

### Bonus Core

Bonus Core реализован как базовый loyalty-модуль после Warehouse Core.

Реализовано:

- настройки бонусной системы организации;
- `PointValue` как стоимость 1 бонусного балла в BYN;
- бонусные счета клиентов;
- история бонусных операций;
- ручная корректировка баланса с обязательной причиной;
- расчёт бонусной скидки при создании и обновлении сделки;
- списание использованных бонусов при successful final deal stage;
- начисление бонусов при successful final deal stage;
- `Refund` для возврата ранее списанных бонусов при completed `DealReturn`;
- `CorrectionDecrease` для отката начислений при completed `DealReturn`;
- `SourceReturnId` correlation для return-origin операций;
- Catalog rule resolution: Product/Service -> Category -> parent category chain -> BonusSettings;
- migration `AddBonusCoreModule`.

Ключевые бизнес-решения:

- используется общий `ApplicationDbContext`;
- `BonusDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Clients, Deals, Catalog, Identity и Organization нет;
- `ClientId`, `DealId`, `CreatedByUserId` и `OrganizationId` хранятся как Guid;
- `BonusPointsUsed` в Deals хранит applied bonus points;
- `BonusDiscountAmount` в Deals хранит денежную скидку в BYN;
- return-origin операции считаются по `DealId`, `SourceReturnId != null` и `Type`;
- `AccrueOnBonusPayment` управляет начислением по сделкам, где бонусы уже использовались;
- duplicate completion защищён через automated BonusTransaction by `DealId`.

### Chat Core with SignalR

Chat Core реализован как коммуникационный модуль после Returns Core.

Реализовано:

- direct chats внутри организации;
- group chats внутри организации;
- client-linked chats через `ClientId`;
- deal-linked chats через `DealId`;
- inter-organization contact requests по email организации;
- approve/reject/cancel contact request flow;
- создание inter-org conversation только через approved contact request;
- explicit participant visibility для inter-org conversations;
- REST API для conversations, messages, participants и contact requests;
- REST fallback отправки сообщений;
- SignalR hub `/hubs/chat`;
- realtime events для сообщений, read status, typing, участников и contact requests;
- edit/delete messages через soft-delete;
- read status через `LastReadMessageId` и `LastReadAt`;
- migration `AddChatCoreModule`.

Ключевые бизнес-решения:

- используется общий `ApplicationDbContext`;
- `ChatDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- связи с другими модулями хранятся как Guid;
- inter-org conversation ограничен двумя организациями;
- пользователи видят inter-org conversation только если являются explicit participants;
- `SenderOrganizationId` и `SenderUserId` сохраняются на каждом сообщении;
- SignalR `access_token` из query string принимается только для `/hubs/chat`;
- Hub тонкий и вызывает MediatR commands.

### Email Campaigns Core

Email Campaigns Core реализован после Chat Core with SignalR как отдельный коммуникационный модуль для клиентских рассылок.

Реализовано:

- per-organization SMTP settings;
- реальная отправка писем через SMTP организации;
- шифрование SMTP password через ASP.NET Core Data Protection;
- SMTP test endpoint;
- templates с placeholders;
- manual campaigns по выбранным клиентам;
- inactive-client automation;
- hosted service для периодического запуска automation;
- run-now endpoint для Swagger/demo проверки;
- recipient status history;
- соблюдение `AllowMarketingEmails`;
- migration `AddEmailCampaignsCoreModule`.

Ключевые бизнес-решения:

- используется общий `ApplicationDbContext`;
- `EmailDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- связи с другими модулями хранятся как Guid;
- campaign sending использует только SMTP-настройки организации;
- глобальный `IEmailSender` не используется для campaign sending;
- automatic inactive-client emails требуют хотя бы одну successful final deal;
- `InactivityDays` и `RepeatAfterDays` настраиваются для организации.

## Следующий рекомендуемый модуль

Следующий логичный backend module после Email Campaigns Core: **Audit Core**.

Почему Audit следующий:

- Базовый жизненный цикл сделки уже закрыт: sale -> warehouse deduction -> bonus write-off/accrual -> return -> warehouse return -> bonus refund/reversal.
- Chat и Email уже закрывают базовые коммуникационные сценарии CRM.
- Audit сможет логировать ключевые действия коммуникационных модулей и основных business flows.
- После Audit нужно добавить API integration tests как отдельный Testing Layer.

## Draft-документация будущих модулей

Для будущих интеграций подготовлены связанные draft-документы:

- `docs/DEALS_MODULE.md` - фактическая документация реализованного Deals MVP.
- `docs/DEALS_MODULE_DRAFT.md` - исторический draft Deals MVP; Returns Core уже описан в `docs/DEALS_MODULE.md`.
- `docs/BONUS_MODULE.md` - фактическая документация реализованного Bonus Core.
- `docs/BONUS_MODULE_DRAFT.md` - исторический draft Bonus и future notes.
- `docs/WAREHOUSE_MODULE.md` - фактическая документация реализованного Warehouse Core.
- `docs/WAREHOUSE_MODULE_DRAFT.md` - исторический draft Warehouse и future notes.

Bonus draft и Warehouse draft сохраняются как исторические design notes. Актуальный Returns flow описан в `docs/DEALS_MODULE.md`, `docs/WAREHOUSE_MODULE.md` и `docs/BONUS_MODULE.md`:

- сделка может использовать бонусы;
- сделка уже списывает товары со склада при successful final stage;
- возвраты возвращают товары и корректируют бонусы.

## Рекомендуемый порядок реализации модулей

1. `Identity` - completed
2. `Clients` - completed
3. `Catalog` - completed
4. `Deals MVP` - completed, see `docs/DEALS_MODULE.md`
5. `Warehouse Core` - core implemented, see `docs/WAREHOUSE_MODULE.md`
6. `Bonus Core` - core implemented, see `docs/BONUS_MODULE.md`
7. `Returns Core inside Deals` - core implemented, see `docs/DEALS_MODULE.md`
8. `Chat Core with SignalR` - core implemented, see `docs/CHAT_MODULE.md`
9. `Email Campaigns Core` - core implemented, see `docs/EMAIL_MODULE.md`
10. `Audit Core` - planned next backend module
11. `API integration tests` - planned Testing Layer after Audit
12. `Minimal React frontend` - planned
13. `Dockerization` - planned
14. `Nginx reverse proxy` - planned
15. `Final diploma documentation` - planned
16. `Advanced analytics` - planned later
17. `External integrations` and API keys - planned later

After Email Campaigns Core, implement Audit Core next. После Audit добавить API integration tests как Testing Layer, затем minimal React frontend, Dockerization и Nginx reverse proxy.

## Future modules

### Deals

Сделки и продажи. MVP/Core реализован.

Реализованные возможности:

- редактируемые этапы сделки через tenant-specific `DealStage`, не enum status;
- история изменения этапов через `DealStageHistory`;
- расчёт суммы, скидок и planned bonus discount fields;
- расчёт бонусной скидки через Bonus Core;
- клиент;
- ответственный пользователь;
- позиции из Catalog со snapshot товара/услуги на момент сделки;
- `StorageId` для товарных позиций;
- Warehouse stock deduction при successful final stage;
- Bonus write-off/accrual при successful final stage;
- Returns Core inside Deals;
- Warehouse return movement при completed `DealReturn`;
- Bonus refund/reversal при completed `DealReturn`;
- planned integration point for Audit;
- связи с Clients, Catalog, Identity, Bonus и Warehouse через Guid ID без жёстких EF navigation/FK.

Не реализовано в Deals Core:

- Audit events.

Deals MVP/Core описан в `docs/DEALS_MODULE.md`. Returns Core уже реализован внутри Deals; модуль всё ещё ждёт Audit events и advanced pipelines/analytics.

### Bonus

Бонусная или loyalty-система. Bonus Core реализован.

Реализованные Core-возможности:

- настройки бонусной программы организации;
- бонусный баланс клиента;
- начисления;
- списания;
- manual corrections;
- история bonus transactions;
- deal discount calculation;
- write-off/accrual при successful final stage;
- refund/reversal при completed `DealReturn`;
- Catalog rule resolution через Product/Service -> Category -> parent chain -> BonusSettings;
- связь с Deals и Catalog через Guid ID и lookup/integration services.

Future scope:

- bonus expiration;
- loyalty levels;
- promotions;
- promo codes;
- audit integration.

Фактическая реализация описана в `docs/BONUS_MODULE.md`. Draft сохранён в `docs/BONUS_MODULE_DRAFT.md`.

### Warehouse

Складской учёт. Warehouse Core реализован.

Реализованные Core-возможности:

- склады организации;
- остатки товаров;
- движения склада;
- ручное поступление;
- ручное списание;
- корректировка остатка;
- sale deduction при successful deal completion;
- return movement при completed `DealReturn`;
- связь с Catalog и Deals через Guid ID и lookup/integration services.

Future scope:

- transfers between storages;
- suppliers;
- purchase orders;
- inventory acts;
- reservations;
- batches/lots;
- audit integration.

Фактическая реализация описана в `docs/WAREHOUSE_MODULE.md`. Draft сохранён в `docs/WAREHOUSE_MODULE_DRAFT.md`.

### Chat

Коммуникации. Chat Core with SignalR реализован.

Реализованные Core-возможности:

- direct/group chats внутри организации;
- client/deal-linked chats;
- inter-organization contact requests;
- inter-organization conversations только через approved request;
- explicit participant visibility;
- REST API для истории, диалогов, участников и fallback send message;
- SignalR realtime через `/hubs/chat`;
- messages, edit/delete, read status и typing events.

Future scope:

- attachments;
- reactions;
- full-text search;
- moderation;
- audit integration;
- frontend UI.

### Email Campaigns

Клиентские email-рассылки. Email Campaigns Core реализован после Chat Core with SignalR.

Реализованные Core-возможности:

- SMTP-настройки организации;
- реальная отправка через SMTP организации;
- encrypted SMTP password через Data Protection;
- test SMTP endpoint;
- email templates;
- manual campaigns;
- inactive-client automation;
- hosted service;
- recipient status history;
- `AllowMarketingEmails` support;
- связь с Clients через Guid ID без внешних FK.

Future scope:

- unsubscribe links;
- bounce/open/click tracking;
- attachments;
- external provider APIs;
- audit integration;
- frontend UI.

Фактическая реализация описана в `docs/EMAIL_MODULE.md`.

### Audit

Аудит действий.

Ожидаемые возможности:

- запись важных действий пользователей;
- кто, когда, что изменил;
- tenant scoping;
- read-only endpoints.

Audit понадобится для фиксации важных бизнес-действий Deals, Bonus и Warehouse.

## Формат следующих итераций

Каждый модуль реализовывать по шагам:

1. Проанализировать текущие проекты и уже существующие правила.
2. Спроектировать domain entities без EF/DataAnnotations атрибутов.
3. Добавить EF configurations в `{Module}.Infrastructure`.
4. Подключить configurations через `IEfConfigurationAssemblyProvider` в `{Module}.Infrastructure`.
5. Создать миграцию только в `Infrastructure`.
6. Добавить предметные repository interfaces и implementations.
7. Добавить MediatR commands/queries/handlers.
8. Добавить validators.
9. Добавить thin controllers.
10. Подключить DI.
11. Проверить build.
12. Проверить migrations/database update при доступной БД.
13. Обновить документацию модуля и общий project context.

Для текущей документационной итерации код, проекты и migrations не создаются.

## Что НЕ нужно реализовывать прямо сейчас

Пока не нужно:

- микросервисы;
- RabbitMQ;
- Redis;
- GraphQL;
- отдельные базы данных;
- отдельные DbContext на модуль;
- generic repository;
- сложный frontend;
- payments;
- invoices;
- payment refunds;
- полноценный Promotions module;
- promo codes;
- date-based promotions.
- Audit integration для Deals Core.
- attachments/reactions/threads/search/moderation внутри Chat Core.

## Обязательные архитектурные ограничения

- Не создавать отдельные DbContext на модуль.
- Не создавать `IdentityDbContext`.
- Не создавать `ClientsDbContext`.
- `ApplicationDbContext` находится только в `Infrastructure/Persistence`.
- Миграции находятся только в `Infrastructure/Migrations`.
- Используется одна PostgreSQL database.
- Используется один `ApplicationDbContext`.
- EF configurations модулей подключаются через `IEfConfigurationAssemblyProvider`, без references из `Infrastructure` на `{Module}.Infrastructure`.
- Не использовать generic `IRepository<T>`.
- Использовать предметные repositories.
- `SaveChangesAsync` не вызывать внутри repositories.
- Commit делать через `IUnitOfWork`.
- Domain не должен содержать EF/DataAnnotations атрибуты.
- EF ограничения описывать через Fluent API.
- Между модулями использовать Guid ID, не жёсткие EF-навигации.
- Все бизнес-данные должны быть tenant-scoped через `OrganizationId`.
- Identity уже реализован, не переписывать без необходимости.

## Known issues / risks

- System admin JWT не предназначен для organization endpoints. Для `/api/identity/roles`, `/api/identity/users`, `/api/clients` и будущих business endpoints нужен JWT пользователя организации с нужными permissions.
- Seeding modules и system admin выполняется при старте приложения через hosted service. `dotnet ef database update` только применяет migrations и не запускает HTTP приложение как рабочий runtime.
- В текущем `appsettings.json` email-настройки могут быть переведены в SMTP mode. Для локального тестирования нужен либо `Email:UseConsole = true`, либо валидные SMTP credentials через безопасный источник конфигурации.
- Если WebApi уже запущен, обычный `dotnet build CrmSystem.slnx` может упасть из-за заблокированных DLL в `bin`. Перед build лучше остановить запущенный `CrmSystem`.
- При добавлении следующего модуля важно не создавать EF-навигации на сущности другого модуля. Использовать Guid ID и application-level checks.
- Clients validators зарегистрированы в `Clients.Application`, а validation pipeline сейчас подключается общим MediatR behavior из `Identity.Application`. Если validation behavior позже будет вынесен из Identity, его нужно перенести в shared application layer или явно подключить для всех модулей.
- Catalog уже содержит bonus/discount rule fields, и Bonus Core использует bonus fields для rule resolution. Не добавлять promo codes или date-based promotions внутри Catalog.
- Deals MVP/Core уже содержит поля для Bonus usage, интегрирован с Warehouse Core и Bonus Core, а Returns Core inside Deals завершает базовый lifecycle продажи и возврата.
- Deals default stages создаются lazy при первом использовании модуля. При конкурентном первом обращении возможен риск дублей, если позже будут добавлены unique constraints на stage name/order.
- Email Campaigns Core хранит SMTP password через Data Protection. Потеря Data Protection keys делает сохранённые SMTP-пароли нерасшифровываемыми.
- Email Campaigns выполняет реальные SMTP side effects. В окружениях, где automation не должна отправлять письма, нужно использовать `EmailAutomation:IsEnabled=false`.
- Следующий backend module: Audit Core. После него запланировать API integration tests, затем minimal React frontend, Dockerization и Nginx reverse proxy.
