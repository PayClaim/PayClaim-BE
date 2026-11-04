using Microsoft.AspNetCore.Http;

namespace PayClaim.Common.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct = default);
    void DeleteFile(string relativeFilePath);
}
