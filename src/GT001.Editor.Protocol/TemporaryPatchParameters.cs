namespace GT001.Editor.Protocol;

public static class TemporaryPatchParameters
{
    private static ParameterOption[] Options(params string[] labels)
        => labels.Select((label, value) => new ParameterOption(value, label)).ToArray();

    private static readonly ParameterOption[] OnOff =
    [
        new(0, "Off"),
        new(1, "On")
    ];

    private static readonly ParameterOption[] CompType = Options(
        "BOSS COMP", "HI-BAND", "LIGHT", "D-COMP", "ORANGE", "FAT", "MILD", "STEREO COMP");

    private static readonly ParameterOption[] OdDsType =
    [
        new(0x00, "MID BOOST"),
        new(0x01, "CLEAN BOOST"),
        new(0x02, "TREBLE BOOST"),
        new(0x03, "CRUNCH"),
        new(0x04, "NATURAL OD"),
        new(0x05, "WARM OD"),
        new(0x06, "FAT DS"),
        new(0x07, "LEAD DS"),
        new(0x08, "METAL DS"),
        new(0x09, "OCT FUZZ"),
        new(0x0A, "BLUES OD"),
        new(0x0B, "OD-1"),
        new(0x0C, "T-SCREAM"),
        new(0x0D, "TURBO OD"),
        new(0x0E, "DIST"),
        new(0x0F, "RAT"),
        new(0x10, "GUV DS"),
        new(0x11, "DST+"),
        new(0x12, "METAL ZONE"),
        new(0x13, "'60S FUZZ"),
        new(0x14, "MUFF FUZZ"),
        new(0x16, "A-DIST")
    ];

    private static readonly ParameterOption[] PreampType =
    [
        new(0x00, "NATURAL CLEAN"),
        new(0x01, "FULL RANGE"),
        new(0x02, "COMBO CRUNCH"),
        new(0x03, "STACK CRUNCH"),
        new(0x04, "HiGAIN STACK"),
        new(0x05, "POWER DRIVE"),
        new(0x06, "EXTREME LEAD"),
        new(0x07, "CORE METAL"),
        new(0x08, "JC-120"),
        new(0x09, "CLEAN TWIN"),
        new(0x0A, "PRO CRUNCH"),
        new(0x0B, "TWEED"),
        new(0x0C, "DELUXE CRUNCH"),
        new(0x0D, "VO DRIVE"),
        new(0x0E, "VO LEAD"),
        new(0x0F, "MATCH DRIVE"),
        new(0x10, "BG LEAD"),
        new(0x11, "BG DRIVE"),
        new(0x12, "MS1959 I"),
        new(0x13, "MS1959 I+II"),
        new(0x14, "R-FIER VINTAGE"),
        new(0x15, "R-FIER MODERN"),
        new(0x16, "T-AMP LEAD"),
        new(0x17, "SLDN"),
        new(0x18, "5150 DRIVE"),
        new(0x1A, "BGNR UB"),
        new(0x1B, "ORNG RB")
    ];

    private static readonly ParameterOption[] GainSwitch = Options("Low", "Middle", "High");

    private static readonly ParameterOption[] SpeakerType = Options(
        "Off", "Original", "1x8", "1x10", "1x12", "2x12", "4x10", "4x12", "8x12");

    private static readonly ParameterOption[] MicType = Options("DYN57", "DYN421", "CND451", "CND87", "FLAT");

    private static readonly ParameterOption[] MicDistance = Options("Off Mic", "On Mic");

    private static readonly ParameterOption[] LowFrequency = Options(
        "Flat", "20.0 Hz", "25.0 Hz", "31.5 Hz", "40.0 Hz", "50.0 Hz", "63.0 Hz", "80.0 Hz",
        "100 Hz", "125 Hz", "160 Hz", "200 Hz", "250 Hz", "315 Hz", "400 Hz", "500 Hz", "630 Hz", "800 Hz");

    private static readonly ParameterOption[] MidFrequency = Options(
        "20.0 Hz", "25.0 Hz", "31.5 Hz", "40.0 Hz", "50.0 Hz", "63.0 Hz", "80.0 Hz", "100 Hz",
        "125 Hz", "160 Hz", "200 Hz", "250 Hz", "315 Hz", "400 Hz", "500 Hz", "630 Hz",
        "800 Hz", "1.00 kHz", "1.25 kHz", "1.60 kHz", "2.00 kHz", "2.50 kHz", "3.15 kHz",
        "4.00 kHz", "5.00 kHz", "6.30 kHz", "8.00 kHz", "10.0 kHz");

    private static readonly ParameterOption[] MidQ = Options("0.5", "1", "2", "4", "8", "16");

    private static readonly ParameterOption[] HighFrequency = Options(
        "630 Hz", "800 Hz", "1.00 kHz", "1.25 kHz", "1.60 kHz", "2.00 kHz", "2.50 kHz",
        "3.15 kHz", "4.00 kHz", "5.00 kHz", "6.30 kHz", "8.00 kHz", "10.0 kHz", "12.5 kHz", "Flat");

    private static readonly ParameterOption[] FxType = Options(
        "T.WAH", "AUTO WAH", "SUB WAH", "ADV.COMP", "LIMITER", "SUB OD/DS", "GRAPHIC EQ", "PARAMETRIC EQ",
        "TONE MODIFY", "GUITAR SIM", "SLOW GEAR", "DEFRETTER", "WAVE SYNTH", "SITAR SIM", "OCTAVE",
        "PITCH SHIFTER", "HARMONIST", "SOUND HOLD", "AC.PROCESSOR", "PHASER", "FLANGER", "TREMOLO",
        "ROTARY 1", "UNI-V", "PAN", "SLICER", "VIBRATO", "RING MOD", "HUMANIZER", "2X2 CHORUS",
        "SUB DELAY", "AC SIM", "ROTARY 2", "TERA ECHO", "OVERTONE");

    private static readonly ParameterOption[] WahMode = Options("LPF", "BPF");
    private static readonly ParameterOption[] WahPolarity = Options("Down", "Up");
    private static readonly ParameterOption[] LimiterType = Options("BOSS LIMITER", "RACK 160D", "VTG RACK U");
    private static readonly ParameterOption[] LimiterRatio = Options("1:1", "1.2:1", "1.5:1", "2:1", "2.8:1", "4:1", "8:1", "16:1", "INF:1", "1.0:1", "1.5:1", "2.0:1", "4.0:1", "8.0:1", "16.0:1", "INF:1", "1.0:1", "2.0:1");
    private static readonly ParameterOption[] ToneModifyType = Options("FAT", "PRESENCE", "MILD", "TIGHT", "ENHANCE", "RESONATOR1", "RESONATOR2", "RESONATOR3");
    private static readonly ParameterOption[] GuitarSimType = Options("S -> H", "H -> S", "H -> HF", "S -> HOLLOW", "H -> HOLLOW", "S -> AC", "H -> AC", "P -> AC");
    private static readonly ParameterOption[] WaveSynthWave = Options("Saw", "Square");
    private static readonly ParameterOption[] OctaveRange = Options("B1-E6", "B1-E5", "B1-E4", "B1-E3");
    private static readonly ParameterOption[] VoiceType = Options("1-Voice", "2-Mono", "2-Stereo");
    private static readonly ParameterOption[] PitchShiftMode = Options("Fast", "Medium", "Slow", "Mono");
    private static readonly ParameterOption[] AcProcessorType = Options("Small", "Medium", "Bright", "Power");
    private static readonly ParameterOption[] PhaserType = Options("4 Stage", "8 Stage", "12 Stage", "Bi-Phase");
    private static readonly ParameterOption[] SpeedSelect = Options("Slow", "Fast");
    private static readonly ParameterOption[] PanType = Options("Auto", "Manual");
    private static readonly ParameterOption[] RingModMode = Options("Normal", "Intelligent");
    private static readonly ParameterOption[] HumanizerMode = Options("Picking", "Auto");
    private static readonly ParameterOption[] Vowel = Options("a", "e", "i", "o", "u");
    private static readonly ParameterOption[] SubDelayType = Options("Mono", "Pan");

