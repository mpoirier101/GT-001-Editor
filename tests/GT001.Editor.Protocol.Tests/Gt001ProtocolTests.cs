using GT001.Editor.Protocol;

namespace GT001.Editor.Protocol.Tests;

public sealed class Gt001ProtocolTests
{
    [Fact]
    public void ChecksumUsesRolandSevenBitFormula()
    {
        var checksum = Gt001Checksum.Calculate([0x60, 0x00, 0x00, 0x30, 0x01]);

        Assert.Equal(0x6F, checksum);
    }

    [Fact]
    public void BuildsIdentityRequest()
    {
        var protocol = new Gt001Protocol();

        Assert.Equal([0xF0, 0x7E, 0x00, 0x06, 0x01, 0xF7], protocol.BuildIdentityRequest());
    }

    [Fact]
    public void BuildsProgramChangeOnChannelOne()
    {
        var protocol = new Gt001Protocol();

        Assert.Equal([0xC0, 0x01], protocol.BuildProgramChange(1));
    }

    [Fact]
    public void BuildsGt001BankSelectControlChangeOnChannelOne()
    {
        var protocol = new Gt001Protocol();

        Assert.Equal([0xB0, 0x00, 0x02], protocol.BuildControlChange(0, 2));
    }

    [Fact]
    public void BuildsTemporaryPatchDataSet()
    {
        var protocol = new Gt001Protocol();
        var parameter = TemporaryPatchParameters.All.Single(p => p.Id == "odds.on");

        var bytes = protocol.BuildDataSet(parameter.TemporaryPatchAddress, parameter.Encode(1));

        Assert.Equal(0xF0, bytes[0]);
        Assert.Equal(0x41, bytes[1]);
        Assert.Equal(0x7F, bytes[2]);
        Assert.Equal([0x00, 0x00, 0x00, 0x06], bytes.Skip(3).Take(4).ToArray());
        Assert.Equal(0x12, bytes[7]);
        Assert.Equal([0x60, 0x00, 0x00, 0x30], bytes.Skip(8).Take(4).ToArray());
        Assert.Equal(0x01, bytes[12]);
        Assert.Equal(0x6F, bytes[^2]);
        Assert.Equal(0xF7, bytes[^1]);
    }

    [Fact]
    public void RejectsOutOfRangeParameterValue()
    {
        var parameter = TemporaryPatchParameters.All.Single(p => p.Id == "odds.drive");

        Assert.Throws<ArgumentOutOfRangeException>(() => parameter.Encode(121));
    }

    [Fact]
    public void RecognizesObservedGt001IdentityReply()
    {
        var protocol = new Gt001Protocol();
        byte[] reply = [0xF0, 0x7E, 0x00, 0x06, 0x02, 0x41, 0x06, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF7];

        Assert.True(protocol.IsGt001IdentityReply(reply));
    }

    [Fact]
    public void OdDsTypeUsesEnumOptions()
    {
        var parameter = TemporaryPatchParameters.All.Single(p => p.Id == "odds.type");

        Assert.Equal(ParameterValueKind.Enum, parameter.Kind);
        Assert.NotNull(parameter.Options);
        Assert.Contains(parameter.Options, option => option.Value == 0x0C && option.Label == "T-SCREAM");
    }

    [Fact]
    public void ModelsFxTypeSpecificTemporaryParameters()
    {
        var fx1TouchWahSens = TemporaryPatchParameters.All.Single(p => p.Id == "fx1.touchWah.sens");
        var fx2TouchWahSens = TemporaryPatchParameters.All.Single(p => p.Id == "fx2.touchWah.sens");
        var fx2TeraEchoHold = TemporaryPatchParameters.All.Single(p => p.Id == "fx2.teraEcho.hold");

        Assert.Equal(new Gt001Address(0x00, 0x00, 0x01, 0x4E), fx1TouchWahSens.Offset);
        Assert.Equal(new Gt001Address(0x00, 0x00, 0x03, 0x5A), fx2TouchWahSens.Offset);
        Assert.Equal(new Gt001Address(0x00, 0x00, 0x05, 0x24), fx2TeraEchoHold.Offset);
        Assert.Equal(ParameterValueKind.Toggle, fx2TeraEchoHold.Kind);
    }

