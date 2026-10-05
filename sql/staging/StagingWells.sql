-- Session-scoped staging table that merge/MergeWells.sql reads from.
DROP TABLE IF EXISTS #StagingWells;

CREATE TABLE #StagingWells
(
    RowNumber      int           NOT NULL,
    WellId         varchar(20)   COLLATE DATABASE_DEFAULT NOT NULL,
    Uwi            varchar(24)   COLLATE DATABASE_DEFAULT NOT NULL,
    Name           nvarchar(100) COLLATE DATABASE_DEFAULT NOT NULL,
    LicenceNumber  varchar(10)   COLLATE DATABASE_DEFAULT NULL,
    LicenseeBaId   varchar(4)    COLLATE DATABASE_DEFAULT NULL,
    Field          nvarchar(50)  COLLATE DATABASE_DEFAULT NULL,
    StatusFluid    nvarchar(20)  COLLATE DATABASE_DEFAULT NULL,
    StatusMode     nvarchar(20)  COLLATE DATABASE_DEFAULT NULL,
    LicenceStatus  nvarchar(20)  COLLATE DATABASE_DEFAULT NULL,
    IsConfidential bit           NOT NULL
);
