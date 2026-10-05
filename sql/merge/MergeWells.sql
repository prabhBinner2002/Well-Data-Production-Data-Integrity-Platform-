-- Upserts #StagingWells into dbo.Wells, keyed on WellId.
-- Returns one row: Inserted, Updated.
DECLARE @changes TABLE (MergeAction nvarchar(10) NOT NULL);

WITH LatestStaged AS
(
    -- The well file repeats a WellId once per linked facility; the last occurrence wins.
    SELECT WellId, Uwi, Name, LicenceNumber, LicenseeBaId, Field, StatusFluid, StatusMode, LicenceStatus, IsConfidential,
           ROW_NUMBER() OVER (PARTITION BY WellId ORDER BY RowNumber DESC) AS Recency
    FROM #StagingWells
)
MERGE dbo.Wells AS target
USING (SELECT * FROM LatestStaged WHERE Recency = 1) AS source
   ON target.WellId = source.WellId
WHEN MATCHED AND EXISTS
(
    SELECT source.Uwi, source.Name, source.LicenceNumber, source.LicenseeBaId, source.Field,
           source.StatusFluid, source.StatusMode, source.LicenceStatus, source.IsConfidential
    EXCEPT
    SELECT target.Uwi, target.Name, target.LicenceNumber, target.LicenseeBaId, target.Field,
           target.StatusFluid, target.StatusMode, target.LicenceStatus, target.IsConfidential
)
THEN UPDATE SET
    Uwi            = source.Uwi,
    Name           = source.Name,
    LicenceNumber  = source.LicenceNumber,
    LicenseeBaId   = source.LicenseeBaId,
    Field          = source.Field,
    StatusFluid    = source.StatusFluid,
    StatusMode     = source.StatusMode,
    LicenceStatus  = source.LicenceStatus,
    IsConfidential = source.IsConfidential
WHEN NOT MATCHED BY TARGET THEN
    INSERT (WellId, Uwi, Name, LicenceNumber, LicenseeBaId, Field, StatusFluid, StatusMode, LicenceStatus, IsConfidential)
    VALUES (source.WellId, source.Uwi, source.Name, source.LicenceNumber, source.LicenseeBaId, source.Field,
            source.StatusFluid, source.StatusMode, source.LicenceStatus, source.IsConfidential)
OUTPUT $action INTO @changes;

SELECT
    COUNT(CASE WHEN MergeAction = 'INSERT' THEN 1 END) AS Inserted,
    COUNT(CASE WHEN MergeAction = 'UPDATE' THEN 1 END) AS Updated
FROM @changes;

DROP TABLE #StagingWells;