    private static readonly ParameterOption[] DelayType = Options(
        "Single", "Pan", "Stereo", "Dual-S", "Dual-P", "Dual-L/R", "Reverse", "Analog", "Tape", "Mod", "SDE-3000");

    private static readonly ParameterOption[] ChorusMode = Options("Mono", "Stereo 1", "Stereo 2");

    private static readonly ParameterOption[] ReverbType = Options(
        "Ambience", "Room", "Hall 1", "Hall 2", "Plate", "Spring", "Modulate");

    private static readonly ParameterOption[] PedalBendPitch = Options(
        "-24", "-23", "-22", "-21", "-20", "-19", "-18", "-17", "-16", "-15", "-14", "-13", "-12",
        "-11", "-10", "-9", "-8", "-7", "-6", "-5", "-4", "-3", "-2", "-1", "0", "+1", "+2", "+3",
        "+4", "+5", "+6", "+7", "+8", "+9", "+10", "+11", "+12", "+13", "+14", "+15", "+16", "+17",
        "+18", "+19", "+20", "+21", "+22", "+23", "+24");

    private static readonly ParameterOption[] WahType = Options(
        "CRY WAH", "VO WAH", "FAT WAH", "LIGHT WAH", "7STRING WAH", "RESO WAH");

    private static readonly ParameterOption[] FootVolumeCurve = Options("Slow 1", "Slow 2", "Normal", "Fast");

    private static readonly ParameterOption[] NoiseSuppressorDetect = Options("Input", "NS Input", "FV Out");

    private static readonly ParameterOption[] AccelType = Options(
        "S-Bend", "Laser Beam", "Ring Mod", "Twist", "Warp", "Feedbacker");

    private static readonly ParameterOption[] SBendPitch = Options("-3 oct", "-2 oct", "-1 oct", "+1 oct", "+2 oct", "+3 oct", "+4 oct");

    private static readonly ParameterOption[] FeedbackerMode = Options("Normal", "Osc");

    private static readonly ParameterOption[] MasterKey = Options(
        "C (Am)", "Db (Bbm)", "D (Bm)", "Eb (Cm)", "E (C#m)", "F (Dm)",
        "F# (D#m)", "G (Em)", "Ab (Fm)", "A (F#m)", "Bb (Gm)", "B (G#m)");

    private static readonly ParameterOption[] MasterBeat = Options(
        "1/1", "2/1", "3/1", "4/1", "5/1", "6/1", "7/1", "8/1",
        "1/2", "2/2", "3/2", "4/2", "5/2", "6/2", "7/2", "8/2",
        "1/4", "2/4", "3/4", "4/4", "5/4", "6/4", "7/4", "8/4",
        "1/8", "2/8", "3/8", "4/8", "5/8", "6/8", "7/8", "8/8");

    private static readonly ParameterOption[] DivMode =
    [
        new(0, "Single"),
        new(1, "Dual")
    ];

    private static readonly ParameterOption[] DivChannel =
    [
        new(0, "Ch. A"),
        new(1, "Ch. B")
    ];

    private static readonly ParameterOption[] DivDynamic =
    [
        new(0, "Off"),
        new(1, "POLAR-"),
        new(2, "POLAR+")
    ];

    private static readonly ParameterOption[] DivFilter =
    [
        new(0, "Off"),
        new(1, "LPF"),
        new(2, "HPF")
    ];

    private static readonly ParameterOption[] CutoffFrequency =
    [
        new(0, "100 Hz"),
        new(1, "125 Hz"),
        new(2, "160 Hz"),
        new(3, "200 Hz"),
        new(4, "250 Hz"),
        new(5, "315 Hz"),
        new(6, "400 Hz"),
        new(7, "500 Hz"),
        new(8, "630 Hz"),
        new(9, "800 Hz"),
        new(10, "1.00 kHz"),
        new(11, "1.25 kHz"),
        new(12, "1.60 kHz"),
        new(13, "2.00 kHz"),
        new(14, "2.50 kHz"),
        new(15, "3.15 kHz"),
        new(16, "4.00 kHz")
    ];

    private static readonly ParameterOption[] MixMode =
    [
        new(0, "Stereo"),
        new(1, "Pan L/R")
    ];

    private sealed record FxParameterTemplate(
        string Group,
        string Name,
        int Offset,
        int Size,
        int Minimum,
        int Maximum,
        ParameterValueKind Kind = ParameterValueKind.Integer,
        string? Unit = null,
        IReadOnlyList<ParameterOption>? Options = null);

    private static ParameterDefinition FxParameter(
        int fxNumber,
        FxParameterTemplate template)
    {
        var block = $"FX{fxNumber}";
        var idName = char.ToLowerInvariant(template.Name[0]) + template.Name[1..]
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace("/", string.Empty)
            .Replace("-", string.Empty);

        return new(
            $"fx{fxNumber}.{template.Group}.{idName}",
            template.Name,
            block,
            Gt001Address.FromLinearValue(MidiWordToLinear(template.Offset)),
            template.Size,
            template.Minimum,
            template.Maximum,
            template.Kind,
            template.Unit,
            template.Options);
    }

    private static int MidiWordToLinear(int wordOffset)
        => ((wordOffset >> 8) * 0x80) + (wordOffset & 0x7F);

    private static int LinearToMidiWord(int linearOffset)
    {
        var address = Gt001Address.FromLinearValue(linearOffset);
        return (address.B2 << 8) | address.B3;
    }

