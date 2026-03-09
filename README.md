# Account Ledger Service (T4)

## 1. Что делает проект

`Account Ledger Service` — REST API для учета финансовых операций клиента:
- `POST /credit` — зачисление средств.
- `POST /debit` — списание средств.
- `POST /revert?id={transactionId}` — отмена ранее выполненной транзакции.
- `GET /balance?id={clientId}` — актуальный баланс клиента.

В доменной модели интерфейс `ITransaction` реализован отдельными типами:
- `CreditTransaction`
- `DebitTransaction`

Сервис реализован в production-style, но без лишнего оверинжиниринга.

## 2. Основные гарантии

- Идемпотентность всех POST-операций:
  - повтор `credit/debit` с тем же `Id` и тем же payload возвращает тот же результат;
  - повтор `revert` для той же транзакции возвращает тот же `revertDateTime` и баланс.
- Защита от гонок и double-spend:
  - при списании используется транзакция БД и `SELECT ... FOR UPDATE` для строки аккаунта.
- Multi-instance safety:
  - нет in-memory состояния;
  - критичные гарантии обеспечиваются PostgreSQL (транзакции, unique PK, row locks).
- Ошибки отдаются в формате `application/problem+json` (RFC 9457 style).

## 3. Стек технологий

- .NET 10
- ASP.NET Core Web API
- MediatR (CQRS)
- EF Core + Npgsql (PostgreSQL)
- Serilog
- xUnit + FluentAssertions
- Integration tests: `WebApplicationFactory` + `Testcontainers.PostgreSql`
- Docker / Docker Compose
- Kubernetes + Helm chart (`devops/helm/account-ledger`)

## 4. Быстрый старт через Docker Compose

```bash
docker compose up --build
```

После старта API доступно: `http://localhost:8080`.

Swagger: `http://localhost:8080/swagger` (в `Development`).

## 5. Локальный запуск без Docker

1. Поднять PostgreSQL.
2. Настроить конфигурацию в YAML:

- базовый файл: `src/AccountLedger.Api/appsettings.yml`
- окружение: `appsettings.Development.yml`

Также можно переопределить строку подключения через переменную окружения:

```bash
ConnectionStrings__Default=Host=localhost;Port=5432;Database=account_ledger;Username=postgres;Password=postgres
```

3. Запустить API:

```bash
dotnet run --project src/AccountLedger.Api
```

При старте автоматически применяются EF migrations.

Параметры, вынесенные в конфиг:
- `Database:Migrations:MaxAttempts`
- `Database:Migrations:DelaySeconds`
- `Infrastructure:DuplicateRead:Attempts`
- `Infrastructure:DuplicateRead:DelayMilliseconds`
- `Api:Swagger:Enabled`

## 6. Примеры curl-запросов

### Credit

```bash
curl -X POST "http://localhost:8080/credit" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "8f0452b2-867b-4ef8-9a9d-3c9c03d9afdf",
    "clientId": "cfaa0d3f-7fea-4423-9f69-ebff826e2f89",
    "dateTime": "2026-03-03T10:00:00Z",
    "amount": 23.05
  }'
```

### Debit

```bash
curl -X POST "http://localhost:8080/debit" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "05eb235c-4955-4c16-bcdd-34e8178228de",
    "clientId": "cfaa0d3f-7fea-4423-9f69-ebff826e2f89",
    "dateTime": "2026-03-03T10:01:00Z",
    "amount": 10.00
  }'
```

### Revert

```bash
curl -X POST "http://localhost:8080/revert?id=05eb235c-4955-4c16-bcdd-34e8178228de"
```

### Balance

```bash
curl "http://localhost:8080/balance?id=cfaa0d3f-7fea-4423-9f69-ebff826e2f89"
```

## 7. Архитектура

Проект разделен по слоям:

- `src/AccountLedger.Api`
  - HTTP-слой, контроллеры, ProblemDetails, DI/bootstrapping.
- `src/AccountLedger.Application`
  - CQRS-команды/запросы (MediatR), контракты use-case.
- `src/AccountLedger.Domain`
  - доменные сущности и правила.
- `src/AccountLedger.Infrastructure`
  - EF Core DbContext, конфигурации, migrations, реализация ledger-логики и доступ к БД.

CQRS:
- Commands: `CreditCommand`, `DebitCommand`, `RevertCommand`
- Query: `GetBalanceQuery`

