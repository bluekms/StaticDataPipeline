namespace SchemaInfoScanner.Schemata;

public sealed record AttributeInfo(string Name, IReadOnlyList<AttributeArgumentInfo> Arguments);

public sealed record AttributeArgumentInfo(string Text, object? Value, bool IsConstant, string? ParameterName);
