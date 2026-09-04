using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSite.Models;

namespace RecipeSite.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EmailModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public string CurrentEmail { get; set; } 

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string? NewEmail { get; set; }
            public string? Name { get; set; }
            public string? Surname { get; set; }
            
            [Phone]
            public string? PhoneNumber { get; set; }
            public DateTime? DateOfBirth { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            
            DateTime? parsedDob = null;
            var dobClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.DateOfBirth)?.Value;
            if (DateTime.TryParse(dobClaim, out var d)) parsedDob = d;

            CurrentEmail = await _userManager.GetEmailAsync(user);

            Input = new InputModel
            {
                NewEmail = CurrentEmail,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value,
                Surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value,
                DateOfBirth = parsedDob
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email && !string.IsNullOrEmpty(Input.NewEmail))
            {
                await _userManager.SetEmailAsync(user, Input.NewEmail);
                await _userManager.SetUserNameAsync(user, Input.NewEmail);
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            }

            await UpdateClaimAsync(user, ClaimTypes.GivenName, Input.Name);
            await UpdateClaimAsync(user, ClaimTypes.Surname, Input.Surname);
            await UpdateClaimAsync(user, ClaimTypes.DateOfBirth, Input.DateOfBirth?.ToString("yyyy-MM-dd"));

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Ваши личные данные успешно сохранены.";
            return RedirectToPage();
        }

        private async Task UpdateClaimAsync(ApplicationUser user, string claimType, string claimValue)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var oldClaim = claims.FirstOrDefault(c => c.Type == claimType);

            if (oldClaim != null) await _userManager.RemoveClaimAsync(user, oldClaim);
            if (!string.IsNullOrEmpty(claimValue)) await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        }
    }
}