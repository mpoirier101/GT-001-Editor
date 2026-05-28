namespace GT001.Editor.Protocol;

public static class Gt001Checksum
{
    public static byte Calculate(IEnumerable<byte> addressAndData)
    {
        var sum = addressAndData.Sum(b => b);
        return (byte)((128 - (sum % 128)) & 0x7F);
    }

    public static bool IsValid(IEnumerable<byte> addressAndData, byte checksum) => Calculate(addressAndData) == checksum;
}
