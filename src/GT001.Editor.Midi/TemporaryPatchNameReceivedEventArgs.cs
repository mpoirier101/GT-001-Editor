namespace GT001.Editor.Midi;

public sealed class TemporaryPatchNameReceivedEventArgs(string name) : EventArgs
{
    public string Name { get; } = name;
}
