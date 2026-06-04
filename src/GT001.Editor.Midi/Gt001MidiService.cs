using GT001.Editor.Core;
using GT001.Editor.Protocol;

namespace GT001.Editor.Midi;

public sealed class Gt001MidiService : IDisposable
{
    private const int DataSetChunkPayloadSize = 241;
    private readonly IMidiTransport _transport;
    private readonly Gt001Protocol _protocol;
    private readonly Dictionary<Gt001Address, string> _pendingRequestLabels = [];
    private byte _deviceId = Gt001Constants.DefaultOutboundDeviceId;
    private bool _disposed;

    public Gt001MidiService(IMidiTransport transport, Gt001Protocol protocol)
    {
        _transport = transport;
        _protocol = protocol;
        _transport.MessageReceived += OnMessageReceived;
        _transport.DiagnosticCreated += OnTransportDiagnosticCreated;
        _transport.PatchChangeReceived += OnTransportPatchChangeReceived;
    }

    public event EventHandler<AppLogEntry>? LogCreated;
    public event EventHandler<IdentityConfirmedEventArgs>? IdentityConfirmed;
    public event EventHandler<ParameterValueReceivedEventArgs>? TemporaryParameterReceived;
    public event EventHandler<TemporaryParametersReceivedEventArgs>? TemporaryParametersReceived;
    public event EventHandler<TemporaryPatchDataReceivedEventArgs>? TemporaryPatchDataReceived;
    public event EventHandler<TemporaryPatchChunkReceivedEventArgs>? TemporaryPatchChunkReceived;
    public event EventHandler<TemporaryPatchNameReceivedEventArgs>? TemporaryPatchNameReceived;
    public event EventHandler<IReadOnlyList<byte>>? FxChainReceived;
    public event EventHandler<MidiPatchChangeEventArgs>? PatchChangeReceived;
    public event EventHandler<PatchNameReceivedEventArgs>? PatchNameReceived;

    public IReadOnlyList<MidiPortInfo> GetInputPorts() => _transport.GetInputPorts();
    public IReadOnlyList<MidiPortInfo> GetOutputPorts() => _transport.GetOutputPorts();

    public void Open(string inputPortId, string outputPortId)
    {
        _transport.Close();
        _pendingRequestLabels.Clear();
        _deviceId = Gt001Constants.DefaultOutboundDeviceId;
        _transport.Open(inputPortId, outputPortId);
        Log(AppLogDirection.Info, "MIDI ports opened.");
    }

    public void Close()
    {
        _transport.Close();
        _pendingRequestLabels.Clear();
        _deviceId = Gt001Constants.DefaultOutboundDeviceId;
        Log(AppLogDirection.Info, "MIDI ports closed.");
    }

    public void RequestIdentity(byte deviceId = Gt001Constants.BroadcastDeviceId)
        => Send(_protocol.BuildIdentityRequest(deviceId), deviceId == Gt001Constants.BroadcastDeviceId ? "RQ1 Identity" : $"RQ1 Identity device={deviceId:X2}");

    public void SendEditorStartupStatus()
        => Send(_protocol.BuildDataSet(new Gt001Address(0x7F, 0x00, 0x00, 0x01), [0x00], _deviceId), "DT1 Editor Startup Status");

    public void SendProgramChange(int programNumber, int channel = 0)
        => Send(_protocol.BuildProgramChange(programNumber, channel), $"Program Change {programNumber + 1}");

    public void SendPatchChange(int bankNumber, int programNumber, int channel = 0)
    {
        Send(_protocol.BuildControlChange(0, bankNumber, channel), $"Bank Select MSB {bankNumber}");
        Send(_protocol.BuildControlChange(32, 0, channel), "Bank Select LSB 0");
        SendProgramChange(programNumber, channel);
    }

