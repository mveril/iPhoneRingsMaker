using System.ComponentModel;
using System.Text.Json;

using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Helpers;
using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Services;

public sealed class M4RProjectManager : IM4RProjectManager
{
    private string? _path;
    private M4RProject? _project;

    public event EventHandler<ProjectEventArgs>? ProjectLoaded;

    public event EventHandler<ProjectEventArgs>? ProjectUnloaded;

    public event EventHandler? FileAttached;

    public bool IsProjectOpen => Project is not null;

    public bool IsFileAttached => _path is not null;

    public bool HasUnsavedChanges
    {
        get; private set;
    }

    public string? Path => _path;

    public M4RProject? Project
    {
        get => _project;
        set
        {
            if (ReferenceEquals(value, _project))
            {
                return;
            }

            var previousProject = _project;
            if (previousProject is not null)
            {
                previousProject.PropertyChanged -= OnProjectPropertyChanged;
            }

            _project = value;
            _path = null;
            HasUnsavedChanges = value is not null;

            if (previousProject is not null)
            {
                ProjectUnloaded?.Invoke(this, new ProjectEventArgs(previousProject));
            }

            if (value is not null)
            {
                value.PropertyChanged += OnProjectPropertyChanged;
                ProjectLoaded?.Invoke(this, new ProjectEventArgs(value));
            }
        }
    }

    public async Task OpenProjectAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var project = await JsonSerializer.DeserializeAsync<M4RProject>(stream, Json.Options, cancellationToken)
            ?? throw new InvalidDataException("The project file is empty or invalid.");

        Project = project;
        _path = System.IO.Path.GetFullPath(path);
        HasUnsavedChanges = false;
        FileAttached?.Invoke(this, EventArgs.Empty);
    }

    public Task SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        if (_path is null)
        {
            throw new InvalidOperationException("The project is not attached to a file.");
        }

        return SaveProjectAsCoreAsync(_path, cancellationToken);
    }

    public async Task SaveProjectAsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = System.IO.Path.GetFullPath(path);
        await SaveProjectAsCoreAsync(fullPath, cancellationToken);
        _path = fullPath;
        FileAttached?.Invoke(this, EventArgs.Empty);
    }

    public ValueTask CloseProjectAsync()
    {
        Project = null;
        _path = null;
        HasUnsavedChanges = false;
        return ValueTask.CompletedTask;
    }

    private async Task SaveProjectAsCoreAsync(string path, CancellationToken cancellationToken)
    {
        var project = Project ?? throw new InvalidOperationException("No project is open.");
        var directory = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The destination directory is invalid.", nameof(path));
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = System.IO.Path.Combine(directory, $".{System.IO.Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, project, Json.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
            HasUnsavedChanges = false;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasUnsavedChanges = true;
    }
}
