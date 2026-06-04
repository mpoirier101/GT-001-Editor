using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using GT001.Editor.Core;

namespace GT001.Editor.Midi;

public sealed class WinMmMidiTransport : IMidiTransport
{
    private const int SysExBufferCount = 16;
    private const int SysExBufferSize = 8192;
    private const int MaxPortNameLength = 32;
    private const uint CallbackFunction = 0x00030000;
    private const uint MmsyserrNoError = 0;
    private const uint MimData = 0x3C3;
    private const uint MimLongData = 0x3C4;
    private const uint MomDone = 0x3C9;

    private readonly object _gate = new();
    private readonly MidiInProc _inputCallback;
    private readonly MidiOutProc _outputCallback;
    private readonly List<SysExInputBuffer> _inputBuffers = [];
    private readonly ConcurrentDictionary<IntPtr, ManualResetEventSlim> _pendingLongSends = [];
    private IntPtr _input;
    private IntPtr _output;
    private bool _closing;
    private int _receivedBankNumber;

    public WinMmMidiTransport()
    {
        _inputCallback = OnInputMessage;
        _outputCallback = OnOutputMessage;
    }

    public event EventHandler<byte[]>? MessageReceived;
    public event EventHandler<string>? DiagnosticCreated;
    public event EventHandler<MidiPatchChangeEventArgs>? PatchChangeReceived;

    public IReadOnlyList<MidiPortInfo> GetInputPorts()
    {
        var count = midiInGetNumDevs();
        var ports = new List<MidiPortInfo>((int)count);
        for (uint i = 0; i < count; i++)
        {
            var result = midiInGetDevCaps(new UIntPtr(i), out var caps, (uint)Marshal.SizeOf<MidiInCaps>());
            if (result == MmsyserrNoError)
            {
                ports.Add(new MidiPortInfo(i.ToString(), caps.ProductName));
            }
        }

        return ports;
    }

    public IReadOnlyList<MidiPortInfo> GetOutputPorts()
    {
        var count = midiOutGetNumDevs();
        var ports = new List<MidiPortInfo>((int)count);
        for (uint i = 0; i < count; i++)
        {
            var result = midiOutGetDevCaps(new UIntPtr(i), out var caps, (uint)Marshal.SizeOf<MidiOutCaps>());
            if (result == MmsyserrNoError)
            {
                ports.Add(new MidiPortInfo(i.ToString(), caps.ProductName));
            }
        }

        return ports;
    }

    public void Open(string inputPortId, string outputPortId)
    {
        Close();

        var inputIndex = uint.Parse(inputPortId);
        ThrowIfInputError(midiInOpen(out _input, inputIndex, _inputCallback, IntPtr.Zero, CallbackFunction), "open input");
        DiagnosticCreated?.Invoke(this, $"WinMM opening input '{GetInputName(inputIndex)}'.");

        lock (_gate)
        {
            _closing = false;
            PrepareInputBuffers();
        }

        ThrowIfInputError(midiInStart(_input), "start input");
        DiagnosticCreated?.Invoke(this, $"WinMM input listening started with {SysExBufferCount} SysEx buffers of {SysExBufferSize} bytes.");

        var outputIndex = uint.Parse(outputPortId);
        ThrowIfOutputError(midiOutOpen(out _output, outputIndex, _outputCallback, IntPtr.Zero, CallbackFunction), "open output");
        DiagnosticCreated?.Invoke(this, $"WinMM opening output '{GetOutputName(outputIndex)}'.");
        DiagnosticCreated?.Invoke(this, "MIDI ports opened through WinMM.");
    }

    public void Close()
    {
        lock (_gate)
        {
            _closing = true;
        }

        if (_input != IntPtr.Zero)
        {
            DiagnosticCreated?.Invoke(this, "WinMM closing input.");
            midiInStop(_input);
            midiInReset(_input);
            ReleaseInputBuffers();
            midiInClose(_input);
            _input = IntPtr.Zero;
        }

        if (_output != IntPtr.Zero)
        {
            DiagnosticCreated?.Invoke(this, "WinMM closing output.");
            midiOutReset(_output);
            midiOutClose(_output);
            _output = IntPtr.Zero;
        }

        foreach (var pending in _pendingLongSends.Values)
        {
            pending.Set();
        }

        _pendingLongSends.Clear();
    }

    public void Send(byte[] message)
    {
        if (_output == IntPtr.Zero)
        {
            throw new InvalidOperationException("MIDI output is not open.");
        }

        SendToDevice(_output, message);
    }

