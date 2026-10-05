-- Session-scoped staging table that merge/MergeWellFacilityLinks.sql reads from.
DROP TABLE IF EXISTS #StagingWellFacilityLinks;

CREATE TABLE #StagingWellFacilityLinks
(
    RowNumber  int         NOT NULL,
    WellId     varchar(20) COLLATE DATABASE_DEFAULT NOT NULL,
    FacilityId varchar(20) COLLATE DATABASE_DEFAULT NOT NULL,
    StartDate  date        NOT NULL
);
