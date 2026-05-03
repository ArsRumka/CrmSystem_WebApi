# План дальнейшей разработки

## Текущий статус

Проект находится на стадии готового Identity foundation/runtime, реализованных бизнес-модулей Clients и Catalog, а также MVP/Core модулей Deals MVP и Warehouse Core.

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
- Bonus module пока не реализован;
- Warehouse Core реализован отдельным модулем и использует Catalog Product через Guid без FK;
- Deals реализован как MVP и использует Catalog snapshot для позиций сделки;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет FK на Identity `Organization`;
- нет `CatalogDbContext`.

### Deals MVP

Deals MVP реализован как третий бизнес-модуль CRM после Clients и Catalog.

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
- permissions через module code `Deals`;
- migration `AddDealsMvpModule`.

Ключевые бизнес-решения:

- `DealStage` используется вместо enum `Status`;
- `DealStage` tenant-specific;
- default stages `New`, `InProgress`, `Completed`, `Cancelled` создаются lazy при первом использовании Deals module;
- `Deal` хранит `BonusPointsUsed` и `BonusDiscountAmount`;
- requested bonus points capped до остатка сделки после item discounts;
- `DealItem.StorageId` используется Warehouse Core при successful final stage;
- Product item требует `StorageId`, Service item требует `StorageId = null`;
- внешних FK/navigation на Clients, Catalog, Identity, Warehouse нет;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- нет `DealsDbContext`;
- Bonus и Returns не реализованы.

Deals MVP не считается полностью финальным CRM-модулем до покрытия:

- Bonus integration;
- Returns;
- Audit.

Returns belong to Deals module.

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
- migration `AddWarehouseCoreModule`.

Ключевые бизнес-решения:

- используется общий `ApplicationDbContext`;
- `WarehouseDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations подключены через `IEfConfigurationAssemblyProvider`;
- внешних FK/navigation на Catalog, Deals, Identity и Organization нет;
- `ProductId`, `DealId`, `CreatedByUserId` хранятся как Guid;
- inactive Catalog Product разрешён для складских операций, если Product существует в той же организации;
- отрицательные остатки запрещены;
- повторное списание сделки защищено через Sale movement by `DealId`.

## Следующий рекомендуемый модуль

Следующий логичный модуль после Warehouse Core: **Bonus Core**.

Почему Bonus следующий:

- Deals уже хранит `BonusPointsUsed` и `BonusDiscountAmount`, но Bonus balance/settings и transactions ещё не реализованы.
- Warehouse Core уже покрывает stock deduction для successful deals.
- Returns в Deals лучше реализовывать после Bonus Core, потому что return должен корректировать и бонусы, и склад.
- Audit лучше делать после основных бизнес-операций Bonus/Warehouse/Returns.

## Draft-документация будущих модулей

Для будущих интеграций подготовлены связанные draft-документы:

- `docs/DEALS_MODULE.md` - фактическая документация реализованного Deals MVP.
- `docs/DEALS_MODULE_DRAFT.md` - исторический draft и future notes для Deals/Returns.
- `docs/BONUS_MODULE_DRAFT.md` - bonus settings, accounts, transactions, accrual/write-off/refund rules.
- `docs/WAREHOUSE_MODULE.md` - фактическая документация реализованного Warehouse Core.
- `docs/WAREHOUSE_MODULE_DRAFT.md` - исторический draft Warehouse и future notes.

Bonus draft и Warehouse draft сохраняются, потому что future Deals flow зависит от их бизнес-решений:

- сделка может использовать бонусы;
- сделка уже списывает товары со склада при successful final stage;
- возвраты должны возвращать товары и корректировать бонусы.

## Рекомендуемый порядок реализации модулей

1. `Identity` - completed
2. `Clients` - completed
3. `Catalog` - completed
4. `Deals MVP` - completed, see `docs/DEALS_MODULE.md`
5. `Warehouse Core` - core implemented, see `docs/WAREHOUSE_MODULE.md`
6. `Bonus Core` - planned next
7. `Returns inside Deals` - planned after Bonus Core
8. `Audit` - planned after core business operations
9. `Chat` - planned

After Warehouse Core, implement Bonus Core next.

## Future modules

### Deals

Сделки и продажи. MVP реализован.

Реализованные возможности:

- редактируемые этапы сделки через tenant-specific `DealStage`, не enum status;
- история изменения этапов через `DealStageHistory`;
- расчёт суммы, скидок и planned bonus discount fields;
- клиент;
- ответственный пользователь;
- позиции из Catalog со snapshot товара/услуги на момент сделки;
- `StorageId` для товарных позиций;
- Warehouse stock deduction при successful final stage;
- planned integration points for Bonus, Returns and Audit;
- связи с Clients, Catalog, Identity, Bonus и Warehouse через Guid ID без жёстких EF navigation/FK.

Не реализовано в MVP:

- Bonus write-off/accrual;
- Returns endpoints/entities;
- Audit events.

Deals MVP описан в `docs/DEALS_MODULE.md`. Модуль не считается финально завершённым до интеграций с Bonus, Returns и Audit.

### Bonus

Бонусная или loyalty-система.

Ожидаемые возможности:

- настройки бонусной программы организации;
- бонусный баланс клиента;
- начисления;
- списания;
- refunds/corrections;
- история bonus transactions;
- связь с Deals и Catalog через Guid ID и lookup/integration services.

Draft описан в `docs/BONUS_MODULE_DRAFT.md`.

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
- связь с Catalog и Deals через Guid ID и lookup/integration services.

Future scope:

- transfers between storages;
- suppliers;
- purchase orders;
- inventory acts;
- reservations;
- batches/lots;
- return movement при возврате сделки;
- audit integration.

Фактическая реализация описана в `docs/WAREHOUSE_MODULE.md`. Draft сохранён в `docs/WAREHOUSE_MODULE_DRAFT.md`.

### Chat

Коммуникации.

Ожидаемые возможности:

- внутренние сообщения;
- межорганизационные сообщения;
- участники и история диалогов;
- базовая история переписки.

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
- организационные SMTP-настройки для клиентских рассылок;
- payments;
- invoices;
- payment refunds;
- полноценный Promotions module;
- promo codes;
- date-based promotions.
- Bonus/Returns/Audit integrations для Deals MVP.

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
- Catalog уже содержит bonus/discount rule fields, но полноценные Bonus и Promotions modules не реализованы. Не добавлять bonus transactions, organization bonus settings, promo codes или date-based promotions внутри Catalog.
- Deals MVP уже содержит поля для Bonus usage и интегрирован с Warehouse Core для stock deduction, но не выполняет bonus balance/settings checks, bonus transactions или returns.
- Deals default stages создаются lazy при первом использовании модуля. При конкурентном первом обращении возможен риск дублей, если позже будут добавлены unique constraints на stage name/order.
