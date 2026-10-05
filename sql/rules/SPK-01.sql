-- SPK-01: a well's production of a product jumped or fell sharply from the previous month.
-- LAG() fetches last month's volume for the same well and product. Only consecutive months are compared.
-- Very small volumes are ignored on both sides because they turn noise into huge percentages.
-- Parameters: @ProductionMonth date, @SpikeThresholdPercent decimal, @MinimumVolume decimal
WITH WellProductMonths AS
(
    SELECT FromToId AS WellId, ProductId, ProductionMonth,
           MIN(ReportingFacilityId) AS FacilityId,
           SUM(Volume)              AS Volume
    FROM dbo.VolumetricRecords
    WHERE FromToIdType = 'WI'
      AND ActivityId = 'PROD'
      AND Volume IS NOT NULL
      AND ProductionMonth <= @ProductionMonth
    GROUP BY FromToId, ProductId, ProductionMonth
),
WithPrevious AS
(
    SELECT WellId, ProductId, ProductionMonth, FacilityId, Volume,
           LAG(Volume)          OVER (PARTITION BY WellId, ProductId ORDER BY ProductionMonth) AS PreviousVolume,
           LAG(ProductionMonth) OVER (PARTITION BY WellId, ProductId ORDER BY ProductionMonth) AS PreviousMonth
    FROM WellProductMonths
),
Changes AS
(
    SELECT WellId, ProductId, FacilityId, Volume, PreviousVolume,
           (Volume - PreviousVolume) * 100.0 / PreviousVolume AS ChangePercent
    FROM WithPrevious
    WHERE ProductionMonth = @ProductionMonth
      AND DATEDIFF(MONTH, PreviousMonth, ProductionMonth) = 1
      AND PreviousVolume >= @MinimumVolume
)
SELECT FacilityId,
       WellId,
       ProductId AS Field,
       CAST(Volume AS nvarchar(150)) AS RawValue,
       CONCAT(ProductId, ' production changed by ', CAST(ChangePercent AS decimal(18, 1)), '% (',
              PreviousVolume, ' to ', Volume, ').') AS Message
FROM Changes
WHERE ABS(ChangePercent) > @SpikeThresholdPercent;
