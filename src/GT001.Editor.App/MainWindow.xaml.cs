using System.ComponentModel;
using System.IO;
using GT001.Editor.Core;
using GT001.Editor.Midi;
using GT001.Editor.Protocol;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace GT001.Editor.App;

public partial class MainWindow : Window
{
    private const string MainGt001PortName = "GT-001 Ver1-1";
    private const string TopLane = "Top";
    private const string BottomLane = "Bottom";
    private const string UserPatchTab = "User";
    private const string FactoryPatchTab = "Factory";
    private const string AfterDivMixTarget = "__AFTER_DIV_MIX__";
    private const byte FxChainComp = 0x00;
    private const byte FxChainPreampA = 0x02;
    private const byte FxChainPreampB = 0x03;
    private const byte FxChainEq = 0x04;
    private const byte FxChainFx1 = 0x05;
    private const byte FxChainFx2 = 0x06;
    private const byte FxChainDelay = 0x07;
    private const byte FxChainChorus = 0x08;
    private const byte FxChainReverb = 0x09;
    private const byte FxChainAccel = 0x0A;
    private const byte FxChainPedalFx = 0x0B;
    private const byte FxChainFootVolume = 0x0C;
    private const byte FxChainNs1 = 0x0D;
    private const byte FxChainNs2 = 0x0E;
    private const byte FxChainOdDs = 0x0F;
    private const byte FxChainUsb = 0x10;
    private const byte FxChainDivStart = 0x11;
    private const byte FxChainChannelSplit = 0x12;
    private const byte FxChainDivEnd = 0x13;
    private sealed record ChainModule(string Id, string DisplayName, IReadOnlyList<string> ParameterBlocks);

    private sealed class PatchSlot(string number, string group, int bankNumber, int programNumber) : INotifyPropertyChanged
    {
        private string _patchName = string.Empty;

        public string Number { get; } = number;
        public string Group { get; } = group;
        public int BankNumber { get; } = bankNumber;
        public int ProgramNumber { get; } = programNumber;
        public string Key { get; } = $"{bankNumber}:{programNumber}";
        public string PatchName => _patchName;
        public string DisplayText => string.IsNullOrWhiteSpace(_patchName) ? Number : $"{Number}  {_patchName}";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetPatchName(string name)
        {
            var normalized = name.Trim();
            if (string.Equals(_patchName, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _patchName = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }

    private sealed record PendingParameterConfirmation(
        ParameterDefinition Definition,
        int ExpectedValue,
        DateTimeOffset IgnoreMismatchesUntil,
        DispatcherTimer VerifyTimer);
    private sealed record PatchWriteTarget(PatchSlot Slot, string Name);

    private readonly EditorState _state = new();
    private readonly Gt001MidiService _midi;
    private readonly PatchSlot[] _userPatchSlots = BuildPatchSlots(UserPatchTab, "U", 0);
    private readonly PatchSlot[] _factoryPatchSlots = BuildPatchSlots(FactoryPatchTab, "P", 2);
    private readonly Dictionary<string, DispatcherTimer> _sendTimers = [];
    private readonly Dictionary<string, int> _pendingValues = [];
    private readonly Dictionary<string, ParameterDefinition> _pendingDefinitions = [];
    private readonly Dictionary<string, PendingParameterConfirmation> _pendingConfirmations = [];
    private readonly HashSet<string> _patchNameRequestTabs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<PatchSlot> _patchNameRequestQueue = [];
    private readonly Dictionary<string, byte> _moduleFxChainValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CMP"] = FxChainComp,
        ["OD"] = FxChainOdDs,
        ["PrA"] = FxChainPreampA,
        ["PrB"] = FxChainPreampB,
        ["EQ"] = FxChainEq,
        ["FX1"] = FxChainFx1,
        ["FX2"] = FxChainFx2,
        ["DLY"] = FxChainDelay,
        ["CHO"] = FxChainChorus,
        ["REV"] = FxChainReverb,
        ["ACC"] = FxChainAccel,
        ["PDL"] = FxChainPedalFx,
        ["FV"] = FxChainFootVolume,
        ["NS1"] = FxChainNs1,
        ["NS2"] = FxChainNs2
    };
    private readonly Dictionary<byte, string> _fxChainModules = new()
    {
        [FxChainComp] = "CMP",
        [FxChainOdDs] = "OD",
        [FxChainPreampA] = "PrA",
        [FxChainPreampB] = "PrB",
        [FxChainEq] = "EQ",
        [FxChainFx1] = "FX1",
        [FxChainFx2] = "FX2",
        [FxChainDelay] = "DLY",
        [FxChainChorus] = "CHO",
        [FxChainReverb] = "REV",
        [FxChainAccel] = "ACC",
        [FxChainPedalFx] = "PDL",
        [FxChainFootVolume] = "FV",
        [FxChainNs1] = "NS1",
        [FxChainNs2] = "NS2"
    };
    private readonly List<string> _moduleOrder = [];
    private readonly Dictionary<string, string> _moduleLanes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PrA"] = TopLane,
        ["PrB"] = BottomLane
    };
    private readonly Dictionary<string, ChainModule> _modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CMP"] = new("CMP", "COMP", ["COMP"]),
        ["OD"] = new("OD", "OD/DS", ["OD/DS"]),
        ["DIV"] = new("DIV", "DIV", ["DIV"]),
        ["PrA"] = new("PrA", "Preamp A", ["Preamp A"]),
        ["PrB"] = new("PrB", "Preamp B", ["Preamp B"]),
        ["MIX"] = new("MIX", "MIX", ["MIX"]),
        ["EQ"] = new("EQ", "EQ", ["EQ"]),
        ["FX1"] = new("FX1", "FX1", ["FX1"]),
        ["FX2"] = new("FX2", "FX2", ["FX2"]),
        ["DLY"] = new("DLY", "DELAY", ["DELAY"]),
        ["CHO"] = new("CHO", "CHORUS", ["CHORUS"]),
        ["REV"] = new("REV", "REVERB", ["REVERB"]),
        ["ACC"] = new("ACC", "ACCEL", ["ACCEL"]),
        ["PDL"] = new("PDL", "PEDAL FX", ["PEDAL FX"]),
        ["FV"] = new("FV", "FOOT VOL", ["FOOT VOLUME"]),
        ["NS1"] = new("NS1", "NS1", ["NS1"]),
        ["NS2"] = new("NS2", "NS2", ["NS2"]),
        ["MST"] = new("MST", "Master", ["MASTER"])
    };
    private readonly Dictionary<int, string> _fxTypeParameterGroups = new()
    {
        [0x00] = "touchWah",
        [0x01] = "autoWah",
        [0x02] = "subWah",
        [0x03] = "advancedComp",
        [0x04] = "limiter",
        [0x05] = "subOdDs",
        [0x06] = "graphicEq",
        [0x07] = "parametricEq",
        [0x08] = "toneModify",
        [0x09] = "guitarSim",
        [0x0A] = "slowGear",
        [0x0B] = "defretter",
        [0x0C] = "waveSynth",
        [0x0D] = "sitarSim",
        [0x0E] = "octave",
        [0x0F] = "pitchShifter",
        [0x10] = "harmonist",
        [0x11] = "soundHold",
        [0x12] = "acProcessor",
        [0x13] = "phaser",
        [0x14] = "flanger",
        [0x15] = "tremolo",
        [0x16] = "rotary1",
        [0x17] = "uniV",
        [0x18] = "pan",
        [0x19] = "slicer",
        [0x1A] = "vibrato",
        [0x1B] = "ringMod",
        [0x1C] = "humanizer",
        [0x1D] = "twoByTwoChorus",
        [0x1E] = "subDelay",
        [0x1F] = "acSim",
        [0x20] = "rotary2",
        [0x21] = "teraEcho",
        [0x22] = "overtone"
    };

