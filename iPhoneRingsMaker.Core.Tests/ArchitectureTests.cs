using System.Reflection;

using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void CoreAssembly_DoesNotReferenceDesktopUiFrameworks()
    {
        var references = typeof(M4RProject).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(references, name => name!.StartsWith("Microsoft.UI", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name!.StartsWith("Microsoft.WindowsAppSDK", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name!.StartsWith("Windows", StringComparison.Ordinal));
    }

    [Fact]
    public void CorePublicApi_DoesNotExposeDesktopUiTypes()
    {
        var publicTypes = typeof(M4RProject).Assembly.GetExportedTypes();
        var exposedTypes = publicTypes
            .SelectMany(GetExposedTypes)
            .Where(type => type.Namespace is not null)
            .ToArray();

        Assert.DoesNotContain(exposedTypes, type => type.Namespace!.StartsWith("Microsoft.UI", StringComparison.Ordinal));
        Assert.DoesNotContain(exposedTypes, type => type.Namespace!.StartsWith("Windows", StringComparison.Ordinal));
    }

    [Fact]
    public void ProjectFactory_CreatesProjectForMediaSource()
    {
        var source = new LocalMediaSource { Path = "song.mp3" };
        IM4RProjectFactory factory = new M4RProjectFactory();

        var project = factory.Create(source);

        Assert.Same(source, project.MediaSource);
    }

    private static IEnumerable<Type> GetExposedTypes(Type type)
    {
        yield return type;
        foreach (var memberType in type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SelectMany(GetMemberTypes))
        {
            yield return memberType;
        }
    }

    private static IEnumerable<Type> GetMemberTypes(MemberInfo member) => member switch
    {
        PropertyInfo property => [property.PropertyType],
        FieldInfo field => [field.FieldType],
        MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType),
        ConstructorInfo constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType),
        _ => [],
    };
}
