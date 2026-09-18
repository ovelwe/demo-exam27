IF DB_ID(N'DemoExam2027') IS NULL
    CREATE DATABASE DemoExam2027;
GO

USE DemoExam2027;
GO

IF OBJECT_ID('dbo.ProductionOrderLines','U') IS NOT NULL DROP TABLE dbo.ProductionOrderLines;
IF OBJECT_ID('dbo.ProductionOrders','U') IS NOT NULL DROP TABLE dbo.ProductionOrders;
IF OBJECT_ID('dbo.SalesOrderLines','U') IS NOT NULL DROP TABLE dbo.SalesOrderLines;
IF OBJECT_ID('dbo.SalesOrders','U') IS NOT NULL DROP TABLE dbo.SalesOrders;
IF OBJECT_ID('dbo.ProductOperations','U') IS NOT NULL DROP TABLE dbo.ProductOperations;
IF OBJECT_ID('dbo.ProductMaterials','U') IS NOT NULL DROP TABLE dbo.ProductMaterials;
IF OBJECT_ID('dbo.Prices','U') IS NOT NULL DROP TABLE dbo.Prices;
IF OBJECT_ID('dbo.Nomenclature','U') IS NOT NULL DROP TABLE dbo.Nomenclature;
IF OBJECT_ID('dbo.Counterparties','U') IS NOT NULL DROP TABLE dbo.Counterparties;
IF OBJECT_ID('dbo.Units','U') IS NOT NULL DROP TABLE dbo.Units;
GO

CREATE TABLE dbo.Units
(
    UnitId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Symbol NVARCHAR(20) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Counterparties
(
    CounterpartyId INT IDENTITY(1,1) PRIMARY KEY,
    ExternalId NVARCHAR(30) NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Inn NVARCHAR(20) NULL,
    Address NVARCHAR(300) NULL,
    Phone NVARCHAR(50) NULL,
    CounterpartyType NVARCHAR(30) NOT NULL,
    CONSTRAINT CK_Counterparties_Type CHECK (CounterpartyType IN (N'Покупатель', N'Поставщик'))
);
GO

CREATE TABLE dbo.Nomenclature
(
    NomenclatureId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(30) NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    NomenclatureType NVARCHAR(30) NOT NULL,
    UnitId INT NOT NULL,
    CONSTRAINT FK_Nomenclature_Units FOREIGN KEY(UnitId) REFERENCES dbo.Units(UnitId),
    CONSTRAINT CK_Nomenclature_Type CHECK (NomenclatureType IN (N'Продукция', N'Материал', N'Операция'))
);
GO

CREATE TABLE dbo.Prices
(
    PriceId INT IDENTITY(1,1) PRIMARY KEY,
    NomenclatureId INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    EffectiveFrom DATE NOT NULL CONSTRAINT DF_Prices_EffectiveFrom DEFAULT (CONVERT(date,GETDATE())),
    CONSTRAINT FK_Prices_Nomenclature FOREIGN KEY(NomenclatureId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT CK_Prices_Positive CHECK (Price >= 0)
);
GO

-- Материальная часть спецификации изделия.
CREATE TABLE dbo.ProductMaterials
(
    ProductId INT NOT NULL,
    MaterialId INT NOT NULL,
    QuantityPerProduct DECIMAL(18,4) NOT NULL,
    CONSTRAINT PK_ProductMaterials PRIMARY KEY(ProductId, MaterialId),
    CONSTRAINT FK_ProductMaterials_Product FOREIGN KEY(ProductId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT FK_ProductMaterials_Material FOREIGN KEY(MaterialId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT CK_ProductMaterials_Qty CHECK (QuantityPerProduct > 0)
);
GO

-- Технологические операции на одну единицу продукции.
CREATE TABLE dbo.ProductOperations
(
    ProductId INT NOT NULL,
    OperationId INT NOT NULL,
    NormTimeHours DECIMAL(18,4) NOT NULL,
    OperationCount DECIMAL(18,4) NOT NULL,
    CONSTRAINT PK_ProductOperations PRIMARY KEY(ProductId, OperationId),
    CONSTRAINT FK_ProductOperations_Product FOREIGN KEY(ProductId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT FK_ProductOperations_Operation FOREIGN KEY(OperationId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT CK_ProductOperations_Norm CHECK (NormTimeHours >= 0),
    CONSTRAINT CK_ProductOperations_Count CHECK (OperationCount > 0)
);
GO

CREATE TABLE dbo.SalesOrders
(
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(30) NOT NULL UNIQUE,
    OrderDate DATE NOT NULL,
    CustomerId INT NOT NULL,
    CONSTRAINT FK_SalesOrders_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Counterparties(CounterpartyId)
);
GO

CREATE TABLE dbo.SalesOrderLines
(
    OrderLineId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UnitSalePrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesOrderLines_Discount DEFAULT 0,
    CONSTRAINT FK_SalesOrderLines_Order FOREIGN KEY(OrderId) REFERENCES dbo.SalesOrders(OrderId),
    CONSTRAINT FK_SalesOrderLines_Product FOREIGN KEY(ProductId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT CK_SalesOrderLines_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_SalesOrderLines_Price CHECK (UnitSalePrice >= 0),
    CONSTRAINT CK_SalesOrderLines_Discount CHECK (DiscountAmount >= 0)
);
GO


CREATE TABLE dbo.ProductionOrders
(
    ProductionOrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(30) NOT NULL UNIQUE,
    LaunchDate DATE NOT NULL,
    Department NVARCHAR(150) NOT NULL
);
GO

CREATE TABLE dbo.ProductionOrderLines
(
    ProductionOrderLineId INT IDENTITY(1,1) PRIMARY KEY,
    ProductionOrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    CONSTRAINT FK_ProductionOrderLines_Order FOREIGN KEY(ProductionOrderId) REFERENCES dbo.ProductionOrders(ProductionOrderId),
    CONSTRAINT FK_ProductionOrderLines_Product FOREIGN KEY(ProductId) REFERENCES dbo.Nomenclature(NomenclatureId),
    CONSTRAINT CK_ProductionOrderLines_Qty CHECK (Quantity > 0)
);
GO

CREATE INDEX IX_Prices_NomenclatureDate ON dbo.Prices(NomenclatureId, EffectiveFrom DESC);
CREATE INDEX IX_SalesOrderLines_OrderId ON dbo.SalesOrderLines(OrderId);
GO
