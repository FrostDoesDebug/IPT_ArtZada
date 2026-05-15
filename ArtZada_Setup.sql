-- ============================================================
-- ArtZada Database Setup Script
-- Database: SQL Server LocalDB / SQL Server Express
-- Compatible with: Visual Studio LocalDB & SQL Server Express
--
-- HOW TO RUN THIS:
--   Option 1 (Visual Studio):
--     Open View > SQL Server Object Explorer
--     Right-click your server > New Query
--     Paste this entire file > Click the Run (▶) button
--
--   Option 2 (SSMS - SQL Server Management Studio):
--     Connect to (localdb)\MSSQLLocalDB  OR  .\SQLEXPRESS
--     Click "New Query"
--     Paste this entire file > Press F5
--
--   Option 3 (sqlcmd via Command Prompt):
--     sqlcmd -S "(localdb)\MSSQLLocalDB" -i ArtZada_Setup.sql
--
-- ============================================================


-- ============================================================
-- STEP 1: Create and use the database
-- ============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ArtZadaDb')
BEGIN
    CREATE DATABASE ArtZadaDb;
END
GO

USE ArtZadaDb;
GO


-- ============================================================
-- STEP 2: Drop tables (safe reset — in reverse FK order)
-- ============================================================

IF OBJECT_ID('dbo.Notifications',  'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.Messages',       'U') IS NOT NULL DROP TABLE dbo.Messages;
IF OBJECT_ID('dbo.Conversations',  'U') IS NOT NULL DROP TABLE dbo.Conversations;
IF OBJECT_ID('dbo.OrderItems',     'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders',         'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.CartItems',      'U') IS NOT NULL DROP TABLE dbo.CartItems;
IF OBJECT_ID('dbo.Carts',          'U') IS NOT NULL DROP TABLE dbo.Carts;
IF OBJECT_ID('dbo.Products',       'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories',     'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users',          'U') IS NOT NULL DROP TABLE dbo.Users;
GO


-- ============================================================
-- STEP 3: Create Tables
-- ============================================================

