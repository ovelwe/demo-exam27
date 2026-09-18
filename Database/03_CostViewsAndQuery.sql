USE DemoExam2027;
GO

IF OBJECT_ID('dbo.v_OrderProductionCost','V') IS NOT NULL
    DROP VIEW dbo.v_OrderProductionCost;
GO

CREATE VIEW dbo.v_OrderProductionCost
AS
WITH CurrentPrices AS
(
    SELECT p.NomenclatureId, p.Price,
           ROW_NUMBER() OVER(PARTITION BY p.NomenclatureId ORDER BY p.EffectiveFrom DESC, p.PriceId DESC) AS rn
    FROM dbo.Prices p
),
MaterialCostByLine AS
(
    SELECT ol.OrderLineId,
           SUM(ol.Quantity * pm.QuantityPerProduct * cp.Price) AS MaterialCost
    FROM dbo.SalesOrderLines ol
    JOIN dbo.ProductMaterials pm ON pm.ProductId = ol.ProductId
    JOIN CurrentPrices cp ON cp.NomenclatureId = pm.MaterialId AND cp.rn = 1
    GROUP BY ol.OrderLineId
),
OperationCostByLine AS
(
    SELECT ol.OrderLineId,
           SUM(ol.Quantity * po.OperationCount * cp.Price) AS OperationCost
    FROM dbo.SalesOrderLines ol
    JOIN dbo.ProductOperations po ON po.ProductId = ol.ProductId
    JOIN CurrentPrices cp ON cp.NomenclatureId = po.OperationId AND cp.rn = 1
    GROUP BY ol.OrderLineId
)
SELECT o.OrderId,
       o.OrderNumber,
       o.OrderDate,
       c.Name AS CustomerName,
       CAST(SUM(ISNULL(mc.MaterialCost, 0)) AS DECIMAL(18,2)) AS MaterialCost,
       CAST(SUM(ISNULL(oc.OperationCost, 0)) AS DECIMAL(18,2)) AS OperationCost,
       CAST(SUM(ISNULL(mc.MaterialCost, 0) + ISNULL(oc.OperationCost, 0)) AS DECIMAL(18,2)) AS TotalCost
FROM dbo.SalesOrders o
JOIN dbo.Counterparties c ON c.CounterpartyId = o.CustomerId
JOIN dbo.SalesOrderLines ol ON ol.OrderId = o.OrderId
LEFT JOIN MaterialCostByLine mc ON mc.OrderLineId = ol.OrderLineId
LEFT JOIN OperationCostByLine oc ON oc.OrderLineId = ol.OrderLineId
GROUP BY o.OrderId, o.OrderNumber, o.OrderDate, c.Name;
GO

-- Главный запрос задания 3: полная себестоимость заказа.
SELECT *
FROM dbo.v_OrderProductionCost
ORDER BY OrderId;
GO

-- Детализация материалов по заказу №1.
DECLARE @OrderId INT = 1;

WITH CurrentPrices AS
(
    SELECT p.NomenclatureId, p.Price,
           ROW_NUMBER() OVER(PARTITION BY p.NomenclatureId ORDER BY p.EffectiveFrom DESC, p.PriceId DESC) AS rn
    FROM dbo.Prices p
)
SELECT n.Name AS [Материал],
       SUM(ol.Quantity * pm.QuantityPerProduct) AS [Количество],
       u.Symbol AS [Ед. изм.],
       cp.Price AS [Цена],
       SUM(ol.Quantity * pm.QuantityPerProduct * cp.Price) AS [Сумма]
FROM dbo.SalesOrderLines ol
JOIN dbo.ProductMaterials pm ON pm.ProductId = ol.ProductId
JOIN dbo.Nomenclature n ON n.NomenclatureId = pm.MaterialId
JOIN dbo.Units u ON u.UnitId = n.UnitId
JOIN CurrentPrices cp ON cp.NomenclatureId = n.NomenclatureId AND cp.rn = 1
WHERE ol.OrderId = @OrderId
GROUP BY n.Name, u.Symbol, cp.Price
ORDER BY n.Name;
GO
