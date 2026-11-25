
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PartnerCRM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private const string AdminEmail = "dusan.fojtu@robotgeorge.com";

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Zadejte e-mail.")]
            [EmailAddress(ErrorMessage = "Neplatný formát e-mailu.")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Zadejte heslo.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";
        }

        public void OnGet()
        {
        }

        // Klasické přihlášení
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToPage("/Index", new { area = "" });

            ModelState.AddModelError(
                string.Empty,
                "Neplatný e-mail nebo heslo.");

            return Page();
        }

        // Zahájení přihlášení přes Google
        public IActionResult OnPostGoogleLogin()
        {
            var redirectUrl = Url.Page(
                "./Login",
                pageHandler: "GoogleCallback");

            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(
                    GoogleDefaults.AuthenticationScheme,
                    redirectUrl!);

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }

        // Návrat z Google
        public async Task<IActionResult> OnGetGoogleCallbackAsync()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Nepodařilo se získat informace z Google účtu.");

                return Page();
            }

            // Získání e-mailu z Google
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                ?? info.Principal.FindFirstValue("email");

            var verifiedEmail =
                info.Principal.FindFirstValue("email_verified")
                ?? info.Principal.FindFirstValue("verified_email");

            // Kontrola ověření e-mailu přímo Googlem
            if (string.IsNullOrWhiteSpace(email) ||
                !string.Equals(
                    verifiedEmail,
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ModelState.AddModelError(
                    string.Empty,
                    "Google účet nemá ověřenou e-mailovou adresu.");

                return Page();
            }

            // Vyhledání již registrovaného uživatele
            var user = await _userManager.FindByEmailAsync(email);

            // Vytvoření prvního administrátora,
            // pokud ještě neexistuje
            if (user == null &&
                string.Equals(
                    email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                user = new IdentityUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    _logger.LogError(
                        "Nepodařilo se vytvořit prvního administrátora.");

                    await HttpContext.SignOutAsync(
                        IdentityConstants.ExternalScheme);

                    ModelState.AddModelError(
                        string.Empty,
                        "Nepodařilo se vytvořit administrátorský účet.");

                    return Page();
                }
            }

            // Neevidovaný uživatel nemá přístup
            if (user == null)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ModelState.AddModelError(
                    string.Empty,
                    "Tento účet není registrován v PartnerCRM. " +
                    "Přístup musí být nejprve schválen administrátorem.");

                return Page();
            }

            // Kontrola, zda Google účet již není propojen
            var existingLogin = await _userManager.FindByLoginAsync(
                info.LoginProvider,
                info.ProviderKey);

            if (existingLogin != null &&
                existingLogin.Id != user.Id)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ModelState.AddModelError(
                    string.Empty,
                    "Tento Google účet je již propojen s jiným uživatelem.");

                return Page();
            }

            // První propojení Google účtu
            if (existingLogin == null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(
                    user,
                    info);

                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError(
                        "Nepodařilo se propojit Google účet uživatele {Email}.",
                        email);

                    await HttpContext.SignOutAsync(
                        IdentityConstants.ExternalScheme);

                    ModelState.AddModelError(
                        string.Empty,
                        "Nepodařilo se propojit Google účet s uživatelem.");

                    return Page();
                }
            }

            // Administrátorovi zajistíme roli Admin
            if (string.Equals(
                email,
                AdminEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(
                        user,
                        "Admin");

                    if (!roleResult.Succeeded)
                    {
                        await HttpContext.SignOutAsync(
                            IdentityConstants.ExternalScheme);

                        ModelState.AddModelError(
                            string.Empty,
                            "Nepodařilo se přidělit administrátorskou roli.");

                        return Page();
                    }
                }
            }

            // Přihlášení uživatele se zachováním jeho rolí
            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // Odstranění dočasného externího přihlášení
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            _logger.LogInformation(
                "Uživatel {Email} se přihlásil přes Google.",
                email);

            return RedirectToPage("/Index", new { area = "" });
        }
    }
}