    public void SendProgramChangeToOutputPort(string outputPortId, int programNumber, int channel = 0)
        => SendToOutputPort(outputPortId, _protocol.BuildProgramChange(programNumber, channel), $"Program Change {programNumber + 1}");

    public void SendPatchChangeToOutputPort(string outputPortId, int bankNumber, int programNumber, int channel = 0)
    {
        SendToOutputPort(outputPortId, _protocol.BuildControlChange(0, bankNumber, channel), $"Bank Select MSB {bankNumber}");
        SendToOutputPort(outputPortId, _protocol.BuildControlChange(32, 0, channel), "Bank Select LSB 0");
        SendProgramChangeToOutputPort(outputPortId, programNumber, channel);
    }

    public void SendTemporaryParameter(ParameterDefinition definition, int value)
    {
        var data = definition.Encode(value);
        Send(_protocol.BuildDataSet(definition.TemporaryPatchAddress, data, _deviceId), $"DT1 {definition.DisplayName}");
    }

    public void RequestTemporaryParameter(ParameterDefinition definition)
    {
        var size = new Gt001Address(0x00, 0x00, 0x00, (byte)definition.Size);
        SendRequest(definition.TemporaryPatchAddress, size, $"RQ1 {definition.DisplayName}", $"DT1 {definition.DisplayName}");
    }

    public void RequestModeledTemporaryPatch()
    {
        var size = TemporaryPatchParameters.GetModeledTemporaryPatchRequestSize();
        SendRequest(Gt001Address.TemporaryPatchBase, size, "RQ1 Temporary Patch", "DT1 Temporary Patch");
    }

    public void RequestTemporaryPatchForWrite()
        => SendRequest(Gt001Address.TemporaryPatchBase, PatchMemory.RegularPatchDataSize, "RQ1 Temporary Patch Write Data", "DT1 Temporary Patch Write Data");

    public void RequestFxChain()
        => SendRequest(FxChain.Address, FxChain.Size, "RQ1 FX Chain", "DT1 FX Chain");

    public void RequestPatchName(int bankNumber, int programNumber)
    {
        var address = PatchMemory.GetPatchAddress(bankNumber, programNumber);
        SendRequest(address, new Gt001Address(0x00, 0x00, 0x00, 0x10), "RQ1 Patch Name", "DT1 Patch Name");
    }

    public void WriteUserPatch(int bankNumber, int programNumber, IReadOnlyList<byte> patchData)
    {
        if (!PatchMemory.IsUserPatch(bankNumber, programNumber))
        {
            throw new ArgumentOutOfRangeException(nameof(bankNumber), "Only User patches U001-U200 can be written.");
        }

        var expectedSize = PatchMemory.RegularPatchDataSize.ToLinearValue();
        if (patchData.Count < expectedSize)
        {
            throw new ArgumentException($"Patch data must contain at least {expectedSize} bytes.", nameof(patchData));
        }

        var baseAddress = PatchMemory.GetPatchAddress(bankNumber, programNumber);
        var data = patchData.Take(expectedSize).ToArray();
        var chunkCount = (data.Length + DataSetChunkPayloadSize - 1) / DataSetChunkPayloadSize;
        for (var offset = 0; offset < data.Length; offset += DataSetChunkPayloadSize)
        {
            var chunk = data
                .Skip(offset)
                .Take(DataSetChunkPayloadSize)
                .ToArray();
            var address = Gt001Address.FromLinearValue(baseAddress.ToLinearValue() + offset);
            Send(
                _protocol.BuildDataSet(address, chunk, _deviceId),
                $"DT1 Write User Patch bank={bankNumber} program={programNumber + 1} chunk={(offset / DataSetChunkPayloadSize) + 1}/{chunkCount}");
            Thread.Sleep(20);
        }
    }

