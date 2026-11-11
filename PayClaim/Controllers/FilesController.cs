using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public FilesController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string path, [FromQuery] string? fileName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("File path is required.");
        }

        // Sanitasi path untuk mencegah directory traversal attacks
        var sanitizedRelative = path.Replace('\\', '/').TrimStart('/');
        if (sanitizedRelative.Contains(".."))
        {
            return BadRequest("Invalid file path.");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, sanitizedRelative.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound("File not found.");
        }

        var downloadName = !string.IsNullOrWhiteSpace(fileName)
            ? fileName
            : Path.GetFileName(fullPath);

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        // PhysicalFile dengan fileDownloadName secara otomatis menyertakan header:
        // Content-Disposition: attachment; filename="<downloadName>"
        // yang memaksa browser untuk langsung men-download dan tidak membukanya di browser PDF viewer.
        return PhysicalFile(fullPath, contentType, downloadName, enableRangeProcessing: true);
    }
}
