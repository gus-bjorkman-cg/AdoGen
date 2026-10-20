\ufeff; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category | Severity | Notes                                |
|---------|----------|----------|--------------------------------------|
| AG014   | Design   | Error    | Abstract types not supported         |
| AG015   | Design   | Error    | Nested types not supported           |
| AG016   | Usage    | Error    | Invalid column name                  |
| AG017   | Usage    | Error    | Unsupported property type            |
| AG018   | Design   | Error    | Multiple profiles for the same type  |
| AG019   | Usage    | Error    | Unsupported profile expression       |

### Removed Rules

| Rule ID | Category    | Severity | Notes                                    |
|---------|-------------|----------|------------------------------------------|
| AG003   | Design      | Error    | Missing required parameter configuration |
| AG009   | Reliability | Warning  | Upsert cannot be generated               |
