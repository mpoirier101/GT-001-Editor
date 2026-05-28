namespace GT001.Editor.Midi;

public sealed class PatchNameReceivedEventArgs(int bankNumber, int programNumber, string name) : EventArgs
{
    public int BankNumber { get; } = bankNumber;
    public int ProgramNumber { get; } = programNumber;
    public string Name { get; } = name;
}
