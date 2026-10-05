-- DUP-01: duplicate volumetric rows in a month.
-- Two rows are duplicates when they share facility, activity, product, and From/To ID.
-- The first occurrence is kept as the original; every later occurrence is reported.
-- Masked rows are excluded: Petrinex sums several confidential movements under "***", so repeats there are expected.
-- Parameters: @ProductionMonth date
WITH Numbered AS
(
    SELECT ReportingFacilityId, ActivityId, ProductId, FromToId, FromToIdType, Volume,
           ROW_NUMBER() OVER
           (
               PARTITION BY ReportingFacilityId, ActivityId, ProductId, FromToId
               ORDER BY SourceRowNumber
           ) AS Occurrence
    FROM dbo.VolumetricRecords
    WHERE ProductionMonth = @ProductionMonth
      AND IsMasked = 0
)
SELECT ReportingFacilityId                              AS FacilityId,
       CASE WHEN FromToIdType = 'WI' THEN FromToId END  AS WellId,
       'Volume'                                         AS Field,
       CAST(Volume AS nvarchar(150))                    AS RawValue,
       CONCAT('Duplicate row (occurrence ', Occurrence, ') for ', ActivityId, ' ', ProductId,
              CASE WHEN FromToId IS NOT NULL THEN CONCAT(' ', FromToId) END, '.') AS Message
FROM Numbered
WHERE Occurrence > 1;
