using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSite.Models;

namespace RecipeSite.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string NewUsername { get; set; } = string.Empty;
        }

        [BindProperty]
        public IFormFile? UploadedAvatar { get; set; }

        [BindProperty]
        public string? SelectedEmoji { get; set; }

        public string CurrentAvatarUrl { get; set; } = string.Empty;
        public string CurrentAvatarPreset { get; set; } = string.Empty;

        private void LoadFromUser(ApplicationUser user)
        {
            CurrentAvatarUrl = user.AvatarUrl ?? string.Empty;
            CurrentAvatarPreset = string.IsNullOrEmpty(CurrentAvatarUrl) ? (user.AvatarPreset ?? "👤") : string.Empty;

            Input = new InputModel
            {
                NewUsername = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : (user.UserName ?? string.Empty)
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            LoadFromUser(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ModelState.Remove("UploadedAvatar");
            ModelState.Remove("SelectedEmoji");

            if (!string.IsNullOrEmpty(Input?.NewUsername))
            {
                user.FirstName = Input.NewUsername;
            }

            if (UploadedAvatar != null && UploadedAvatar.Length > 0)
            {
                const long maxSizeBytes = 2 * 1024 * 1024;
                if (UploadedAvatar.Length > maxSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, "Файл слишком большой (максимум 2 МБ).");
                    LoadFromUser(user);
                    return Page();
                }

                using var memoryStream = new MemoryStream();
                await UploadedAvatar.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                var contentType = string.IsNullOrEmpty(UploadedAvatar.ContentType)
                    ? "image/png"
                    : UploadedAvatar.ContentType;

                user.AvatarUrl = $"data:{contentType};base64,{base64}";
                user.AvatarPreset = null;
            }
            else if (!string.IsNullOrEmpty(SelectedEmoji))
            {
                user.AvatarPreset = SelectedEmoji;
                user.AvatarUrl = null;
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Профиль успешно обновлен.";
            return RedirectToPage();
        }
    }
}