-- Inventory Management System - MySQL 8.0+ schema (idempotent).
-- The application creates the database named in appsettings.json and runs this script on startup.
-- To run it manually:
--   CREATE DATABASE IF NOT EXISTS inventory_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--   USE inventory_db;
--   (then execute this file)

CREATE TABLE IF NOT EXISTS Categories (
    CategoryID  INT          NOT NULL AUTO_INCREMENT,
    Name        VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    PRIMARY KEY (CategoryID),
    UNIQUE KEY UQ_Categories_Name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Parts (
    PartID          INT           NOT NULL AUTO_INCREMENT,
    CategoryID      INT           NULL,
    SKU             VARCHAR(50)   NOT NULL,
    Name            VARCHAR(150)  NOT NULL,
    UnitPrice       DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    QuantityInStock INT           NOT NULL DEFAULT 0,
    MinThreshold    INT           NOT NULL DEFAULT 5,
    PRIMARY KEY (PartID),
    UNIQUE KEY UQ_Parts_SKU (SKU),
    KEY IX_Parts_Category (CategoryID),
    KEY IX_Parts_Name (Name),
    CONSTRAINT FK_Parts_Categories FOREIGN KEY (CategoryID)
        REFERENCES Categories (CategoryID) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT CK_Parts_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_Parts_Quantity  CHECK (QuantityInStock >= 0),
    CONSTRAINT CK_Parts_Threshold CHECK (MinThreshold >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Stock_Movements (
    MovementID   INT                            NOT NULL AUTO_INCREMENT,
    PartID       INT                            NOT NULL,
    MovementType ENUM('IN','OUT','ADJUSTMENT')  NOT NULL,
    Quantity     INT                            NOT NULL,
    MovementDate TIMESTAMP                      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes        VARCHAR(255)                   NULL,
    PRIMARY KEY (MovementID),
    KEY IX_Movements_Part (PartID, MovementDate),
    CONSTRAINT FK_Movements_Parts FOREIGN KEY (PartID)
        REFERENCES Parts (PartID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Starter categories (ignored if they already exist).
INSERT IGNORE INTO Categories (Name, Description) VALUES
    ('Electronics', 'Electronic components and devices'),
    ('Mechanical',  'Mechanical parts and hardware'),
    ('Consumables', 'Items that are regularly used up'),
    ('Tools',       'Hand and power tools'),
    ('Other',       'Uncategorized items');
