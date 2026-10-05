-- Upserts #StagingBusinessAssociates into dbo.BusinessAssociates, keyed on BaId.
-- Rows are only updated when a value actually differs, so a repeat run changes nothing.
-- Returns one row: Inserted, Updated.
DECLARE @changes TABLE (MergeAction nvarchar(10) NOT NULL);

WITH LatestStaged AS
(
    -- The file can repeat a key; the last occurrence wins.
    SELECT BaId, LegalName, AbbreviatedName, CorporateStatus,
           ROW_NUMBER() OVER (PARTITION BY BaId ORDER BY RowNumber DESC) AS Recency
    FROM #StagingBusinessAssociates
)
MERGE dbo.BusinessAssociates AS target
USING (SELECT BaId, LegalName, AbbreviatedName, CorporateStatus FROM LatestStaged WHERE Recency = 1) AS source
   ON target.BaId = source.BaId
WHEN MATCHED AND EXISTS
(
    -- EXCEPT compares NULLs as equal, which a chain of <> comparisons would not.
    SELECT source.LegalName, source.AbbreviatedName, source.CorporateStatus
    EXCEPT
    SELECT target.LegalName, target.AbbreviatedName, target.CorporateStatus
)
THEN UPDATE SET
    LegalName       = source.LegalName,
    AbbreviatedName = source.AbbreviatedName,
    CorporateStatus = source.CorporateStatus
WHEN NOT MATCHED BY TARGET THEN
    INSERT (BaId, LegalName, AbbreviatedName, CorporateStatus)
    VALUES (source.BaId, source.LegalName, source.AbbreviatedName, source.CorporateStatus)
OUTPUT $action INTO @changes;

SELECT
    COUNT(CASE WHEN MergeAction = 'INSERT' THEN 1 END) AS Inserted,
    COUNT(CASE WHEN MergeAction = 'UPDATE' THEN 1 END) AS Updated
FROM @changes;

DROP TABLE #StagingBusinessAssociates;
