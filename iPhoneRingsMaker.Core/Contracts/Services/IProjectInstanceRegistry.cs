namespace iPhoneRingsMaker.Core.Contracts.Services;

public interface IProjectInstanceRegistry
{
    bool TryClaim(string path);

    void Release();
}
