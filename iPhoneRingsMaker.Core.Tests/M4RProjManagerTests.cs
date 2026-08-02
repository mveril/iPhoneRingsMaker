using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Services;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class M4RProjManagerTests : IDisposable
{
    private readonly string _testDirectory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"iPhoneRingsMaker.Tests.{Guid.NewGuid():N}");

    public M4RProjManagerTests()
    {
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task SaveProjectAsAsync_WritesProjectAndClearsDirtyState()
    {
        var manager = new M4RProjManager { Project = CreateProject() };
        var path = System.IO.Path.Combine(_testDirectory, "project.m4rproj");

        await manager.SaveProjectAsAsync(path, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path));
        Assert.True(manager.IsFileAttached);
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public async Task OpenProjectAsync_WithInvalidJson_PreservesCurrentProject()
    {
        var manager = new M4RProjManager { Project = CreateProject() };
        var originalProject = manager.Project;
        var path = System.IO.Path.Combine(_testDirectory, "invalid.m4rproj");
        await File.WriteAllTextAsync(path, "{ invalid json", TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(
            () => manager.OpenProjectAsync(path, TestContext.Current.CancellationToken));

        Assert.Same(originalProject, manager.Project);
    }

    [Fact]
    public async Task ProjectChange_AfterSave_MarksProjectDirty()
    {
        var manager = new M4RProjManager { Project = CreateProject() };
        var path = System.IO.Path.Combine(_testDirectory, "project.m4rproj");
        await manager.SaveProjectAsAsync(path, TestContext.Current.CancellationToken);

        manager.Project!.StartTime = TimeSpan.FromSeconds(1);

        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public async Task CloseProjectAsync_ClearsProjectPathAndDirtyState()
    {
        var manager = new M4RProjManager { Project = CreateProject() };
        var path = System.IO.Path.Combine(_testDirectory, "project.m4rproj");
        await manager.SaveProjectAsAsync(path, TestContext.Current.CancellationToken);

        await manager.CloseProjectAsync();

        Assert.Null(manager.Project);
        Assert.Null(manager.Path);
        Assert.False(manager.IsProjectOpen);
        Assert.False(manager.IsFileAttached);
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public async Task SaveProjectAsync_WithoutAttachedFile_ThrowsInvalidOperationException()
    {
        var manager = new M4RProjManager { Project = CreateProject() };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SaveProjectAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenProjectAsync_WithValidProject_RaisesExpectedEvents()
    {
        var source = new M4RProjManager { Project = CreateProject() };
        var path = System.IO.Path.Combine(_testDirectory, "project.m4rproj");
        await source.SaveProjectAsAsync(path, TestContext.Current.CancellationToken);
        var manager = new M4RProjManager();
        var projectLoaded = 0;
        var fileAttached = 0;
        manager.ProjectLoaded += (_, _) => projectLoaded++;
        manager.FileAttached += (_, _) => fileAttached++;

        await manager.OpenProjectAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(1, projectLoaded);
        Assert.Equal(1, fileAttached);
        Assert.True(manager.IsProjectOpen);
        Assert.True(manager.IsFileAttached);
        Assert.False(manager.HasUnsavedChanges);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static M4RProj CreateProject()
    {
        return new M4RProj
        {
            MediaSource = new LocalMediaSource { Path = "sample.mp3" },
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromSeconds(30),
        };
    }
}
