# Grocery Store Inventory (VB.NET Windows Forms)

## Step 1: Open in Visual Studio
1. Install Visual Studio Community with the **.NET desktop development** workload (Visual Studio Installer → Modify → Workloads).
2. Open `GroceryStoreInventory.vbproj` from this folder. Visual Studio restores `MySql.Data` when package restore is available.
3. Copy `appsettings.example.json` to `appsettings.local.json`, then enter the MySQL account you configured in XAMPP/phpMyAdmin in `Uid` and `Pwd`. The values are intentionally blank; no account is assumed. Keep the local file private.

## Step 2: Prepare XAMPP database
1. Start **Apache** and **MySQL** in the XAMPP Control Panel.
2. Open `http://localhost/phpmyadmin`, choose **Import**, select `database.sql`, and click **Import**. This creates `grocery_inventory` and its tables.
3. In phpMyAdmin, use an existing MySQL account or create a dedicated account with access to `grocery_inventory`; enter those same credentials in `appsettings.local.json`.
4. Ensure the connection's host and port match your XAMPP MySQL settings (defaults here are `127.0.0.1:3306`).

## Step 3: Run
Press **F5**. The first time, choose **Create first account** on the login screen. Passwords are stored as salted PBKDF2 hashes. Sign in to reach the dashboard and navigation tabs.

## Included features
- Product add/edit/search/deactivate (deactivation preserves history).
- Stock-in/out with balance validation and transaction history.
- Inventory, low-stock, and stock transaction reports.
- Dashboard counts and stock valuation.

If `dotnet` is available in a terminal, the equivalent build command is `dotnet build GroceryStoreInventory.vbproj`.
