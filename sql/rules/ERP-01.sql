-- ERP-01: wells that produced in the month but have no ERP cost centre mapping.
-- An anti-join from the monthly production view to the mapping table.
-- Nothing is reported until at least one mapping exists: before that, ERP integration has not been set up
-- and every well would be flagged.
-- Parameters: @ProductionMonth date
SELECT production.FacilityId,
       production.WellId,
       'CostCentre' AS Field,
       CAST(NULL AS nvarchar(150)) AS RawValue,
       CONCAT('Well ', production.WellId, ' has no ERP cost centre and will be exported as UNMAPPED.') AS Message
FROM dbo.vw_MonthlyWellProduction AS production
LEFT JOIN dbo.ErpCostCentreMappings AS mapping
       ON mapping.WellId = production.WellId
WHERE production.ProductionMonth = @ProductionMonth
  AND mapping.Id IS NULL
  AND EXISTS (SELECT 1 FROM dbo.ErpCostCentreMappings);
