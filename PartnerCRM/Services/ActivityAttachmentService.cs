
using PartnerCRM.Models;

namespace PartnerCRM.Services
{
    public class ActivityAttachmentService
    {
        private readonly string _storagePath;

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".doc", ".docx",
            ".xls", ".xlsx", ".jpg",
            ".jpeg", ".png", ".txt"
        };

        public ActivityAttachmentService(
            IWebHostEnvironment environment)
        {
            _storagePath = Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                "ActivityAttachments");

            Directory.CreateDirectory(_storagePath);
        }

        public async Task<string> SaveAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Soubor je prázdný.");

            if (file.Length > 20 * 1024 * 1024)
                throw new InvalidOperationException(
                    "Maximální velikost souboru je 20 MB.");

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Tento typ souboru není povolen.");

            var storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(
                _storagePath,
                storedFileName);

            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(stream, cancellationToken);

            return storedFileName;
        }

        public string GetFilePath(string storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName) ||
                storedFileName != Path.GetFileName(storedFileName))
            {
                throw new InvalidOperationException(
                    "Neplatný název souboru.");
            }

            return Path.Combine(_storagePath, storedFileName);
        }

        public void Delete(string storedFileName)
        {
            var filePath = GetFilePath(storedFileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
