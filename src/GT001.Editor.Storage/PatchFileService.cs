using System.Text.Json;
using GT001.Editor.Core;

namespace GT001.Editor.Storage;

public sealed class PatchFileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string path, PatchState patch, CancellationToken cancellationToken = default)
    {
        var file = new PatchFile(
            "gt001-editor.patch",
            1,
            patch.Name,
            patch.Parameters.ToDictionary(p => p.Definition.Id, p => p.Value));

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, file, Options, cancellationToken);
    }

    public async Task<PatchFile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PatchFile>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException("Patch file is empty or invalid.");
    }
}
