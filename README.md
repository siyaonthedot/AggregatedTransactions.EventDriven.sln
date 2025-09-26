# 🏦 Aggregated Transactions (Event-Driven)

This project demonstrates an **event-driven microservices architecture** for aggregating customer banking transactions from multiple sources (Bank A, Bank B).  
It uses **ASP.NET Core 8**, **Kafka**, and **PostgreSQL** to provide a consolidated transaction API.

---

## 🚀 Features
- **Aggregator API**
  - Exposes REST endpoints to query transactions
  - Supports filtering, pagination, authentication
- **Aggregator Worker**
  - Consumes events from Kafka
  - Persists transactions into PostgreSQL
- **Bank A & Bank B APIs**
  - Mock upstream bank services
  - Publish transaction events to Kafka
- **Authentication**
  - JWT with refresh tokens
  - User registration and login
- **Infrastructure**
  - PostgreSQL for persistence
  - Kafka + Zookeeper for event-driven messaging
  - Kafka UI for topic inspection
  - pgAdmin for DB management
  - NLog + OpenTelemetry for logging & monitoring

---

## 🛠️ Tech Stack
- **.NET 8** (ASP.NET Core, BackgroundServices, Dapper)
- **Kafka** (Confluent)
- **PostgreSQL** (with pgAdmin)
- **Docker Compose**
- **NLog** + **OpenTelemetry**

---

## 📂 Solution Structure


AggregatedTransactions.EventDriven.sln
│
├── src/
│ ├── Aggregator.Api/ # Main REST API
│ ├── Aggregator.Worker/ # Background service
│ ├── BankA.Api/ # Mock bank service A
│ ├── BankB.Api/ # Mock bank service B
│ ├── Contracts/ # Shared DTOs & Events
│ ├── Infrastructure/ # Persistence & Messaging
│ └── Shared/ # Logging & Monitoring
│
├── tests/
│ ├── Aggregator.Api.Tests/
│ ├── Aggregator.Worker.Tests/
│ └── BankAdapters.Tests/
│
├── docker/
│ ├── docker-compose.yml
│ └── docker-compose.override.yml
│
└── README.md

2. Access Services

Aggregator API → http://localhost:5000

Bank A API → http://localhost:5001

Bank B API → http://localhost:5002

PostgreSQL → localhost:5432 (user: postgres, pass: postgres, db: aggregator)

pgAdmin → http://localhost:5050
 (login: admin@admin.com / admin)

Kafka Broker → localhost:9092

Kafka UI → http://localhost:8080

🔐 Authentication Flow

1.  Register a user

POST /api/auth/register
{
  "username": "admin",
  "password": "P@ssw0rd!"
}

2 . Login

POST /api/auth/login
{
  "username": "admin",
  "password": "P@ssw0rd!"
}

Returns: { "accessToken": "...", "refreshToken": "..." }

3. Use Token
Add header:
Authorization: Bearer <accessToken>

4. Refresh Token
   
   POST /api/auth/refresh
{
  "refreshToken": "..."
}

🧪 Testing

Unit tests live in the /tests folder

Run with:

dotnet test

📈 Monitoring

Logs → via NLog (console, file, or centralized logging)

Tracing & Metrics → via OpenTelemetry

📝 Notes

Ensure you’re using .NET 8 SDK when building locally.

Bank services (Bank A, Bank B) only publish to Kafka; they don’t need their own DBs.

Aggregator Worker is the only consumer that persists data into PostgreSQL.








