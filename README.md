# Employee Tech Challenge (.NET 8 + React + SQL Server + Docker)

A full-stack application to manage employees of a fictional company.

Features included:
- Employees CRUD (Create / Read / Update / Delete)
- JWT Authentication
- Role-based permission rules
- SQL Server database (Docker)
- React UI consuming the API
- Swagger API documentation
- Unit tests

## Tech Stack

- **Backend:** .NET 8 Web API
- **Database:** SQL Server + Entity Framework Core
- **Authentication:** JWT Bearer
- **Frontend:** React + Vite
- **Containerization:** Docker + Docker Compose


## Requirements

### Recommended
- Docker Desktop

### Optional (for local development without Docker)
- .NET 8 SDK
- Node.js 18+


## Project Structure

```text
employee-tech-challenge/
├── EmployeeService.Api/        # .NET 8 Web API
├── EmployeeService.Tests/      # Unit & integration tests
├── employees-ui/               # React frontend (Vite)
├── docker-compose.yml
└── README.md
```

## Run with Docker (Recommended)

From the repository root, run:

```bash
docker compose up -d --build
```


## URLs

When running with Docker:

- **React UI:** http://localhost:5173
- **Swagger (API documentation):** http://localhost:8080/swagger

## Default Seed Admin (Bootstrap)

On the **first start**, the system automatically seeds an initial **Director** user.

### Credentials
- **DocumentNumber:** 1000
- **Password:** 12345678
- **Role:** Director

Use this user to log in and create other employees.

> The seed only runs if the database is empty.

The API returns a JWT token.

Send the token in requests using the header:

```http
Authorization: Bearer <token>
```

All Employees CRUD endpoints require authentication.

## Business Rules Implemented

- First and last name are required
- DocumentNumber is required and unique
- Email is required and unique
- Employee must have at least **2 phone numbers**
- Employee must be **18 years or older**
- Manager can be another employee
- A user cannot create or assign a role higher than their own
- Manager must have an equal or higher role than the employee

## Database & Migrations

- Entity Framework Core is used for data access
- Migrations are applied automatically on startup
- `DbSeeder` runs:
  - `db.Database.MigrateAsync()`
  - Seeds the initial Director user if the database is empty


### Run migrations manually (outside Docker)

```bash
dotnet ef database update --project EmployeeService.Api --startup-project EmployeeService.Api 
```

## Run Locally (Without Docker)

### API

```bash
cd EmployeeService.Api
dotnet run
```

Swagger:

http://localhost:5000/swagger (or configured port)

## UI

```bash
cd employees-ui
npm install
npm run dev
```

### UI URL

http://localhost:5173


## Tests

Run all backend tests from the repository root:

```bash
cd EmployeeService.Tests
dotnet test
```

## Docker Notes

- SQL Server runs in its own container
- API runs in a separate container
- Database data is persisted using Docker volumes
- The API depends on the DB container with health checks

## Logging

The application uses ASP.NET Core built-in logging via ILogger.

## Final Notes

This project demonstrates:

- Full-stack integration (React + .NET API)
- Validation and business rules enforcement
- Secure JWT authentication
- Role-based authorization
- Dockerized development environment
- Swagger API documentation
- Automated tests
