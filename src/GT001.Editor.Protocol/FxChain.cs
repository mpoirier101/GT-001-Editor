namespace GT001.Editor.Protocol;

public static class FxChain
{
    public const int PositionCount = 20;
    public static Gt001Address Offset { get; } = new(0x00, 0x00, 0x07, 0x20);
    public static Gt001Address Address => Gt001Address.TemporaryPatchBase.Add(Offset);
    public static Gt001Address Size { get; } = new(0x00, 0x00, 0x00, PositionCount);

    public static IReadOnlyList<byte> DefaultOrder { get; } =
    [
        0x00, // COMP
        0x01, // reserved here; SEND/RETURN is not a visible GT-001 module
        0x0F, // OD/DS
        0x11, // DIV start boundary
        0x02, // PREAMP A
        0x12, // channel split boundary
        0x03, // PREAMP B
        0x13, // DIV end boundary
        0x04, // EQ
        0x05, // FX1
        0x06, // FX2
        0x07, // DELAY
        0x08, // CHORUS
        0x09, // REVERB
        0x0A, // ACCEL
        0x0B, // PEDAL FX
        0x0C, // FOOT VOLUME
        0x0D, // NS1
        0x0E, // NS2
        0x10  // USB
    ];

    public static bool TryDecode(Gt001Address address, IReadOnlyList<byte> payload, out byte[] positions)
    {
        positions = [];
        var messageStart = address.ToLinearValue();
        var chainStart = Address.ToLinearValue();
        var chainEnd = chainStart + PositionCount;
        var messageEnd = messageStart + payload.Count;

        if (messageStart > chainStart || messageEnd < chainEnd)
        {
            return false;
        }

        positions = payload
            .Skip(chainStart - messageStart)
            .Take(PositionCount)
            .ToArray();
        return positions.Length == PositionCount && positions.All(value => value <= 0x13);
    }
}
