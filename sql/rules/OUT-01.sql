-- OUT-01: a well's production is far from its own trailing 12-month average.
-- Windowed AVG and STDEV look at the 12 rows before the current month for the same well and product,
-- so each well is judged against its own history, not against other wells.
-- Parameters: @ProductionMonth date, @OutlierStandardDeviations decimal, @MinimumHistoryMonths int
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
      AND ProductionMonth >= DATEADD(MONTH, -12, @ProductionMonth)
    GROUP BY FromToId, ProductId, ProductionMonth
),
WithTrailing AS
(
    SELECT WellId, ProductId, ProductionMonth, FacilityId, Volume,
           AVG(Volume)   OVER (PARTITION BY WellId, ProductId ORDER BY ProductionMonth
                               ROWS BETWEEN 12 PRECEDING AND 1 PRECEDING) AS TrailingAverage,
           STDEV(Volume) OVER (PARTITION BY WellId, ProductId ORDER BY ProductionMonth
                               ROWS BETWEEN 12 PRECEDING AND 1 PRECEDING) AS TrailingDeviation,
           COUNT(*)      OVER (PARTITION BY WellId, ProductId ORDER BY ProductionMonth
                               ROWS BETWEEN 12 PRECEDING AND 1 PRECEDING) AS HistoryMonths
    FROM WellProductMonths
)
SELECT FacilityId,
       WellId,
       ProductId AS Field,
       CAST(Volume AS nvarchar(150)) AS RawValue,
       CONCAT(ProductId, ' volume ', Volume, ' is ',
              CAST(ABS(Volume - TrailingAverage) / TrailingDeviation AS decimal(18, 1)),
              ' standard deviations from the trailing average of ',
              CAST(TrailingAverage AS decimal(18, 1)), ' over ', HistoryMonths, ' months.') AS Message
FROM WithTrailing
WHERE ProductionMonth = @ProductionMonth
  AND HistoryMonths >= @MinimumHistoryMonths
  AND TrailingDeviation > 0
  AND ABS(Volume - TrailingAverage) > @OutlierStandardDeviations * TrailingDeviation;
