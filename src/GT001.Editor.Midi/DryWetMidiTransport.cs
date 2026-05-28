using GT001.Editor.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace GT001.Editor.Midi;

public sealed class DryWetMidiTransport : IMidiTransport
{
    private InputDevice? _input;
    private OutputDevice? _output;

    public event EventHandler<byte[]>? MessageReceived;
    public event EventHandler<string>? DiagnosticCreated;
    public event EventHandler<MidiPatchChangeEventArgs>? PatchChangeReceived;
    private int _receivedBankNumber;

    public IReadOnlyList<MidiPortInfo> GetInputPorts()
    {
        return InputDevice.GetAll()
            .Select((device, index) => new MidiPortInfo(index.ToString(), device.Name))
            .ToArray();
    }

    public IReadOnlyList<MidiPortInfo> GetOutputPorts()
    {
        return OutputDevice.GetAll()
            .Select((device, index) => new MidiPortInfo(index.ToString(), device.Name))
            .ToArray();
    }

    public void Open(string inputPortId, string outputPortId)
    {
        Close();

        _input = InputDevice.GetByIndex(int.Parse(inputPortId));
        DiagnosticCreated?.Invoke(this, $"Opening input '{_input.Name}'.");
        _input.WaitForCompleteSysExEvent = true;
        _input.ErrorOccurred += OnErrorOccurred;
        _input.EventReceived += OnEventReceived;
        _input.StartEventsListening();
        DiagnosticCreated?.Invoke(this, $"Input listening started. IsListeningForEvents={_input.IsListeningForEvents}; WaitForCompleteSysExEvent={_input.WaitForCompleteSysExEvent}.");

        _output = OutputDevice.GetByIndex(int.Parse(outputPortId));
        DiagnosticCreated?.Invoke(this, $"Opening output '{_output.Name}'.");
    }

    public void Close()
    {
        if (_input is not null)
        {
            DiagnosticCreated?.Invoke(this, $"Closing input '{_input.Name}'. IsListeningForEvents={_input.IsListeningForEvents}.");
            _input.EventReceived -= OnEventReceived;
            _input.ErrorOccurred -= OnErrorOccurred;
            _input.Dispose();
            _input = null;
        }

        if (_output is not null)
        {
            DiagnosticCreated?.Invoke(this, $"Closing output '{_output.Name}'.");
            _output.Dispose();
            _output = null;
        }
    }

    public void Send(byte[] message)
    {
        if (_output is null)
        {
            throw new InvalidOperationException("MIDI output is not open.");
        }

        SendToDevice(_output, message);
    }

    public void SendToOutputPort(string outputPortId, byte[] message)
    {
        using var output = OutputDevice.GetByIndex(int.Parse(outputPortId));
        DiagnosticCreated?.Invoke(this, $"Opening one-shot output '{output.Name}'.");
        SendToDevice(output, message);
    }

    private void SendToDevice(OutputDevice output, byte[] message)
    {
        if (message.Length >= 2 && message[0] == 0xF0 && message[^1] == 0xF7)
        {
            DiagnosticCreated?.Invoke(this, $"DryWetMIDI sending SysEx length={message.Length}; inputListening={_input?.IsListeningForEvents.ToString() ?? "none"}.");
            output.SendEvent(new NormalSysExEvent(message.Skip(1).Take(message.Length - 2).ToArray()));
            return;
        }

        if (message.Length == 2 && (message[0] & 0xF0) == 0xC0)
        {
            var channel = (byte)(message[0] & 0x0F);
            var programNumber = message[1];
            DiagnosticCreated?.Invoke(this, $"DryWetMIDI sending Program Change channel={channel + 1}; program={programNumber + 1}.");
            output.SendEvent(new ProgramChangeEvent((SevenBitNumber)programNumber)
            {
                Channel = (FourBitNumber)channel
            });
            return;
        }

        if (message.Length == 3 && (message[0] & 0xF0) == 0xB0)
        {
            var channel = (byte)(message[0] & 0x0F);
            var controllerNumber = message[1];
            var value = message[2];
            DiagnosticCreated?.Invoke(this, $"DryWetMIDI sending Control Change channel={channel + 1}; controller={controllerNumber}; value={value}.");
            output.SendEvent(new ControlChangeEvent((SevenBitNumber)controllerNumber, (SevenBitNumber)value)
            {
                Channel = (FourBitNumber)channel
            });
            return;
        }

        throw new NotSupportedException("Phase 1 transport sends SysEx, Control Change, and Program Change messages only.");
    }

    public void Dispose() => Close();

    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        DiagnosticCreated?.Invoke(this, $"DryWetMIDI EventReceived type={e.Event.GetType().Name}.");
        if (e.Event is NormalSysExEvent sysEx)
        {
            var data = sysEx.Data;
            var hasTerminator = data.Length > 0 && data[^1] == 0xF7;
            DiagnosticCreated?.Invoke(this, $"NormalSysExEvent dataLength={data.Length}; hasTerminator={hasTerminator}.");
            var bytes = new byte[data.Length + (hasTerminator ? 1 : 2)];
            bytes[0] = 0xF0;
            Array.Copy(data, 0, bytes, 1, data.Length);
            bytes[^1] = 0xF7;
            MessageReceived?.Invoke(this, bytes);
            return;
        }

        if (e.Event is ControlChangeEvent controlChange
            && controlChange.ControlNumber == (SevenBitNumber)0)
        {
            _receivedBankNumber = controlChange.ControlValue;
            DiagnosticCreated?.Invoke(this, $"Bank Select received channel={(int)controlChange.Channel + 1}; bank={_receivedBankNumber}.");
            return;
        }

        if (e.Event is ProgramChangeEvent programChange)
        {
            var programNumber = (int)programChange.ProgramNumber;
            DiagnosticCreated?.Invoke(this, $"Program Change received channel={(int)programChange.Channel + 1}; bank={_receivedBankNumber}; program={programNumber + 1}.");
            PatchChangeReceived?.Invoke(this, new MidiPatchChangeEventArgs(_receivedBankNumber, programNumber));
        }
    }

    private void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
    {
        DiagnosticCreated?.Invoke(this, $"DryWetMIDI input error: {e.Exception.GetType().Name}: {e.Exception.Message}");
    }
}
