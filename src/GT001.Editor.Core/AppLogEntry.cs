namespace GT001.Editor.Core;

public enum AppLogDirection
{
    Info,
    Inbound,
    Outbound,
    Error
}

public sealed record AppLogEntry(DateTimeOffset Timestamp, AppLogDirection Direction, string Message, byte[]? Bytes = null)
{
    public string BytesText => Bytes is { Length: > 0 }
        ? string.Join(" ", Bytes.Select(b => b.ToString("X2")))
        : string.Empty;
}
