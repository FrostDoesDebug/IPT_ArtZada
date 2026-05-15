# Users / Credentials

## Admin (hardcoded admin portal login)
- Username: `admin`
- Password: `admin123`
- Login route: `/Admin/Login`
- Source: `Controllers/AdminController.cs` (`DemoAdminUsername`, `DemoAdminPassword`)

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

## Important note
The SQL script currently inserts `PasswordHash = 'REPLACE_WITH_ASPNET_IDENTITY_PBKDF2_HASH'` for seeded users. With the current login code (`Controllers/LoginController.cs`) doing direct string compare against `PasswordHash`, these seeded accounts will not work until `PasswordHash` values are replaced with actual values expected by your auth flow.
