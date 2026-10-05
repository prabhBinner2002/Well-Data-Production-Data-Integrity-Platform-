-- Session-scoped staging table that merge/MergeFacilities.sql reads from.
DROP TABLE IF EXISTS #StagingFacilities;

CREATE TABLE #StagingFacilities
(
    RowNumber          int           NOT NULL,
    FacilityId         varchar(20)   COLLATE DATABASE_DEFAULT NOT NULL,
    FacilityType       varchar(2)    COLLATE DATABASE_DEFAULT NOT NULL,
    Name               nvarchar(100) COLLATE DATABASE_DEFAULT NOT NULL,
    SubType            varchar(3)    COLLATE DATABASE_DEFAULT NULL,
    SubTypeDescription nvarchar(100) COLLATE DATABASE_DEFAULT NULL,
    Location           varchar(20)   COLLATE DATABASE_DEFAULT NULL,
    OperationalStatus  nvarchar(30)  COLLATE DATABASE_DEFAULT NULL,
    OperatorBaId       varchar(4)    COLLATE DATABASE_DEFAULT NOT NULL
);
