-- Session-scoped staging table that merge/MergeBusinessAssociates.sql reads from.
DROP TABLE IF EXISTS #StagingBusinessAssociates;

CREATE TABLE #StagingBusinessAssociates
(
    RowNumber       int           NOT NULL,
    BaId            varchar(4)    COLLATE DATABASE_DEFAULT NOT NULL,
    LegalName       nvarchar(150) COLLATE DATABASE_DEFAULT NOT NULL,
    AbbreviatedName nvarchar(50)  COLLATE DATABASE_DEFAULT NULL,
    CorporateStatus nvarchar(30)  COLLATE DATABASE_DEFAULT NULL
);