    private readonly string _logFilePath;
    private string _selectedModuleId = "OD";
    private string _selectedPatchTab = UserPatchTab;
    private string? _draggedModuleId;
    private string? _dragInsertBeforeModuleId;
    private string? _dragInsertLane;
    private bool _dragInsertLaneAfterLastModule;
    private bool _dragInsertAfterLastModule;
    private global::Windows.Foundation.Point _dragStartPoint;
    private bool _isModuleDragging;
    private bool _isUpdatingPatchSelectionFromDevice;
    private Border? _dragSourceTile;
    private string? _lastTappedModuleId;
    private DateTimeOffset _lastModuleTapAt;
    private bool _isConnected;
    private bool _isConnecting;
    private bool _identityReplySeen;
    private bool _syncAwaitingReply;
    private bool _syncRequestedAfterConnect;
    private bool _isBuildingParameterPanel;
    private bool _isWritingPatch;
    private int _busyAnimationFrame;
    private int _currentPatchBankNumber;
    private int _syncRetryCount;
    private PatchSlot? _pendingWriteTarget;
    private string _pendingWriteName = string.Empty;
    private DispatcherTimer? _syncTimeoutTimer;
    private DispatcherTimer? _busyAnimationTimer;
    private DispatcherTimer? _patchNameRequestTimer;
    private DispatcherTimer? _writeTimeoutTimer;
    private byte[] _fxChainPositions = FxChain.DefaultOrder.ToArray();

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1440, 900));
        Closed += MainWindow_Closed;
        _logFilePath = CreateLogFilePath();

        _midi = new Gt001MidiService(new DryWetMidiTransport(), new Gt001Protocol());
        _midi.LogCreated += (_, entry) => DispatcherQueue.TryEnqueue(() => AddLog(entry));
        _midi.PatchChangeReceived += (_, args) => DispatcherQueue.TryEnqueue(() => SelectPatchFromDevice(args.BankNumber, args.ProgramNumber));
        _midi.PatchNameReceived += (_, args) => DispatcherQueue.TryEnqueue(() => ApplyPatchName(args.BankNumber, args.ProgramNumber, args.Name));
        _midi.TemporaryPatchDataReceived += (_, args) => DispatcherQueue.TryEnqueue(() => CompletePendingWrite(args.Payload));
        _midi.FxChainReceived += (_, positions) => DispatcherQueue.TryEnqueue(() =>
        {
            ApplyFxChainPositions(positions);
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Synced FX chain: {string.Join(" ", positions.Select(value => value.ToString("X2")))}"));
        });
        _midi.IdentityConfirmed += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            _identityReplySeen = true;
            ConnectionStatusButton.Content = "GT-001 identified";
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, "GT-001 identity reply confirmed."));
            if (!_syncAwaitingReply && !_syncRequestedAfterConnect)
            {
                _syncRequestedAfterConnect = true;
                ScheduleTemporaryPatchSync(resetRetryCount: true);
                return;
            }

            if (_syncAwaitingReply && _syncRetryCount < 2)
            {
                _syncRetryCount++;
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, "Identity reply received during sync; retrying temporary patch request."));
                ScheduleTemporaryPatchSync(resetRetryCount: false);
            }
        });
        _midi.TemporaryParameterReceived += (_, args) => DispatcherQueue.TryEnqueue(() =>
        {
            if (ShouldIgnoreParameterSync(args.Definition, args.Value))
            {
                return;
            }

            _syncAwaitingReply = false;
            _syncTimeoutTimer?.Stop();
            _isConnecting = false;
            ConnectionStatusButton.Content = "GT-001 synced";
            CompleteParameterConfirmation(args.Definition, args.Value);
            _state.Patch = _state.Patch.WithParameterValue(args.Definition.Id, args.Value, ParameterSyncStatus.Synced);
            BuildEffectChain();
            UpdateDivMixContainer();
            BuildParameterPanel();
            UpdatePatchStatusText();
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Synced {args.Definition.Block}: {args.Definition.DisplayName} = {args.Definition.FormatValue(args.Value)}"));
        });

        ApplyPatchTab(UserPatchTab);

        RefreshPorts();
        BuildEffectChain();
        UpdateDivMixContainer();
        BuildParameterPanel();
        UpdatePatchStatusText();
        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Session log: {_logFilePath}"));
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private static PatchSlot[] BuildPatchSlots(string group, string prefix, int firstBankNumber)
    {
        var slots = new List<PatchSlot>(200);
        for (var index = 0; index < 200; index++)
        {
            slots.Add(new PatchSlot(
                $"{prefix}{index + 1:000}",
                group,
                firstBankNumber + (index / 100),
                index % 100));
        }

        return slots.ToArray();
    }

    private IReadOnlyList<PatchSlot> CurrentPatchSlots
        => _selectedPatchTab == FactoryPatchTab ? _factoryPatchSlots : _userPatchSlots;

    private void PatchTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tab })
        {
            ApplyPatchTab(tab);
        }
    }

    private void ApplyPatchTab(string tab)
    {
        _selectedPatchTab = tab == FactoryPatchTab ? FactoryPatchTab : UserPatchTab;
        PatchList.ItemsSource = CurrentPatchSlots;
        UpdatePatchTabVisuals();
        QueuePatchNameRequestsForCurrentTab();
        UpdateWriteButtonState();
    }

    private void UpdatePatchTabVisuals()
    {
        UserPatchesTabButton.Background = _selectedPatchTab == UserPatchTab
            ? GetBrushResource("AccentBrush")
            : GetBrushResource("PanelAltBackground");
        FactoryPatchesTabButton.Background = _selectedPatchTab == FactoryPatchTab
            ? GetBrushResource("AccentBrush")
            : GetBrushResource("PanelAltBackground");
    }

    private void ApplyPatchName(int bankNumber, int programNumber, string name)
    {
        var patchSlot = _userPatchSlots.Concat(_factoryPatchSlots)
            .FirstOrDefault(slot => slot.BankNumber == bankNumber && slot.ProgramNumber == programNumber);
        patchSlot?.SetPatchName(name);
    }

    private void QueuePatchNameRequestsForCurrentTab()
    {
        if (!_isConnected || !_patchNameRequestTabs.Add(_selectedPatchTab))
        {
            return;
        }

        foreach (var patchSlot in CurrentPatchSlots.Where(slot => string.IsNullOrWhiteSpace(slot.PatchName)))
        {
            _patchNameRequestQueue.Enqueue(patchSlot);
        }

        StartPatchNameRequestTimer();
    }

    private void StartPatchNameRequestTimer()
    {
        _patchNameRequestTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
        _patchNameRequestTimer.Stop();
        _patchNameRequestTimer.Tick -= PatchNameRequestTimer_Tick;
        _patchNameRequestTimer.Tick += PatchNameRequestTimer_Tick;
        _patchNameRequestTimer.Start();
    }

    private void PatchNameRequestTimer_Tick(object? sender, object e)
    {
        if (!_isConnected || _patchNameRequestQueue.Count == 0)
        {
            _patchNameRequestTimer?.Stop();
            return;
        }

        RequestPatchName(_patchNameRequestQueue.Dequeue());
    }

    private void RequestPatchName(PatchSlot patchSlot)
    {
        if (!_isConnected)
        {
            return;
        }

        try
        {
            _midi.RequestPatchName(patchSlot.BankNumber, patchSlot.ProgramNumber);
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private void PatchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatchList.SelectedItem is not PatchSlot patchSlot)
        {
            UpdateWriteButtonState();
            return;
        }

        _state.Patch = _state.Patch with { Name = patchSlot.Number };
        UpdateWriteButtonState();
        if (_isUpdatingPatchSelectionFromDevice)
        {
            UpdatePatchStatusText();
            return;
        }

        _syncAwaitingReply = false;
        _syncTimeoutTimer?.Stop();
        UpdatePatchStatusText();

        if (!_isConnected)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Selected patch {patchSlot.DisplayText}; connect MIDI ports to change hardware patch."));
            return;
        }

        try
        {
            SendPatchSelection(patchSlot.BankNumber, patchSlot.ProgramNumber);
            RequestPatchName(patchSlot);
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Patch change requested: {patchSlot.DisplayText}."));
            SchedulePatchChangeSync();
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private void SelectPatchFromDevice(int bankNumber, int programNumber)
    {
        var patchSlot = _userPatchSlots.Concat(_factoryPatchSlots)
            .FirstOrDefault(slot => slot.BankNumber == bankNumber && slot.ProgramNumber == programNumber);
        if (patchSlot is null)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Received patch change bank={bankNumber}, program={programNumber + 1}; no matching visible patch."));
            return;
        }

        _currentPatchBankNumber = bankNumber;
        _state.Patch = _state.Patch with { Name = patchSlot.Number };
        _isUpdatingPatchSelectionFromDevice = true;
        try
        {
            ApplyPatchTab(patchSlot.Group);
            PatchList.SelectedValue = patchSlot.Key;
            PatchList.SelectedItem = patchSlot;
            PatchList.ScrollIntoView(patchSlot);
        }
        finally
        {
            _isUpdatingPatchSelectionFromDevice = false;
        }

        RequestPatchName(patchSlot);
        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Device selected patch {patchSlot.DisplayText}."));
        UpdatePatchStatusText();
        UpdateWriteButtonState();
    }

    private async void WritePatch_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Connect MIDI ports before writing a patch."));
            return;
        }

        if (_isConnecting || _syncAwaitingReply || _isWritingPatch)
        {
            return;
        }

        var target = await ShowWriteTargetDialogAsync();
        if (target is null)
        {
            return;
        }

        try
        {
            _pendingWriteTarget = target.Slot;
            _pendingWriteName = target.Name;
            _isWritingPatch = true;
            PatchStatusText.Text = "Writing...";
            PatchStatusText.Foreground = GetBrushResource("WarmBrush");
            UpdateBusyIndicator();
            UpdateWriteButtonState();
            _midi.RequestTemporaryPatchForWrite();
            StartWriteTimeout();
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Writing current TEMP patch to {target.Slot.Number} as '{target.Name}'."));
        }
        catch (Exception ex)
        {
            CompleteWriteWithError(ex.Message);
        }
    }

    private async Task<PatchWriteTarget?> ShowWriteTargetDialogAsync()
    {
        var selectedSlot = PatchList.SelectedItem is PatchSlot { Group: UserPatchTab } selectedUserSlot
            ? selectedUserSlot
            : _userPatchSlots[0];

        var targetCombo = new ComboBox
        {
            ItemsSource = _userPatchSlots,
            DisplayMemberPath = nameof(PatchSlot.DisplayText),
            SelectedValuePath = nameof(PatchSlot.Key),
            SelectedItem = selectedSlot,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var nameBox = new TextBox
        {
            Text = selectedSlot.PatchName,
            MaxLength = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var content = new StackPanel { Spacing = 12, Width = 360 };
        content.Children.Add(new TextBlock { Text = "User Patch" });
        content.Children.Add(targetCombo);
        content.Children.Add(new TextBlock { Text = "Patch Name" });
        content.Children.Add(nameBox);

        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Write patch?",
            Content = content,
            PrimaryButtonText = "Write",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || targetCombo.SelectedItem is not PatchSlot targetSlot)
        {
            return null;
        }

        return new PatchWriteTarget(targetSlot, NormalizePatchName(nameBox.Text));
    }

    private void StartWriteTimeout()
    {
        _writeTimeoutTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _writeTimeoutTimer.Stop();
        _writeTimeoutTimer.Tick -= WriteTimeout_Tick;
        _writeTimeoutTimer.Tick += WriteTimeout_Tick;
        _writeTimeoutTimer.Start();
    }

    private void WriteTimeout_Tick(object? sender, object e)
    {
        CompleteWriteWithError("Patch write timed out waiting for temporary patch data.");
    }

    private void CompletePendingWrite(IReadOnlyList<byte> payload)
    {
        if (!_isWritingPatch || _pendingWriteTarget is not PatchSlot patchSlot)
        {
            return;
        }

        try
        {
            _writeTimeoutTimer?.Stop();
            var writeData = payload.Take(PatchMemory.RegularPatchDataSize.ToLinearValue()).ToArray();
            EncodePatchName(_pendingWriteName, writeData);
            _midi.WriteUserPatch(patchSlot.BankNumber, patchSlot.ProgramNumber, writeData);
            patchSlot.SetPatchName(_pendingWriteName);
            RequestPatchName(patchSlot);
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Wrote current TEMP patch to {patchSlot.DisplayText}."));
            _pendingWriteTarget = null;
            _pendingWriteName = string.Empty;
            _isWritingPatch = false;
            PatchStatusText.Text = "Written";
            PatchStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
            UpdateBusyIndicator();
            UpdateWriteButtonState();
        }
        catch (Exception ex)
        {
            CompleteWriteWithError(ex.Message);
        }
    }

    private void CompleteWriteWithError(string message)
    {
        _writeTimeoutTimer?.Stop();
        _pendingWriteTarget = null;
        _pendingWriteName = string.Empty;
        _isWritingPatch = false;
        UpdatePatchStatusText();
        UpdateBusyIndicator();
        UpdateWriteButtonState();
        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, message));
    }

    private static string NormalizePatchName(string name)
    {
        var chars = name.Trim()
            .Take(16)
            .Select(value => value is >= ' ' and <= '}' ? value : ' ')
            .ToArray();
        var normalized = new string(chars).TrimEnd();
        return string.IsNullOrWhiteSpace(normalized) ? "UNTITLED" : normalized;
    }

    private static void EncodePatchName(string name, byte[] payload)
    {
        var normalized = NormalizePatchName(name).PadRight(16);
        for (var i = 0; i < 16 && i < payload.Length; i++)
        {
            var value = normalized[i];
            payload[i] = (byte)(value is >= ' ' and <= '}' ? value : ' ');
        }
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            Disconnect();
            return;
        }

        if (InputPortsCombo.SelectedValue is not string inputId || OutputPortsCombo.SelectedValue is not string outputId)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Select both MIDI input and output ports."));
            return;
        }

        try
        {
            _isConnected = false;
            _isConnecting = true;
            _identityReplySeen = false;
            _syncAwaitingReply = false;
            _syncRequestedAfterConnect = false;
            _syncRetryCount = 0;
            _currentPatchBankNumber = 0;
            _patchNameRequestTabs.Clear();
            _patchNameRequestQueue.Clear();
            _syncTimeoutTimer?.Stop();
            UpdateBusyIndicator();
            _midi.Open(inputId, outputId);
            _isConnected = true;
            ConnectButton.Content = "Disconnect";
            ConnectionStatusButton.Content = "Connected";
            ConnectionStatusButton.Flyout.Hide();
            QueuePatchNameRequestsForCurrentTab();
            UpdateWriteButtonState();
            ScheduleConnectWakeupSequence();
            ScheduleConnectAutoSync();
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _isConnecting = false;
            UpdateBusyIndicator();
            UpdateWriteButtonState();
            ConnectionStatusButton.Content = "Connection error";
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private void ScheduleConnectWakeupSequence()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                _midi.RequestIdentity("Identity Wakeup");
                ScheduleConnectIdentityVerify();
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                UpdateBusyIndicator();
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
            }
        };
        timer.Start();
    }

    private void ScheduleConnectIdentityVerify()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_identityReplySeen)
            {
                return;
            }

            try
            {
                _midi.RequestIdentity("Identity Verify");
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                UpdateBusyIndicator();
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
            }
        };
        timer.Start();
    }

    private void ScheduleConnectAutoSync()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1400) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!_isConnected || _syncAwaitingReply || _syncRequestedAfterConnect)
            {
                return;
            }

            try
            {
                _syncRequestedAfterConnect = true;
                RequestTemporaryPatchSync(resetRetryCount: true);
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                UpdateBusyIndicator();
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
            }
        };
        timer.Start();
    }

    private void ScheduleTemporaryPatchSync(bool resetRetryCount = true)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                RequestTemporaryPatchSync(resetRetryCount);
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                UpdateBusyIndicator();
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
            }
        };
        timer.Start();
    }

    private void SchedulePatchChangeSync()
    {
        _syncAwaitingReply = true;
        _syncRequestedAfterConnect = true;
        PatchStatusText.Text = "Syncing...";
        UpdateBusyIndicator();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!_isConnected)
            {
                return;
            }

            try
            {
                RequestTemporaryPatchSync(resetRetryCount: true);
            }
            catch (Exception ex)
            {
                _syncAwaitingReply = false;
                UpdatePatchStatusText();
                AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
            }
        };
        timer.Start();
    }

    private void SendPatchSelection(int bankNumber, int programNumber)
    {
        var patchOutput = GetPatchChangeOutputPort();
        if (patchOutput is not null)
        {
            if (_currentPatchBankNumber != bankNumber)
            {
                _midi.SendPatchChangeToOutputPort(patchOutput.Id, bankNumber, programNumber);
                _currentPatchBankNumber = bankNumber;
            }
            else
            {
                _midi.SendProgramChangeToOutputPort(patchOutput.Id, programNumber);
            }

            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Patch change sent on '{patchOutput.Name}'."));
            return;
        }

        if (_currentPatchBankNumber != bankNumber)
        {
            _midi.SendPatchChange(bankNumber, programNumber);
            _currentPatchBankNumber = bankNumber;
            return;
        }

        _midi.SendProgramChange(programNumber);
    }

    private MidiPortInfo? GetPatchChangeOutputPort()
    {
        var selectedOutputId = OutputPortsCombo.SelectedValue as string;
        return _state.OutputPorts.FirstOrDefault(port =>
            !port.Id.Equals(selectedOutputId, StringComparison.OrdinalIgnoreCase)
            && port.Name.Contains("GT-001", StringComparison.OrdinalIgnoreCase)
            && port.Name.Contains("DAW CTRL", StringComparison.OrdinalIgnoreCase));
    }

    private void RequestTemporaryPatchSync(bool resetRetryCount)
    {
        if (resetRetryCount)
        {
            _syncRetryCount = 0;
        }

        _syncAwaitingReply = true;
        _isConnecting = false;
        PatchStatusText.Text = "Syncing...";
        UpdateBusyIndicator();
        _midi.RequestModeledTemporaryPatch();
        _midi.RequestFxChain();
        StartSyncTimeout();
    }

    private void StartSyncTimeout()
    {
        _syncTimeoutTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _syncTimeoutTimer.Stop();
        _syncTimeoutTimer.Tick -= SyncTimeout_Tick;
        _syncTimeoutTimer.Tick += SyncTimeout_Tick;
        _syncTimeoutTimer.Start();
    }

    private void SyncTimeout_Tick(object? sender, object e)
    {
        _syncTimeoutTimer?.Stop();
        if (!_syncAwaitingReply)
        {
            return;
        }

        if (_syncRetryCount >= 2)
        {
            _syncAwaitingReply = false;
            _isConnecting = false;
            UpdatePatchStatusText();
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Temporary patch sync timed out."));
            return;
        }

        _syncRetryCount++;
        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Temporary patch sync timed out; retry {_syncRetryCount}."));
        RequestTemporaryPatchSync(resetRetryCount: false);
    }

    private void RefreshPorts()
    {
        try
        {
            _state.InputPorts.Clear();
            foreach (var port in _midi.GetInputPorts())
            {
                _state.InputPorts.Add(port);
            }

            _state.OutputPorts.Clear();
            foreach (var port in _midi.GetOutputPorts())
            {
                _state.OutputPorts.Add(port);
            }

            InputPortsCombo.ItemsSource = _state.InputPorts;
            OutputPortsCombo.ItemsSource = _state.OutputPorts;

            SelectGt001Port(InputPortsCombo);
            SelectGt001Port(OutputPortsCombo);

            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Found {_state.InputPorts.Count} input and {_state.OutputPorts.Count} output MIDI ports."));
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Input ports: {string.Join(", ", _state.InputPorts.Select(port => $"{port.Id}:{port.Name}"))}"));
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Output ports: {string.Join(", ", _state.OutputPorts.Select(port => $"{port.Id}:{port.Name}"))}"));
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private static void SelectGt001Port(ComboBox comboBox)
    {
        var item = comboBox.Items.OfType<MidiPortInfo>()
            .FirstOrDefault(p => p.Name.Equals(MainGt001PortName, StringComparison.OrdinalIgnoreCase))
            ?? comboBox.Items.OfType<MidiPortInfo>()
                .FirstOrDefault(p => p.Name.Contains("GT-001", StringComparison.OrdinalIgnoreCase)
                    && !p.Name.Contains("DAW CTRL", StringComparison.OrdinalIgnoreCase)
                    && !p.Name.EndsWith(" CTRL", StringComparison.OrdinalIgnoreCase))
            ?? comboBox.Items.OfType<MidiPortInfo>()
                .FirstOrDefault(p => p.Name.Contains("BOSS", StringComparison.OrdinalIgnoreCase));

        comboBox.SelectedItem = item ?? comboBox.Items.OfType<MidiPortInfo>().FirstOrDefault();
    }

    private void BuildEffectChain()
    {
        EnsureModuleOrder();
        EffectChainPanel.Children.Remove(DivMixContainer);
        EffectChainPanel.Children.Clear();
        var divMixAdded = false;
        foreach (var moduleId in _moduleOrder)
        {
            if (moduleId is "DIV" or "PrA" or "PrB" or "MIX")
            {
                if (!divMixAdded)
                {
                    UpdateDivMixContainer();
                    EffectChainPanel.Children.Add(DivMixContainer);
                    divMixAdded = true;
                }

                continue;
            }

            if (_moduleLanes.ContainsKey(moduleId))
            {
                continue;
            }

            if (!_modules.TryGetValue(moduleId, out var moduleDefinition))
            {
                continue;
            }

            var module = CreateModuleTile(moduleDefinition);
            EffectChainPanel.Children.Add(module);
        }

        EffectChainPanel.Children.Add(CreateModuleTile(_modules["MST"]));
    }

    private Border CreateModuleTile(ChainModule module)
    {
        var isSelected = module.Id == _selectedModuleId;
        var isOn = IsModuleOn(module);
        var tile = new Border
        {
            Width = 52,
            Height = 42,
            Margin = new Thickness(0, 0, 5, 0),
            Padding = new Thickness(3, 4, 3, 4),
            CornerRadius = new CornerRadius(0),
            BorderBrush = isSelected ? GetBrushResource("WarmBrush") : isOn ? GetBrushResource("MutedTextBrush") : GetBrushResource("RailBackground"),
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
            Background = isSelected ? GetBrushResource("AccentBrush") : GetBrushResource("PanelAltBackground"),
            Opacity = 1.0,
            Tag = module.Id,
            Child = BuildModuleButtonContent(module, isSelected)
        };
        tile.PointerPressed += ModuleTile_PointerPressed;
        tile.PointerMoved += ModuleTile_PointerMoved;
        tile.PointerReleased += ModuleTile_PointerReleased;
        tile.PointerCanceled += ModuleTile_PointerCanceled;
        tile.Tapped += ModuleTile_Tapped;
        return tile;
    }

    private void UpdateDivMixContainer()
    {
        DivMixContainer.ClearValue(FrameworkElement.WidthProperty);
        DivMixContainer.ClearValue(FrameworkElement.HeightProperty);
        DivMixContainer.MinWidth = 176;
        DivMixContainer.Padding = new Thickness(4);
        DivMixContainer.Background = GetBrushResource("PanelAltBackground");
        DivMixContainer.BorderBrush = new SolidColorBrush(Colors.SlateGray);
        DivMixContainer.BorderThickness = new Thickness(1);
        DivMixChannelsPanel.Visibility = Visibility.Visible;
        MixModuleButton.Visibility = Visibility.Visible;
        DivModuleButton.Content = "DIV";

        UpdateContainerModuleButton(DivModuleButton, "DIV");
        UpdateContainerModuleButton(MixModuleButton, "MIX");
        UpdateChannelDropZoneVisuals();
        BuildDivMixChannelModules();
    }

    private void UpdateContainerModuleButton(Button button, string moduleId)
    {
        var isSelected = _selectedModuleId == moduleId;
        var isOn = IsModuleOn(_modules[moduleId]);
        button.Background = isSelected ? GetBrushResource("AccentBrush") : GetBrushResource("PanelBackground");
        button.Foreground = isSelected || isOn ? new SolidColorBrush(Colors.White) : GetBrushResource("MutedTextBrush");
        button.BorderBrush = isSelected ? GetBrushResource("WarmBrush") : isOn ? GetBrushResource("MutedTextBrush") : GetBrushResource("RailBackground");
        button.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
        button.Opacity = 1.0;
    }

    private void BuildDivMixChannelModules()
    {
        TopChannelPanel.Children.Clear();
        BottomChannelPanel.Children.Clear();

        foreach (var moduleId in _moduleOrder.Where(moduleId => _moduleLanes.TryGetValue(moduleId, out var lane) && lane == TopLane))
        {
            TopChannelPanel.Children.Add(CreateContainerModuleTile(moduleId));
        }

        foreach (var moduleId in _moduleOrder.Where(moduleId => _moduleLanes.TryGetValue(moduleId, out var lane) && lane == BottomLane))
        {
            BottomChannelPanel.Children.Add(CreateContainerModuleTile(moduleId));
        }
    }

    private Border CreateContainerModuleTile(string moduleId)
    {
        var module = _modules[moduleId];
        var isSelected = _selectedModuleId == moduleId;
        var isOn = IsModuleOn(module);
        var tile = new Border
        {
            Width = 42,
            Height = 28,
            Padding = new Thickness(3, 2, 3, 2),
            CornerRadius = new CornerRadius(2),
            BorderBrush = isSelected ? GetBrushResource("WarmBrush") : isOn ? GetBrushResource("MutedTextBrush") : GetBrushResource("RailBackground"),
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
            Background = isSelected ? GetBrushResource("AccentBrush") : GetBrushResource("PanelBackground"),
            Opacity = 1.0,
            Tag = moduleId,
            Child = new TextBlock
            {
                Text = module.Id,
                FontSize = 11,
                Foreground = isSelected || isOn ? new SolidColorBrush(Colors.White) : GetBrushResource("MutedTextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        tile.PointerPressed += ModuleTile_PointerPressed;
        tile.PointerMoved += ModuleTile_PointerMoved;
        tile.PointerReleased += ModuleTile_PointerReleased;
        tile.PointerCanceled += ModuleTile_PointerCanceled;
        tile.Tapped += ModuleTile_Tapped;
        return tile;
    }

    private bool IsLaneInsertionTarget(string moduleId)
        => _dragInsertLane is not null && _dragInsertBeforeModuleId == moduleId;

    private void UpdateChannelDropZoneVisuals()
    {
        TopChannelDropZone.BorderBrush = new SolidColorBrush(Colors.Transparent);
        TopChannelDropZone.BorderThickness = new Thickness(0);
        BottomChannelDropZone.BorderBrush = new SolidColorBrush(Colors.Transparent);
        BottomChannelDropZone.BorderThickness = new Thickness(0);
    }

    private void DivMixContainer_Tapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

    private void ContainerModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string moduleId } || !_modules.TryGetValue(moduleId, out var module))
        {
            return;
        }

        _selectedModuleId = moduleId;
        SelectedBlockText.Text = module.DisplayName;

        var now = DateTimeOffset.Now;
        var isDoubleClick = _lastTappedModuleId == moduleId && now - _lastModuleTapAt <= TimeSpan.FromMilliseconds(550);
        _lastTappedModuleId = moduleId;
        _lastModuleTapAt = now;

        if (isDoubleClick)
        {
            ToggleModule(module);
            return;
        }

        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module tapped: {module.DisplayName}."));
        BuildEffectChain();
        BuildParameterPanel();
    }

    private UIElement BuildModuleButtonContent(ChainModule module, bool isSelected)
    {
        var isOn = IsModuleOn(module);
        var content = new StackPanel
        {
            Spacing = 2,
            Children =
            {
                new TextBlock
                {
                    Text = module.Id,
                    FontSize = 14,
                    Foreground = isSelected || isOn ? new SolidColorBrush(Colors.White) : GetBrushResource("MutedTextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
            }
        };

        return content;
    }

    private void ModuleTile_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border { Tag: string moduleId } tile)
        {
            return;
        }

        if (IsLockedModule(moduleId))
        {
            return;
        }

        var point = e.GetCurrentPoint(EffectChainPanel);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _draggedModuleId = moduleId;
        _dragSourceTile = tile;
        _dragStartPoint = point.Position;
        _isModuleDragging = false;
        tile.CapturePointer(e.Pointer);
    }

    private void ModuleTile_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedModuleId is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(EffectChainPanel);
        if (!point.Properties.IsLeftButtonPressed)
        {
            ClearModuleDragState();
            return;
        }

        if (!_isModuleDragging && Math.Abs(point.Position.X - _dragStartPoint.X) < 12)
        {
            return;
        }

        if (!_isModuleDragging)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module drag started: {_draggedModuleId}."));
        }

        _isModuleDragging = true;
        var laneTarget = GetDivMixLaneInsertionTarget(point.Position);
        if (_dragInsertLane != laneTarget.Lane
            || _dragInsertBeforeModuleId != laneTarget.ModuleId
            || _dragInsertLaneAfterLastModule != laneTarget.AfterLast)
        {
            _dragInsertLane = laneTarget.Lane;
            _dragInsertBeforeModuleId = laneTarget.ModuleId;
            _dragInsertLaneAfterLastModule = laneTarget.AfterLast;
            UpdateDragTargetVisuals();
        }

        if (laneTarget.Lane is not null)
        {
            e.Handled = true;
            return;
        }

        var target = GetModuleInsertionTarget(point.Position.X);
        if (_dragInsertBeforeModuleId != target.ModuleId || _dragInsertAfterLastModule != target.AfterLast)
        {
            _dragInsertBeforeModuleId = target.ModuleId;
            _dragInsertAfterLastModule = target.AfterLast;
            UpdateDragTargetVisuals();
        }

        e.Handled = true;
    }

    private void ModuleTile_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var wasDragging = _isModuleDragging;
        var moved = false;
        if (_isModuleDragging && _draggedModuleId is not null && _dragInsertBeforeModuleId == AfterDivMixTarget)
        {
            MoveModuleAfterDivMix(_draggedModuleId);
            moved = true;
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module moved: {_draggedModuleId} after DIV/MIX."));
        }
        else if (_isModuleDragging && _draggedModuleId is not null && _dragInsertLane is not null && _dragInsertBeforeModuleId is not null)
        {
            MoveModuleToLane(_draggedModuleId, _dragInsertLane, _dragInsertBeforeModuleId, _dragInsertLaneAfterLastModule);
            moved = true;
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, _dragInsertLaneAfterLastModule
                ? $"Module moved: {_draggedModuleId} after {_dragInsertBeforeModuleId} in {_dragInsertLane} channel."
                : $"Module moved: {_draggedModuleId} before {_dragInsertBeforeModuleId} in {_dragInsertLane} channel."));
        }
        else if (_isModuleDragging && _draggedModuleId is not null && _dragInsertBeforeModuleId is not null)
        {
            MoveModule(_draggedModuleId, _dragInsertBeforeModuleId, _dragInsertAfterLastModule);
            moved = true;
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, _dragInsertAfterLastModule
                ? $"Module moved: {_draggedModuleId} after {_dragInsertBeforeModuleId}."
                : $"Module moved: {_draggedModuleId} before {_dragInsertBeforeModuleId}."));
        }

        if (moved)
        {
            SendFxChainFromCurrentLayout();
        }

        ClearModuleDragState(rebuildChain: wasDragging);
    }

    private void ModuleTile_PointerCanceled(object sender, PointerRoutedEventArgs e) => ClearModuleDragState();

    private void ModuleTile_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_isModuleDragging || sender is not Border { Tag: string moduleId } || !_modules.TryGetValue(moduleId, out var module))
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var isDoubleTap = _lastTappedModuleId == moduleId && now - _lastModuleTapAt <= TimeSpan.FromMilliseconds(550);
        _lastTappedModuleId = moduleId;
        _lastModuleTapAt = now;

        if (isDoubleTap)
        {
            ToggleModule(module);
            e.Handled = true;
            return;
        }

        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module tapped: {module.DisplayName}."));
        _selectedModuleId = moduleId;
        SelectedBlockText.Text = module.DisplayName;
        BuildEffectChain();
        UpdateDivMixContainer();
        BuildParameterPanel();
    }

    private void ToggleModule(ChainModule module)
    {
        var onOffState = _state.Patch.Parameters.FirstOrDefault(parameter =>
            module.ParameterBlocks.Contains(parameter.Definition.Block)
            && parameter.Definition.Kind == ParameterValueKind.Toggle
            && parameter.Definition.DisplayName.Equals("On/Off", StringComparison.OrdinalIgnoreCase));
        if (onOffState is null)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module toggle ignored; {module.DisplayName} has no modeled On/Off parameter."));
            return;
        }

        var nextValue = onOffState.Value == 0 ? 1 : 0;
        AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Module toggle: {module.DisplayName} -> {onOffState.Definition.FormatValue(nextValue)}."));
        SendPendingParameterNow(onOffState.Definition, nextValue);
        BuildEffectChain();
        UpdateDivMixContainer();
        BuildParameterPanel();
    }

    private (string? ModuleId, bool AfterLast) GetModuleInsertionTarget(double pointerX)
    {
        foreach (var tile in EffectChainPanel.Children.OfType<Border>())
        {
            if (tile.Tag is string lockedModuleId && IsLockedModule(lockedModuleId))
            {
                continue;
            }

            var transform = tile.TransformToVisual(EffectChainPanel);
            var left = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0)).X;
            var right = left + tile.ActualWidth;
            var midpoint = left + tile.ActualWidth / 2;

            if (tile == DivMixContainer)
            {
                if (pointerX < left || pointerX > right)
                {
                    continue;
                }

                if (pointerX <= midpoint)
                {
                    return ("DIV", false);
                }

                if (pointerX <= right)
                {
                    return (AfterDivMixTarget, true);
                }

                continue;
            }

            if (tile.Tag is not string moduleId || moduleId == _draggedModuleId)
            {
                continue;
            }

            if (pointerX < left || pointerX > right)
            {
                continue;
            }

            if (pointerX < midpoint)
            {
                return (moduleId, false);
            }

            return (moduleId, true);
        }

        return (null, false);
    }

    private static bool IsLockedModule(string moduleId)
        => moduleId == "MST";

    private void MoveModule(string draggedModuleId, string targetModuleId, bool insertAfterTarget)
    {
        if (targetModuleId == AfterDivMixTarget)
        {
            MoveModuleAfterDivMix(draggedModuleId);
            return;
        }

        if (draggedModuleId == targetModuleId && !insertAfterTarget)
        {
            return;
        }

        var sourceIndex = _moduleOrder.IndexOf(draggedModuleId);
        var targetIndex = _moduleOrder.IndexOf(targetModuleId);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }

        _moduleLanes.Remove(draggedModuleId);
        _moduleOrder.RemoveAt(sourceIndex);
        targetIndex = _moduleOrder.IndexOf(targetModuleId);
        if (targetIndex < 0)
        {
            BuildEffectChain();
            return;
        }

        if (insertAfterTarget)
        {
            targetIndex++;
        }

        _moduleOrder.Insert(targetIndex, draggedModuleId);
        BuildEffectChain();
    }

    private void MoveModuleAfterDivMix(string draggedModuleId)
    {
        if (draggedModuleId is "DIV" or "PrA" or "PrB" or "MIX")
        {
            return;
        }

        if (!_moduleOrder.Contains(draggedModuleId))
        {
            return;
        }

        _moduleLanes.Remove(draggedModuleId);
        _moduleOrder.Remove(draggedModuleId);
        var mixIndex = _moduleOrder.IndexOf("MIX");
        if (mixIndex < 0)
        {
            _moduleOrder.Add(draggedModuleId);
        }
        else
        {
            _moduleOrder.Insert(mixIndex + 1, draggedModuleId);
        }

        BuildEffectChain();
    }

    private void MoveModuleToLane(string draggedModuleId, string lane, string targetModuleId, bool insertAfterTarget)
    {
        if (draggedModuleId is "DIV" or "PrA" or "PrB" or "MIX" || draggedModuleId == targetModuleId)
        {
            return;
        }

        if (!_moduleOrder.Contains(draggedModuleId) || !_moduleOrder.Contains(targetModuleId))
        {
            return;
        }

        _moduleLanes[draggedModuleId] = lane;

        _moduleOrder.Remove(draggedModuleId);
        var targetIndex = _moduleOrder.IndexOf(targetModuleId);
        _moduleOrder.Insert(insertAfterTarget ? targetIndex + 1 : targetIndex, draggedModuleId);

        BuildEffectChain();
    }

    private void ClearModuleDragState(bool rebuildChain = true)
    {
        _dragSourceTile?.ReleasePointerCaptures();
        _dragSourceTile = null;
        _draggedModuleId = null;
        _dragInsertBeforeModuleId = null;
        _dragInsertLane = null;
        _dragInsertLaneAfterLastModule = false;
        _dragInsertAfterLastModule = false;
        _isModuleDragging = false;
        if (rebuildChain)
        {
            BuildEffectChain();
        }
    }

    private void UpdateModuleDragVisuals()
    {
        foreach (var tile in EffectChainPanel.Children.OfType<Border>())
        {
            if (tile == DivMixContainer)
            {
                tile.BorderBrush = _dragInsertBeforeModuleId is "DIV" or AfterDivMixTarget
                    ? GetBrushResource("WarmBrush")
                    : new SolidColorBrush(Colors.SlateGray);
                tile.BorderThickness = _dragInsertBeforeModuleId switch
                {
                    "DIV" => new Thickness(4, 1, 1, 1),
                    AfterDivMixTarget => new Thickness(1, 1, 4, 1),
                    _ => new Thickness(1)
                };
                continue;
            }

            if (tile.Tag is not string moduleId)
            {
                continue;
            }

            var isInsertionTarget = moduleId == _dragInsertBeforeModuleId;
            var isSelected = moduleId == _selectedModuleId;
            tile.BorderBrush = isInsertionTarget || isSelected ? GetBrushResource("WarmBrush") : GetBrushResource("MutedTextBrush");
            tile.BorderThickness = isInsertionTarget
                ? (_dragInsertAfterLastModule ? new Thickness(1, 1, 4, 1) : new Thickness(4, 1, 1, 1))
                : (isSelected ? new Thickness(2) : new Thickness(1));
        }
    }

    private void UpdateDragTargetVisuals()
    {
        UpdateModuleDragVisuals();
        UpdateChannelDropZoneVisuals();
        UpdateLaneModuleDragVisuals(TopChannelPanel);
        UpdateLaneModuleDragVisuals(BottomChannelPanel);
    }

    private void UpdateLaneModuleDragVisuals(StackPanel panel)
    {
        foreach (var tile in panel.Children.OfType<Border>())
        {
            if (tile.Tag is not string moduleId)
            {
                continue;
            }

            var isInsertionTarget = IsLaneInsertionTarget(moduleId);
            var isSelected = _selectedModuleId == moduleId;
            tile.BorderBrush = isSelected || isInsertionTarget ? GetBrushResource("WarmBrush") : GetBrushResource("MutedTextBrush");
            tile.BorderThickness = isInsertionTarget
                ? (_dragInsertLaneAfterLastModule ? new Thickness(1, 1, 4, 1) : new Thickness(4, 1, 1, 1))
                : (isSelected ? new Thickness(2) : new Thickness(1));
        }
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private (string? Lane, string? ModuleId, bool AfterLast) GetDivMixLaneInsertionTarget(global::Windows.Foundation.Point point)
    {
        if (_draggedModuleId is "DIV" or "PrA" or "PrB" or "MIX")
        {
            return (null, null, false);
        }

        if (IsPointInside(TopChannelDropZone, point))
        {
            return GetLaneInsertionTarget(TopLane, TopChannelPanel, point);
        }

        if (IsPointInside(BottomChannelDropZone, point))
        {
            return GetLaneInsertionTarget(BottomLane, BottomChannelPanel, point);
        }

        return (null, null, false);
    }

    private (string Lane, string ModuleId, bool AfterLast) GetLaneInsertionTarget(string lane, StackPanel panel, global::Windows.Foundation.Point point)
    {
        string? lastModuleId = null;
        foreach (var tile in panel.Children.OfType<Border>())
        {
            if (tile.Tag is not string moduleId || moduleId == _draggedModuleId)
            {
                continue;
            }

            lastModuleId = moduleId;
            var transform = tile.TransformToVisual(EffectChainPanel);
            var left = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0)).X;
            var midpoint = left + tile.ActualWidth / 2;
            if (point.X < midpoint)
            {
                return (lane, moduleId, false);
            }
        }

        var anchor = lane == TopLane ? "PrA" : "PrB";
        return (lane, lastModuleId ?? anchor, true);
    }

    private bool IsPointInside(FrameworkElement element, global::Windows.Foundation.Point point)
    {
        var transform = element.TransformToVisual(EffectChainPanel);
        var topLeft = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));
        return point.X >= topLeft.X
            && point.X <= topLeft.X + element.ActualWidth
            && point.Y >= topLeft.Y
            && point.Y <= topLeft.Y + element.ActualHeight;
    }

    private void ApplyFxChainPositions(IReadOnlyList<byte> positions)
    {
        if (positions.Count != FxChain.PositionCount || positions.Distinct().Count() != FxChain.PositionCount)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Ignored invalid FX chain response."));
            return;
        }

        _fxChainPositions = positions.ToArray();
        var divIndex = Array.IndexOf(_fxChainPositions, FxChainDivStart);
        var splitIndex = Array.IndexOf(_fxChainPositions, FxChainChannelSplit);
        var mixIndex = Array.IndexOf(_fxChainPositions, FxChainDivEnd);
        if (divIndex < 0 || splitIndex < 0 || mixIndex < 0 || divIndex >= splitIndex || splitIndex >= mixIndex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Ignored FX chain response with invalid DIV/MIX order."));
            return;
        }

        _moduleOrder.Clear();
        _moduleLanes.Clear();

        foreach (var value in _fxChainPositions)
        {
            if (value == FxChainDivStart)
            {
                _moduleOrder.Add("DIV");
                continue;
            }

            if (value == FxChainDivEnd)
            {
                _moduleOrder.Add("MIX");
                continue;
            }

            if (!_fxChainModules.TryGetValue(value, out var moduleId))
            {
                continue;
            }

            _moduleOrder.Add(moduleId);
            var valueIndex = Array.IndexOf(_fxChainPositions, value);
            if (valueIndex > divIndex && valueIndex < splitIndex)
            {
                _moduleLanes[moduleId] = TopLane;
            }
            else if (valueIndex > splitIndex && valueIndex < mixIndex)
            {
                _moduleLanes[moduleId] = BottomLane;
            }
        }

        EnsureModuleOrder();
        EnsureRequiredLaneModules();
        BuildEffectChain();
        UpdateDivMixContainer();
    }

    private void SendFxChainFromCurrentLayout()
    {
        if (!_isConnected)
        {
            return;
        }

        var current = _fxChainPositions.Length == FxChain.PositionCount
            ? _fxChainPositions.ToArray()
            : FxChain.DefaultOrder.ToArray();
        var desiredKnownValues = BuildKnownFxChainOrderFromLayout();
        var knownValueSet = desiredKnownValues.ToHashSet();
        var desiredIndex = 0;

        for (var i = 0; i < current.Length; i++)
        {
            if (!knownValueSet.Contains(current[i]))
            {
                continue;
            }

            current[i] = desiredKnownValues[desiredIndex++];
        }

        if (desiredIndex != desiredKnownValues.Count || current.Distinct().Count() != FxChain.PositionCount)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "FX chain send skipped; generated chain was invalid."));
            return;
        }

        try
        {
            _midi.SendFxChain(current);
            _fxChainPositions = current;
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Sent FX chain: {string.Join(" ", current.Select(value => value.ToString("X2")))}"));
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private IReadOnlyList<byte> BuildKnownFxChainOrderFromLayout()
    {
        var beforeDiv = _moduleOrder
            .TakeWhile(moduleId => moduleId != "DIV")
            .Where(moduleId => !_moduleLanes.ContainsKey(moduleId))
            .Select(GetFxChainValue)
            .OfType<byte>();
        var top = _moduleOrder
            .Where(moduleId => _moduleLanes.TryGetValue(moduleId, out var lane) && lane == TopLane)
            .Select(GetFxChainValue)
            .OfType<byte>();
        var bottom = _moduleOrder
            .Where(moduleId => _moduleLanes.TryGetValue(moduleId, out var lane) && lane == BottomLane)
            .Select(GetFxChainValue)
            .OfType<byte>();
        var afterMix = _moduleOrder
            .SkipWhile(moduleId => moduleId != "MIX")
            .Skip(1)
            .Where(moduleId => !_moduleLanes.ContainsKey(moduleId))
            .Select(GetFxChainValue)
            .OfType<byte>();

        return beforeDiv
            .Append(FxChainDivStart)
            .Concat(top)
            .Append(FxChainChannelSplit)
            .Concat(bottom)
            .Append(FxChainDivEnd)
            .Concat(afterMix)
            .ToArray();
    }

    private byte? GetFxChainValue(string moduleId)
        => _moduleFxChainValues.TryGetValue(moduleId, out var value) ? value : null;

    private void EnsureRequiredLaneModules()
    {
        if (!_moduleOrder.Contains("PrA"))
        {
            _moduleOrder.Insert(Math.Min(_moduleOrder.Count, Math.Max(0, _moduleOrder.IndexOf("DIV") + 1)), "PrA");
        }

        if (!_moduleOrder.Contains("PrB"))
        {
            var mixIndex = _moduleOrder.IndexOf("MIX");
            _moduleOrder.Insert(mixIndex >= 0 ? mixIndex : _moduleOrder.Count, "PrB");
        }

        _moduleLanes["PrA"] = TopLane;
        _moduleLanes["PrB"] = BottomLane;
    }

    private void EnsureModuleOrder()
    {
        if (_moduleOrder.Count > 0)
        {
            return;
        }

        foreach (var value in FxChain.DefaultOrder)
        {
            if (value == FxChainDivStart)
            {
                _moduleOrder.Add("DIV");
                continue;
            }

            if (value == FxChainDivEnd)
            {
                _moduleOrder.Add("MIX");
                continue;
            }

            if (_fxChainModules.TryGetValue(value, out var moduleId))
            {
                _moduleOrder.Add(moduleId);
            }
        }

        EnsureRequiredLaneModules();
    }

    private IReadOnlyList<string> GetSelectedParameterBlocks()
    {
        return _modules.TryGetValue(_selectedModuleId, out var module)
            ? module.ParameterBlocks
            : [_selectedModuleId];
    }

    private bool IsModuleOn(ChainModule module)
    {
        var onOffStates = _state.Patch.Parameters
            .Where(parameter => module.ParameterBlocks.Contains(parameter.Definition.Block)
                && parameter.Definition.Kind == ParameterValueKind.Toggle
                && parameter.Definition.DisplayName.Equals("On/Off", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return onOffStates.Length == 0 || onOffStates.Any(parameter => parameter.Value != 0);
    }

    private bool HasModeledToggle(ChainModule module)
        => _state.Patch.Parameters.Any(parameter =>
            module.ParameterBlocks.Contains(parameter.Definition.Block)
            && parameter.Definition.Kind == ParameterValueKind.Toggle
            && parameter.Definition.DisplayName.Equals("On/Off", StringComparison.OrdinalIgnoreCase));

    private void BuildParameterPanel()
    {
        _isBuildingParameterPanel = true;
        ParametersPanel.Children.Clear();
        HeaderControlsPanel.Visibility = Visibility.Collapsed;
        HeaderOnOffPanel.Visibility = Visibility.Collapsed;
        HeaderTypePanel.Visibility = Visibility.Collapsed;
        HeaderOnOffCheckBox.Checked -= HeaderOnOffCheckBox_Changed;
        HeaderOnOffCheckBox.Unchecked -= HeaderOnOffCheckBox_Changed;
        HeaderTypeComboBox.SelectionChanged -= HeaderTypeComboBox_SelectionChanged;
        HeaderOnOffCheckBox.Tag = null;
        HeaderTypeComboBox.Tag = null;

        try
        {
            var selectedBlocks = GetSelectedParameterBlocks();
            if (selectedBlocks.Count == 1 && selectedBlocks[0] == "DIV")
            {
                BuildDivParameterPanel();
                return;
            }

            var visibleStates = _state.Patch.Parameters
                .Where(p => selectedBlocks.Contains(p.Definition.Block) && ShouldDisplayParameter(p.Definition))
                .ToList();
            ConfigureHeaderControls(visibleStates);

            BuildCompactParameterPanel(visibleStates.Where(state => !IsHeaderParameter(state.Definition)).ToList());
        }
        finally
        {
            _isBuildingParameterPanel = false;
        }
    }

    private void ConfigureHeaderControls(IReadOnlyList<ParameterState> states)
    {
        var onOffState = states.FirstOrDefault(state => IsOnOffParameter(state.Definition));
        var typeState = states.FirstOrDefault(state => IsPrimaryTypeParameter(state.Definition));

        if (onOffState is not null)
        {
            HeaderOnOffCheckBox.Tag = onOffState.Definition;
            HeaderOnOffCheckBox.IsChecked = onOffState.Value != 0;
            HeaderOnOffCheckBox.Checked += HeaderOnOffCheckBox_Changed;
            HeaderOnOffCheckBox.Unchecked += HeaderOnOffCheckBox_Changed;
            HeaderOnOffPanel.Visibility = Visibility.Visible;
        }

        if (typeState is not null && typeState.Definition.Options is { Count: > 0 } options)
        {
            HeaderTypeComboBox.Tag = typeState.Definition;
            HeaderTypeComboBox.ItemsSource = SortParameterOptions(FilterParameterOptions(typeState.Definition, options));
            HeaderTypeComboBox.SelectedValue = typeState.Value;
            HeaderTypeComboBox.SelectionChanged += HeaderTypeComboBox_SelectionChanged;
            HeaderTypePanel.Visibility = Visibility.Visible;
        }

        HeaderControlsPanel.Visibility = onOffState is not null || typeState is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void HeaderOnOffCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isBuildingParameterPanel || HeaderOnOffCheckBox.Tag is not ParameterDefinition definition)
        {
            return;
        }

        SendParameter(definition, HeaderOnOffCheckBox.IsChecked == true ? 1 : 0);
    }

    private void HeaderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBuildingParameterPanel || HeaderTypeComboBox.Tag is not ParameterDefinition definition)
        {
            return;
        }

        if (HeaderTypeComboBox.SelectedValue is int value)
        {
            SendPendingParameterNow(definition, value);
            if (ShouldRebuildParameterPanelAfterChange(definition))
            {
                BuildParameterPanel();
            }
        }
    }

    private static bool IsOnOffParameter(ParameterDefinition definition)
        => definition.Kind == ParameterValueKind.Toggle
            && definition.DisplayName.Equals("On/Off", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrimaryTypeParameter(ParameterDefinition definition)
        => definition.Kind == ParameterValueKind.Enum
            && definition.DisplayName is "Type" or "FX Type";

    private static bool IsHeaderParameter(ParameterDefinition definition)
        => IsOnOffParameter(definition) || IsPrimaryTypeParameter(definition);

    private static IReadOnlyList<ParameterOption> SortParameterOptions(IReadOnlyList<ParameterOption> options)
        => options
            .OrderBy(option => option.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(option => option.Value)
            .ToArray();

    private static IReadOnlyList<ParameterOption> FilterParameterOptions(
        ParameterDefinition definition,
        IReadOnlyList<ParameterOption> options)
    {
        if (definition.Id == "fx1.type")
        {
            return options
                .Where(option => option.Value is not 0x21 and not 0x22)
                .ToArray();
        }

        return options;
    }

    private void BuildCompactParameterPanel(IReadOnlyList<ParameterState> states)
    {
        var optionStates = states
            .Where(state => state.Definition.Kind is ParameterValueKind.Toggle or ParameterValueKind.Enum)
            .ToList();
        var ringStates = states
            .Where(state => state.Definition.Kind == ParameterValueKind.Integer)
            .ToList();

        if (optionStates.Count > 0)
        {
            ParametersPanel.Children.Add(CreateOptionGrid(optionStates));
        }

        if (ringStates.Count > 0)
        {
            ParametersPanel.Children.Add(CreateRingGrid(ringStates));
        }
    }

    private Grid CreateOptionGrid(IReadOnlyList<ParameterState> states)
    {
        const int columns = 3;
        var grid = new Grid
        {
            ColumnSpacing = 24,
            RowSpacing = 8,
            Margin = new Thickness(0, 0, 0, 14)
        };

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < states.Count; index++)
        {
            var row = index / columns;
            while (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var editor = CreateInlineParameterEditor(states[index]);
            Grid.SetColumn(editor, index % columns);
            Grid.SetRow(editor, row);
            grid.Children.Add(editor);
        }

        return grid;
    }

    private Grid CreateRingGrid(IReadOnlyList<ParameterState> states)
    {
        const int columns = 6;
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12,
            Margin = new Thickness(0, 0, 0, 8)
        };

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        }

        for (var index = 0; index < states.Count; index++)
        {
            var row = index / columns;
            while (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var ring = CreateParameterRing(states[index]);
            Grid.SetColumn(ring, index % columns);
            Grid.SetRow(ring, row);
            grid.Children.Add(ring);
        }

        return grid;
    }

    private FrameworkElement CreateInlineParameterEditor(ParameterState state)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        row.Children.Add(new TextBlock
        {
            Text = state.Definition.DisplayName,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        });

        var editor = CreateParameterEditor(state);
        row.Children.Add(editor);
        return row;
    }

    private bool ShouldDisplayParameter(ParameterDefinition definition)
    {
        if (definition.Block == "DIV")
        {
            var dividerMode = GetParameterValue("divider.mode");
            return dividerMode == 0
                ? definition.Id is "divider.mode" or "divider.channelSelect"
                : definition.Id != "divider.channelSelect";
        }

        if (definition.Block is "FX1" or "FX2")
        {
            if (definition.Id is "fx1.on" or "fx1.type" or "fx2.on" or "fx2.type")
            {
                return true;
            }

            var typeValue = GetParameterValue(definition.Block == "FX1" ? "fx1.type" : "fx2.type");
            return _fxTypeParameterGroups.TryGetValue(typeValue, out var group)
                && definition.Id.StartsWith($"{definition.Block.ToLowerInvariant()}.{group}.", StringComparison.Ordinal);
        }

        return true;
    }

    private static bool ShouldRebuildParameterPanelAfterChange(ParameterDefinition definition)
        => definition.Id is "divider.mode" or "fx1.type" or "fx2.type";

    private void BuildDivParameterPanel()
    {
        var mode = GetParameterState("divider.mode");
        if (mode is null)
        {
            return;
        }

        ParametersPanel.Children.Add(CreateCompactParameterRow(mode, 120));

        if (mode.Value == 0)
        {
            if (GetParameterState("divider.channelSelect") is { } channelSelect)
            {
                ParametersPanel.Children.Add(CreateCompactParameterRow(channelSelect, 120));
            }

            return;
        }

        var channels = new Grid
        {
            ColumnSpacing = 24,
            Margin = new Thickness(0, 14, 0, 0)
        };
        channels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        channels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });

        var channelA = CreateDivChannelPanel(
            "Channel A",
            "divider.channelA.dynamic",
            "divider.channelA.dynamicSens",
            "divider.channelA.filter",
            "divider.channelA.cutoff");
        Grid.SetColumn(channelA, 0);
        channels.Children.Add(channelA);

        var channelB = CreateDivChannelPanel(
            "Channel B",
            "divider.channelB.dynamic",
            "divider.channelB.dynamicSens",
            "divider.channelB.filter",
            "divider.channelB.cutoff");
        Grid.SetColumn(channelB, 1);
        channels.Children.Add(channelB);

        ParametersPanel.Children.Add(channels);
    }

    private StackPanel CreateDivChannelPanel(string title, string dynamicId, string sensId, string filterId, string cutoffId)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (GetParameterState(dynamicId) is { } dynamic)
        {
            panel.Children.Add(CreateCompactParameterRow(dynamic, 150));
        }

        if (GetParameterState(sensId) is { } sens)
        {
            var ring = CreateParameterRing(sens);
            ring.Margin = new Thickness(8, 2, 0, 6);
            panel.Children.Add(ring);
        }

        if (GetParameterState(filterId) is { } filter)
        {
            panel.Children.Add(CreateCompactParameterRow(filter, 150));
        }

        if (GetParameterState(cutoffId) is { } cutoff)
        {
            panel.Children.Add(CreateCompactParameterRow(cutoff, 150));
        }

        return panel;
    }

    private Grid CreateCompactParameterRow(ParameterState state, double labelWidth)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = state.Definition.DisplayName,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(label);

        var editor = CreateParameterEditor(state);
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);
        return row;
    }

    private FrameworkElement CreateParameterEditor(ParameterState state)
    {
        if (state.Definition.Kind == ParameterValueKind.Toggle)
        {
            var checkBox = new CheckBox
            {
                IsChecked = state.Value != 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.Checked += (_, _) => SendParameter(state.Definition, 1);
            checkBox.Unchecked += (_, _) => SendParameter(state.Definition, 0);
            return checkBox;
        }

        if (state.Definition.Kind == ParameterValueKind.Enum && state.Definition.Options is { Count: > 0 } options)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = SortParameterOptions(FilterParameterOptions(state.Definition, options)),
                DisplayMemberPath = nameof(ParameterOption.Label),
                SelectedValuePath = nameof(ParameterOption.Value),
                SelectedValue = state.Value,
                MinWidth = 96,
                VerticalAlignment = VerticalAlignment.Center
            };
            comboBox.SelectionChanged += (_, _) =>
            {
                if (_isBuildingParameterPanel)
                {
                    return;
                }

                if (comboBox.SelectedValue is int value)
                {
                    SendPendingParameterNow(state.Definition, value);
                    if (ShouldRebuildParameterPanelAfterChange(state.Definition))
                    {
                        BuildParameterPanel();
                    }
                }
            };
            return comboBox;
        }

        return CreateParameterRing(state);
    }

    private ParameterRingControl CreateParameterRing(ParameterState state)
    {
        var ringPresentation = GetParameterRingPresentation(state.Definition);
        var ring = new ParameterRingControl(
            state.Definition.DisplayName,
            state.Definition.RawMinimum,
            state.Definition.RawMaximum,
            state.Value,
            ringPresentation.ResetValue,
            ringPresentation.FormatValue,
            ringPresentation.IsCentered);

        ring.ValueChangedByUser += (_, value) => PreviewParameter(state.Definition, value);
        ring.ValueCommitted += (_, value) => SendPendingParameterNow(state.Definition, value);
        return ring;
    }

    private ParameterState? GetParameterState(string parameterId)
        => _state.Patch.Parameters.FirstOrDefault(parameter => parameter.Definition.Id == parameterId);

    private int GetParameterValue(string parameterId)
        => _state.Patch.Parameters.FirstOrDefault(parameter => parameter.Definition.Id == parameterId)?.Value ?? 0;

    private sealed record ParameterRingPresentation(
        int ResetValue,
        Func<int, string> FormatValue,
        bool IsCentered);

    private static ParameterRingPresentation GetParameterRingPresentation(ParameterDefinition definition)
    {
        if (IsCenteredParameter(definition))
        {
            var center = GetParameterCenter(definition);
            return new ParameterRingPresentation(
                center,
                value => FormatCenteredParameter(definition, value, center),
                true);
        }

        return new ParameterRingPresentation(
            definition.RawMinimum,
            definition.FormatValue,
            false);
    }

    private static bool IsCenteredParameter(ParameterDefinition definition)
        => definition.Id is
            "odds.tone" or
            "odds.bottom" or
            "comp.tone" or
            "preampA.tcomp" or
            "preampA.bass" or
            "preampA.middle" or
            "preampA.treble" or
            "preampB.tcomp" or
            "preampB.bass" or
            "preampB.middle" or
            "preampB.treble" or
            "mix.balance" or
            "eq.lowGain" or
            "eq.lowMidGain" or
            "eq.highMidGain" or
            "eq.highGain" or
            "master.lowGain" or
            "master.midGain" or
            "master.highGain" or
            "eq.level";

    private static int GetParameterCenter(ParameterDefinition definition)
        => definition.RawMinimum + ((definition.RawMaximum - definition.RawMinimum) / 2);

    private static string FormatCenteredParameter(ParameterDefinition definition, int value, int center)
    {
        var centeredValue = value - center;
        var prefix = centeredValue > 0 ? "+" : string.Empty;
        var formattedValue = $"{prefix}{centeredValue}";

        return definition.Unit is { Length: > 0 }
            ? $"{formattedValue} {definition.Unit}"
            : formattedValue;
    }

    private void PreviewParameter(ParameterDefinition definition, int value)
    {
        SetParameterState(definition, value, ParameterSyncStatus.Dirty);
        _pendingValues[definition.Id] = value;
        _pendingDefinitions[definition.Id] = definition;
    }

    private void SendParameter(ParameterDefinition definition, int value)
    {
        SetParameterState(definition, value, ParameterSyncStatus.Dirty);

        try
        {
            _pendingValues[definition.Id] = value;
            _pendingDefinitions[definition.Id] = definition;
            if (!_sendTimers.TryGetValue(definition.Id, out var timer))
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    SendPendingParameterNow(definition);
                };
                _sendTimers[definition.Id] = timer;
            }

            timer.Stop();
            timer.Start();
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private void SendPendingParameterNow(ParameterDefinition definition)
    {
        if (!_pendingValues.TryGetValue(definition.Id, out var value))
        {
            return;
        }

        SendPendingParameterNow(definition, value);
    }

    private void SendPendingParameterNow(ParameterDefinition definition, int value)
    {
        if (IsParameterAlreadySent(definition, value))
        {
            return;
        }

        if (!_isConnected)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, "Connect MIDI ports before editing parameters."));
            return;
        }

        _pendingValues[definition.Id] = value;
        _pendingDefinitions[definition.Id] = definition;

        if (_sendTimers.TryGetValue(definition.Id, out var timer))
        {
            timer.Stop();
        }

        try
        {
            _midi.SendTemporaryParameter(_pendingDefinitions.GetValueOrDefault(definition.Id, definition), value);
            SetParameterState(definition, value, ParameterSyncStatus.Sent);
            ScheduleParameterConfirmation(definition, value);
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Sent {definition.Block}: {definition.DisplayName} = {definition.FormatValue(value)}"));
        }
        catch (Exception ex)
        {
            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Error, ex.Message));
        }
    }

    private bool IsParameterAlreadySent(ParameterDefinition definition, int value)
    {
        var parameter = _state.Patch.Parameters.FirstOrDefault(parameter => parameter.Definition.Id == definition.Id);
        return parameter?.Value == value && parameter.Status == ParameterSyncStatus.Sent;
    }

    private void ScheduleParameterConfirmation(ParameterDefinition definition, int expectedValue)
    {
        if (_pendingConfirmations.Remove(definition.Id, out var existing))
        {
            existing.VerifyTimer.Stop();
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!_isConnected)
            {
                return;
            }

            AddLog(new AppLogEntry(DateTimeOffset.Now, AppLogDirection.Info, $"Verifying {definition.Block}: {definition.DisplayName}."));
            _midi.RequestTemporaryParameter(definition);
        };

        _pendingConfirmations[definition.Id] = new PendingParameterConfirmation(
            definition,
            expectedValue,
            DateTimeOffset.Now.AddMilliseconds(250),
            timer);
        timer.Start();
    }

    private bool ShouldIgnoreParameterSync(ParameterDefinition definition, int value)
    {
        if (!_pendingConfirmations.TryGetValue(definition.Id, out var pending))
        {
            return false;
        }

        if (value == pending.ExpectedValue)
        {
            return false;
        }

        if (DateTimeOffset.Now > pending.IgnoreMismatchesUntil)
        {
            return false;
        }

        AddLog(new AppLogEntry(
            DateTimeOffset.Now,
            AppLogDirection.Info,
            $"Ignored stale sync for {definition.Block}: {definition.DisplayName} = {definition.FormatValue(value)}; awaiting {definition.FormatValue(pending.ExpectedValue)}."));
        return true;
    }

    private void CompleteParameterConfirmation(ParameterDefinition definition, int value)
    {
        if (!_pendingConfirmations.Remove(definition.Id, out var pending))
        {
            return;
        }

        pending.VerifyTimer.Stop();
        if (value != pending.ExpectedValue)
        {
            AddLog(new AppLogEntry(
                DateTimeOffset.Now,
                AppLogDirection.Info,
                $"Device confirmed {definition.Block}: {definition.DisplayName} = {definition.FormatValue(value)} instead of {definition.FormatValue(pending.ExpectedValue)}."));
        }
    }

    private void AddLog(AppLogEntry entry)
    {
        _state.LogEntries.Insert(0, entry);
        while (_state.LogEntries.Count > 500)
        {
            _state.LogEntries.RemoveAt(_state.LogEntries.Count - 1);
        }

        try
        {
            File.AppendAllText(
                _logFilePath,
                $"{entry.Timestamp:O}\t{entry.Direction}\t{entry.Message}\t{entry.BytesText}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never interrupt live MIDI editing.
        }
    }

    private static string CreateLogFilePath()
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        return Path.Combine(logDirectory, $"gt001-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _isConnected = false;
        _syncAwaitingReply = false;
        _isConnecting = false;
        _isWritingPatch = false;
        _pendingWriteTarget = null;
        _pendingWriteName = string.Empty;
        _syncTimeoutTimer?.Stop();
        _writeTimeoutTimer?.Stop();
        _patchNameRequestTimer?.Stop();
        UpdateBusyIndicator();
        ClearParameterConfirmations();
        _midi.Dispose();
    }

    private void Disconnect()
    {
        _syncAwaitingReply = false;
        _isConnecting = false;
        _isWritingPatch = false;
        _pendingWriteTarget = null;
        _pendingWriteName = string.Empty;
        _identityReplySeen = false;
        _syncRequestedAfterConnect = false;
        _syncRetryCount = 0;
        _syncTimeoutTimer?.Stop();
        _writeTimeoutTimer?.Stop();
        _patchNameRequestTimer?.Stop();
        _patchNameRequestTabs.Clear();
        _patchNameRequestQueue.Clear();
        UpdateBusyIndicator();
        UpdateWriteButtonState();
        ClearParameterConfirmations();
        _midi.Close();
        _isConnected = false;
        ConnectionStatusButton.Content = "Disconnected";
        ConnectButton.Content = "Connect";
    }

    private void ClearParameterConfirmations()
    {
        foreach (var confirmation in _pendingConfirmations.Values)
        {
            confirmation.VerifyTimer.Stop();
        }

        _pendingConfirmations.Clear();
    }

    private void SetParameterState(ParameterDefinition definition, int value, ParameterSyncStatus status)
    {
        _state.Patch = _state.Patch.WithParameterValue(definition.Id, value, status);

        if (definition.Kind == ParameterValueKind.Toggle
            && definition.DisplayName.Equals("On/Off", StringComparison.OrdinalIgnoreCase))
        {
            BuildEffectChain();
            UpdateDivMixContainer();
        }

        UpdatePatchStatusText();
    }

    private void UpdatePatchStatusText()
    {
        UpdateBusyIndicator();
        UpdateWriteButtonState();

        if (_syncAwaitingReply)
        {
            PatchStatusText.Text = "Syncing...";
            PatchStatusText.Foreground = GetBrushResource("MutedTextBrush");
            return;
        }

        if (_isWritingPatch)
        {
            PatchStatusText.Text = "Writing...";
            PatchStatusText.Foreground = GetBrushResource("WarmBrush");
            return;
        }

        var dirty = _state.Patch.CountByStatus(ParameterSyncStatus.Dirty);
        var sent = _state.Patch.CountByStatus(ParameterSyncStatus.Sent);
        var synced = _state.Patch.CountByStatus(ParameterSyncStatus.Synced);
        var unknown = _state.Patch.CountByStatus(ParameterSyncStatus.Unknown);

        if (dirty > 0)
        {
            PatchStatusText.Text = $"{dirty} dirty";
            PatchStatusText.Foreground = new SolidColorBrush(Colors.Orange);
        }
        else if (sent > 0)
        {
            PatchStatusText.Text = $"{sent} sent";
            PatchStatusText.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
        }
        else if (synced > 0 && unknown == 0)
        {
            PatchStatusText.Text = "Synced";
            PatchStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
        }
        else if (synced > 0)
        {
            PatchStatusText.Text = $"{synced} synced, {unknown} unknown";
            PatchStatusText.Foreground = GetBrushResource("MutedTextBrush");
        }
        else
        {
            PatchStatusText.Text = "Not synced";
            PatchStatusText.Foreground = GetBrushResource("MutedTextBrush");
        }
    }

    private void UpdateBusyIndicator()
    {
        var isBusy = _isConnecting || _syncAwaitingReply || _isWritingPatch;
        BusyOverlay.Visibility = isBusy
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (isBusy)
        {
            StartBusyAnimation();
        }
        else
        {
            StopBusyAnimation();
        }
    }

    private void StartBusyAnimation()
    {
        _busyAnimationFrame = 0;
        UpdateBusyAnimationText();
        _busyAnimationTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _busyAnimationTimer.Tick -= BusyAnimationTimer_Tick;
        _busyAnimationTimer.Tick += BusyAnimationTimer_Tick;
        _busyAnimationTimer.Start();
    }

    private void StopBusyAnimation()
    {
        _busyAnimationTimer?.Stop();
        BusyText.Text = "Working...";
    }

    private void BusyAnimationTimer_Tick(object? sender, object e)
    {
        _busyAnimationFrame = (_busyAnimationFrame + 1) % 4;
        UpdateBusyAnimationText();
    }

    private void UpdateBusyAnimationText()
    {
        var action = _isWritingPatch
            ? "Writing"
            : _syncAwaitingReply
                ? "Syncing"
                : "Connecting";
        BusyText.Text = $"{action} GT-001{new string('.', _busyAnimationFrame)}";
    }

    private void UpdateWriteButtonState()
    {
        if (WritePatchButton is null)
        {
            return;
        }

        WritePatchButton.IsEnabled = _isConnected
            && !_isConnecting
            && !_syncAwaitingReply
            && !_isWritingPatch;
    }

    private static Brush GetBrushResource(string key)
    {
        return Application.Current.Resources[key] is Brush brush
            ? brush
            : new SolidColorBrush(Colors.White);
    }
}
