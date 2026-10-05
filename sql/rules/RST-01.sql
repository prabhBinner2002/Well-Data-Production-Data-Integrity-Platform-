-- RST-01: values that differ from the first version of the month that was loaded (a restatement).
-- VolumetricRecords is a system-versioned temporal table. FOR SYSTEM_TIME ALL returns the current rows
-- plus every earlier version, and a closed version is one whose PeriodEnd is not the open-ended maximum.
-- Rows are matched on facility, activity, product, and From/To ID. Rows whose From/To ID is masked are
-- skipped because several of them can share those values and cannot be told apart.
-- Parameters: @ProductionMonth date
WITH Original AS
(
    SELECT ReportingFacilityId, ActivityId, ProductId, FromToId, FromToIdType, Volume, Energy, Hours,
           ROW_NUMBER() OVER
           (
               PARTITION BY ReportingFacilityId, ActivityId, ProductId, FromToId
               ORDER BY PeriodStart
           ) AS VersionNumber
    FROM dbo.VolumetricRecords FOR SYSTEM_TIME ALL
    WHERE ProductionMonth = @ProductionMonth
      AND PeriodEnd < '9999-12-31'
      AND (FromToId IS NULL OR FromToId <> '***')
),
FirstVersion AS
(
    SELECT * FROM Original WHERE VersionNumber = 1
)
-- Values that changed. CROSS APPLY (VALUES ...) unpivots the three measures so each is compared in one place.
SELECT latest.ReportingFacilityId                                    AS FacilityId,
       CASE WHEN latest.FromToIdType = 'WI' THEN latest.FromToId END AS WellId,
       changed.Field,
       CAST(changed.NewValue AS nvarchar(150))                       AS RawValue,
       CONCAT(latest.ActivityId, ' ', latest.ProductId, ' ', changed.Field, ' restated from ',
              COALESCE(changed.OldValue, '(none)'), ' to ', COALESCE(changed.NewValue, '(none)'), '.') AS Message
FROM dbo.VolumetricRecords AS latest
INNER JOIN FirstVersion AS original
        ON original.ReportingFacilityId = latest.ReportingFacilityId
       AND original.ActivityId = latest.ActivityId
       -- INTERSECT treats two NULLs as a match, which = does not.
       AND EXISTS (SELECT original.ProductId, original.FromToId INTERSECT SELECT latest.ProductId, latest.FromToId)
CROSS APPLY
(
    VALUES ('Volume', CAST(original.Volume AS nvarchar(50)), CAST(latest.Volume AS nvarchar(50))),
           ('Energy', CAST(original.Energy AS nvarchar(50)), CAST(latest.Energy AS nvarchar(50))),
           ('Hours',  CAST(original.Hours  AS nvarchar(50)), CAST(latest.Hours  AS nvarchar(50)))
) AS changed (Field, OldValue, NewValue)
WHERE latest.ProductionMonth = @ProductionMonth
  AND (latest.FromToId IS NULL OR latest.FromToId <> '***')
  AND EXISTS (SELECT changed.OldValue EXCEPT SELECT changed.NewValue)

UNION ALL

-- Rows that were in the first version and are no longer reported at all.
SELECT original.ReportingFacilityId,
       CASE WHEN original.FromToIdType = 'WI' THEN original.FromToId END,
       'Volume',
       CAST(NULL AS nvarchar(150)),
       CONCAT(original.ActivityId, ' ', original.ProductId, ' row with volume ',
              COALESCE(CAST(original.Volume AS nvarchar(50)), '(none)'), ' was removed in a restatement.')
FROM FirstVersion AS original
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.VolumetricRecords AS latest
    WHERE latest.ProductionMonth = @ProductionMonth
      AND latest.ReportingFacilityId = original.ReportingFacilityId
      AND latest.ActivityId = original.ActivityId
      AND EXISTS (SELECT original.ProductId, original.FromToId INTERSECT SELECT latest.ProductId, latest.FromToId)
);