    public void SendToOutputPort(string outputPortId, byte[] message)
    {
        var outputIndex = uint.Parse(outputPortId);
        ThrowIfOutputError(midiOutOpen(out var output, outputIndex, _outputCallback, IntPtr.Zero, CallbackFunction), "open one-shot output");
        DiagnosticCreated?.Invoke(this, $"WinMM opening one-shot output '{GetOutputName(outputIndex)}'.");
        try
        {
            SendToDevice(output, message);
        }
        finally
        {
            midiOutReset(output);
            midiOutClose(output);
        }
    }

    public void Dispose() => Close();

    private void PrepareInputBuffers()
    {
        for (var i = 0; i < SysExBufferCount; i++)
        {
            var buffer = SysExInputBuffer.Create(SysExBufferSize);
            ThrowIfInputError(midiInPrepareHeader(_input, buffer.HeaderPointer, (uint)Marshal.SizeOf<MidiHeader>()), "prepare input SysEx buffer");
            ThrowIfInputError(midiInAddBuffer(_input, buffer.HeaderPointer, (uint)Marshal.SizeOf<MidiHeader>()), "queue input SysEx buffer");
            _inputBuffers.Add(buffer);
        }
    }

    private void ReleaseInputBuffers()
    {
        foreach (var buffer in _inputBuffers)
        {
            midiInUnprepareHeader(_input, buffer.HeaderPointer, (uint)Marshal.SizeOf<MidiHeader>());
            buffer.Dispose();
        }

        _inputBuffers.Clear();
    }

    private void RequeueInputBuffer(IntPtr headerPointer)
    {
        lock (_gate)
        {
            if (_closing || _input == IntPtr.Zero)
            {
                return;
            }

            var header = Marshal.PtrToStructure<MidiHeader>(headerPointer);
            header.BytesRecorded = 0;
            Marshal.StructureToPtr(header, headerPointer, false);
            var result = midiInAddBuffer(_input, headerPointer, (uint)Marshal.SizeOf<MidiHeader>());
            if (result != MmsyserrNoError)
            {
                DiagnosticCreated?.Invoke(this, $"WinMM could not requeue SysEx buffer: {GetInputError(result)}");
            }
        }
    }

    private void SendToDevice(IntPtr output, byte[] message)
    {
        if (message.Length >= 2 && message[0] == 0xF0 && message[^1] == 0xF7)
        {
            DiagnosticCreated?.Invoke(this, $"WinMM sending SysEx length={message.Length}.");
            SendSysEx(output, message);
            return;
        }

        if (message.Length == 2 && (message[0] & 0xF0) == 0xC0)
        {
            var channel = (byte)(message[0] & 0x0F);
            var programNumber = message[1];
            DiagnosticCreated?.Invoke(this, $"WinMM sending Program Change channel={channel + 1}; program={programNumber + 1}.");
            ThrowIfOutputError(midiOutShortMsg(output, PackShortMessage(message)), "send Program Change");
            return;
        }

        if (message.Length == 3 && (message[0] & 0xF0) == 0xB0)
        {
            var channel = (byte)(message[0] & 0x0F);
            var controllerNumber = message[1];
            var value = message[2];
            DiagnosticCreated?.Invoke(this, $"WinMM sending Control Change channel={channel + 1}; controller={controllerNumber}; value={value}.");
            ThrowIfOutputError(midiOutShortMsg(output, PackShortMessage(message)), "send Control Change");
            return;
        }

        throw new NotSupportedException("WinMM transport sends SysEx, Control Change, and Program Change messages only.");
    }

