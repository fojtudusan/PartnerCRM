
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PartnerCRM.Data;
using PartnerCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace PartnerCRM.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class InviteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InviteModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "Zadejte e-mailovou adresu.")]
        [EmailAddress(ErrorMessage = "Zadejte platnou e-mailovou adresu.")]
        public string Email { get; set; } = "";

        [BindProperty]
        [Required]
        public string Role { get; set; } = "Uživatel – čtení a zápis";

        public string? InvitationUrl { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            Email = Email.Trim();

            string[] allowedRoles =
            {
                "Uživatel – čtení a zápis",
                "Uživatel – pouze čtení"
            };

            if (!allowedRoles.Contains(Role))
            {
                ModelState.AddModelError(nameof(Role), "Neplatná role.");
            }

            if (!ModelState.IsValid)
                return Page();

            var normalizedEmail = _userManager.NormalizeEmail(Email);

            // Kontrola, zda uživatel již existuje
            var existingUser = await _userManager.FindByEmailAsync(Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(Email),
                    "Uživatel s touto e-mailovou adresou již existuje.");

                return Page();
            }

            var now = DateTime.UtcNow;

            // Kontrola dosud platné pozvánky
            var existingInvitation = await _context.UserInvitations
                .AnyAsync(i =>
                    i.Email.ToUpper() == normalizedEmail &&
                    i.AcceptedAt == null &&
                    i.ExpiresAt > now);

            if (existingInvitation)
            {
                ModelState.AddModelError(
                    nameof(Email),
                    "Pro tuto e-mailovou adresu již existuje platná pozvánka.");

                return Page();
            }

            // Generování bezpečného náhodného tokenu
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // Do databáze ukládáme pouze SHA-256 hash tokenu
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            var invitation = new UserInvitation
            {
                Email = Email,
                Role = Role,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(7),
                CreatedByUserId = _userManager.GetUserId(User)
            };

            _context.UserInvitations.Add(invitation);

            await _context.SaveChangesAsync();

            // Odkaz na stránku, kterou vytvoříme v dalším kroku
            InvitationUrl =
                $"{Request.Scheme}://{Request.Host}/Identity/Account/AcceptInvitation?token={Uri.EscapeDataString(token)}";

            return Page();
        }
    }
}
