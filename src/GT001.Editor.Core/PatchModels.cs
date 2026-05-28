using GT001.Editor.Protocol;

namespace GT001.Editor.Core;

public enum ParameterSyncStatus
{
    Unknown,
    Synced,
    Dirty,
    Sent
}

public sealed record ParameterState(ParameterDefinition Definition, int Value, ParameterSyncStatus Status)
{
    public bool IsDirty => Status == ParameterSyncStatus.Dirty;
    public string DisplayValue => Definition.FormatValue(Value);
}

public sealed record PatchState(string Name, IReadOnlyList<ParameterState> Parameters)
{
    public static PatchState CreateDefault()
    {
        var parameters = TemporaryPatchParameters.All
            .Select(p => new ParameterState(p, p.RawMinimum, ParameterSyncStatus.Unknown))
            .ToArray();

        return new PatchState("Temporary Patch", parameters);
    }

    public PatchState WithParameterValue(string parameterId, int value, bool isDirty = false)
        => WithParameterValue(parameterId, value, isDirty ? ParameterSyncStatus.Dirty : ParameterSyncStatus.Synced);

    public PatchState WithParameterValue(string parameterId, int value, ParameterSyncStatus status)
    {
        var parameters = Parameters
            .Select(parameter => parameter.Definition.Id == parameterId
                ? parameter with { Value = value, Status = status }
                : parameter)
            .ToArray();

        return this with { Parameters = parameters };
    }

    public int CountByStatus(ParameterSyncStatus status)
        => Parameters.Count(parameter => parameter.Status == status);
}
