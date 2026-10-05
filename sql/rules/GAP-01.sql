-- GAP-01: wells that reappear after one or more months with nothing reported.
-- LAG() fetches each well's previous reported month; a gap is a difference of more than one month.
-- A SHUTIN row counts as reported, because the operator told Petrinex the well was shut in.
-- Parameters: @ProductionMonth date
WITH WellMonths AS
(
    SELECT FromToId AS WellId, ProductionMonth, MIN(ReportingFacilityId) AS FacilityId
    FROM dbo.VolumetricRecords
    WHERE FromToIdType = 'WI'
      AND ActivityId IN ('PROD', 'INJ', 'SHUTIN')
      AND ProductionMonth <= @ProductionMonth
    GROUP BY FromToId, ProductionMonth
),
WithPrevious AS
(
    SELECT WellId, FacilityId, ProductionMonth,
           LAG(ProductionMonth) OVER (PARTITION BY WellId ORDER BY ProductionMonth) AS PreviousMonth
    FROM WellMonths
)
SELECT FacilityId,
       WellId,
       'ProductionMonth' AS Field,
       CAST(NULL AS nvarchar(150)) AS RawValue,
       CONCAT('Well reported nothing for ', DATEDIFF(MONTH, PreviousMonth, ProductionMonth) - 1,
              ' month(s) before this month. Last reported ', FORMAT(PreviousMonth, 'yyyy-MM'), '.') AS Message
FROM WithPrevious
WHERE ProductionMonth = @ProductionMonth
  AND DATEDIFF(MONTH, PreviousMonth, ProductionMonth) > 1;
