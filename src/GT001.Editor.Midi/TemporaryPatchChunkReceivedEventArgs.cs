using GT001.Editor.Protocol;

namespace GT001.Editor.Midi;

public sealed class TemporaryPatchChunkReceivedEventArgs(Gt001Address address, IReadOnlyList<byte> payload) : EventArgs
{
    public Gt001Address Address { get; } = address;
    public IReadOnlyList<byte> Payload { get; } = payload;
}
