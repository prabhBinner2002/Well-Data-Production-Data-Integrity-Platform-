-- Inserts links from #StagingWellFacilityLinks into dbo.WellFacilityLinks.
-- A link is identified by well, facility, and start date, and has no other columns, so there is nothing to update.
-- Links whose well or facility is not loaded are left out by the inner joins.
-- Returns one row: Inserted, Updated.
DECLARE @changes TABLE (MergeAction nvarchar(10) NOT NULL);

WITH DistinctStaged AS
(
    SELECT DISTINCT well.Id AS WellId, facility.Id AS FacilityId, staged.StartDate
    FROM #StagingWellFacilityLinks AS staged
    INNER JOIN dbo.Wells AS well
            ON well.WellId = staged.WellId
    INNER JOIN dbo.Facilities AS facility
            ON facility.FacilityId = staged.FacilityId
)
MERGE dbo.WellFacilityLinks AS target
USING DistinctStaged AS source
   ON target.WellId = source.WellId
  AND target.FacilityId = source.FacilityId
  AND target.StartDate = source.StartDate
WHEN NOT MATCHED BY TARGET THEN
    INSERT (WellId, FacilityId, StartDate)
    VALUES (source.WellId, source.FacilityId, source.StartDate)
OUTPUT $action INTO @changes;

SELECT
    COUNT(CASE WHEN MergeAction = 'INSERT' THEN 1 END) AS Inserted,
    COUNT(CASE WHEN MergeAction = 'UPDATE' THEN 1 END) AS Updated
FROM @changes;

DROP TABLE #StagingWellFacilityLinks;
