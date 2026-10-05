-- RAT-01: water cut or gas-oil ratio outside the configured bounds.
-- Conditional aggregation pivots each well's product rows into oil, gas, and water columns in one pass.
-- Water cut = water / (oil + water). Gas-oil ratio = gas / oil in m3 per m3 (gas is reported in e3m3, so x 1000).
-- Wells below the minimum oil volume are skipped: a ratio over almost no oil says nothing.
-- Parameters: @ProductionMonth date, @MaxWaterCut decimal, @MaxGasOilRatio decimal, @MinimumVolume decimal
WITH WellTotals AS
(
    SELECT FromToId AS WellId,
           MIN(ReportingFacilityId) AS FacilityId,
           SUM(CASE WHEN ProductId = 'OIL'   THEN Volume END) AS Oil,
           SUM(CASE WHEN ProductId = 'GAS'   THEN Volume END) AS Gas,
           SUM(CASE WHEN ProductId = 'WATER' THEN Volume END) AS Water
    FROM dbo.VolumetricRecords
    WHERE ProductionMonth = @ProductionMonth
      AND FromToIdType = 'WI'
      AND ActivityId = 'PROD'
    GROUP BY FromToId
),
Ratios AS
(
    SELECT WellId, FacilityId, Oil, Gas, Water,
           Water / NULLIF(Oil + Water, 0) AS WaterCut,
           Gas * 1000.0 / NULLIF(Oil, 0)  AS GasOilRatio
    FROM WellTotals
    WHERE Oil >= @MinimumVolume
)
SELECT FacilityId, WellId,
       'WaterCut' AS Field,
       CAST(CAST(WaterCut AS decimal(9, 4)) AS nvarchar(150)) AS RawValue,
       CONCAT('Water cut ', CAST(WaterCut * 100 AS decimal(9, 1)), '% exceeds the limit (oil ', Oil, ', water ', Water, ').') AS Message
FROM Ratios
WHERE WaterCut > @MaxWaterCut

UNION ALL

SELECT FacilityId, WellId,
       'GasOilRatio' AS Field,
       CAST(CAST(GasOilRatio AS decimal(18, 1)) AS nvarchar(150)) AS RawValue,
       CONCAT('Gas-oil ratio ', CAST(GasOilRatio AS decimal(18, 0)), ' m3/m3 exceeds the limit (oil ', Oil, ', gas ', Gas, ').') AS Message
FROM Ratios
WHERE GasOilRatio > @MaxGasOilRatio;
