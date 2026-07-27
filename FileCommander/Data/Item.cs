namespace FileCommander.Data;

record Item(
    int Index,
    string Name,
    Value[] Values

);

record Value(
    string? StringVal = null,
    long? LongVal = null,
    long? DateVal = null,
    bool? BoolVal = null
// TODO VersionVal
);