    [Fact]
    public void FxTypeAllowsOvertoneRawValue()
    {
        var fx2Type = TemporaryPatchParameters.All.Single(p => p.Id == "fx2.type");

        Assert.Equal("OVERTONE", fx2Type.FormatValue(0x22));
        Assert.Equal([0x22], fx2Type.Encode(0x22));
    }

    [Fact]
    public void FormatsEnumParameterValuesWithLabels()
    {
        var parameter = TemporaryPatchParameters.All.Single(p => p.Id == "odds.type");

        Assert.Equal("T-SCREAM", parameter.FormatValue(0x0C));
    }

    [Fact]
    public void DecodesModeledParametersInsideTemporaryPatchBlock()
    {
        var payload = new byte[TemporaryPatchParameters.GetModeledTemporaryPatchRequestSize().ToLinearValue()];
        payload[0x31] = 0x0C;
        payload[0x52] = 77;

        var message = new Gt001SysExMessage(
            Gt001Constants.DefaultOutboundDeviceId,
            Gt001SysExCommand.DataSet,
            Gt001Address.TemporaryPatchBase,
            payload);

        var values = TemporaryPatchParameters.DecodeFromDataSet(message);

        Assert.Contains(values, value => value.Definition.Id == "odds.type" && value.Value == 0x0C);
        Assert.Contains(values, value => value.Definition.Id == "preampA.gain" && value.Value == 77);
    }

    [Fact]
    public void ParsesDataSetWithDuplicateTerminatorFromMidiTransport()
    {
        var protocol = new Gt001Protocol();
        var parameter = TemporaryPatchParameters.All.Single(p => p.Id == "odds.type");
        var bytes = protocol.BuildDataSet(parameter.TemporaryPatchAddress, parameter.Encode(0x0C), deviceId: 0x00).Append((byte)0xF7).ToArray();

        var parsed = protocol.TryParseDataSet(bytes, out var message);

        Assert.True(parsed);
        Assert.NotNull(message);
        Assert.Equal(parameter.TemporaryPatchAddress, message.Address);
        Assert.Equal([0x0C], message.Payload);
    }

    [Fact]
    public void BuildsFxChainRequest()
    {
        var protocol = new Gt001Protocol();

        var bytes = protocol.BuildRequestData(FxChain.Address, FxChain.Size);

        Assert.Equal([0x60, 0x00, 0x07, 0x20], bytes.Skip(8).Take(4).ToArray());
        Assert.Equal([0x00, 0x00, 0x00, 0x14], bytes.Skip(12).Take(4).ToArray());
    }

    [Fact]
    public void MapsPatchMemoryAddresses()
    {
        Assert.Equal(new Gt001Address(0x10, 0x00, 0x00, 0x00), PatchMemory.GetPatchAddress(0, 0));
        Assert.Equal(new Gt001Address(0x10, 0x01, 0x00, 0x00), PatchMemory.GetPatchAddress(0, 1));
        Assert.Equal(new Gt001Address(0x11, 0x47, 0x00, 0x00), PatchMemory.GetPatchAddress(1, 99));
        Assert.Equal(new Gt001Address(0x20, 0x00, 0x00, 0x00), PatchMemory.GetPatchAddress(2, 0));
        Assert.Equal(new Gt001Address(0x21, 0x47, 0x00, 0x00), PatchMemory.GetPatchAddress(3, 99));
    }

    [Fact]
    public void BuildsRegularPatchWriteDataSet()
    {
        var protocol = new Gt001Protocol();
        var payload = Enumerable.Repeat((byte)0x20, PatchMemory.RegularPatchDataSize.ToLinearValue()).ToArray();

        var bytes = protocol.BuildDataSet(PatchMemory.GetPatchAddress(0, 0), payload);

        Assert.Equal([0x10, 0x00, 0x00, 0x00], bytes.Skip(8).Take(4).ToArray());
        Assert.Equal(payload.Length, bytes.Length - 14);
        Assert.Equal(0xF7, bytes[^1]);
    }

    [Fact]
    public void DecodesFxChainFromTemporaryPatchBlock()
    {
        var payload = new byte[FxChain.Offset.ToLinearValue() + FxChain.PositionCount];
        for (var i = 0; i < FxChain.PositionCount; i++)
        {
            payload[FxChain.Offset.ToLinearValue() + i] = FxChain.DefaultOrder[i];
        }

        Assert.True(FxChain.TryDecode(Gt001Address.TemporaryPatchBase, payload, out var positions));
        Assert.Equal(FxChain.DefaultOrder, positions);
    }
}
