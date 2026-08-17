# Stock Reservation System

Internal stock reservation module for a wholesale distribution company.

## Technology

- .NET 8 / ASP.NET Core Web API
- C#
- EF Core 8
- PostgreSQL
- React + TypeScript
- xUnit
- Docker

## Architecture

The solution uses a clean separation of responsibilities:

API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
PostgreSQL

API – Controllers, HTTP requests/responses
Application – Services, queries and business workflows
Domain – Entities and business rules
Infrastructure – EF Core, PostgreSQL, caching and persistence
Frontend – React + TypeScript

##Run Locally
#1. Start PostgreSQL
docker compose up -d postgres

#Create the database
From the solution directory:

dotnet ef database update \
  --project src/StockReservation.Infrastructure \
  --startup-project src/StockReservation.Api

If EF tooling is not installed:
dotnet tool install --global dotnet-ef

#Default configuration:

Database: StockReservation
User: postgres
Password: postgres
Port: 5432

#2. Run the API
dotnet run --project src/StockReservation.Api

Swagger:
http://localhost:64525/swagger

#3. Run the React frontend
cd frontend
npm install
npm run dev

#Frontend:
http://localhost:5173

Main APIs
GET  /api/purchase-orders?warehouseId=1
POST /api/reservations
POST /api/reservations/{id}/release
GET  /api/finance/committed-stock-value


#Key Design Decisions

##Concurrency
Stock reservations are protected using database-level concurrency control so
that concurrent requests cannot reserve more stock than is available.

##Historical Cost
Each reservation stores a UnitCostSnapshot. Finance reporting uses this
historical cost rather than the item's current standard cost.

##Audit
Reservation and release actions create immutable audit records.

##Quantity Precision
Unit items: whole quantities
Weight items: up to 3 decimal places


##Assumptions
Only approved POs with outstanding quantities are available for reservation.
Reservations can be partially fulfilled and released.
A reservation cannot be released beyond its remaining quantity.
PostgreSQL is the source of truth for stock availability.
Authentication is simplified using request headers for this exercise.