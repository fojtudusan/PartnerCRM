using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;
using PartnerCRM.Services;

namespace PartnerCRM.Pages.Activities
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivityAttachmentService _attachmentService;

        public DetailsModel(
            ApplicationDbContext context,
            ActivityAttachmentService attachmentService)
        {
            _context = context;
            _attachmentService = attachmentService;
        }

        public Activity Activity { get; set; } = null!;

        public IList<CrmTask> Tasks { get; set; }
            = new List<CrmTask>();

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        [TempData]
        public string? AttachmentMessage { get; set; }

        // NAČTENÍ DETAILU AKTIVITY
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var activity = await _context.Activities
                .Include(a => a.Company)
                .Include(a => a.Contact)
                .Include(a => a.MarketingCampaign)
                .Include(a => a.Attachments)
                .Include(a => a.ParentActivity)
                    .ThenInclude(p => p!.Company)
                .Include(a => a.ChildActivities)
                    .ThenInclude(c => c.Company)
                .Include(a => a.ChildActivities)
                    .ThenInclude(c => c.Contact)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            Activity = activity;

            Tasks = await _context.Tasks
                .Where(t => t.ActivityId == id)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return Page();
        }

        // NAHRÁNÍ PŘÍLOHY
        public async Task<IActionResult> OnPostUploadAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var activityExists = await _context.Activities
                .AnyAsync(a => a.Id == id);

            if (!activityExists)
                return NotFound();

            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                AttachmentMessage = "Vyberte soubor k nahrání.";
                return RedirectToPage("./Details", new { id });
            }

            if (Path.GetFileName(UploadedFile.FileName).Length > 255)
            {
                AttachmentMessage = "Název souboru je příliš dlouhý.";
                return RedirectToPage("./Details", new { id });
            }

            string storedFileName;

            try
            {
                storedFileName = await _attachmentService.SaveAsync(
                    UploadedFile,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                AttachmentMessage = ex.Message;
                return RedirectToPage("./Details", new { id });
            }

            var attachment = new ActivityAttachment
            {
                ActivityId = id,
                OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                StoredFileName = storedFileName,
                ContentType = UploadedFile.ContentType,
                FileSize = UploadedFile.Length,
                UploadedAt = DateTime.Now
            };

            _context.ActivityAttachments.Add(attachment);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                AttachmentMessage = "Příloha byla úspěšně nahrána.";
            }
            catch
            {
                _attachmentService.Delete(storedFileName);
                throw;
            }

            return RedirectToPage("./Details", new { id });
        }

        // STAŽENÍ PŘÍLOHY
        public async Task<IActionResult> OnGetDownloadAsync(
            int id,
            int attachmentId)
        {
            var attachment = await _context.ActivityAttachments
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.ActivityId == id);

            if (attachment == null)
                return NotFound();

            var filePath = _attachmentService.GetFilePath(
                attachment.StoredFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            return PhysicalFile(
                filePath,
                "application/octet-stream",
                attachment.OriginalFileName);
        }

        // ODSTRANĚNÍ PŘÍLOHY
        public async Task<IActionResult> OnPostDeleteAsync(
            int id,
            int attachmentId)
        {
            var attachment = await _context.ActivityAttachments
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.ActivityId == id);

            if (attachment == null)
                return NotFound();

            var storedFileName = attachment.StoredFileName;

            _context.ActivityAttachments.Remove(attachment);

            await _context.SaveChangesAsync();

            _attachmentService.Delete(storedFileName);

            AttachmentMessage = "Příloha byla úspěšně odstraněna.";

            return RedirectToPage("./Details", new { id });
        }
    }
}