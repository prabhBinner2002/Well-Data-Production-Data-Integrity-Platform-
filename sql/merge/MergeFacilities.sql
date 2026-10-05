-- Upserts #StagingFacilities into dbo.Facilities, keyed on FacilityId.
-- Facilities whose operator is not in dbo.BusinessAssociates are left out by the inner join.
-- Returns one row: Inserted, Updated.
DECLARE @changes TABLE (MergeAction nvarchar(10) NOT NULL);

WITH LatestStaged AS
(
    SELECT staged.FacilityId, staged.FacilityType, staged.Name, staged.SubType, staged.SubTypeDescription,
           staged.Location, staged.OperationalStatus, ba.Id AS OperatorId,
           ROW_NUMBER() OVER (PARTITION BY staged.FacilityId ORDER BY staged.RowNumber DESC) AS Recency
    FROM #StagingFacilities AS staged
    INNER JOIN dbo.BusinessAssociates AS ba
            ON ba.BaId = staged.OperatorBaId
)
MERGE dbo.Facilities AS target
USING (SELECT * FROM LatestStaged WHERE Recency = 1) AS source
   ON target.FacilityId = source.FacilityId
WHEN MATCHED AND EXISTS
(
    SELECT source.FacilityType, source.Name, source.SubType, source.SubTypeDescription,
           source.Location, source.OperationalStatus, source.OperatorId
    EXCEPT
    SELECT target.FacilityType, target.Name, target.SubType, target.SubTypeDescription,
           target.Location, target.OperationalStatus, target.OperatorId
)
THEN UPDATE SET
    FacilityType       = source.FacilityType,
    Name               = source.Name,
    SubType            = source.SubType,
    SubTypeDescription = source.SubTypeDescription,
    Location           = source.Location,
    OperationalStatus  = source.OperationalStatus,
    OperatorId         = source.OperatorId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (FacilityId, FacilityType, Name, SubType, SubTypeDescription, Location, OperationalStatus, OperatorId)
    VALUES (source.FacilityId, source.FacilityType, source.Name, source.SubType, source.SubTypeDescription,
            source.Location, source.OperationalStatus, source.OperatorId)
OUTPUT $action INTO @changes;

SELECT
    COUNT(CASE WHEN MergeAction = 'INSERT' THEN 1 END) AS Inserted,
    COUNT(CASE WHEN MergeAction = 'UPDATE' THEN 1 END) AS Updated
FROM @changes;

DROP TABLE #StagingFacilities;
