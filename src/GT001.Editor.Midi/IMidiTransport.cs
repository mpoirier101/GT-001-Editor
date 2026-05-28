using GT001.Editor.Core;

namespace GT001.Editor.Midi;

public interface IMidiTransport : IDisposable
{
    event EventHandler<byte[]>? MessageReceived;
    event EventHandler<string>? DiagnosticCreated;
    event EventHandler<MidiPatchChangeEventArgs>? PatchChangeReceived;

    IReadOnlyList<MidiPortInfo> GetInputPorts();
    IReadOnlyList<MidiPortInfo> GetOutputPorts();
    void Open(string inputPortId, string outputPortId);
    void Close();
    void Send(byte[] message);
    void SendToOutputPort(string outputPortId, byte[] message);
}