-- ------------------------------------------------------------
-- Users
-- Role:          'Buyer' | 'Seller' | 'Admin'
-- AccountStatus: 'Pending' | 'Active' | 'Banned'
-- ------------------------------------------------------------
CREATE TABLE dbo.Users (
    UserId          INT            IDENTITY(1,1)  NOT NULL,
    Username        NVARCHAR(50)                  NOT NULL,
    Email           NVARCHAR(150)                 NOT NULL,
    PasswordHash    NVARCHAR(256)                 NOT NULL,
    FullName        NVARCHAR(100)                 NOT NULL,
    ProfileImage    NVARCHAR(300)                     NULL,
    Bio             NVARCHAR(500)                     NULL,
    Role            NVARCHAR(20)                  NOT NULL  DEFAULT 'Buyer',
    AccountStatus   NVARCHAR(20)                  NOT NULL  DEFAULT 'Pending',
    BanReason       NVARCHAR(500)                     NULL,
    IsSeller        BIT                           NOT NULL  DEFAULT 0,
    IsActive        BIT                           NOT NULL  DEFAULT 1,
    CreatedAt       DATETIME                      NOT NULL  DEFAULT GETDATE(),
    UpdatedAt       DATETIME                          NULL,

    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE      (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE      (Email),
    CONSTRAINT CK_Users_Role     CHECK       (Role          IN ('Buyer', 'Seller', 'Admin')),
    CONSTRAINT CK_Users_Status   CHECK       (AccountStatus IN ('Pending', 'Active', 'Banned'))
);
GO

-- ------------------------------------------------------------
-- Categories
-- Art types / mediums available on the platform
-- ------------------------------------------------------------
CREATE TABLE dbo.Categories (
    CategoryId   INT           IDENTITY(1,1)  NOT NULL,
    Name         NVARCHAR(100)                NOT NULL,
    Description  NVARCHAR(300)                    NULL,

    CONSTRAINT PK_Categories       PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Name  UNIQUE      (Name)
);
GO

-- ------------------------------------------------------------
-- Products
-- Artworks listed by sellers
-- ------------------------------------------------------------
CREATE TABLE dbo.Products (
    ProductId    INT             IDENTITY(1,1)  NOT NULL,
    SellerId     INT                            NOT NULL,
    CategoryId   INT                            NOT NULL,
    Name         NVARCHAR(200)                  NOT NULL,
    Description  NVARCHAR(2000)                     NULL,
    Price        DECIMAL(10,2)                  NOT NULL,
    Stock        INT                            NOT NULL  DEFAULT 1,
    ImagePath    NVARCHAR(300)                      NULL,
    Medium       NVARCHAR(100)                      NULL,  -- e.g. "Digital Art", "Charcoal"
    Dimensions   NVARCHAR(100)                      NULL,  -- e.g. "763x763 cm"
    IsFlashSale  BIT                            NOT NULL  DEFAULT 0,
    IsAvailable  BIT                            NOT NULL  DEFAULT 1,
    CreatedAt    DATETIME                       NOT NULL  DEFAULT GETDATE(),
    UpdatedAt    DATETIME                           NULL,

    CONSTRAINT PK_Products           PRIMARY KEY (ProductId),
    CONSTRAINT FK_Products_Seller    FOREIGN KEY (SellerId)   REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Products_Category  FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT CK_Products_Price     CHECK       (Price >= 0),
    CONSTRAINT CK_Products_Stock     CHECK       (Stock >= 0)
);
GO

-- ------------------------------------------------------------
-- Carts
-- One cart per user (persistent)
-- ------------------------------------------------------------
CREATE TABLE dbo.Carts (
    CartId     INT      IDENTITY(1,1)  NOT NULL,
    UserId     INT                     NOT NULL,
    CreatedAt  DATETIME               NOT NULL  DEFAULT GETDATE(),

    CONSTRAINT PK_Carts        PRIMARY KEY (CartId),
    CONSTRAINT FK_Carts_User   FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Carts_User   UNIQUE      (UserId)  -- one cart per user
);
GO

-- ------------------------------------------------------------
-- CartItems
-- Individual product lines inside a cart
-- ------------------------------------------------------------
CREATE TABLE dbo.CartItems (
    CartItemId  INT      IDENTITY(1,1)  NOT NULL,
    CartId      INT                     NOT NULL,
    ProductId   INT                     NOT NULL,
    Quantity    INT                     NOT NULL  DEFAULT 1,
    AddedAt     DATETIME               NOT NULL  DEFAULT GETDATE(),

    CONSTRAINT PK_CartItems          PRIMARY KEY (CartItemId),
    CONSTRAINT FK_CartItems_Cart     FOREIGN KEY (CartId)    REFERENCES dbo.Carts(CartId),
    CONSTRAINT FK_CartItems_Product  FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
    CONSTRAINT CK_CartItems_Qty      CHECK       (Quantity >= 1)
);
GO

-- ------------------------------------------------------------
-- Orders
-- Status flow: ToPay > ToShip > ToReceive > Completed
-- ------------------------------------------------------------
CREATE TABLE dbo.Orders (
    OrderId        INT             IDENTITY(1,1)  NOT NULL,
    BuyerId        INT                            NOT NULL,
    FullName       NVARCHAR(100)                  NOT NULL,
    Address        NVARCHAR(500)                  NOT NULL,
    ContactNumber  NVARCHAR(20)                   NOT NULL,
    PaymentMethod  NVARCHAR(50)                   NOT NULL,  -- 'COD' | 'GCash'
    TotalAmount    DECIMAL(10,2)                  NOT NULL,
    Status         NVARCHAR(50)                   NOT NULL  DEFAULT 'ToPay',
    CreatedAt      DATETIME                       NOT NULL  DEFAULT GETDATE(),
    UpdatedAt      DATETIME                           NULL,

    CONSTRAINT PK_Orders               PRIMARY KEY (OrderId),
    CONSTRAINT FK_Orders_Buyer         FOREIGN KEY (BuyerId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Orders_Payment       CHECK       (PaymentMethod IN ('COD', 'GCash')),
    CONSTRAINT CK_Orders_Status        CHECK       (Status IN ('ToPay', 'ToShip', 'ToReceive', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Orders_TotalAmount   CHECK       (TotalAmount >= 0)
);
GO

-- ------------------------------------------------------------
-- OrderItems
-- Individual product lines inside an order
-- Subtotal is auto-calculated as a persisted computed column
-- ------------------------------------------------------------
CREATE TABLE dbo.OrderItems (
    OrderItemId  INT            IDENTITY(1,1)  NOT NULL,
    OrderId      INT                           NOT NULL,
    ProductId    INT                           NOT NULL,
    Quantity     INT                           NOT NULL,
    UnitPrice    DECIMAL(10,2)                 NOT NULL,
    Subtotal     AS (Quantity * UnitPrice)     PERSISTED,

    CONSTRAINT PK_OrderItems          PRIMARY KEY (OrderItemId),
    CONSTRAINT FK_OrderItems_Order    FOREIGN KEY (OrderId)    REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderItems_Product  FOREIGN KEY (ProductId)  REFERENCES dbo.Products(ProductId),
    CONSTRAINT CK_OrderItems_Qty      CHECK       (Quantity >= 1),
    CONSTRAINT CK_OrderItems_Price    CHECK       (UnitPrice >= 0)
);
GO

-- ------------------------------------------------------------
-- Conversations
-- One conversation thread per buyer-seller pair
-- ------------------------------------------------------------
CREATE TABLE dbo.Conversations (
    ConversationId  INT      IDENTITY(1,1)  NOT NULL,
    BuyerId         INT                     NOT NULL,
    SellerId        INT                     NOT NULL,
    CreatedAt       DATETIME               NOT NULL  DEFAULT GETDATE(),

    CONSTRAINT PK_Conversations          PRIMARY KEY (ConversationId),
    CONSTRAINT FK_Conversations_Buyer    FOREIGN KEY (BuyerId)  REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Conversations_Seller   FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId)
);
GO

-- ------------------------------------------------------------
-- Messages
-- Individual chat messages inside a conversation
-- ------------------------------------------------------------
CREATE TABLE dbo.Messages (
    MessageId       INT            IDENTITY(1,1)  NOT NULL,
    ConversationId  INT                           NOT NULL,
    SenderId        INT                           NOT NULL,
    Body            NVARCHAR(2000)                NOT NULL,
    IsRead          BIT                           NOT NULL  DEFAULT 0,
    SentAt          DATETIME                      NOT NULL  DEFAULT GETDATE(),

    CONSTRAINT PK_Messages                PRIMARY KEY (MessageId),
    CONSTRAINT FK_Messages_Conversation   FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(ConversationId),
    CONSTRAINT FK_Messages_Sender         FOREIGN KEY (SenderId)       REFERENCES dbo.Users(UserId)
);
GO

-- ------------------------------------------------------------
-- Notifications
-- Type: 'Purchase' | 'Message' | 'LowStock' | 'Sale'
-- ------------------------------------------------------------
CREATE TABLE dbo.Notifications (
    NotificationId  INT            IDENTITY(1,1)  NOT NULL,
    UserId          INT                           NOT NULL,
    Title           NVARCHAR(200)                 NOT NULL,
    Body            NVARCHAR(500)                 NOT NULL,
    Type            NVARCHAR(50)                  NOT NULL,
    IsRead          BIT                           NOT NULL  DEFAULT 0,
    CreatedAt       DATETIME                      NOT NULL  DEFAULT GETDATE(),

    CONSTRAINT PK_Notifications        PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_User   FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Notifications_Type   CHECK       (Type IN ('Purchase', 'Message', 'LowStock', 'Sale'))
);
GO


-- ============================================================
-- STEP 4: Indexes (for common query patterns)
-- ============================================================

-- Find all products by a seller
CREATE INDEX IX_Products_SellerId   ON dbo.Products(SellerId);

-- Filter products by category
CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

-- Find cart items by cart
CREATE INDEX IX_CartItems_CartId    ON dbo.CartItems(CartId);

-- Find orders by buyer
CREATE INDEX IX_Orders_BuyerId      ON dbo.Orders(BuyerId);

-- Find order items by order
CREATE INDEX IX_OrderItems_OrderId  ON dbo.OrderItems(OrderId);

-- Find messages by conversation
CREATE INDEX IX_Messages_ConvId     ON dbo.Messages(ConversationId);

-- Find notifications by user (unread first)
CREATE INDEX IX_Notifications_UserId ON dbo.Notifications(UserId, IsRead);

-- Find conversations by buyer or seller
CREATE INDEX IX_Conversations_BuyerId  ON dbo.Conversations(BuyerId);
CREATE INDEX IX_Conversations_SellerId ON dbo.Conversations(SellerId);

-- Find users by account status (for Admin pending/ban queries)
CREATE INDEX IX_Users_AccountStatus ON dbo.Users(AccountStatus);
GO


-- ============================================================
-- STEP 5: Seed Data
-- ============================================================

-- ------------------------------------------------------------
-- Categories (art mediums from the wireframe filter sidebar)
-- ------------------------------------------------------------
INSERT INTO dbo.Categories (Name, Description) VALUES
('Digital Art',      'Digitally created artworks using software tools'),
('Oil Painting',     'Traditional oil on canvas artworks'),
('Watercolor',       'Watercolor paintings on paper or canvas'),
('Sketch / Drawing', 'Pencil, charcoal, ink, and graphite sketches'),
('Photography',      'Fine art photography prints'),
('Mixed Media',      'Combination of multiple art forms and materials'),
('Charcoal',         'Charcoal drawings on paper'),
('Colored Pencil',   'Illustrations using colored pencils'),
('Graphite',         'Graphite pencil artworks'),
('Pastel',           'Soft or oil pastel artworks'),
('Ink',              'Ink-based illustrations and calligraphy'),
('Others',           'Other art mediums not listed above');
GO

-- ------------------------------------------------------------
-- Default Admin Account
--
-- Username: admin
-- Password: Admin@1234
--
-- NOTE: This PasswordHash is a placeholder.
--       Replace it with a real PBKDF2 hash generated by
--       ASP.NET Identity's PasswordHasher before going live.
--       You can generate it by calling:
--         var hasher = new PasswordHasher();
--         string hash = hasher.HashPassword("Admin@1234");
--       Then UPDATE this row with the real hash.
-- ------------------------------------------------------------
INSERT INTO dbo.Users (
    Username,
    Email,
    PasswordHash,
    FullName,
    Role,
    AccountStatus,
    IsSeller,
    IsActive
) VALUES (
    'admin',
    'admin@qcu.edu.ph',
    'REPLACE_WITH_ASPNET_IDENTITY_PBKDF2_HASH',
    'ArtZada Administrator',
    'Admin',
    'Active',
    0,
    1
);
GO

-- ------------------------------------------------------------
-- Sample Seller Account (for testing)
--
-- Username: kwekkweksastix
-- Password: Test@1234  (replace hash before use)
-- ------------------------------------------------------------
INSERT INTO dbo.Users (
    Username,
    Email,
    PasswordHash,
    FullName,
    Role,
    AccountStatus,
    IsSeller,
    IsActive,
    Bio
) VALUES (
    'kwekkweksastix',
    'kwek@qcu.edu.ph',
    'REPLACE_WITH_ASPNET_IDENTITY_PBKDF2_HASH',
    'KwekKweksaStix',
    'Seller',
    'Active',
    1,
    1,
    'Digital Artist · Illustrator · Concept Artist'
);
GO

-- ------------------------------------------------------------
-- Sample Buyer Account (for testing)
--
-- Username: pamili_is_love
-- Password: Test@1234  (replace hash before use)
-- ------------------------------------------------------------
INSERT INTO dbo.Users (
    Username,
    Email,
    PasswordHash,
    FullName,
    Role,
    AccountStatus,
    IsSeller,
    IsActive
) VALUES (
    'pamili_is_love',
    'pamili@qcu.edu.ph',
    'REPLACE_WITH_ASPNET_IDENTITY_PBKDF2_HASH',
    'Pamili Is Love',
    'Buyer',
    'Active',
    0,
    1
);
GO

-- ------------------------------------------------------------
-- Sample Products (linked to seller UserId = 2)
-- ------------------------------------------------------------
INSERT INTO dbo.Products (
    SellerId, CategoryId, Name, Description,
    Price, Stock, Medium, Dimensions, IsFlashSale, IsAvailable
) VALUES
(2, 1, 'Chubb ang Matabang Aso',
 'A digital illustration of a chubby Shiba Inu in a cool pose.',
 100.00, 10, 'Digital Art', '763x763 cm', 0, 1),

(2, 1, 'Boss',
 'A bold digital concept art piece.',
 250.00, 10, 'Digital Art', '1024x1024 px', 0, 1),

(2, 4, 'Drawing Commission',
 'Custom sketch commission. Send a reference photo and get a hand-drawn portrait.',
 350.00, 5, 'Graphite', 'A4 size', 1, 1);
GO

-- ------------------------------------------------------------
-- Sample Cart for Buyer (UserId = 3)
-- ------------------------------------------------------------
INSERT INTO dbo.Carts (UserId) VALUES (3);
GO

-- Cart item: Buyer added "Chubb" (ProductId = 1) to cart
INSERT INTO dbo.CartItems (CartId, ProductId, Quantity)
VALUES (1, 1, 1);
GO

-- ------------------------------------------------------------
-- Sample Conversation between Buyer (3) and Seller (2)
-- ------------------------------------------------------------
INSERT INTO dbo.Conversations (BuyerId, SellerId)
VALUES (3, 2);
GO

INSERT INTO dbo.Messages (ConversationId, SenderId, Body, IsRead) VALUES
(1, 2, 'Kwekkwek ka ba?',        0),
(1, 3, 'Bakit?',                 1),
(1, 2, 'Sarap mo kasi tusukin',  0),
(1, 3, 'haha',                   1);
GO

-- ------------------------------------------------------------
-- Sample Notifications
-- ------------------------------------------------------------
INSERT INTO dbo.Notifications (UserId, Title, Body, Type, IsRead) VALUES
-- Buyer notification: message reply
(3, 'New Message', 'KwekKweksaStix has replied to your message', 'Message', 0),

-- Buyer notification: purchase success
(3, 'Order Confirmed', 'Chubb has been purchased successfully!', 'Purchase', 0),

-- Seller notification: low stock warning
(2, 'Low Stock Warning', 'Warning: Only 3 pieces of Chubb remaining!', 'LowStock', 0),

-- Seller notification: item sold
(2, 'Item Sold', 'Chubb has been sold successfully!', 'Sale', 0);
GO


-- ============================================================
-- STEP 6: Verify — show row counts for all tables
-- ============================================================

SELECT 'Users'         AS TableName, COUNT(*) AS TotalRows FROM dbo.Users
UNION ALL
SELECT 'Categories',                 COUNT(*)             FROM dbo.Categories
UNION ALL
SELECT 'Products',                   COUNT(*)             FROM dbo.Products
UNION ALL
SELECT 'Carts',                      COUNT(*)             FROM dbo.Carts
UNION ALL
SELECT 'CartItems',                  COUNT(*)             FROM dbo.CartItems
UNION ALL
SELECT 'Orders',                     COUNT(*)             FROM dbo.Orders
UNION ALL
SELECT 'OrderItems',                 COUNT(*)             FROM dbo.OrderItems
UNION ALL
SELECT 'Conversations',              COUNT(*)             FROM dbo.Conversations
UNION ALL
SELECT 'Messages',                   COUNT(*)             FROM dbo.Messages
UNION ALL
SELECT 'Notifications',              COUNT(*)             FROM dbo.Notifications;
GO

-- ============================================================
-- Setup complete.
-- Expected row counts after running this script:
--   Users         = 3  (admin, seller, buyer)
--   Categories    = 12
--   Products      = 3
--   Carts         = 1
--   CartItems     = 1
--   Orders        = 0  (no orders yet)
--   OrderItems    = 0
--   Conversations = 1
--   Messages      = 4
--   Notifications = 4
-- ============================================================