namespace GT001.Editor.Protocol;

public enum Gt001SysExCommand
{
    RequestData = Gt001Constants.RequestDataCommand,
    DataSet = Gt001Constants.DataSetCommand
}

public sealed record Gt001SysExMessage(byte DeviceId, Gt001SysExCommand Command, Gt001Address Address, byte[] Payload)
{
    public byte[] ToBytes()
    {
        var body = new List<byte>
        {
            Gt001Constants.RolandManufacturerId,
            DeviceId
        };
        body.AddRange(Gt001Constants.ModelId);
        body.Add((byte)Command);
        body.AddRange(Address.ToBytes());
        body.AddRange(Payload);
        body.Add(Gt001Checksum.Calculate(Address.ToBytes().Concat(Payload)));

        return [0xF0, .. body, 0xF7];
    }
}