    private static IReadOnlyList<ParameterDefinition> BuildFxParameters()
    {
        var fx1 = BuildStandardFxTemplates(0x0142).Select(template => FxParameter(1, template));
        var fx2 = BuildStandardFxTemplates(0x034E).Select(template => FxParameter(2, template));
        var special = new[]
        {
            FxParameter(1, new("acSim", "Body", 0x0504, 1, 0, 100)),
            FxParameter(1, new("acSim", "Low", 0x0505, 1, 0, 100)),
            FxParameter(1, new("acSim", "High", 0x0506, 1, 0, 100)),
            FxParameter(1, new("acSim", "Level", 0x0507, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Speed Select", 0x0508, 1, 0, 1, ParameterValueKind.Enum, Options: SpeedSelect)),
            FxParameter(1, new("rotary2", "Rate Slow", 0x0509, 1, 0, 0x71)),
            FxParameter(1, new("rotary2", "Rate Fast", 0x050A, 1, 0, 0x71)),
            FxParameter(1, new("rotary2", "Depth", 0x050B, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Rise Time", 0x050C, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Fall Time", 0x050D, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Balance", 0x050E, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Level", 0x050F, 1, 0, 100)),
            FxParameter(1, new("rotary2", "Direct Mix", 0x0510, 1, 0, 100)),
            FxParameter(2, new("acSim", "Body", 0x0511, 1, 0, 100)),
            FxParameter(2, new("acSim", "Low", 0x0512, 1, 0, 100)),
            FxParameter(2, new("acSim", "High", 0x0513, 1, 0, 100)),
            FxParameter(2, new("acSim", "Level", 0x0514, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Speed Select", 0x0515, 1, 0, 1, ParameterValueKind.Enum, Options: SpeedSelect)),
            FxParameter(2, new("rotary2", "Rate Slow", 0x0516, 1, 0, 0x71)),
            FxParameter(2, new("rotary2", "Rate Fast", 0x0517, 1, 0, 0x71)),
            FxParameter(2, new("rotary2", "Depth", 0x0518, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Rise Time", 0x0519, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Fall Time", 0x051A, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Balance", 0x051B, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Level", 0x051C, 1, 0, 100)),
            FxParameter(2, new("rotary2", "Direct Mix", 0x051D, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Mode", 0x051E, 1, 0, 1, ParameterValueKind.Enum, Options: Options("Mono", "Stereo"))),
            FxParameter(2, new("teraEcho", "Spread Time", 0x051F, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Feedback", 0x0520, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Effect Level", 0x0521, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Tone", 0x0522, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Direct Mix", 0x0523, 1, 0, 100)),
            FxParameter(2, new("teraEcho", "Hold", 0x0524, 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff)),
            FxParameter(2, new("overtone", "Upper Level", 0x0525, 1, 0, 100)),
            FxParameter(2, new("overtone", "Lower Level", 0x0526, 1, 0, 100)),
            FxParameter(2, new("overtone", "Direct Mix", 0x0527, 1, 0, 100)),
            FxParameter(2, new("overtone", "Detune", 0x0528, 1, 0, 100)),
            FxParameter(2, new("overtone", "Tone", 0x0529, 1, 0, 100))
        };

        return fx1.Concat(fx2).Concat(special).ToArray();
    }

    private static IReadOnlyList<FxParameterTemplate> BuildStandardFxTemplates(int baseOffset)
    {
        var delta = MidiWordToLinear(baseOffset) - MidiWordToLinear(0x0142);
        FxParameterTemplate P(string group, string name, int fx1Offset, int size, int max, ParameterValueKind kind = ParameterValueKind.Integer, IReadOnlyList<ParameterOption>? options = null, string? unit = null, int min = 0)
            => new(group, name, LinearToMidiWord(MidiWordToLinear(fx1Offset) + delta), size, min, max, kind, unit, options);

        return
        [
            P("subOdDs", "Type", 0x0142, 1, 0x14, ParameterValueKind.Enum, OdDsType), P("subOdDs", "Drive", 0x0143, 1, 120), P("subOdDs", "Bottom", 0x0144, 1, 100), P("subOdDs", "Tone", 0x0145, 1, 100), P("subOdDs", "Solo Sw", 0x0146, 1, 1, ParameterValueKind.Toggle, OnOff), P("subOdDs", "Solo Level", 0x0147, 1, 100), P("subOdDs", "Effect Level", 0x0148, 1, 100), P("subOdDs", "Direct Mix", 0x0149, 1, 100),
            P("touchWah", "Mode", 0x014C, 1, 1, ParameterValueKind.Enum, WahMode), P("touchWah", "Polarity", 0x014D, 1, 1, ParameterValueKind.Enum, WahPolarity), P("touchWah", "Sens", 0x014E, 1, 100), P("touchWah", "Freq", 0x014F, 1, 100), P("touchWah", "Peak", 0x0150, 1, 100), P("touchWah", "Direct Mix", 0x0151, 1, 100), P("touchWah", "Effect Level", 0x0152, 1, 100),
            P("autoWah", "Mode", 0x0154, 1, 1, ParameterValueKind.Enum, WahMode), P("autoWah", "Freq", 0x0155, 1, 100), P("autoWah", "Peak", 0x0156, 1, 100), P("autoWah", "Rate", 0x0157, 1, 0x71), P("autoWah", "Depth", 0x0158, 1, 100), P("autoWah", "Direct Mix", 0x0159, 1, 100), P("autoWah", "Effect Level", 0x015A, 1, 100),
            P("subWah", "Type", 0x015C, 1, 5, ParameterValueKind.Enum, WahType), P("subWah", "Pedal Pos", 0x015D, 1, 100), P("subWah", "Pedal Min", 0x015E, 1, 100), P("subWah", "Pedal Max", 0x015F, 1, 100), P("subWah", "Effect Level", 0x0160, 1, 100), P("subWah", "Direct Mix", 0x0161, 1, 100),
            P("advancedComp", "Type", 0x0163, 1, 7, ParameterValueKind.Enum, CompType), P("advancedComp", "Sustain", 0x0164, 1, 100), P("advancedComp", "Attack", 0x0165, 1, 100), P("advancedComp", "Tone", 0x0166, 1, 100), P("advancedComp", "Level", 0x0167, 1, 100),
            P("limiter", "Type", 0x0169, 1, 2, ParameterValueKind.Enum, LimiterType), P("limiter", "Attack", 0x016A, 1, 100), P("limiter", "Threshold", 0x016B, 1, 100), P("limiter", "Ratio", 0x016C, 1, 0x11, ParameterValueKind.Enum, LimiterRatio), P("limiter", "Release", 0x016D, 1, 100), P("limiter", "Level", 0x016E, 1, 100),
            P("graphicEq", "31 Hz", 0x0170, 1, 0x28), P("graphicEq", "62 Hz", 0x0171, 1, 0x28), P("graphicEq", "125 Hz", 0x0172, 1, 0x28), P("graphicEq", "250 Hz", 0x0173, 1, 0x28), P("graphicEq", "500 Hz", 0x0174, 1, 0x28), P("graphicEq", "1 kHz", 0x0175, 1, 0x28), P("graphicEq", "2 kHz", 0x0176, 1, 0x28), P("graphicEq", "4 kHz", 0x0177, 1, 0x28), P("graphicEq", "8 kHz", 0x0178, 1, 0x28), P("graphicEq", "16 kHz", 0x0179, 1, 0x28), P("graphicEq", "Level", 0x017A, 1, 0x28),
            P("parametricEq", "Low Cut", 0x017C, 1, 0x11, ParameterValueKind.Enum, LowFrequency), P("parametricEq", "Low Gain", 0x017D, 1, 0x28, unit: "dB"), P("parametricEq", "Low-Mid Freq", 0x017E, 1, 0x1B, ParameterValueKind.Enum, MidFrequency), P("parametricEq", "Low-Mid Q", 0x017F, 1, 5, ParameterValueKind.Enum, MidQ), P("parametricEq", "Low-Mid Gain", 0x0200, 1, 0x28, unit: "dB"), P("parametricEq", "High-Mid Freq", 0x0201, 1, 0x1B, ParameterValueKind.Enum, MidFrequency), P("parametricEq", "High-Mid Q", 0x0202, 1, 5, ParameterValueKind.Enum, MidQ), P("parametricEq", "High-Mid Gain", 0x0203, 1, 0x28, unit: "dB"), P("parametricEq", "High Gain", 0x0204, 1, 0x28, unit: "dB"), P("parametricEq", "High Cut", 0x0205, 1, 0x0E, ParameterValueKind.Enum, HighFrequency), P("parametricEq", "Level", 0x0206, 1, 0x28, unit: "dB"),
            P("toneModify", "Type", 0x0208, 1, 7, ParameterValueKind.Enum, ToneModifyType), P("toneModify", "Reso", 0x0209, 1, 100), P("toneModify", "Low", 0x020A, 1, 100), P("toneModify", "High", 0x020B, 1, 100), P("toneModify", "Level", 0x020C, 1, 100),
            P("guitarSim", "Type", 0x020E, 1, 7, ParameterValueKind.Enum, GuitarSimType), P("guitarSim", "Low", 0x020F, 1, 100), P("guitarSim", "High", 0x0210, 1, 100), P("guitarSim", "Level", 0x0211, 1, 100), P("guitarSim", "Body", 0x0212, 1, 100),
            P("slowGear", "Sens", 0x0214, 1, 100), P("slowGear", "Rise Time", 0x0215, 1, 100), P("slowGear", "Level", 0x0216, 1, 100),
            P("defretter", "Tone", 0x0218, 1, 100), P("defretter", "Sens", 0x0219, 1, 100), P("defretter", "Attack", 0x021A, 1, 100), P("defretter", "Depth", 0x021B, 1, 100), P("defretter", "Reso", 0x021C, 1, 100), P("defretter", "Effect Level", 0x021D, 1, 100), P("defretter", "Direct Mix", 0x021E, 1, 100),
            P("waveSynth", "Wave", 0x0220, 1, 1, ParameterValueKind.Enum, WaveSynthWave), P("waveSynth", "Cutoff", 0x0221, 1, 100), P("waveSynth", "Reso", 0x0222, 1, 100), P("waveSynth", "Filter Sens", 0x0223, 1, 100), P("waveSynth", "Filter Decay", 0x0224, 1, 100), P("waveSynth", "Filter Depth", 0x0225, 1, 100), P("waveSynth", "Synth Level", 0x0226, 1, 100), P("waveSynth", "Direct Mix", 0x0227, 1, 100),
            P("sitarSim", "Tone", 0x0229, 1, 100), P("sitarSim", "Sens", 0x022A, 1, 100), P("sitarSim", "Depth", 0x022B, 1, 100), P("sitarSim", "Reso", 0x022C, 1, 100), P("sitarSim", "Buzz", 0x022D, 1, 100), P("sitarSim", "Effect Level", 0x022E, 1, 100), P("sitarSim", "Direct Mix", 0x022F, 1, 100),
            P("octave", "Range", 0x0231, 1, 3, ParameterValueKind.Enum, OctaveRange), P("octave", "Octave Level", 0x0232, 1, 100), P("octave", "Direct Mix", 0x0233, 1, 100),
            P("pitchShifter", "Voice", 0x0235, 1, 2, ParameterValueKind.Enum, VoiceType), P("pitchShifter", "PS1 Mode", 0x0236, 1, 3, ParameterValueKind.Enum, PitchShiftMode), P("pitchShifter", "PS1 Pitch", 0x0237, 1, 0x30), P("pitchShifter", "PS1 Fine", 0x0238, 1, 100), P("pitchShifter", "PS1 Pre Delay", 0x0239, 2, 0x0233), P("pitchShifter", "PS1 Level", 0x023B, 1, 100), P("pitchShifter", "PS2 Mode", 0x023C, 1, 3, ParameterValueKind.Enum, PitchShiftMode), P("pitchShifter", "PS2 Pitch", 0x023D, 1, 0x30), P("pitchShifter", "PS2 Fine", 0x023E, 1, 100), P("pitchShifter", "PS2 Pre Delay", 0x023F, 2, 0x0233), P("pitchShifter", "PS2 Level", 0x0241, 1, 100), P("pitchShifter", "Feedback", 0x0242, 1, 100), P("pitchShifter", "Direct Mix", 0x0243, 1, 100),
            P("harmonist", "Voice", 0x0245, 1, 2, ParameterValueKind.Enum, VoiceType), P("harmonist", "HR1 Harmony", 0x0246, 1, 0x1D), P("harmonist", "HR1 Pre Delay", 0x0247, 2, 0x0233), P("harmonist", "HR1 Level", 0x0249, 1, 100), P("harmonist", "HR2 Harmony", 0x024A, 1, 0x1D), P("harmonist", "HR2 Pre Delay", 0x024B, 2, 0x0233), P("harmonist", "HR2 Level", 0x024D, 1, 100), P("harmonist", "Feedback", 0x024E, 1, 100), P("harmonist", "Direct Mix", 0x024F, 1, 100),
            P("soundHold", "Hold", 0x0269, 1, 1, ParameterValueKind.Toggle, OnOff), P("soundHold", "Rise Time", 0x026A, 1, 100), P("soundHold", "Effect Level", 0x026B, 1, 120),
            P("acProcessor", "Type", 0x026D, 1, 3, ParameterValueKind.Enum, AcProcessorType), P("acProcessor", "Bass", 0x026E, 1, 100), P("acProcessor", "Middle", 0x026F, 1, 100), P("acProcessor", "Middle Freq", 0x0270, 1, 0x1B, ParameterValueKind.Enum, MidFrequency), P("acProcessor", "Treble", 0x0271, 1, 100), P("acProcessor", "Presence", 0x0272, 1, 100), P("acProcessor", "Level", 0x0273, 1, 100),
            P("phaser", "Type", 0x0275, 1, 3, ParameterValueKind.Enum, PhaserType), P("phaser", "Rate", 0x0276, 1, 0x71), P("phaser", "Depth", 0x0277, 1, 100), P("phaser", "Manual", 0x0278, 1, 100), P("phaser", "Reso", 0x0279, 1, 100), P("phaser", "Step Rate", 0x027A, 1, 0x72), P("phaser", "Effect Level", 0x027B, 1, 100), P("phaser", "Direct Mix", 0x027C, 1, 100),
            P("flanger", "Rate", 0x027E, 1, 0x71), P("flanger", "Depth", 0x027F, 1, 100), P("flanger", "Manual", 0x0300, 1, 100), P("flanger", "Reso", 0x0301, 1, 100), P("flanger", "Separation", 0x0302, 1, 100), P("flanger", "Low Cut", 0x0303, 1, 0x0A), P("flanger", "Effect Level", 0x0304, 1, 100), P("flanger", "Direct Mix", 0x0305, 1, 100),
            P("tremolo", "Wave Shape", 0x0307, 1, 100), P("tremolo", "Rate", 0x0308, 1, 0x71), P("tremolo", "Depth", 0x0309, 1, 100), P("tremolo", "Level", 0x030A, 1, 100),
            P("rotary1", "Speed Select", 0x030C, 1, 1, ParameterValueKind.Enum, SpeedSelect), P("rotary1", "Rate Slow", 0x030D, 1, 0x71), P("rotary1", "Rate Fast", 0x030E, 1, 0x71), P("rotary1", "Rise Time", 0x030F, 1, 100), P("rotary1", "Fall Time", 0x0310, 1, 100), P("rotary1", "Depth", 0x0311, 1, 100), P("rotary1", "Level", 0x0312, 1, 100),
            P("uniV", "Rate", 0x0314, 1, 0x71), P("uniV", "Depth", 0x0315, 1, 100), P("uniV", "Level", 0x0316, 1, 100),
            P("pan", "Type", 0x0318, 1, 1, ParameterValueKind.Enum, PanType), P("pan", "Position", 0x0319, 1, 100), P("pan", "Wave Shape", 0x031A, 1, 100), P("pan", "Rate", 0x031B, 1, 0x71), P("pan", "Depth", 0x031C, 1, 100), P("pan", "Level", 0x031D, 1, 100),
            P("slicer", "Pattern", 0x031F, 1, 0x13), P("slicer", "Rate", 0x0320, 1, 0x71), P("slicer", "Trigger Sens", 0x0321, 1, 100), P("slicer", "Effect Level", 0x0322, 1, 100), P("slicer", "Direct Mix", 0x0323, 1, 100),
            P("vibrato", "Rate", 0x0325, 1, 0x71), P("vibrato", "Depth", 0x0326, 1, 100), P("vibrato", "Trigger", 0x0327, 1, 1, ParameterValueKind.Toggle, OnOff), P("vibrato", "Rise Time", 0x0328, 1, 100), P("vibrato", "Level", 0x0329, 1, 100),
            P("ringMod", "Mode", 0x032B, 1, 1, ParameterValueKind.Enum, RingModMode), P("ringMod", "Freq", 0x032C, 1, 100), P("ringMod", "Effect Level", 0x032D, 1, 100), P("ringMod", "Direct Mix", 0x032E, 1, 100),
            P("humanizer", "Mode", 0x0330, 1, 1, ParameterValueKind.Enum, HumanizerMode), P("humanizer", "Vowel 1", 0x0331, 1, 4, ParameterValueKind.Enum, Vowel), P("humanizer", "Vowel 2", 0x0332, 1, 4, ParameterValueKind.Enum, Vowel), P("humanizer", "Sens", 0x0333, 1, 100), P("humanizer", "Rate", 0x0334, 1, 0x71), P("humanizer", "Depth", 0x0335, 1, 100), P("humanizer", "Manual", 0x0336, 1, 100), P("humanizer", "Level", 0x0337, 1, 100),
            P("twoByTwoChorus", "Xover Freq", 0x0339, 1, 0x10), P("twoByTwoChorus", "Low Rate", 0x033A, 1, 0x71), P("twoByTwoChorus", "Low Depth", 0x033B, 1, 100), P("twoByTwoChorus", "Low Pre Delay", 0x033C, 1, 0x50), P("twoByTwoChorus", "Low Level", 0x033D, 1, 100), P("twoByTwoChorus", "High Rate", 0x033E, 1, 0x71), P("twoByTwoChorus", "High Depth", 0x033F, 1, 100), P("twoByTwoChorus", "High Pre Delay", 0x0340, 1, 0x50), P("twoByTwoChorus", "High Level", 0x0341, 1, 100), P("twoByTwoChorus", "Direct Level", 0x0342, 1, 100),
            P("subDelay", "Type", 0x0343, 1, 1, ParameterValueKind.Enum, SubDelayType), P("subDelay", "Delay Time", 0x0344, 2, 0x076F, unit: "ms", min: 1), P("subDelay", "Feedback", 0x0346, 1, 100), P("subDelay", "High Cut", 0x0347, 1, 0x0E, ParameterValueKind.Enum, HighFrequency), P("subDelay", "Effect Level", 0x0348, 1, 120), P("subDelay", "Direct Mix", 0x0349, 1, 100), P("subDelay", "Tap Time", 0x034A, 1, 100, unit: "%")
        ];
    }

    public static IReadOnlyList<ParameterDefinition> All { get; } =
    [
        new("comp.on", "On/Off", "COMP", new(0x00, 0x00, 0x00, 0x20), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("comp.type", "Type", "COMP", new(0x00, 0x00, 0x00, 0x21), 1, 0, 0x07, ParameterValueKind.Enum, Options: CompType),
        new("comp.sustain", "Sustain", "COMP", new(0x00, 0x00, 0x00, 0x22), 1, 0, 100, ParameterValueKind.Integer),
        new("comp.attack", "Attack", "COMP", new(0x00, 0x00, 0x00, 0x23), 1, 0, 100, ParameterValueKind.Integer),
        new("comp.tone", "Tone", "COMP", new(0x00, 0x00, 0x00, 0x24), 1, 0, 100, ParameterValueKind.Integer),
        new("comp.level", "Level", "COMP", new(0x00, 0x00, 0x00, 0x25), 1, 0, 100, ParameterValueKind.Integer),
        new("odds.on", "On/Off", "OD/DS", new(0x00, 0x00, 0x00, 0x30), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("odds.type", "Type", "OD/DS", new(0x00, 0x00, 0x00, 0x31), 1, 0, 0x16, ParameterValueKind.Enum, Options: OdDsType),
        new("odds.drive", "Drive", "OD/DS", new(0x00, 0x00, 0x00, 0x32), 1, 0, 120, ParameterValueKind.Integer),
        new("odds.bottom", "Bottom", "OD/DS", new(0x00, 0x00, 0x00, 0x33), 1, 0, 100, ParameterValueKind.Integer),
        new("odds.tone", "Tone", "OD/DS", new(0x00, 0x00, 0x00, 0x34), 1, 0, 100, ParameterValueKind.Integer),
        new("odds.solo", "Solo Sw", "OD/DS", new(0x00, 0x00, 0x00, 0x35), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("odds.soloLevel", "Solo Level", "OD/DS", new(0x00, 0x00, 0x00, 0x36), 1, 0, 100, ParameterValueKind.Integer),
        new("odds.level", "Effect Level", "OD/DS", new(0x00, 0x00, 0x00, 0x37), 1, 0, 100, ParameterValueKind.Integer),
        new("odds.directMix", "Direct Mix", "OD/DS", new(0x00, 0x00, 0x00, 0x38), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.on", "On/Off", "Preamp A", new(0x00, 0x00, 0x00, 0x50), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampA.type", "Type", "Preamp A", new(0x00, 0x00, 0x00, 0x51), 1, 0, 0x1B, ParameterValueKind.Enum, Options: PreampType),
        new("preampA.gain", "Gain", "Preamp A", new(0x00, 0x00, 0x00, 0x52), 1, 0, 120, ParameterValueKind.Integer),
        new("preampA.tcomp", "T-Comp", "Preamp A", new(0x00, 0x00, 0x00, 0x53), 1, 0, 20, ParameterValueKind.Integer),
        new("preampA.bass", "Bass", "Preamp A", new(0x00, 0x00, 0x00, 0x54), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.middle", "Middle", "Preamp A", new(0x00, 0x00, 0x00, 0x55), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.treble", "Treble", "Preamp A", new(0x00, 0x00, 0x00, 0x56), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.presence", "Presence", "Preamp A", new(0x00, 0x00, 0x00, 0x57), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.level", "Level", "Preamp A", new(0x00, 0x00, 0x00, 0x58), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.bright", "Bright", "Preamp A", new(0x00, 0x00, 0x00, 0x59), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampA.gainSwitch", "Gain Sw", "Preamp A", new(0x00, 0x00, 0x00, 0x5A), 1, 0, 2, ParameterValueKind.Enum, Options: GainSwitch),
        new("preampA.solo", "Solo Sw", "Preamp A", new(0x00, 0x00, 0x00, 0x5B), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampA.soloLevel", "Solo Level", "Preamp A", new(0x00, 0x00, 0x00, 0x5C), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.speakerType", "Sp Type", "Preamp A", new(0x00, 0x00, 0x00, 0x5D), 1, 0, 0x09, ParameterValueKind.Enum, Options: SpeakerType),
        new("preampA.micType", "Mic Type", "Preamp A", new(0x00, 0x00, 0x00, 0x5E), 1, 0, 0x04, ParameterValueKind.Enum, Options: MicType),
        new("preampA.micDistance", "Mic Distance", "Preamp A", new(0x00, 0x00, 0x00, 0x5F), 1, 0, 1, ParameterValueKind.Enum, Options: MicDistance),
        new("preampA.micPosition", "Mic Position", "Preamp A", new(0x00, 0x00, 0x00, 0x60), 1, 0, 10, ParameterValueKind.Integer),
        new("preampA.micLevel", "Mic Level", "Preamp A", new(0x00, 0x00, 0x00, 0x61), 1, 0, 100, ParameterValueKind.Integer),
        new("preampA.directMix", "Direct Mix", "Preamp A", new(0x00, 0x00, 0x00, 0x62), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.on", "On/Off", "Preamp B", new(0x00, 0x00, 0x01, 0x00), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampB.type", "Type", "Preamp B", new(0x00, 0x00, 0x01, 0x01), 1, 0, 0x1B, ParameterValueKind.Enum, Options: PreampType),
        new("preampB.gain", "Gain", "Preamp B", new(0x00, 0x00, 0x01, 0x02), 1, 0, 120, ParameterValueKind.Integer),
        new("preampB.tcomp", "T-Comp", "Preamp B", new(0x00, 0x00, 0x01, 0x03), 1, 0, 20, ParameterValueKind.Integer),
        new("preampB.bass", "Bass", "Preamp B", new(0x00, 0x00, 0x01, 0x04), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.middle", "Middle", "Preamp B", new(0x00, 0x00, 0x01, 0x05), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.treble", "Treble", "Preamp B", new(0x00, 0x00, 0x01, 0x06), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.presence", "Presence", "Preamp B", new(0x00, 0x00, 0x01, 0x07), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.level", "Level", "Preamp B", new(0x00, 0x00, 0x01, 0x08), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.bright", "Bright", "Preamp B", new(0x00, 0x00, 0x01, 0x09), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampB.gainSwitch", "Gain Sw", "Preamp B", new(0x00, 0x00, 0x01, 0x0A), 1, 0, 2, ParameterValueKind.Enum, Options: GainSwitch),
        new("preampB.solo", "Solo Sw", "Preamp B", new(0x00, 0x00, 0x01, 0x0B), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("preampB.soloLevel", "Solo Level", "Preamp B", new(0x00, 0x00, 0x01, 0x0C), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.speakerType", "Sp Type", "Preamp B", new(0x00, 0x00, 0x01, 0x0D), 1, 0, 0x09, ParameterValueKind.Enum, Options: SpeakerType),
        new("preampB.micType", "Mic Type", "Preamp B", new(0x00, 0x00, 0x01, 0x0E), 1, 0, 0x04, ParameterValueKind.Enum, Options: MicType),
        new("preampB.micDistance", "Mic Distance", "Preamp B", new(0x00, 0x00, 0x01, 0x0F), 1, 0, 1, ParameterValueKind.Enum, Options: MicDistance),
        new("preampB.micPosition", "Mic Position", "Preamp B", new(0x00, 0x00, 0x01, 0x10), 1, 0, 10, ParameterValueKind.Integer),
        new("preampB.micLevel", "Mic Level", "Preamp B", new(0x00, 0x00, 0x01, 0x11), 1, 0, 100, ParameterValueKind.Integer),
        new("preampB.directMix", "Direct Mix", "Preamp B", new(0x00, 0x00, 0x01, 0x12), 1, 0, 100, ParameterValueKind.Integer),
        new("divider.mode", "Mode", "DIV", new(0x00, 0x00, 0x06, 0x40), 1, 0, 1, ParameterValueKind.Enum, Options: DivMode),
        new("divider.channelSelect", "Channel Select", "DIV", new(0x00, 0x00, 0x06, 0x41), 1, 0, 1, ParameterValueKind.Enum, Options: DivChannel),
        new("divider.channelA.dynamic", "Ch. A Dynamic", "DIV", new(0x00, 0x00, 0x06, 0x42), 1, 0, 2, ParameterValueKind.Enum, Options: DivDynamic),
        new("divider.channelA.dynamicSens", "Ch. A Dynamic Sens", "DIV", new(0x00, 0x00, 0x06, 0x43), 1, 0, 100, ParameterValueKind.Integer),
        new("divider.channelA.filter", "Ch. A Filter", "DIV", new(0x00, 0x00, 0x06, 0x44), 1, 0, 2, ParameterValueKind.Enum, Options: DivFilter),
        new("divider.channelA.cutoff", "Ch. A Cutoff Freq", "DIV", new(0x00, 0x00, 0x06, 0x45), 1, 0, 16, ParameterValueKind.Enum, Options: CutoffFrequency),
        new("divider.channelB.dynamic", "Ch. B Dynamic", "DIV", new(0x00, 0x00, 0x06, 0x46), 1, 0, 2, ParameterValueKind.Enum, Options: DivDynamic),
        new("divider.channelB.dynamicSens", "Ch. B Dynamic Sens", "DIV", new(0x00, 0x00, 0x06, 0x47), 1, 0, 100, ParameterValueKind.Integer),
        new("divider.channelB.filter", "Ch. B Filter", "DIV", new(0x00, 0x00, 0x06, 0x48), 1, 0, 2, ParameterValueKind.Enum, Options: DivFilter),
        new("divider.channelB.cutoff", "Ch. B Cutoff Freq", "DIV", new(0x00, 0x00, 0x06, 0x49), 1, 0, 16, ParameterValueKind.Enum, Options: CutoffFrequency),
        new("mix.mode", "Mode", "MIX", new(0x00, 0x00, 0x06, 0x50), 1, 0, 1, ParameterValueKind.Enum, Options: MixMode),
        new("mix.balance", "Ch. A/B Balance", "MIX", new(0x00, 0x00, 0x06, 0x51), 1, 0, 100, ParameterValueKind.Integer),
        new("mix.spread", "Spread", "MIX", new(0x00, 0x00, 0x06, 0x52), 1, 0, 100, ParameterValueKind.Integer),
        new("eq.on", "On/Off", "EQ", new(0x00, 0x00, 0x01, 0x30), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("eq.lowCut", "Low Cut", "EQ", new(0x00, 0x00, 0x01, 0x31), 1, 0, 0x11, ParameterValueKind.Enum, Options: LowFrequency),
        new("eq.lowGain", "Low Gain", "EQ", new(0x00, 0x00, 0x01, 0x32), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("eq.lowMidFreq", "Low-Mid Freq", "EQ", new(0x00, 0x00, 0x01, 0x33), 1, 0, 0x1B, ParameterValueKind.Enum, Options: MidFrequency),
        new("eq.lowMidQ", "Low-Mid Q", "EQ", new(0x00, 0x00, 0x01, 0x34), 1, 0, 0x05, ParameterValueKind.Enum, Options: MidQ),
        new("eq.lowMidGain", "Low-Mid Gain", "EQ", new(0x00, 0x00, 0x01, 0x35), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("eq.highMidFreq", "High-Mid Freq", "EQ", new(0x00, 0x00, 0x01, 0x36), 1, 0, 0x1B, ParameterValueKind.Enum, Options: MidFrequency),
        new("eq.highMidQ", "High-Mid Q", "EQ", new(0x00, 0x00, 0x01, 0x37), 1, 0, 0x05, ParameterValueKind.Enum, Options: MidQ),
        new("eq.highMidGain", "High-Mid Gain", "EQ", new(0x00, 0x00, 0x01, 0x38), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("eq.highGain", "High Gain", "EQ", new(0x00, 0x00, 0x01, 0x39), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("eq.highCut", "High Cut", "EQ", new(0x00, 0x00, 0x01, 0x3A), 1, 0, 0x0E, ParameterValueKind.Enum, Options: HighFrequency),
        new("eq.level", "Level", "EQ", new(0x00, 0x00, 0x01, 0x3B), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("fx1.on", "On/Off", "FX1", new(0x00, 0x00, 0x01, 0x40), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("fx1.type", "FX Type", "FX1", new(0x00, 0x00, 0x01, 0x41), 1, 0, 0x22, ParameterValueKind.Enum, Options: FxType),
        new("fx2.on", "On/Off", "FX2", new(0x00, 0x00, 0x03, 0x4C), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("fx2.type", "FX Type", "FX2", new(0x00, 0x00, 0x03, 0x4D), 1, 0, 0x22, ParameterValueKind.Enum, Options: FxType),
        .. BuildFxParameters(),
        new("delay.on", "On/Off", "DELAY", new(0x00, 0x00, 0x05, 0x60), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("delay.type", "Type", "DELAY", new(0x00, 0x00, 0x05, 0x61), 1, 0, 0x0A, ParameterValueKind.Enum, Options: DelayType),
        new("delay.time", "Delay Time", "DELAY", new(0x00, 0x00, 0x05, 0x62), 2, 1, 0x0F5D, ParameterValueKind.Integer, "ms"),
        new("delay.feedback", "Feedback", "DELAY", new(0x00, 0x00, 0x05, 0x64), 1, 0, 100, ParameterValueKind.Integer),
        new("delay.highCut", "High Cut", "DELAY", new(0x00, 0x00, 0x05, 0x65), 1, 0, 0x0E, ParameterValueKind.Enum, Options: HighFrequency),
        new("delay.level", "Effect Level", "DELAY", new(0x00, 0x00, 0x05, 0x66), 1, 0, 120, ParameterValueKind.Integer),
        new("delay.directMix", "Direct Mix", "DELAY", new(0x00, 0x00, 0x05, 0x67), 1, 0, 100, ParameterValueKind.Integer),
        new("delay.panTapTime", "Pan Tap Time", "DELAY", new(0x00, 0x00, 0x05, 0x68), 1, 0, 100, ParameterValueKind.Integer, "%"),
        new("delay.modRate", "Mod Rate", "DELAY", new(0x00, 0x00, 0x05, 0x73), 1, 0, 100, ParameterValueKind.Integer),
        new("delay.modDepth", "Mod Depth", "DELAY", new(0x00, 0x00, 0x05, 0x74), 1, 0, 100, ParameterValueKind.Integer),
        new("chorus.on", "On/Off", "CHORUS", new(0x00, 0x00, 0x06, 0x00), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("chorus.mode", "Mode", "CHORUS", new(0x00, 0x00, 0x06, 0x01), 1, 0, 2, ParameterValueKind.Enum, Options: ChorusMode),
        new("chorus.rate", "Rate", "CHORUS", new(0x00, 0x00, 0x06, 0x02), 1, 0, 0x71, ParameterValueKind.Integer),
        new("chorus.depth", "Depth", "CHORUS", new(0x00, 0x00, 0x06, 0x03), 1, 0, 100, ParameterValueKind.Integer),
        new("chorus.preDelay", "Pre Delay", "CHORUS", new(0x00, 0x00, 0x06, 0x04), 1, 0, 0x50, ParameterValueKind.Integer),
        new("chorus.lowCut", "Low Cut", "CHORUS", new(0x00, 0x00, 0x06, 0x05), 1, 0, 0x11, ParameterValueKind.Enum, Options: LowFrequency),
        new("chorus.highCut", "High Cut", "CHORUS", new(0x00, 0x00, 0x06, 0x06), 1, 0, 0x0E, ParameterValueKind.Enum, Options: HighFrequency),
        new("chorus.level", "Effect Level", "CHORUS", new(0x00, 0x00, 0x06, 0x07), 1, 0, 100, ParameterValueKind.Integer),
        new("chorus.directLevel", "Direct Level", "CHORUS", new(0x00, 0x00, 0x06, 0x08), 1, 0, 100, ParameterValueKind.Integer),
        new("reverb.on", "On/Off", "REVERB", new(0x00, 0x00, 0x06, 0x10), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("reverb.type", "Type", "REVERB", new(0x00, 0x00, 0x06, 0x11), 1, 0, 0x06, ParameterValueKind.Enum, Options: ReverbType),
        new("reverb.time", "Time", "REVERB", new(0x00, 0x00, 0x06, 0x12), 1, 0, 0x63, ParameterValueKind.Integer),
        new("reverb.preDelay", "Pre Delay", "REVERB", new(0x00, 0x00, 0x06, 0x13), 2, 0, 0x0374, ParameterValueKind.Integer, "ms"),
        new("reverb.lowCut", "Low Cut", "REVERB", new(0x00, 0x00, 0x06, 0x15), 1, 0, 0x11, ParameterValueKind.Enum, Options: LowFrequency),
        new("reverb.highCut", "High Cut", "REVERB", new(0x00, 0x00, 0x06, 0x16), 1, 0, 0x0E, ParameterValueKind.Enum, Options: HighFrequency),
        new("reverb.density", "Density", "REVERB", new(0x00, 0x00, 0x06, 0x17), 1, 0, 10, ParameterValueKind.Integer),
        new("reverb.level", "Effect Level", "REVERB", new(0x00, 0x00, 0x06, 0x18), 1, 0, 100, ParameterValueKind.Integer),
        new("reverb.directMix", "Direct Mix", "REVERB", new(0x00, 0x00, 0x06, 0x19), 1, 0, 100, ParameterValueKind.Integer),
        new("reverb.springSens", "Spring Sens", "REVERB", new(0x00, 0x00, 0x06, 0x1A), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.on", "On/Off", "PEDAL FX", new(0x00, 0x00, 0x06, 0x20), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("pedalFx.bendPitch", "Bend Pitch", "PEDAL FX", new(0x00, 0x00, 0x06, 0x22), 1, 0, 0x30, ParameterValueKind.Enum, Options: PedalBendPitch),
        new("pedalFx.bendPosition", "Bend Position", "PEDAL FX", new(0x00, 0x00, 0x06, 0x23), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.bendLevel", "Bend Level", "PEDAL FX", new(0x00, 0x00, 0x06, 0x24), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.bendDirectMix", "Bend Direct Mix", "PEDAL FX", new(0x00, 0x00, 0x06, 0x25), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.wahType", "Wah Type", "PEDAL FX", new(0x00, 0x00, 0x06, 0x26), 1, 0, 0x05, ParameterValueKind.Enum, Options: WahType),
        new("pedalFx.wahPosition", "Wah Position", "PEDAL FX", new(0x00, 0x00, 0x06, 0x27), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.wahMin", "Wah Min", "PEDAL FX", new(0x00, 0x00, 0x06, 0x28), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.wahMax", "Wah Max", "PEDAL FX", new(0x00, 0x00, 0x06, 0x29), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.wahLevel", "Wah Level", "PEDAL FX", new(0x00, 0x00, 0x06, 0x2A), 1, 0, 100, ParameterValueKind.Integer),
        new("pedalFx.wahDirectMix", "Wah Direct Mix", "PEDAL FX", new(0x00, 0x00, 0x06, 0x2B), 1, 0, 100, ParameterValueKind.Integer),
        new("footVolume.curve", "Volume Curve", "FOOT VOLUME", new(0x00, 0x00, 0x06, 0x30), 1, 0, 0x03, ParameterValueKind.Enum, Options: FootVolumeCurve),
        new("footVolume.min", "Volume Min", "FOOT VOLUME", new(0x00, 0x00, 0x06, 0x31), 1, 0, 100, ParameterValueKind.Integer),
        new("footVolume.max", "Volume Max", "FOOT VOLUME", new(0x00, 0x00, 0x06, 0x32), 1, 0, 100, ParameterValueKind.Integer),
        new("footVolume.level", "Level", "FOOT VOLUME", new(0x00, 0x00, 0x06, 0x33), 1, 0, 100, ParameterValueKind.Integer),
        new("ns1.on", "On/Off", "NS1", new(0x00, 0x00, 0x06, 0x63), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("ns1.threshold", "Threshold", "NS1", new(0x00, 0x00, 0x06, 0x64), 1, 0, 100, ParameterValueKind.Integer),
        new("ns1.release", "Release", "NS1", new(0x00, 0x00, 0x06, 0x65), 1, 0, 100, ParameterValueKind.Integer),
        new("ns1.detect", "Detect", "NS1", new(0x00, 0x00, 0x06, 0x66), 1, 0, 0x02, ParameterValueKind.Enum, Options: NoiseSuppressorDetect),
        new("ns2.on", "On/Off", "NS2", new(0x00, 0x00, 0x06, 0x68), 1, 0, 1, ParameterValueKind.Toggle, Options: OnOff),
        new("ns2.threshold", "Threshold", "NS2", new(0x00, 0x00, 0x06, 0x69), 1, 0, 100, ParameterValueKind.Integer),
        new("ns2.release", "Release", "NS2", new(0x00, 0x00, 0x06, 0x6A), 1, 0, 100, ParameterValueKind.Integer),
        new("ns2.detect", "Detect", "NS2", new(0x00, 0x00, 0x06, 0x6B), 1, 0, 0x02, ParameterValueKind.Enum, Options: NoiseSuppressorDetect),
        new("accel.type", "Type", "ACCEL", new(0x00, 0x00, 0x06, 0x70), 1, 0, 0x05, ParameterValueKind.Enum, Options: AccelType),
        new("accel.sBendPitch", "S-Bend Pitch", "ACCEL", new(0x00, 0x00, 0x06, 0x71), 1, 0, 0x06, ParameterValueKind.Enum, Options: SBendPitch),
        new("accel.sBendRise", "S-Bend Rise Time", "ACCEL", new(0x00, 0x00, 0x06, 0x72), 1, 0, 100, ParameterValueKind.Integer),
        new("accel.sBendFall", "S-Bend Fall Time", "ACCEL", new(0x00, 0x00, 0x06, 0x73), 1, 0, 100, ParameterValueKind.Integer),
        new("accel.feedbackerMode", "Feedbacker Mode", "ACCEL", new(0x00, 0x00, 0x07, 0x04), 1, 0, 1, ParameterValueKind.Enum, Options: FeedbackerMode),
        new("accel.feedbackerDepth", "Feedbacker Depth", "ACCEL", new(0x00, 0x00, 0x07, 0x05), 1, 0, 100, ParameterValueKind.Integer),
        new("accel.feedbackerRise", "Feedbacker Rise Time", "ACCEL", new(0x00, 0x00, 0x07, 0x06), 1, 0, 100, ParameterValueKind.Integer),
        new("master.patchLevel", "Patch Level", "MASTER", new(0x00, 0x00, 0x07, 0x10), 1, 0, 100, ParameterValueKind.Integer),
        new("master.lowGain", "Master Low Gain", "MASTER", new(0x00, 0x00, 0x07, 0x11), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("master.midFreq", "Master Mid Freq", "MASTER", new(0x00, 0x00, 0x07, 0x12), 1, 0, 0x1B, ParameterValueKind.Enum, Options: MidFrequency),
        new("master.midQ", "Master Mid Q", "MASTER", new(0x00, 0x00, 0x07, 0x13), 1, 0, 0x05, ParameterValueKind.Enum, Options: MidQ),
        new("master.midGain", "Master Mid Gain", "MASTER", new(0x00, 0x00, 0x07, 0x14), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("master.highGain", "Master High Gain", "MASTER", new(0x00, 0x00, 0x07, 0x15), 1, 0, 0x28, ParameterValueKind.Integer, "dB"),
        new("master.bpm", "Master BPM", "MASTER", new(0x00, 0x00, 0x07, 0x16), 2, 40, 250, ParameterValueKind.Integer),
        new("master.key", "Master Key", "MASTER", new(0x00, 0x00, 0x07, 0x18), 1, 0, 0x0B, ParameterValueKind.Enum, Options: MasterKey),
        new("master.beat", "Master Beat", "MASTER", new(0x00, 0x00, 0x07, 0x19), 1, 0, 0x1F, ParameterValueKind.Enum, Options: MasterBeat)
    ];

    public static ParameterDefinition? FindByTemporaryPatchAddress(Gt001Address address)
    {
        return All.FirstOrDefault(parameter => parameter.TemporaryPatchAddress == address);
    }

    public static Gt001Address GetModeledTemporaryPatchRequestSize()
    {
        var size = All
            .Where(parameter => parameter.Block is
                "COMP" or
                "OD/DS" or
                "Preamp A" or
                "Preamp B" or
                "EQ" or
                "FX1" or
                "FX2" or
                "DELAY" or
                "CHORUS" or
                "REVERB" or
                "PEDAL FX" or
                "FOOT VOLUME" or
                "DIV" or
                "MIX" or
                "NS1" or
                "NS2" or
                "ACCEL" or
                "MASTER")
            .Max(parameter => parameter.Offset.ToLinearValue() + parameter.Size);
        return Gt001Address.FromLinearValue(size);
    }

    public static IReadOnlyList<ParameterValueSnapshot> DecodeFromDataSet(Gt001SysExMessage message)
    {
        var messageStart = message.Address.ToLinearValue();
        var messageEnd = messageStart + message.Payload.Length;
        var values = new List<ParameterValueSnapshot>();

        foreach (var parameter in All)
        {
            var parameterStart = parameter.TemporaryPatchAddress.ToLinearValue();
            var parameterEnd = parameterStart + parameter.Size;
            if (parameterStart < messageStart || parameterEnd > messageEnd)
            {
                continue;
            }

            var payloadOffset = parameterStart - messageStart;
            try
            {
                var value = parameter.Decode(message.Payload.Skip(payloadOffset).Take(parameter.Size).ToArray());
                values.Add(new ParameterValueSnapshot(parameter, value));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Sparse or synthetic payloads can contain invalid placeholders for modeled fields.
                // Keep decoding the rest of the snapshot instead of dropping the whole response.
            }
        }

        return values;
    }
}
