# Users / Credentials

## Admin (hardcoded admin portal login)
- Username: `admin`
- Password: `admin123`
- Login route: `/Admin/Login`
- Source: `Controllers/AdminController.cs` (`DemoAdminUsername`, `DemoAdminPassword`)

## Admin (database login)
- Username: `admin`
- Password: `Admin@1234`
- Login route: `/Login/Login` (main login)
- Note: `/Admin/Login` now accepts DB admin credentials too.

## SQL Seed Accounts (from setup script)
These are defined in `App_Data/ArtZada_Setup.sql`:

- Admin
  - Username: `admin`
  - Password: `Admin@1234`
- Seller
  - Username: `kwekkweksastix`
  - Password: `Test@1234`
- Buyer
  - Username: `pamili_is_love`
  - Password: `Test@1234`


