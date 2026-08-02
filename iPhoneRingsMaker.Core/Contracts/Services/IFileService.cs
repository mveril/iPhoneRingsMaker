namespace iPhoneRingsMaker.Core.Contracts.Services;

public interface IFileService
{
    T Read<T>(string folderPath, string fileName);

    ValueTask<T> ReadAsync<T>(string folderPath, string fileName);

    void Save<T>(string folderPath, string fileName, T content);

    Task SaveAsync<T>(string folderPath, string fileName, T content);

    void Delete(string folderPath, string fileName);
}
