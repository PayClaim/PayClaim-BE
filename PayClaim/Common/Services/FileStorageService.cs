using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PayClaim.Common.Exceptions;

namespace PayClaim.Common.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
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

        if (!await HasValidSignatureAsync(file, extension, ct))
        {
            throw new BusinessRuleException($"File content does not match the '{extension}' file signature.");
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

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, CancellationToken ct)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (bytesRead < 4) return false;

        return extension switch
        {
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => bytesRead >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                                       && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }
}
