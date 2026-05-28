namespace GT001.Editor.Protocol;

public static class Gt001Constants
{
    public const byte RolandManufacturerId = 0x41;
    public const byte RequestDataCommand = 0x11;
    public const byte DataSetCommand = 0x12;
    public const byte BroadcastDeviceId = 0x7F;
    public const byte DefaultOutboundDeviceId = BroadcastDeviceId;

    public static readonly byte[] ModelId = [0x00, 0x00, 0x00, 0x06];
    public static readonly byte[] IdentityRequest = [0xF0, 0x7E, 0x00, 0x06, 0x01, 0xF7];
    // Observed from real GT-001 hardware Identity Reply:
    // F0 7E dev 06 02 41 06 03 00 00 00 00 00 00 F7
    public static readonly byte[] Gt001IdentityFamilyCode = [0x06, 0x03];
}
