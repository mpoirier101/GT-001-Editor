using GT001.Editor.Core;
using GT001.Editor.Protocol;

namespace GT001.Editor.Midi;

public sealed class Gt001MidiService : IDisposable
{
    private readonly IMidiTransport _transport;
    private readonly Gt001Protocol _protocol;
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
    public event EventHandler? IdentityConfirmed;
    public event EventHandler<ParameterValueReceivedEventArgs>? TemporaryParameterReceived;
    public event EventHandler<TemporaryPatchDataReceivedEventArgs>? TemporaryPatchDataReceived;
    public event EventHandler<IReadOnlyList<byte>>? FxChainReceived;
    public event EventHandler<MidiPatchChangeEventArgs>? PatchChangeReceived;
    public event EventHandler<PatchNameReceivedEventArgs>? PatchNameReceived;

    public IReadOnlyList<MidiPortInfo> GetInputPorts() => _transport.GetInputPorts();
    public IReadOnlyList<MidiPortInfo> GetOutputPorts() => _transport.GetOutputPorts();

    public void Open(string inputPortId, string outputPortId)
    {
        _transport.Close();
        _transport.Open(inputPortId, outputPortId);
        Log(AppLogDirection.Info, "MIDI ports opened.");
    }

    public void Close()
    {
        _transport.Close();
        Log(AppLogDirection.Info, "MIDI ports closed.");
    }

    public void RequestIdentity(string label = "Identity Request") => Send(_protocol.BuildIdentityRequest(), label);

    public void SendProgramChange(int programNumber, int channel = 0)
        => Send(_protocol.BuildProgramChange(programNumber, channel), $"Program Change {programNumber + 1}");

    public void SendPatchChange(int bankNumber, int programNumber, int channel = 0)
    {
        Send(_protocol.BuildControlChange(0, bankNumber, channel), $"Bank Select {bankNumber}");
        SendProgramChange(programNumber, channel);
    }

    public void SendProgramChangeToOutputPort(string outputPortId, int programNumber, int channel = 0)
        => SendToOutputPort(outputPortId, _protocol.BuildProgramChange(programNumber, channel), $"Program Change {programNumber + 1}");

    public void SendPatchChangeToOutputPort(string outputPortId, int bankNumber, int programNumber, int channel = 0)
    {
        SendToOutputPort(outputPortId, _protocol.BuildControlChange(0, bankNumber, channel), $"Bank Select {bankNumber}");
        SendProgramChangeToOutputPort(outputPortId, programNumber, channel);
    }

    public void SendTemporaryParameter(ParameterDefinition definition, int value)
    {
        var data = definition.Encode(value);
        Send(_protocol.BuildDataSet(definition.TemporaryPatchAddress, data), $"DT1 {definition.DisplayName}");
    }

    public void RequestTemporaryParameter(ParameterDefinition definition)
    {
        var size = new Gt001Address(0x00, 0x00, 0x00, (byte)definition.Size);
        Send(_protocol.BuildRequestData(definition.TemporaryPatchAddress, size), $"RQ1 {definition.DisplayName}");
    }

    public void RequestModeledTemporaryPatch()
    {
        var size = TemporaryPatchParameters.GetModeledTemporaryPatchRequestSize();
        Send(_protocol.BuildRequestData(Gt001Address.TemporaryPatchBase, size), $"RQ1 Temporary Patch ({size})");
    }

    public void RequestTemporaryPatchForWrite()
        => Send(_protocol.BuildRequestData(Gt001Address.TemporaryPatchBase, PatchMemory.RegularPatchDataSize), $"RQ1 Temporary Patch Write Data ({PatchMemory.RegularPatchDataSize})");

    public void RequestFxChain()
        => Send(_protocol.BuildRequestData(FxChain.Address, FxChain.Size), "RQ1 FX Chain");

    public void RequestPatchName(int bankNumber, int programNumber)
    {
        var address = PatchMemory.GetPatchAddress(bankNumber, programNumber);
        Send(_protocol.BuildRequestData(address, new Gt001Address(0x00, 0x00, 0x00, 0x10)), $"RQ1 Patch Name bank={bankNumber} program={programNumber + 1}");
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

        var address = PatchMemory.GetPatchAddress(bankNumber, programNumber);
        Send(_protocol.BuildDataSet(address, patchData.Take(expectedSize).ToArray()), $"DT1 Write User Patch bank={bankNumber} program={programNumber + 1}");
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

        Send(_protocol.BuildDataSet(FxChain.Address, positions), "DT1 FX Chain");
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

    private void SendToOutputPort(string outputPortId, byte[] bytes, string label)
    {
        _transport.SendToOutputPort(outputPortId, bytes);
        Log(AppLogDirection.Outbound, $"{label} on output {outputPortId}", bytes);
    }

    private void OnMessageReceived(object? sender, byte[] bytes)
    {
        Log(AppLogDirection.Inbound, "MIDI received", bytes);
        if (_protocol.IsGt001IdentityReply(bytes))
        {
            IdentityConfirmed?.Invoke(this, EventArgs.Empty);
        }

        if (_protocol.TryParseDataSet(bytes, out var message) && message is not null)
        {
            if (TryDecodePatchName(message, out var patchName)
                && patchName is not null)
            {
                PatchNameReceived?.Invoke(this, patchName);
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
                Log(AppLogDirection.Info, $"DT1 parsed at {message.Address}, no modeled parameters in {message.Payload.Length} byte payload.");
                return;
            }

            foreach (var snapshot in values)
            {
                try
                {
                    TemporaryParameterReceived?.Invoke(this, new ParameterValueReceivedEventArgs(snapshot.Definition, snapshot.Value, bytes));
                }
                catch (Exception ex)
                {
                    Log(AppLogDirection.Error, $"Could not apply {snapshot.Definition.DisplayName}: {ex.Message}");
                }
            }
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

    private void Log(AppLogDirection direction, string message, byte[]? bytes = null)
    {
        LogCreated?.Invoke(this, new AppLogEntry(DateTimeOffset.Now, direction, message, bytes));
    }
}
