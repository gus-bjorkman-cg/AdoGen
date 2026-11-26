\ufeff; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category | Severity | Notes                                |
|---------|----------|----------|--------------------------------------|
| AG014   | Design   | Error    | Abstract types not supported         |
| AG015   | Design   | Error    | Unsupported containing type          |
| AG016   | Usage    | Error    | Invalid column name                  |
| AG017   | Usage    | Error    | Unsupported property type            |
| AG018   | Design   | Error    | Multiple profiles for the same type  |
| AG019   | Usage    | Error    | Unsupported profile expression       |
| AG020   | Design   | Info     | Key-only model                       |
| AG021   | Usage    | Error    | Unknown profile method               |
| AG022   | Usage    | Error    | Ignored property is in use           |
| AG023   | Usage    | Error    | AsString requires a non-flags enum   |
| AG024   | Design   | Error    | Struct model not supported           |
| AG025   | Usage    | Error    | Column type cannot be indexed        |

### Removed Rules

| Rule ID | Category    | Severity | Notes                                    |
|---------|-------------|----------|------------------------------------------|
| AG003   | Design      | Error    | Missing required parameter configuration |
| AG009   | Reliability | Warning  | Upsert cannot be generated               |