    public void SendFxChain(IReadOnlyList<byte> positions)
    {
        if (positions.Count != FxChain.PositionCount)
        {
            throw new ArgumentException($"FX chain must contain {FxChain.PositionCount} positions.", nameof(positions));
        }

        if (positions.Distinct().Count() != FxChain.PositionCount)
        {
            throw new ArgumentException("FX chain positions must not conflict.", nameof(positions));
        }

        Send(_protocol.BuildDataSet(FxChain.Address, positions, _deviceId), "DT1 FX Chain");
    }

    public void RequestTemporaryParameters(IEnumerable<ParameterDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            RequestTemporaryParameter(definition);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _transport.MessageReceived -= OnMessageReceived;
        _transport.DiagnosticCreated -= OnTransportDiagnosticCreated;
        _transport.PatchChangeReceived -= OnTransportPatchChangeReceived;
        Close();
        _disposed = true;
    }

    private void Send(byte[] bytes, string label)
    {
        _transport.Send(bytes);
        Log(AppLogDirection.Outbound, label, bytes);
    }

    private void SendRequest(Gt001Address address, Gt001Address size, string requestLabel, string responseLabel)
    {
        _pendingRequestLabels[address] = responseLabel;
        Send(_protocol.BuildRequestData(address, size, _deviceId), requestLabel);
    }

    private void SendToOutputPort(string outputPortId, byte[] bytes, string label)
    {
        _transport.SendToOutputPort(outputPortId, bytes);
        Log(AppLogDirection.Outbound, $"{label} on output {outputPortId}", bytes);
    }

    private void OnMessageReceived(object? sender, byte[] bytes)
    {
        if (_protocol.IsGt001IdentityReply(bytes))
        {
            _deviceId = bytes[2];
            Log(AppLogDirection.Inbound, "DT1 Identity", bytes);
            IdentityConfirmed?.Invoke(this, new IdentityConfirmedEventArgs(_deviceId));
            return;
        }

        if (!_protocol.TryParseDataSet(bytes, out var message) || message is null)
        {
            Log(AppLogDirection.Inbound, "MIDI unrecognized", bytes);
            return;
        }

        Log(AppLogDirection.Inbound, GetInboundDataSetLabel(message), bytes);

        if (TryDecodePatchName(message, out var patchName)
            && patchName is not null)
        {
            PatchNameReceived?.Invoke(this, patchName);
            return;
        }

        if (TryDecodeTemporaryPatchName(message, out var temporaryPatchName))
        {
            TemporaryPatchNameReceived?.Invoke(this, new TemporaryPatchNameReceivedEventArgs(temporaryPatchName));
        }

        if (PatchMemory.IsTemporaryPatchAddress(message.Address))
        {
            TemporaryPatchChunkReceived?.Invoke(this, new TemporaryPatchChunkReceivedEventArgs(message.Address, message.Payload));
        }

        if (message.Address == Gt001Address.TemporaryPatchBase
            && message.Payload.Length >= PatchMemory.RegularPatchDataSize.ToLinearValue())
        {
            TemporaryPatchDataReceived?.Invoke(this, new TemporaryPatchDataReceivedEventArgs(message.Payload));
        }

        if (FxChain.TryDecode(message.Address, message.Payload, out var fxChainPositions))
        {
            FxChainReceived?.Invoke(this, fxChainPositions);
        }

        IReadOnlyList<ParameterValueSnapshot> values;
        try
        {
            values = TemporaryPatchParameters.DecodeFromDataSet(message);
        }
        catch (Exception ex)
        {
            Log(AppLogDirection.Error, $"Could not decode DT1 at {message.Address}: {ex.Message}");
            return;
        }

        if (values.Count == 0)
        {
            if (PatchMemory.IsModeledTemporaryPatchAddress(message.Address))
            {
                Log(AppLogDirection.Info, $"DT1 temporary patch data at {message.Address} is outside the currently modeled editor parameters ({message.Payload.Length} byte payload).");
            }
            else
            {
                Log(AppLogDirection.Info, $"DT1 device data at {message.Address} is outside the currently modeled editor parameters ({message.Payload.Length} byte payload).");
            }

            return;
        }

        var receivedValues = new List<ParameterValueReceivedEventArgs>(values.Count);
        foreach (var snapshot in values)
        {
            try
            {
                var args = new ParameterValueReceivedEventArgs(snapshot.Definition, snapshot.Value, bytes);
                receivedValues.Add(args);
                TemporaryParameterReceived?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Log(AppLogDirection.Error, $"Could not apply {snapshot.Definition.DisplayName}: {ex.Message}");
            }
        }

        if (receivedValues.Count > 0)
        {
            TemporaryParametersReceived?.Invoke(this, new TemporaryParametersReceivedEventArgs(receivedValues));
        }
    }

