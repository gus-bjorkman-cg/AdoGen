; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0.0
| Rule ID | Category    | Severity | Notes                                                |
|---------|-------------|----------|------------------------------------------------------|
| AG001   | Design      | Error    | Type must be partial                                 |
| AG002   | Design      | Error    | Missing SqlProfile                                   |
| AG003   | Design      | Error    | Missing required parameter configuration             |
| AG004   | Usage       | Error    | String property requires explicit SqlDbType and Size |
| AG005   | Usage       | Error    | Decimal property requires Precision and Scale        |
| AG006   | Usage       | Error    | Binary property requires explicit SqlDbType and Size |
| AG007   | Usage       | Error    | Non-constant configuration argument                  |
| AG008   | Reliability | Error    | Missing key configuration                            |
| AG009   | Reliability | Warning  | Upsert cannot be generated                           |