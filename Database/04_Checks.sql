USE DemoExam2027;
GO

-- 1. Проверка справочников.
SELECT * FROM dbo.Counterparties ORDER BY Name;
SELECT * FROM dbo.Nomenclature ORDER BY NomenclatureType, Name;

-- 2. Проверка спецификации изделия.
SELECT p.Name AS Product, m.Name AS Material, pm.QuantityPerProduct, u.Symbol
FROM dbo.ProductMaterials pm
JOIN dbo.Nomenclature p ON p.NomenclatureId=pm.ProductId
JOIN dbo.Nomenclature m ON m.NomenclatureId=pm.MaterialId
JOIN dbo.Units u ON u.UnitId=m.UnitId;

SELECT p.Name AS Product, op.Name AS Operation, po.NormTimeHours, po.OperationCount
FROM dbo.ProductOperations po
JOIN dbo.Nomenclature p ON p.NomenclatureId=po.ProductId
JOIN dbo.Nomenclature op ON op.NomenclatureId=po.OperationId;

-- 3. Проверка итогового расчета.
SELECT * FROM dbo.v_OrderProductionCost;
GO
