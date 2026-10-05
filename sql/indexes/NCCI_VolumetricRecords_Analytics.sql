-- Nonclustered columnstore index for facility-level and product-level analytics: totals by month, operator,
-- facility, activity, and product. Those queries read a few columns from very many rows, which is what
-- columnstore compresses and scans well.
-- FromToId is deliberately left out. With it included, the optimizer also chose this index for the per-well
-- queries behind vw_MonthlyWellProduction, and on 1 million rows that plan measured about six times slower
-- than the ordered rowstore index IX_VolumetricRecords_Well_Month. Without the column, well-level queries
-- stay on the rowstore index and month-wide aggregates use the columnstore.
CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_VolumetricRecords_Analytics
ON dbo.VolumetricRecords
(
    ProductionMonth, OperatorBaId, ReportingFacilityId, ActivityId, ProductId,
    FromToIdType, Volume, Energy, IsMasked
);
