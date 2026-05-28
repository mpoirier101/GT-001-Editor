namespace GT001.Editor.Storage;

public sealed record PatchFile(string Format, int Version, string Name, Dictionary<string, int> Parameters);