    private void OnTransportDiagnosticCreated(object? sender, string message)
    {
        Log(AppLogDirection.Info, $"Transport: {message}");
    }

    private void OnTransportPatchChangeReceived(object? sender, MidiPatchChangeEventArgs e)
    {
        PatchChangeReceived?.Invoke(this, e);
    }

    private string GetInboundDataSetLabel(Gt001SysExMessage message)
    {
        if (_pendingRequestLabels.Remove(message.Address, out var responseLabel))
        {
            return responseLabel;
        }

        if (FxChain.TryDecode(message.Address, message.Payload, out _))
        {
            return "DT1 FX Chain";
        }

        if (TryDecodePatchName(message, out _))
        {
            return "DT1 Patch Name";
        }

        if (message.Address == Gt001Address.TemporaryPatchBase)
        {
            return "DT1 Temporary Patch";
        }

        var parameter = TemporaryPatchParameters.FindByTemporaryPatchAddress(message.Address);
        if (parameter is not null)
        {
            return $"DT1 {parameter.DisplayName}";
        }

        if (PatchMemory.IsTemporaryPatchAddress(message.Address))
        {
            return $"DT1 Temporary Patch Data {message.Address}";
        }

        return $"DT1 {message.Address}";
    }

    private static bool TryDecodePatchName(Gt001SysExMessage message, out PatchNameReceivedEventArgs? patchName)
    {
        patchName = null;
        if (message.Payload.Length < 16 || message.Address.B2 != 0 || message.Address.B3 != 0)
        {
            return false;
        }

        var groupBase = message.Address.B0 is 0x10 or 0x11
            ? 0x10
            : message.Address.B0 is 0x20 or 0x21
                ? 0x20
                : -1;
        if (groupBase < 0)
        {
            return false;
        }

        var groupIndex = ((message.Address.B0 - groupBase) * 128) + message.Address.B1;
        if (groupIndex is < 0 or > 199)
        {
            return false;
        }

        var bankNumber = groupBase == 0x10
            ? groupIndex / 100
            : 2 + (groupIndex / 100);
        var programNumber = groupIndex % 100;
        var name = new string(message.Payload.Take(16)
            .Select(value => value is >= 0x20 and <= 0x7D ? (char)value : ' ')
            .ToArray()).TrimEnd();

        patchName = new PatchNameReceivedEventArgs(bankNumber, programNumber, name);
        return true;
    }

    private static bool TryDecodeTemporaryPatchName(Gt001SysExMessage message, out string name)
    {
        name = string.Empty;
        if (message.Address != Gt001Address.TemporaryPatchBase || message.Payload.Length < 16)
        {
            return false;
        }

        name = DecodePatchName(message.Payload);
        return true;
    }

    private static string DecodePatchName(IEnumerable<byte> bytes)
    {
        return new string(bytes.Take(16)
            .Select(value => value is >= 0x20 and <= 0x7D ? (char)value : ' ')
            .ToArray()).TrimEnd();
    }

    private void Log(AppLogDirection direction, string message, byte[]? bytes = null)
    {
        LogCreated?.Invoke(this, new AppLogEntry(DateTimeOffset.Now, direction, message, bytes));
    }
}
