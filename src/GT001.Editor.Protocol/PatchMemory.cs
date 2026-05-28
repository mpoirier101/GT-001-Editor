namespace GT001.Editor.Protocol;

public static class PatchMemory
{
    public const int UserPatchCount = 200;
    public const int FactoryPatchCount = 200;
    public const int ProgramsPerBank = 100;

    public static Gt001Address RegularPatchDataSize { get; } = new(0x00, 0x00, 0x0A, 0x31);

    public static Gt001Address GetPatchAddress(int bankNumber, int programNumber)
    {
        var absolutePatchIndex = (bankNumber * ProgramsPerBank) + programNumber;
        if (absolutePatchIndex < 0 || absolutePatchIndex >= UserPatchCount + FactoryPatchCount)
        {
            throw new ArgumentOutOfRangeException(nameof(bankNumber), "Patch bank/program must resolve to U001-U200 or P001-P200.");
        }

        var patchBase = absolutePatchIndex < UserPatchCount ? 0x10 : 0x20;
        var groupIndex = absolutePatchIndex % UserPatchCount;
        return new Gt001Address((byte)(patchBase + (groupIndex / 128)), (byte)(groupIndex % 128), 0x00, 0x00);
    }

    public static bool IsUserPatch(int bankNumber, int programNumber)
    {
        var absolutePatchIndex = (bankNumber * ProgramsPerBank) + programNumber;
        return absolutePatchIndex is >= 0 and < UserPatchCount;
    }
}
