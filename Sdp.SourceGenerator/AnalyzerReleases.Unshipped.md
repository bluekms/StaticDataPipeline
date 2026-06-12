; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDP0001 | Sdp.SourceGenerator | Error | StaticDataRecord must be partial
SDP0002 | Sdp.SourceGenerator | Error | Containing type must be partial
SDP0003 | Sdp.SourceGenerator | Error | Only one [Key] attribute is allowed per record
SDP0004 | Sdp.SourceGenerator | Error | CSV mapper does not support this parameter
SDP0005 | Sdp.SourceGenerator | Error | CountRange and Length are mutually exclusive
SDP0006 | Sdp.SourceGenerator | Error | Record must declare a primary constructor
SDP0007 | Sdp.SourceGenerator | Error | CountRange requires SingleColumnCollection
SDP0008 | Sdp.SourceGenerator | Error | Collection parameter must not be nullable
SDP0009 | Sdp.SourceGenerator | Error | SingleColumnCollection cannot be applied to FrozenDictionary
SDP0010 | Sdp.SourceGenerator | Error | Range bound is out of the parameter type's range
SDP0011 | Sdp.SourceGenerator | Error | Use the numeric [Range] form instead of the typed form
SDP0012 | Sdp.SourceGenerator | Error | StaticDataRecord cannot be generic
SDP0013 | Sdp.SourceGenerator | Error | Length and SingleColumnCollection are mutually exclusive
SDP0014 | Sdp.SourceGenerator | Error | Length must be at least 1
SDP0015 | Sdp.SourceGenerator | Error | Range minimum must not exceed maximum
SDP0016 | Sdp.SourceGenerator | Error | Attribute is not applicable to this parameter type
SDP0017 | Sdp.SourceGenerator | Error | FrozenDictionary value record must have exactly one [Key] parameter
SDP0018 | Sdp.SourceGenerator | Error | FrozenDictionary key type must match the value record's [Key] parameter
SDP0019 | Sdp.SourceGenerator | Error | CountRange bounds must be non-negative
SDP0202 | Sdp.SourceGenerator | Error | TableSet record must be partial
SDP0203 | Sdp.SourceGenerator | Error | StaticDataTable subclass must be partial
SDP0204 | Sdp.SourceGenerator | Error | [ForeignKey] and [SwitchForeignKey] cannot coexist
SDP0205 | Sdp.SourceGenerator | Error | Foreign key target TableSet member not found
SDP0206 | Sdp.SourceGenerator | Error | Foreign key target column not found
SDP0207 | Sdp.SourceGenerator | Error | Foreign key target is SingleColumnCollection
SDP0208 | Sdp.SourceGenerator | Error | SwitchForeignKey duplicate condition value
SDP0209 | Sdp.SourceGenerator | Error | SwitchForeignKey condition column not found
SDP0210 | Sdp.SourceGenerator | Error | SwitchForeignKey condition columns must be identical
SDP0211 | Sdp.SourceGenerator | Error | Foreign key column type does not match target column type
SDP0212 | Sdp.SourceGenerator | Error | SwitchForeignKey condition value can never match the enum condition column
SDP0213 | Sdp.SourceGenerator | Error | TableSet member must be a StaticDataTable subclass
SDP0214 | Sdp.SourceGenerator | Error | Foreign key target column is a collection type
SDP0215 | Sdp.SourceGenerator | Error | Table record type must have [StaticDataRecord]
SDP0216 | Sdp.SourceGenerator | Error | TableSet must be a record declared in the current compilation
SDP0217 | Sdp.SourceGenerator | Error | TableSet member table must be declared in the current compilation
SDP0218 | Sdp.SourceGenerator | Error | StaticDataManager type arguments must be closed types
SDP0219 | Sdp.SourceGenerator | Error | StaticDataManager subclass must be partial
SDP0301 | Sdp.SourceGenerator | Error | ViewSet record must be partial
SDP0302 | Sdp.SourceGenerator | Error | ViewSet member must be non-nullable
SDP0303 | Sdp.SourceGenerator | Error | StaticDataView subclass must be partial
SDP0304 | Sdp.SourceGenerator | Error | StaticDataView subclass must have a single constructor accepting TTableSet
SDP0305 | Sdp.SourceGenerator | Error | ViewSet member must be a StaticDataView subclass
SDP0306 | Sdp.SourceGenerator | Error | ViewSet must be a record declared in the current compilation
SDP0307 | Sdp.SourceGenerator | Error | ViewSet cannot be shared by managers with different TableSets
SDP0308 | Sdp.SourceGenerator | Error | View must target the manager's TableSet
