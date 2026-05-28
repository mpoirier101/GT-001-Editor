namespace GT001.Editor.Midi;

public sealed class MidiPatchChangeEventArgs(int bankNumber, int programNumber) : EventArgs
{
    public int BankNumber { get; } = bankNumber;
    public int ProgramNumber { get; } = programNumber;
}