    private void SendSysEx(IntPtr output, byte[] message)
    {
        var dataPointer = Marshal.AllocHGlobal(message.Length);
        var headerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<MidiHeader>());
        var completion = new ManualResetEventSlim(false);
        try
        {
            Marshal.Copy(message, 0, dataPointer, message.Length);
            var header = MidiHeader.Create(dataPointer, message.Length);
            Marshal.StructureToPtr(header, headerPointer, false);
            _pendingLongSends[headerPointer] = completion;

            ThrowIfOutputError(midiOutPrepareHeader(output, headerPointer, (uint)Marshal.SizeOf<MidiHeader>()), "prepare output SysEx");
            ThrowIfOutputError(midiOutLongMsg(output, headerPointer, (uint)Marshal.SizeOf<MidiHeader>()), "send SysEx");
            if (!completion.Wait(TimeSpan.FromSeconds(5)))
            {
                DiagnosticCreated?.Invoke(this, "WinMM SysEx send did not signal completion within 5 seconds.");
            }

            midiOutUnprepareHeader(output, headerPointer, (uint)Marshal.SizeOf<MidiHeader>());
        }
        finally
        {
            _pendingLongSends.TryRemove(headerPointer, out _);
            completion.Dispose();
            Marshal.FreeHGlobal(headerPointer);
            Marshal.FreeHGlobal(dataPointer);
        }
    }

    private void OnInputMessage(IntPtr handle, uint message, IntPtr instance, IntPtr parameter1, IntPtr parameter2)
    {
        try
        {
            if (message == MimLongData)
            {
                HandleLongInput(parameter1);
                return;
            }

            if (message == MimData)
            {
                HandleShortInput(parameter1);
            }
        }
        catch (Exception ex)
        {
            DiagnosticCreated?.Invoke(this, $"WinMM input callback error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void HandleLongInput(IntPtr headerPointer)
    {
        var header = Marshal.PtrToStructure<MidiHeader>(headerPointer);
        if (header.BytesRecorded > 0)
        {
            var bytes = new byte[header.BytesRecorded];
            Marshal.Copy(header.Data, bytes, 0, bytes.Length);
            var hasTerminator = bytes.Length > 0 && bytes[^1] == 0xF7;
            DiagnosticCreated?.Invoke(this, $"WinMM SysEx received length={bytes.Length}; hasTerminator={hasTerminator}.");
            MessageReceived?.Invoke(this, bytes);
        }

        RequeueInputBuffer(headerPointer);
    }

    private void HandleShortInput(IntPtr packedMessage)
    {
        var data = packedMessage.ToInt64();
        var status = (byte)(data & 0xFF);
        var data1 = (byte)((data >> 8) & 0xFF);
        var data2 = (byte)((data >> 16) & 0xFF);

        if ((status & 0xF0) == 0xB0)
        {
            if (data1 == 0)
            {
                _receivedBankNumber = data2;
                DiagnosticCreated?.Invoke(this, $"WinMM Bank Select MSB received channel={(status & 0x0F) + 1}; bank={_receivedBankNumber}.");
            }
            else if (data1 == 32)
            {
                DiagnosticCreated?.Invoke(this, $"WinMM Bank Select LSB received channel={(status & 0x0F) + 1}; value={data2}.");
            }

            return;
        }

        if ((status & 0xF0) == 0xC0)
        {
            DiagnosticCreated?.Invoke(this, $"WinMM Program Change received channel={(status & 0x0F) + 1}; bank={_receivedBankNumber}; program={data1 + 1}.");
            PatchChangeReceived?.Invoke(this, new MidiPatchChangeEventArgs(_receivedBankNumber, data1));
        }
    }

    private void OnOutputMessage(IntPtr handle, uint message, IntPtr instance, IntPtr parameter1, IntPtr parameter2)
    {
        if (message == MomDone && _pendingLongSends.TryGetValue(parameter1, out var completion))
        {
            completion.Set();
        }
    }

    private static uint PackShortMessage(byte[] message)
    {
        var packed = (uint)message[0];
        if (message.Length > 1)
        {
            packed |= (uint)(message[1] << 8);
        }

        if (message.Length > 2)
        {
            packed |= (uint)(message[2] << 16);
        }

        return packed;
    }

    private static string GetInputName(uint index)
    {
        var result = midiInGetDevCaps(new UIntPtr(index), out var caps, (uint)Marshal.SizeOf<MidiInCaps>());
        return result == MmsyserrNoError ? caps.ProductName : index.ToString();
    }

    private static string GetOutputName(uint index)
    {
        var result = midiOutGetDevCaps(new UIntPtr(index), out var caps, (uint)Marshal.SizeOf<MidiOutCaps>());
        return result == MmsyserrNoError ? caps.ProductName : index.ToString();
    }

    private static void ThrowIfInputError(uint result, string operation)
    {
        if (result != MmsyserrNoError)
        {
            throw new InvalidOperationException($"WinMM failed to {operation}: {GetInputError(result)}");
        }
    }

    private static void ThrowIfOutputError(uint result, string operation)
    {
        if (result != MmsyserrNoError)
        {
            throw new InvalidOperationException($"WinMM failed to {operation}: {GetOutputError(result)}");
        }
    }

    private static string GetInputError(uint result)
    {
        var builder = new StringBuilder(256);
        return midiInGetErrorText(result, builder, builder.Capacity) == MmsyserrNoError
            ? builder.ToString()
            : $"error {result}";
    }

    private static string GetOutputError(uint result)
    {
        var builder = new StringBuilder(256);
        return midiOutGetErrorText(result, builder, builder.Capacity) == MmsyserrNoError
            ? builder.ToString()
            : $"error {result}";
    }

    private sealed class SysExInputBuffer : IDisposable
    {
        private SysExInputBuffer(IntPtr dataPointer, IntPtr headerPointer)
        {
            DataPointer = dataPointer;
            HeaderPointer = headerPointer;
        }

        public IntPtr DataPointer { get; }
        public IntPtr HeaderPointer { get; }

        public static SysExInputBuffer Create(int length)
        {
            var dataPointer = Marshal.AllocHGlobal(length);
            var headerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<MidiHeader>());
            var header = MidiHeader.Create(dataPointer, length);
            Marshal.StructureToPtr(header, headerPointer, false);
            return new SysExInputBuffer(dataPointer, headerPointer);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(HeaderPointer);
            Marshal.FreeHGlobal(DataPointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MidiHeader
    {
        public IntPtr Data;
        public int BufferLength;
        public int BytesRecorded;
        public IntPtr User;
        public int Flags;
        public IntPtr Next;
        public IntPtr Reserved;
        public int Offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] ReservedArray;

        public static MidiHeader Create(IntPtr dataPointer, int length)
            => new()
            {
                Data = dataPointer,
                BufferLength = length,
                BytesRecorded = 0,
                User = IntPtr.Zero,
                Flags = 0,
                Next = IntPtr.Zero,
                Reserved = IntPtr.Zero,
                Offset = 0,
                ReservedArray = new IntPtr[8]
            };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MidiInCaps
    {
        public ushort ManufacturerId;
        public ushort ProductId;
        public uint DriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPortNameLength)]
        public string ProductName;
        public uint Support;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MidiOutCaps
    {
        public ushort ManufacturerId;
        public ushort ProductId;
        public uint DriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPortNameLength)]
        public string ProductName;
        public ushort Technology;
        public ushort Voices;
        public ushort Notes;
        public ushort ChannelMask;
        public uint Support;
    }

    private delegate void MidiInProc(IntPtr handle, uint message, IntPtr instance, IntPtr parameter1, IntPtr parameter2);
    private delegate void MidiOutProc(IntPtr handle, uint message, IntPtr instance, IntPtr parameter1, IntPtr parameter2);

    [DllImport("winmm.dll")]
    private static extern uint midiInGetNumDevs();

    [DllImport("winmm.dll", EntryPoint = "midiInGetDevCapsW", CharSet = CharSet.Unicode)]
    private static extern uint midiInGetDevCaps(UIntPtr deviceId, out MidiInCaps caps, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiInOpen(out IntPtr handle, uint deviceId, MidiInProc callback, IntPtr instance, uint flags);

    [DllImport("winmm.dll")]
    private static extern uint midiInPrepareHeader(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiInUnprepareHeader(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiInAddBuffer(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiInStart(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern uint midiInStop(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern uint midiInReset(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern uint midiInClose(IntPtr handle);

    [DllImport("winmm.dll", EntryPoint = "midiInGetErrorTextW", CharSet = CharSet.Unicode)]
    private static extern uint midiInGetErrorText(uint error, StringBuilder text, int size);

    [DllImport("winmm.dll")]
    private static extern uint midiOutGetNumDevs();

    [DllImport("winmm.dll", EntryPoint = "midiOutGetDevCapsW", CharSet = CharSet.Unicode)]
    private static extern uint midiOutGetDevCaps(UIntPtr deviceId, out MidiOutCaps caps, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiOutOpen(out IntPtr handle, uint deviceId, MidiOutProc callback, IntPtr instance, uint flags);

    [DllImport("winmm.dll")]
    private static extern uint midiOutPrepareHeader(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiOutUnprepareHeader(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiOutLongMsg(IntPtr handle, IntPtr header, uint size);

    [DllImport("winmm.dll")]
    private static extern uint midiOutShortMsg(IntPtr handle, uint message);

    [DllImport("winmm.dll")]
    private static extern uint midiOutReset(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern uint midiOutClose(IntPtr handle);

    [DllImport("winmm.dll", EntryPoint = "midiOutGetErrorTextW", CharSet = CharSet.Unicode)]
    private static extern uint midiOutGetErrorText(uint error, StringBuilder text, int size);
}
