namespace GT001.Editor.Protocol;

public enum ParameterValueKind
{
    Integer,
    Toggle,
    Enum
}

public sealed record ParameterOption(int Value, string Label);

public sealed record ParameterDefinition(
    string Id,
    string DisplayName,
    string Block,
    Gt001Address Offset,
    int Size,
    int RawMinimum,
    int RawMaximum,
    ParameterValueKind Kind,
    string? Unit = null,
    IReadOnlyList<ParameterOption>? Options = null)
{
    public Gt001Address TemporaryPatchAddress => Gt001Address.TemporaryPatchBase.Add(Offset);

    public byte[] Encode(int value)
    {
        if (value < RawMinimum || value > RawMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"{DisplayName} must be between {RawMinimum} and {RawMaximum}.");
        }

        return Size switch
        {
            1 => [(byte)value],
            2 => [(byte)((value >> 7) & 0x7F), (byte)(value & 0x7F)],
            _ => throw new NotSupportedException($"Parameter size {Size} is not supported yet.")
        };
    }

    public int Decode(IReadOnlyList<byte> data)
    {
        if (data.Count < Size)
        {
            throw new ArgumentException($"{DisplayName} needs {Size} data byte(s).", nameof(data));
        }

        var value = Size switch
        {
            1 => data[0],
            2 => (data[0] << 7) | data[1],
            _ => throw new NotSupportedException($"Parameter size {Size} is not supported yet.")
        };

        if (value < RawMinimum || value > RawMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(data), $"{DisplayName} decoded value {value} is outside {RawMinimum}-{RawMaximum}.");
        }

        return value;
    }

    public string FormatValue(int value)
    {
        if (Options?.FirstOrDefault(option => option.Value == value) is { } option)
        {
            return option.Label;
        }

        return Unit is { Length: > 0 }
            ? $"{value} {Unit}"
            : value.ToString();
    }
}
