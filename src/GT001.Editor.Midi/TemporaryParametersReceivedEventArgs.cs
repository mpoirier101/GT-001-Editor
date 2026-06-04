namespace GT001.Editor.Midi;

public sealed class TemporaryParametersReceivedEventArgs(IReadOnlyList<ParameterValueReceivedEventArgs> values) : EventArgs
{
    public IReadOnlyList<ParameterValueReceivedEventArgs> Values { get; } = values;
}
