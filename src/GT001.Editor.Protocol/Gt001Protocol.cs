namespace GT001.Editor.Protocol;

public sealed class Gt001Protocol
{
    public byte[] BuildIdentityRequest() => Gt001Constants.IdentityRequest.ToArray();

    public byte[] BuildProgramChange(int programNumber, int channel = 0)
    {
        if (programNumber is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(programNumber), "Program number must be in the MIDI 0-127 range.");
        }

        if (channel is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "MIDI channel must be in the 0-15 range.");
        }

        return [(byte)(0xC0 | channel), (byte)programNumber];
    }

    public byte[] BuildControlChange(int controllerNumber, int value, int channel = 0)
    {
        if (controllerNumber is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(controllerNumber), "Controller number must be in the MIDI 0-127 range.");
        }

        if (value is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Control Change value must be in the MIDI 0-127 range.");
        }

        if (channel is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "MIDI channel must be in the 0-15 range.");
        }

        return [(byte)(0xB0 | channel), (byte)controllerNumber, (byte)value];
    }

    public byte[] BuildRequestData(Gt001Address address, Gt001Address size, byte deviceId = Gt001Constants.DefaultOutboundDeviceId)
    {
        return new Gt001SysExMessage(deviceId, Gt001SysExCommand.RequestData, address, size.ToBytes()).ToBytes();
    }

    public byte[] BuildDataSet(Gt001Address address, IReadOnlyList<byte> data, byte deviceId = Gt001Constants.DefaultOutboundDeviceId)
    {
        ValidateSevenBit(data, nameof(data));
        return new Gt001SysExMessage(deviceId, Gt001SysExCommand.DataSet, address, data.ToArray()).ToBytes();
    }

    public bool IsGt001IdentityReply(IReadOnlyList<byte> message)
    {
        return message.Count >= 15
            && message[0] == 0xF0
            && message[1] == 0x7E
            && message[3] == 0x06
            && message[4] == 0x02
            && message[5] == Gt001Constants.RolandManufacturerId
            && message[6] == Gt001Constants.Gt001IdentityFamilyCode[0]
            && message[7] == Gt001Constants.Gt001IdentityFamilyCode[1]
            && message[^1] == 0xF7;
    }

    public bool TryParseDataSet(IReadOnlyList<byte> message, out Gt001SysExMessage? parsed)
    {
        parsed = null;
        if (message.Count < 15 || message[0] != 0xF0)
        {
            return false;
        }

        var terminatorIndex = FindTerminatorIndex(message);
        if (terminatorIndex < 14)
        {
            return false;
        }

        if (message[1] != Gt001Constants.RolandManufacturerId || message[2] > 0x1F)
        {
            return false;
        }

        if (!Gt001Constants.ModelId.SequenceEqual(message.Skip(3).Take(4)))
        {
            return false;
        }

        if (message[7] != Gt001Constants.DataSetCommand)
        {
            return false;
        }

        var address = new Gt001Address(message[8], message[9], message[10], message[11]);
        var payload = message.Skip(12).Take(terminatorIndex - 13).ToArray();
        var checksum = message[terminatorIndex - 1];

        if (!Gt001Checksum.IsValid(address.ToBytes().Concat(payload), checksum))
        {
            return false;
        }

        parsed = new Gt001SysExMessage(message[2], Gt001SysExCommand.DataSet, address, payload);
        return true;
    }

    private static void ValidateSevenBit(IEnumerable<byte> data, string parameterName)
    {
        if (data.Any(b => b > 0x7F))
        {
            throw new ArgumentOutOfRangeException(parameterName, "GT-001 SysEx payload bytes must be 7-bit values.");
        }
    }

    private static int FindTerminatorIndex(IReadOnlyList<byte> message)
    {
        for (var i = 1; i < message.Count; i++)
        {
            if (message[i] == 0xF7)
            {
                return i;
            }
        }

        return -1;
    }
}
