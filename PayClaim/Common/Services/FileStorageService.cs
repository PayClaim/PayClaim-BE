using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PayClaim.Common.Exceptions;

namespace PayClaim.Common.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new BusinessRuleException("Uploaded file is empty or missing.");
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            throw new BusinessRuleException("File size exceeds the maximum limit of 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new BusinessRuleException($"File format '{extension}' is not allowed. Allowed formats: {string.Join(", ", AllowedExtensions)}");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var targetFolder = Path.Combine(webRoot, "uploads", subFolder);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(targetFolder, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }

    public void DeleteFile(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath)) return;

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var trimmedPath = relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webRoot, trimmedPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
