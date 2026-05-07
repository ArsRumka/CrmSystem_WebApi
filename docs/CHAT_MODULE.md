# Chat Module

## Назначение

Chat отвечает за:

- внутренние чаты сотрудников организации;
- direct-диалоги;
- групповые диалоги;
- чаты по клиентам;
- чаты по сделкам;
- межорганизационные чаты;
- запросы на переписку между организациями;
- realtime-обмен сообщениями через SignalR;
- хранение истории сообщений;
- управление участниками;
- read-status;
- typing events.

## Архитектура

Chat реализован как отдельный модуль:

- `Chat.Domain`;
- `Chat.Application`;
- `Chat.Infrastructure`;
- `Chat.Presentation`.

Архитектурные правила:

- используется общий `Infrastructure/Persistence/ApplicationDbContext.cs`;
- `ChatDbContext` не создавался;
- миграции находятся только в `Infrastructure/Migrations`;
- EF configurations регистрируются через `IEfConfigurationAssemblyProvider` в `AddChatInfrastructure()`;
- внешних FK/navigation на Identity User, Identity Organization, Clients и Deals нет;
- связи с другими модулями выполняются только через Guid;
- SignalR Hub находится в `Chat.Presentation`;
- REST endpoints используют MediatR;
- Hub является тонким слоем и вызывает application commands.

`ApplicationDbContext` не ссылается на `Chat.Infrastructure` напрямую. Chat repositories используют общий `ApplicationDbContext`, но repositories не вызывают `SaveChangesAsync`; сохранение выполняется через общий `IUnitOfWork`.

## Особенность tenant scoping

Большинство CRM-модулей строго tenant-scoped через `OrganizationId`. Chat является исключением для межорганизационных диалогов.

Для обычных direct/group/client/deal conversations данные остаются внутри одной организации. Для `InterOrganization` conversation:

- conversation связан с двумя организациями;
- доступ определяется не только `OrganizationId`, а explicit participant membership;
- только явные участники видят conversation и сообщения;
- сообщение хранит `SenderOrganizationId` и `SenderUserId`;
- conversation хранит `OwnerOrganizationId`;
- organization membership хранится через `ChatConversationOrganization`.

Это позволяет пользователю писать как представитель своей организации, но не открывает межорганизационный чат всем сотрудникам организации автоматически.

## Основные сущности

### ChatConversation

Поля:

- `Id`
- `Type`
- `OwnerOrganizationId`
- `Title`
- `ClientId`
- `DealId`
- `IsActive`
- `CreatedAt`
- `CreatedByUserId`
- `UpdatedAt`
- `DeletedAt`
- `DeletedByUserId`

Правила:

- `Direct`, `Group`, `Client`, `Deal` conversations создаются внутри организации;
- `Client` conversation содержит `ClientId`;
- `Deal` conversation содержит `DealId`;
- `InterOrganization` conversation нельзя создать напрямую через `POST /api/chat/conversations`;
- `InterOrganization` создаётся только через approved contact request;
- conversation удаляется через soft-delete;
- soft-deleted conversation не принимает новые сообщения.

### ChatConversationOrganization

Поля:

- `Id`
- `ConversationId`
- `OrganizationId`
- `IsActive`
- `JoinedAt`
- `LeftAt`

Правила:

- хранит организации-участники conversation;
- для внутриорганизационных чатов обычно одна организация;
- для InterOrganization Core - ровно две организации;
- `OrganizationId` хранится как Guid без FK на Identity Organization.

### ChatParticipant

Поля:

- `Id`
- `ConversationId`
- `OrganizationId`
- `UserId`
- `IsActive`
- `JoinedAt`
- `LeftAt`
- `LastReadMessageId`
- `LastReadAt`

Правила:

- только active participant может отправлять сообщение;
- пользователь видит только conversations, где он participant;
- remove participant выполняется soft remove;
- нельзя удалить последнего active participant;
- в inter-org conversation пользователь может добавлять/удалять только участников своей организации;
- нельзя удалить последнего active participant своей organization в inter-org conversation.

### ChatMessage

Поля:

- `Id`
- `ConversationId`
- `SenderOrganizationId`
- `SenderUserId`
- `Text`
- `CreatedAt`
- `EditedAt`
- `DeletedAt`
- `DeletedByUserId`
- `IsDeleted`

Правила:

- сообщение отправляет active participant;
- `Text` max 4000;
- edit allowed only for sender;
- delete is soft-delete;
- deleted message остаётся в истории;
- response для deleted message возвращает `IsDeleted = true` и `Text = null`.

### ChatContactRequest

Поля:

- `Id`
- `RequesterOrganizationId`
- `TargetOrganizationId`
- `RequesterUserId`
- `Message`
- `Status`
- `CreatedAt`
- `RespondedAt`
- `RespondedByUserId`
- `ConversationId`
- `RejectionReason`
- `CancelledAt`
- `CancelledByUserId`

Правила:

- request создаётся по email целевой организации;
- нельзя отправить request в свою организацию;
- нельзя создать duplicate pending request;
- нельзя создать request, если уже есть active inter-org conversation между организациями;
- `Pending` можно approve/reject/cancel;
- `Approved`, `Rejected`, `Cancelled` immutable;
- approve создаёт `InterOrganization` conversation.

## Enums

`ChatConversationType`:

- `Direct`
- `Group`
- `Client`
- `Deal`
- `InterOrganization`

