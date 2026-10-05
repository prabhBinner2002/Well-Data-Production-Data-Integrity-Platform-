# Test fixtures

All rows are real Petrinex data for production month 2026-06. Real data is mostly clean, so the error cases are real rows with one value deliberately changed.

| File | Contents |
|---|---|
| `Vol_2026-06_sample.csv` | 130 rows for five batteries reported by operators 0XZ4 and A04N (6 proration headers, 2 SHUTIN rows, 19 masked rows), plus 6 rows from operator 0007 that the subset filter must drop. |
| `Vol_2026-06_with_errors.csv` | The sample with the injections listed below. |
| `Vol_2026-06_restated.csv` | The sample as if Petrinex regenerated the month (see below). |
| `BusinessAssociate_sample.csv`, `Facility_sample.csv`, `Well_sample.csv`, `WellFacilityLink_sample.csv` | Master data rows for the sample's operators, facilities, and wells, plus 4 unrelated rows each. |

Two of the batteries (ABBT0113245, ABBT0058572) were reported by A04N in June 2026 but belong to another operator in the current master data, so their five wells are not loaded for the subset. Rule REF-01 reports them.

## Injected errors (`Vol_2026-06_with_errors.csv`)

| Rule | Row | Change |
|---|---|---|
| FMT-01 | PROD OIL, well ABWI100013604905W500 | Volume `38.6` to `thirty-eight` |
| FMT-01 | FUEL GAS, facility ABBT0175610 | Production month `2026-06` to `2026-05` |
| MIS-01 | DISP GAS to ABBT6850172 | Volume `72.6` to blank |
| RNG-01 | PROD WATER, well ABWI100023604905W500 | Volume `0.7` to `-0.7` |
| RNG-02 | PROD OIL, well ABWI100033604905W500 | Hours `720` to `745` |
| CON-01 | PROD OIL, well ABWI102023604905W500 | Hours `720` to `0` |
| REF-02 | REC from ABGP0001088 | Product `C5-SP` to `C9-ZZ` |
| REF-02 | VENT GAS, facility ABBT0131180 | Activity `VENT` to `VNTX` |
| DUP-01 | PROD GAS, well ABWI100040307406W600 | Row repeated at the end of the file |
| none | DIFF GAS, facility ABBT0131180 | Volume `0.0` to `-1.5`, which is legitimate for DIFF |

Seven rows carry an Error and are quarantined. The CON-01 row is a Warning and still loads.

## Restatement (`Vol_2026-06_restated.csv`)

| Row | Change |
|---|---|
| PROD OIL, well ABWI100013604905W500 | Volume `38.6` to `41.2` |
| PROD OIL, well ABWI100052107409W602 | Hours `433` to `455` |
| PROD OIL, well ABWI100163606223W400 | Hours `***` to `698` (confidentiality lifted) |
| FLARE GAS, facility ABBT0058572 | Row removed |
