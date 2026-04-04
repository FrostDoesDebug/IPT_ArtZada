# ArtZada Role Access Guide

This document explains how to access each role, which pages each role can use, and how legacy routes behave after role-based enforcement.

## 0. Default Local Port

- Default local port is fixed to 44357.
- Use this base URL for local run: https://localhost:44357/

## 1. Roles and Login Entry Points

- Guest (not logged in)
  - Landing page: /Landing/Landing
  - Login page: /Login/Login
  - Signup page: /Login/Signup
  - Admin login page: /Admin/Login

- Client
  - Login using /Login/Login or create an account at /Login/Signup
  - Default role after user login/signup: Client

- Seller
  - Login as Client first, then switch role from Client Account page
  - Direct switch URL: /Client/SwitchToSeller

- Admin
  - Login page: /Admin/Login
  - Demo credentials:
    - Username: admin
    - Password: admin123

## 2. Role-Based Redirect Rules

- If a user is not logged in and opens Client or Seller pages, they are redirected to /Login/Login.
- If a Client opens Seller routes, they are redirected to /Client/Index.
- If a Seller opens Client routes, they are redirected to /Seller/Store.
- If an Admin opens Client or Seller routes, they are redirected to /Admin/UserOverview.

## 3. Role Page Map

### Client role

- /Client/Index
- /Client/Product
- /Client/Message
- /Client/Notifications
- /Client/Analytics
- /Client/Account
- /Client/Cart

### Seller role

- /Seller/Store
- /Seller/AddItem
- /Seller/EditItem
- /Seller/Message
- /Seller/Notifications
- /Seller/Analytics
- /Seller/Account

### Admin role

- /Admin/UserOverview
- /Admin/Pending
- /Admin/BannedListed
- /Admin/Account
- /Admin/Logout

## 4. Role Switching

- Client to Seller:
  - UI button in Client Account page
  - Route: /Client/SwitchToSeller

- Seller to Client:
  - UI button in Seller Account page
  - Route: /Seller/SwitchToClient

- Sign out:
  - Route: /Login/Logout

## 5. Legacy Home Routes

Legacy Home routes are kept for compatibility and now redirect to role pages:

- /Home/Index -> role home page
- /Home/Message -> /Client/Message or /Seller/Message
- /Home/Notifications -> /Client/Notifications or /Seller/Notifications
- /Home/Analytics -> /Client/Analytics or /Seller/Analytics
- /Home/Account -> /Client/Account or /Seller/Account
- /Home/SellerSideStore -> /Seller/Store
- /Home/SellerSideStoreAddItem -> /Seller/AddItem
- /Home/SellerSideEditItem -> /Seller/EditItem

## 6. Non-Existing Pages Addressed

These views were added as placeholders so the Visual Studio project stays complete:

- Views/Home/About.cshtml
- Views/Home/Contact.cshtml

## 7. Visual Studio Refresh and Reload Steps

If VS Code edits do not immediately reflect in Visual Studio:

1. In Visual Studio, right-click the project and click Reload Project.
2. In Solution Explorer, click Show All Files, then include any excluded files.
3. Run Build -> Clean Solution, then Build -> Rebuild Solution.
4. If still stale, close Visual Studio, delete bin and obj folders, reopen the solution, then Rebuild.
5. Confirm the changed files are listed in ArtZada.csproj (important for classic ASP.NET MVC projects).
