# Inventory System — Managing Data Using a Database

This is my Week 9 project for Industrial Programming.  
The goal of this assignment was to modify the previous inventory system so that it uses a real database instead of static data.  
I made a new version that connects to a SQLite database using Entity Framework Core.

---

## Project Description

The program manages an inventory system with orders and stock.  
It shows queued orders, processed orders, and available stock.  
When an order is processed, the stock is updated and the data is saved in the database so it is not lost when the program closes.

The user interface is made with WPF, and all data is stored in a file called `inventory.sqlite`.

---

## Features

- Uses Entity Framework Core with SQLite for data storage.
- Reads data from the database at startup instead of using hardcoded data.
- Displays queued orders, processed orders, and stock in the window.
- Buttons allow the user to:
  - Refresh data from the database.
  - Process the next order (updates stock and moves it to processed).
  - Reset the database to demo data.
- Uses `.Include()` and `.ThenInclude()` to load related data.

---

## How to Run

Requirements:
- .NET 8 SDK
- Windows (WPF application)

Steps:
1. Open the project in Visual Studio or a terminal.
2. Build and run:
   ```bash
   dotnet build
   dotnet run --project InventoryApp
 all stock quantities are stored as `decimal`. For `UnitItem` the quantity represents counts; for `BulkItem` it represents the measurement (e.g., kg).
- If an order exceeds available stock, processing is blocked with a message.
- `TotalRevenue` is computed from processed orders.
