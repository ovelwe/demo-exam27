USE DemoExam2027;
GO

INSERT INTO dbo.Units(Name, Symbol)
VALUES (N'Штука', N'шт'),
       (N'Тысяча штук', N'тыс. шт'),
       (N'Час', N'ч');
GO

DECLARE @uPiece INT = (SELECT UnitId FROM dbo.Units WHERE Symbol=N'шт');
DECLARE @uThousand INT = (SELECT UnitId FROM dbo.Units WHERE Symbol=N'тыс. шт');
DECLARE @uHour INT = (SELECT UnitId FROM dbo.Units WHERE Symbol=N'ч');

-- Покупатель с примера "Заказ покупателя".
INSERT INTO dbo.Counterparties(ExternalId, Name, Inn, Address, Phone, CounterpartyType)
VALUES (N'DEMO-CUSTOMER', N'ИП Томилин Александр Сергеевич', NULL, NULL, NULL, N'Покупатель');

-- Номенклатура по предоставленным скриншотам.
INSERT INTO dbo.Nomenclature(Code, Name, NomenclatureType, UnitId) VALUES
(N'ФР-00000034', N'Стол кухонный "Самобранка"', N'Продукция', @uPiece),
(N'ФР-00000009', N'Столешница круглая', N'Материал', @uPiece),
(N'ФР-00000013', N'Мебельная деталь 500x800', N'Материал', @uPiece),
(N'ФР-00000016', N'Мебельная деталь 600x800', N'Материал', @uPiece),
(N'ФР-00000027', N'Евровинт 6,5x5', N'Материал', @uThousand),
(N'ФР-00000026', N'Опора', N'Материал', @uPiece),
(N'ФР-00000053', N'Сборка модулей', N'Операция', @uHour),
(N'ФР-00000052', N'Распил ДСП, МДФ и листового материала', N'Операция', @uHour),
(N'ФР-00000494', N'Упаковка', N'Операция', @uHour);
GO

-- Прайс-лист с первого предоставленного скриншота.
INSERT INTO dbo.Prices(NomenclatureId, Price, EffectiveFrom)
SELECT NomenclatureId,
       CASE Name
           WHEN N'Евровинт 6,5x5' THEN 595.00
           WHEN N'Мебельная деталь 500x800' THEN 95.00
           WHEN N'Мебельная деталь 600x800' THEN 140.00
           WHEN N'Опора' THEN 245.00
           WHEN N'Распил ДСП, МДФ и листового материала' THEN 450.00
           WHEN N'Сборка модулей' THEN 1400.00
           WHEN N'Столешница круглая' THEN 3250.00
           WHEN N'Упаковка' THEN 950.00
       END,
       '2026-04-01'
FROM dbo.Nomenclature
WHERE Name IN
(
 N'Евровинт 6,5x5', N'Мебельная деталь 500x800', N'Мебельная деталь 600x800',
 N'Опора', N'Распил ДСП, МДФ и листового материала', N'Сборка модулей',
 N'Столешница круглая', N'Упаковка'
);
GO

DECLARE @product INT = (SELECT NomenclatureId FROM dbo.Nomenclature WHERE Name=N'Стол кухонный "Самобранка"');

INSERT INTO dbo.ProductMaterials(ProductId, MaterialId, QuantityPerProduct)
SELECT @product, NomenclatureId,
       CASE Name
         WHEN N'Столешница круглая' THEN 1.000
         WHEN N'Мебельная деталь 500x800' THEN 2.000
         WHEN N'Мебельная деталь 600x800' THEN 4.000
         WHEN N'Евровинт 6,5x5' THEN 0.012
         WHEN N'Опора' THEN 4.000
       END
FROM dbo.Nomenclature
WHERE Name IN
(N'Столешница круглая', N'Мебельная деталь 500x800', N'Мебельная деталь 600x800', N'Евровинт 6,5x5', N'Опора');

INSERT INTO dbo.ProductOperations(ProductId, OperationId, NormTimeHours, OperationCount)
SELECT @product, NomenclatureId,
       CASE Name
         WHEN N'Сборка модулей' THEN 0.75
         WHEN N'Распил ДСП, МДФ и листового материала' THEN 1.50
         WHEN N'Упаковка' THEN 0.50
       END,
       1.0
FROM dbo.Nomenclature
WHERE Name IN (N'Сборка модулей', N'Распил ДСП, МДФ и листового материала', N'Упаковка');
GO

DECLARE @customer INT = (SELECT CounterpartyId FROM dbo.Counterparties WHERE ExternalId=N'DEMO-CUSTOMER');
DECLARE @product INT = (SELECT NomenclatureId FROM dbo.Nomenclature WHERE Name=N'Стол кухонный "Самобранка"');

INSERT INTO dbo.SalesOrders(OrderNumber, OrderDate, CustomerId)
VALUES (N'1', '2026-04-22', @customer);

DECLARE @orderId INT = SCOPE_IDENTITY();

INSERT INTO dbo.SalesOrderLines(OrderId, ProductId, Quantity, UnitSalePrice, DiscountAmount)
VALUES (@orderId, @product, 2.000, 14120.00, 1412.00);
GO


-- Заказ на производство №1 от 23.04.2026 со скриншота.
DECLARE @productForProduction INT = (SELECT NomenclatureId FROM dbo.Nomenclature WHERE Name=N'Стол кухонный "Самобранка"');
INSERT INTO dbo.ProductionOrders(OrderNumber, LaunchDate, Department)
VALUES (N'1', '2026-04-23', N'Основное подразделение');
DECLARE @productionOrderId INT = SCOPE_IDENTITY();
INSERT INTO dbo.ProductionOrderLines(ProductionOrderId, ProductId, Quantity)
VALUES (@productionOrderId, @productForProduction, 2.000);
GO
