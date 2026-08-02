using System.Diagnostics.CodeAnalysis;

using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Contracts.Services;

public interface IM4RProjectManager
{
    event EventHandler<ProjectEventArgs>? ProjectLoaded;

    event EventHandler<ProjectEventArgs>? ProjectUnloaded;

    event EventHandler? FileAttached;

    [MemberNotNullWhen(true, nameof(Project))]
    bool IsProjectOpen
    {
        get;
    }

    bool IsFileAttached
    {
        get;
    }

    bool HasUnsavedChanges
    {
        get;
    }

    string? Path
    {
        get;
    }

    M4RProject? Project
    {
        get; set;
    }

    Task OpenProjectAsync(string path, CancellationToken cancellationToken = default);

    Task SaveProjectAsync(CancellationToken cancellationToken = default);

    Task SaveProjectAsAsync(string path, CancellationToken cancellationToken = default);

    ValueTask CloseProjectAsync();
}
