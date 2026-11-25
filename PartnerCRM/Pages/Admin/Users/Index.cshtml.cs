
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;

namespace PartnerCRM.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserRow> Users { get; set; } = new();

        public List<InvitationRow> Invitations { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserRow
        {
            public string Id { get; set; } = "";
            public string Email { get; set; } = "";
            public List<string> Roles { get; set; } = new();
        }

        public class InvitationRow
        {
            public int Id { get; set; }
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsExpired { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Registrovaní uživatelé
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                Users.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    Roles = roles.ToList()
                });
            }

            // Dosud nepřijaté pozvánky
            var invitations = await _context.UserInvitations
                .Where(i => i.AcceptedAt == null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var now = DateTime.UtcNow;

            foreach (var invitation in invitations)
            {
                Invitations.Add(new InvitationRow
                {
                    Id = invitation.Id,
                    Email = invitation.Email,
                    Role = invitation.Role,
                    CreatedAt = invitation.CreatedAt,
                    ExpiresAt = invitation.ExpiresAt,
                    IsExpired = invitation.ExpiresAt <= now
                });
            }
        }

        // Zrušení pozvánky
        public async Task<IActionResult> OnPostCancelInvitationAsync(int id)
        {
            var invitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.AcceptedAt == null);

            if (invitation == null)
            {
                StatusMessage = "Pozvánka již neexistuje nebo byla přijata.";
                return RedirectToPage();
            }

            _context.UserInvitations.Remove(invitation);
            await _context.SaveChangesAsync();

            StatusMessage = "Pozvánka byla úspěšně zrušena.";

            return RedirectToPage();
        }
    }
}
