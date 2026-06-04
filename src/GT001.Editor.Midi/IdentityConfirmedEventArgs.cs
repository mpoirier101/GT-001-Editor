namespace GT001.Editor.Midi;

public sealed class IdentityConfirmedEventArgs(byte deviceId) : EventArgs
{
    public byte DeviceId { get; } = deviceId;
}
