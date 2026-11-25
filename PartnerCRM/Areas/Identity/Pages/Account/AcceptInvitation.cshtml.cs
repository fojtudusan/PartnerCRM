
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;

namespace PartnerCRM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class AcceptInvitationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AcceptInvitationModel> _logger;

        public AcceptInvitationModel(
            ApplicationDbContext context,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<AcceptInvitationModel> logger)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = "";

        public string? ErrorMessage { get; set; }

        private static readonly string[] AllowedRoles =
        {
            "Uživatel – čtení a zápis",
            "Uživatel – pouze čtení"
        };

        private string? GetTokenHash(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(hash);
        }

        private async Task<Models.UserInvitation?> FindValidInvitationAsync(
            string token)
        {
            var tokenHash = GetTokenHash(token);

            if (tokenHash == null)
                return null;

            var invitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

            if (invitation == null ||
                invitation.AcceptedAt != null ||
                invitation.ExpiresAt <= DateTime.UtcNow ||
                !AllowedRoles.Contains(invitation.Role))
            {
                return null;
            }

            return invitation;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var invitation = await FindValidInvitationAsync(Token);

            if (invitation == null)
            {
                ErrorMessage =
                    "Pozvánka není platná, již byla použita nebo její platnost vypršela.";
            }

            return Page();
        }

        // Zahájení Google přihlášení
        public async Task<IActionResult> OnPostGoogleLoginAsync(
            string token)
        {
            Token = token;

            var invitation = await FindValidInvitationAsync(Token);

            if (invitation == null)
            {
                ErrorMessage =
                    "Pozvánka není platná nebo její platnost vypršela.";

                return Page();
            }

            var redirectUrl = Url.Page(
                "./AcceptInvitation",
                pageHandler: "GoogleCallback",
                values: new { token = Token });

            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(
                    GoogleDefaults.AuthenticationScheme,
                    redirectUrl!);

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }

        // Návrat z Google a dokončení registrace
        public async Task<IActionResult> OnGetGoogleCallbackAsync(
            string token)
        {
            Token = token;

            var invitation = await FindValidInvitationAsync(Token);

            if (invitation == null)
            {
                return await ShowErrorAsync(
                    "Pozvánka není platná nebo její platnost vypršela.");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return await ShowErrorAsync(
                    "Nepodařilo se získat informace z Google účtu.");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            var verifiedEmail =
                info.Principal.FindFirstValue("email_verified")
                ?? info.Principal.FindFirstValue("verified_email");

            if (string.IsNullOrWhiteSpace(email) ||
                !string.Equals(
                    verifiedEmail,
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await ShowErrorAsync(
                    "Google účet nemá ověřenou e-mailovou adresu.");
            }

            // Google e-mail musí odpovídat pozvánce
            var normalizedGoogleEmail =
                _userManager.NormalizeEmail(email);

            var normalizedInvitationEmail =
                _userManager.NormalizeEmail(invitation.Email);

            if (!string.Equals(
                normalizedGoogleEmail,
                normalizedInvitationEmail,
                StringComparison.Ordinal))
            {
                return await ShowErrorAsync(
                    "Přihlásili jste se jiným Google účtem, než na který byla pozvánka vystavena.");
            }

            // Kontrola, zda účet již neexistuje
            var existingUser =
                await _userManager.FindByEmailAsync(invitation.Email);

            if (existingUser != null)
            {
                return await ShowErrorAsync(
                    "Uživatel s touto e-mailovou adresou již v systému existuje.");
            }

            // Kontrola, zda Google účet již není propojen
            var existingLogin = await _userManager.FindByLoginAsync(
                info.LoginProvider,
                info.ProviderKey);

            if (existingLogin != null)
            {
                return await ShowErrorAsync(
                    "Tento Google účet je již propojen s jiným uživatelem.");
            }

            // Vytvoření nového uživatele
            var user = new IdentityUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Nepodařilo se vytvořit účet pro pozvaného uživatele.");

                return await ShowErrorAsync(
                    "Nepodařilo se vytvořit uživatelský účet.");
            }

            // Propojení Google účtu
            var addLoginResult = await _userManager.AddLoginAsync(
                user,
                info);

            if (!addLoginResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return await ShowErrorAsync(
                    "Nepodařilo se propojit Google účet.");
            }

            // Přidělení role podle pozvánky
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                invitation.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return await ShowErrorAsync(
                    "Nepodařilo se přidělit uživatelskou roli.");
            }

            // Označení pozvánky jako použité
            invitation.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Přihlášení nového uživatele
            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            return RedirectToPage("/Index", new { area = "" });
        }

        private async Task<IActionResult> ShowErrorAsync(string message)
        {
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ErrorMessage = message;

            return Page();
        }
    }
}
