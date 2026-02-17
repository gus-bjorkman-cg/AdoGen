; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0.0
| Rule ID | Category    | Severity | Notes                                                                      |
|---------|-------------|----------|----------------------------------------------------------------------------|
| AG001   | Design      | Error    | Type must be partial                                                       |
| AG002   | Usage       | Error    | String property requires explicit SqlDbType and Size                       |
| AG003   | Usage       | Error    | Decimal property requires Precision and Scale                              |
| AG004   | Usage       | Error    | Binary property requires explicit SqlDbType and Size                       |
| AG006   | Usage       | Error    | Cannot find ISqlResult interface; ensure AdoGen.Abstractions is referenced |
| AG007   | Usage       | Error    | Non-constant configuration argument; use literal or const                  |
| AG008   | Usage       | Error    | Cannot find ISqlDomainModel<T>; ensure AdoGen.Abstractions is referenced   |
| AG009   | Reliability | Error    | Missing key configuration; Update/Delete/Upsert cannot be generated        |
| AG010   | Design      | Warning  | Upsert cannot be generated; no non-identity match key for MERGE            |