-- One row per well per month with its produced oil, gas, water, and condensate side by side.
-- Volumes are m3 for liquids and e3m3 for gas. A masked volume is NULL and is left out of the sums.
-- ProducingHours is the highest hours on any of the well's rows, because Petrinex reports hours on one product row only.
CREATE OR ALTER VIEW dbo.vw_MonthlyWellProduction
AS
SELECT ProductionMonth,
       FromToId                 AS WellId,
       MIN(ReportingFacilityId) AS FacilityId,
       MIN(OperatorBaId)        AS OperatorBaId,
       SUM(CASE WHEN ProductId = 'OIL'   THEN Volume END) AS OilVolume,
       SUM(CASE WHEN ProductId = 'GAS'   THEN Volume END) AS GasVolume,
       SUM(CASE WHEN ProductId = 'WATER' THEN Volume END) AS WaterVolume,
       SUM(CASE WHEN ProductId = 'COND'  THEN Volume END) AS CondensateVolume,
       MAX(Hours)                    AS ProducingHours,
       CAST(MAX(CAST(IsMasked AS int)) AS bit) AS IsMasked
FROM dbo.VolumetricRecords
WHERE FromToIdType = 'WI'
  AND ActivityId = 'PROD'
GROUP BY ProductionMonth, FromToId;
