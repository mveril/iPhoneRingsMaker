using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Contracts.Services;

using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace iPhoneRingsMaker.Activation;

internal sealed class M4RProjActivationHandler
    : ActivationHandler<IFileActivatedEventArgs>
{
    private const string ProjectFileExtension = ".m4rproj";

    private readonly IM4RProjectManager _m4RProjectManager;

    public M4RProjActivationHandler(IM4RProjectManager m4RProjectManager)
    {
        ArgumentNullException.ThrowIfNull(m4RProjectManager);

        _m4RProjectManager = m4RProjectManager;
    }

    protected async override Task HandleInternalAsync(
        IFileActivatedEventArgs args)
    {
        var projectFile = GetProjectFile(args)
            ?? throw new InvalidOperationException(
                "The file activation does not contain an M4R project.");

        if (string.Equals(
            _m4RProjectManager.Path,
            Path.GetFullPath(projectFile.Path),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _m4RProjectManager.OpenProjectAsync(projectFile.Path);
    }

    protected override bool CanHandleInternal(
        IFileActivatedEventArgs args)
    {
        return GetProjectFile(args) is not null;
    }

    private static StorageFile? GetProjectFile(
        IFileActivatedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args.Files
            .OfType<StorageFile>()
            .FirstOrDefault(static file =>
                string.Equals(
                    file.FileType,
                    ProjectFileExtension,
                    StringComparison.OrdinalIgnoreCase));
    }
}
