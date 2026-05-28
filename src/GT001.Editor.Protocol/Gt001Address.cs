namespace GT001.Editor.Protocol;

public readonly record struct Gt001Address(byte B0, byte B1, byte B2, byte B3)
{
    public static Gt001Address TemporaryPatchBase { get; } = new(0x60, 0x00, 0x00, 0x00);

    public byte[] ToBytes() => [B0, B1, B2, B3];

    public Gt001Address Add(Gt001Address offset)
    {
        var value = ToLinearValue() + offset.ToLinearValue();
        return FromLinearValue(value);
    }

    public int ToLinearValue() => (B0 << 21) | (B1 << 14) | (B2 << 7) | B3;

    public static Gt001Address FromLinearValue(int value)
    {
        if (value < 0 || value > 0x0FFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "GT-001 addresses are four 7-bit bytes.");
        }

        return new(
            (byte)((value >> 21) & 0x7F),
            (byte)((value >> 14) & 0x7F),
            (byte)((value >> 7) & 0x7F),
            (byte)(value & 0x7F));
    }

    public override string ToString() => $"{B0:X2} {B1:X2} {B2:X2} {B3:X2}";
}
