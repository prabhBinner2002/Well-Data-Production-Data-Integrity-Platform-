-- REF-01: volumes reported against a well that is not in master data.
-- An anti-join: LEFT JOIN to Wells and keep only the rows where no well matched.
-- One issue per facility and well, however many product rows the well has.
-- Parameters: @ProductionMonth date
SELECT record.ReportingFacilityId AS FacilityId,
       record.FromToId            AS WellId,
       'FromToID'                 AS Field,
       record.FromToId            AS RawValue,
       CONCAT('Well ', record.FromToId, ' is not in master data (', COUNT(*), ' rows).') AS Message
FROM dbo.VolumetricRecords AS record
LEFT JOIN dbo.Wells AS well
       ON well.WellId = record.FromToId
WHERE record.ProductionMonth = @ProductionMonth
  AND record.FromToIdType = 'WI'
  AND well.Id IS NULL
GROUP BY record.ReportingFacilityId, record.FromToId;