`ChatContactRequestStatus`:

- `Pending`
- `Approved`
- `Rejected`
- `Cancelled`

## REST API endpoints

Conversations:

```http
GET /api/chat/conversations
GET /api/chat/conversations/{id}
POST /api/chat/conversations
PUT /api/chat/conversations/{id}
DELETE /api/chat/conversations/{id}
```

Messages:

```http
GET /api/chat/conversations/{id}/messages
POST /api/chat/conversations/{id}/messages
PUT /api/chat/messages/{id}
DELETE /api/chat/messages/{id}
POST /api/chat/conversations/{id}/read
```

Participants:

```http
POST /api/chat/conversations/{id}/participants
DELETE /api/chat/conversations/{id}/participants/{userId}
```

Contact requests:

```http
POST /api/chat/contact-requests
GET /api/chat/contact-requests/incoming
GET /api/chat/contact-requests/outgoing
POST /api/chat/contact-requests/{id}/approve
POST /api/chat/contact-requests/{id}/reject
POST /api/chat/contact-requests/{id}/cancel
```

## SignalR

Hub route:

```text
/hubs/chat
```

Hub methods:

- `JoinConversation`
- `LeaveConversation`
- `SendMessage`
- `MarkAsRead`
- `Typing`

Realtime events:

- `MessageReceived`
- `MessageEdited`
- `MessageDeleted`
- `ConversationRead`
- `UserTyping`
- `ParticipantAdded`
- `ParticipantRemoved`
- `ContactRequestReceived`

Groups:

- `Conversation:{conversationId}`
- `User:{userId}`
- `Organization:{organizationId}`

Authentication:

- REST uses regular Bearer JWT;
- SignalR supports `access_token` query string only for `/hubs/chat`;
- normal REST JWT flow is not changed.

Rules:

- `JoinConversation` allowed only for active participant;
- `SendMessage` allowed only for active participant and `Chat/Create`;
- `MarkAsRead` requires active participant and `Chat/Update`;
- `Typing` does not persist data and only broadcasts realtime event.

REST fallback `POST /api/chat/conversations/{id}/messages` sends through the same MediatR command as Hub `SendMessage`. Controllers and Hub broadcast after a successful MediatR call; business rules remain in Application handlers.

## Inter-organization chat flow

1. Organization A sends contact request to Organization B by organization email.
2. Request status is `Pending`.
3. Organization B sees incoming request.
4. Organization B approves or rejects.
5. On approve:
   - `InterOrganization` conversation is created;
   - requester organization and target organization are added;
   - requester user and approver user are added as participants;
   - request status becomes `Approved`;
   - `ConversationId` is stored on request.
6. Only explicit participants can see and write in the conversation.

Ограничения:

- inter-org chat limited to two organizations;
- cannot create inter-org conversation directly;
- users cannot add participants from another organization;
- users from the same organization who are not explicit participants cannot see the conversation.

## Permissions

Module code:

```text
Chat
```

Mapping:

- `Chat / Read`:
  - `GET /api/chat/conversations`
  - `GET /api/chat/conversations/{id}`
  - `GET /api/chat/conversations/{id}/messages`
  - `GET /api/chat/contact-requests/incoming`
  - `GET /api/chat/contact-requests/outgoing`
- `Chat / Create`:
  - `POST /api/chat/conversations`
  - `POST /api/chat/conversations/{id}/messages`
  - `POST /api/chat/contact-requests`
- `Chat / Update`:
  - `PUT /api/chat/conversations/{id}`
  - `PUT /api/chat/messages/{id}`
  - `POST /api/chat/conversations/{id}/read`
  - `POST /api/chat/conversations/{id}/participants`
  - `DELETE /api/chat/conversations/{id}/participants/{userId}`
  - `POST /api/chat/contact-requests/{id}/approve`
  - `POST /api/chat/contact-requests/{id}/reject`
  - `POST /api/chat/contact-requests/{id}/cancel`
- `Chat / Delete`:
  - `DELETE /api/chat/conversations/{id}`
  - `DELETE /api/chat/messages/{id}`

REST uses `RequirePermissionAttribute`. SignalR hub uses application-level permission checks because dynamic policy attributes are not enough for hub methods. Hub methods call the same application commands used by REST fallback where applicable.

## EF/database details

Таблицы:

- `ChatConversations`
- `ChatConversationOrganizations`
- `ChatParticipants`
- `ChatMessages`
- `ChatContactRequests`

Internal Chat FK:

- `ChatConversationOrganizations.ConversationId -> ChatConversations.Id`
- `ChatParticipants.ConversationId -> ChatConversations.Id`
- `ChatMessages.ConversationId -> ChatConversations.Id`
- `ChatContactRequests.ConversationId -> ChatConversations.Id`

Внешних FK нет:

- no FK to Identity User;
- no FK to Identity Organization;
- no FK to Clients;
- no FK to Deals.

`ClientId`, `DealId`, `SenderOrganizationId`, `SenderUserId`, `RequesterOrganizationId`, `TargetOrganizationId` and participant `OrganizationId`/`UserId` are Guid references only.

Migration:

```text
20260507173218_AddChatCoreModule
```

## Out of scope / Future scope

Не реализовано в Chat Core:

- attachments;
- file upload;
- image messages;
- reactions;
- threads;
- message forwarding;
- full-text search;
- moderation;
- organization blacklist;
- email notifications;
- audit integration;
- frontend UI.
