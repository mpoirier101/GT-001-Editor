namespace GT001.Editor.Core;

public sealed record MidiPortInfo(string Id, string Name);

public enum DeviceConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public sealed record DeviceConnectionState(
    DeviceConnectionStatus Status,
    MidiPortInfo? InputPort,
    MidiPortInfo? OutputPort,
    string Detail)
{
    public static DeviceConnectionState Disconnected { get; } = new(DeviceConnectionStatus.Disconnected, null, null, "No GT-001 connected");
}
