namespace GT001.Editor.Midi;

public sealed class TemporaryPatchDataReceivedEventArgs(IReadOnlyList<byte> payload) : EventArgs
{
    public IReadOnlyList<byte> Payload { get; } = payload;
}
