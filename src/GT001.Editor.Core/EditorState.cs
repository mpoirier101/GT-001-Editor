using System.Collections.ObjectModel;

namespace GT001.Editor.Core;

public sealed class EditorState
{
    public ObservableCollection<MidiPortInfo> InputPorts { get; } = [];
    public ObservableCollection<MidiPortInfo> OutputPorts { get; } = [];
    public ObservableCollection<AppLogEntry> LogEntries { get; } = [];

    public DeviceConnectionState Connection { get; set; } = DeviceConnectionState.Disconnected;
    public PatchState Patch { get; set; } = PatchState.CreateDefault();

    public void AddLog(AppLogDirection direction, string message, byte[]? bytes = null)
    {
        LogEntries.Insert(0, new AppLogEntry(DateTimeOffset.Now, direction, message, bytes));
        while (LogEntries.Count > 500)
        {
            LogEntries.RemoveAt(LogEntries.Count - 1);
        }
    }
}
