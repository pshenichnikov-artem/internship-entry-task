# Tic Tac Toe NxN API (.NET 9 + PostgreSQL)

Реализация REST API для игры в крестики-нолики NxN. Поддерживает регистрацию игроков, создание игр, выполнение ходов, идемпотентность, сохранение состояния после рестарта и вероятностную подмену символа на каждом третьем ходу.

## Запуск проекта

1. Перейдите в папку `Project`:
```bash
cd Project
```

2. Запустите проект с помощью Docker Compose:
```bash
docker-compose up --build
```
Приложение будет доступно по адресу http://localhost:8080.

## Переменные окружения
Определяются в docker-compose.yml:

Game__BoardSize=3 — размер поля
Game__WinConditionLength=3 — количество символов в ряд для победы
ConnectionStrings__DefaultConnection — строка подключения к PostgreSQL
Jwt__Key, Jwt__Issuer, Jwt__Audience — параметры для JWT
ASPNETCORE_ENVIRONMENT=Development

## Health Check
GET /health
Возвращает 200 OK при успешном запуске контейнера.

## Архитектура
Разделение на слои: Controllers, Services, Repositories
Авторизация через JWT
EF Core + миграции
PostgreSQL в Docker с volume (postgres_data)
Единый формат ответа через ApiResponse
Версионирование API через v1
Фильтрация и сортировка через DTO
Обработка ошибок и валидация через фильтр ValidateModelAttribute

## Особенности реализации
Идемпотентность
Каждый ход содержит ClientMoveId
При повторной отправке идентичного хода возвращается 200 OK с тем же ETag
Идемпотентность обеспечивается через уникальный индекс GameId + ClientMoveId и обработку исключения DbUpdateException

## API
1. PlayerController
POST /api/v1/Player/register — регистрация игрока
POST /api/v1/Player/login — вход
GET /api/v1/Player/{id} — получить игрока по ID
POST /api/v1/Player/search — поиск игроков (фильтр, сортировка, пагинация)

2. GameController
POST /api/v1/Game — создать игру
GET /api/v1/Game/{id} — получить игру по ID
POST /api/v1/Game/search — поиск игр

3. MoveController
POST /api/v1/Move — сделать ход (с поддержкой идемпотентности)
GET /api/v1/Move/{id} — получить ход
POST /api/v1/Move/search — фильтр и сортировка ходов

## Формат ответа

При успехе
```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

При ошибке:
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": 400,
    "message": "Ошибка валидации данных",
    "details": {
      "username": ["Поле обязательно"]
    }
  }
}
```

## Тестирование
Проект покрыт unit и интеграционными тестами с использованием xUnit(>70%). 

### Запуск тестов
```bash
cd Project
dotnet test .\IntegrationTests\IntegrationTests.csproj --collect:"XPlat Code Coverage"
dotnet test .\ServicesTests\ServicesTests.csproj --collect:"XPlat Code Coverage"
```

### Генерация отчета покрытия
```bash
reportgenerator -reports:"IntegrationTests\TestResults\{НАЗВАНИЕ ПАПКИ}\coverage.cobertura.xml;ServicesTests\TestResults\{НАЗВАНИЕ ПАПКИ}\coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### CI не настроен по причине блокировки GitHub Actions по региону