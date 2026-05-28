using GT001.Editor.Protocol;

namespace GT001.Editor.Midi;

public sealed class ParameterValueReceivedEventArgs(ParameterDefinition definition, int value, byte[] rawMessage) : EventArgs
{
    public ParameterDefinition Definition { get; } = definition;
    public int Value { get; } = value;
    public byte[] RawMessage { get; } = rawMessage;
}