## 8. Описание БД

### Accounts
- `ClientId` (PK, uuid)
- `Balance` (numeric(18,2))
- `UpdatedAt` (timestamptz)

### Transactions
- `Id` (PK, uuid)
- `ClientId` (FK -> Accounts.ClientId)
- `Type` (`Credit` / `Debit` / `Revert`)
- `Amount` (numeric(18,2))
- `OccurredAt` (дата из запроса)
- `InsertedAt` (серверное время)
- `BalanceAfter`
- `Status` (`Applied` / `Reverted`)
- `RevertedAt` (nullable)
- `RevertTransactionId` (nullable)

Индексы:
- PK по `Id` (даёт уникальность для идемпотентности).
- Индекс `IX_Transactions_ClientId`.

## 9. Как реализована идемпотентность

- Для `credit/debit`:
  - сначала ищем `Transactions.Id`;
  - если запись уже есть и payload совпадает, возвращаем старый результат;
  - если payload отличается — `409 Conflict`.
- При гонке двух одинаковых запросов на один `Id`:
  - уникальный PK в БД защищает от дубля;
  - второй запрос ловит unique violation, перечитывает сохраненную транзакцию и возвращает тот же ответ.
- Для `revert`:
  - исходная транзакция блокируется `FOR UPDATE`;
  - если уже `Reverted`, возвращается результат первой отмены.

## 10. Как предотвращается double-spend

Для debit/revert используется:
- транзакция БД;
- `SELECT ... FOR UPDATE` на строку аккаунта;
- проверка баланса и обновление в пределах одной транзакции.

Это гарантирует, что параллельные списания не смогут одновременно потратить один и тот же баланс.

## 11. Как работает revert

Используется компенсационная операция:
- исходная транзакция не удаляется;
- создается новая транзакция типа `Revert`;
- исходная получает `Status=Reverted`, `RevertedAt`, `RevertTransactionId`.

Стратегия для revert credit:
- если текущего баланса недостаточно, вернуть credit нельзя (`409 Conflict`), потому что иначе баланс уйдет в минус.

## 12. Как запустить тесты

Unit:

```bash
dotnet test tests/AccountLedger.UnitTests/AccountLedger.UnitTests.csproj
```

Integration (нужен запущенный Docker daemon):

```bash
dotnet test tests/AccountLedger.IntegrationTests/AccountLedger.IntegrationTests.csproj
```

## 13. Как деплоить в Kubernetes через Helm

Chart:
- `devops/helm/account-ledger`

Chart создает:
- `Namespace` (опционально)
- `ConfigMap`
- `Secret`
- `Deployment`/`Service` для API
- `Deployment`/`Service`/`PVC` для PostgreSQL

Установка в `dev`:

```bash
helm upgrade --install account-ledger ./devops/helm/account-ledger \
  --namespace account-ledger-dev \
  --create-namespace
```

Перед деплоем в кластер подставить реальные параметры image и секретов:

```bash
helm upgrade --install account-ledger ./devops/helm/account-ledger \
  --namespace account-ledger-dev \
  --create-namespace \
  --set api.image.repository=ghcr.io/<org>/account-ledger-api \
  --set api.image.tag=<tag> \
  --set postgres.auth.password=<strong_password>
```

При необходимости можно отключить создание секрета chart-ом и использовать внешний secret:

```bash
helm upgrade --install account-ledger ./devops/helm/account-ledger \
  --namespace account-ledger-dev \
  --create-namespace \
  --set secret.create=false \
  --set secret.name=account-ledger-secret
```

## 14. Ограничения и допущения

- В Helm chart PostgreSQL разворачивается в том же кластере для демонстрации тестового.
  Для production рекомендуется managed PostgreSQL.
- Аутентификация/авторизация намеренно не добавлялись (в ТЗ не требуются).
- Денежная точность ограничена `numeric(18,2)` и двумя знаками после запятой.

## Структура проекта

```text
src/
  AccountLedger.Api
  AccountLedger.Application
  AccountLedger.Domain
  AccountLedger.Infrastructure
tests/
  AccountLedger.UnitTests
  AccountLedger.IntegrationTests
devops/
  k8s/
    dev/
  helm/
    account-ledger/
docker-compose.yml
Dockerfile
infa-helm-values/
docs/
```
