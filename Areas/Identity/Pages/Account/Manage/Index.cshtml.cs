using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
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

        public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Оставляем в модели только никнейм. Файлы будем читать напрямую.
        public class InputModel
        {
            public string NewUsername { get; set; } = string.Empty;
        }

        public string CurrentAvatar { get; set; } = "👤";

        private async Task LoadAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var nicknameClaim = claims.FirstOrDefault(c => c.Type == "Nickname")?.Value;
            var avatarClaim = claims.FirstOrDefault(c => c.Type == "Avatar")?.Value;

            if (string.IsNullOrEmpty(avatarClaim) || avatarClaim == "Avatar" || avatarClaim.Contains("Avatar"))
            {
                avatarClaim = user.AvatarUrl ?? user.AvatarPreset ?? "👤";
            }

            CurrentAvatar = avatarClaim;

            Input = new InputModel
            {
                NewUsername = !string.IsNullOrEmpty(nicknameClaim) ? nicknameClaim : user.UserName ?? string.Empty
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

            var claims = await _userManager.GetClaimsAsync(user);

            // 1. Сохраняем никнейм
            if (!string.IsNullOrEmpty(Input?.NewUsername))
            {
                var oldNicknameClaim = claims.FirstOrDefault(c => c.Type == "Nickname");
                if (oldNicknameClaim != null) await _userManager.RemoveClaimAsync(user, oldNicknameClaim);
                await _userManager.AddClaimAsync(user, new Claim("Nickname", Input.NewUsername));

                if (user.UserName != Input.NewUsername) await _userManager.SetUserNameAsync(user, Input.NewUsername);
            }

            string avatarValue = string.Empty;

            // 2. БЕЗОТКАЗНЫЙ СПОСОБ: читаем файл напрямую из запроса
            var uploadedFile = HttpContext.Request.Form.Files.FirstOrDefault();

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
                avatarValue = "/avatars/" + uniqueFileName;
            }
            else
            {
                // 3. Если файла нет, читаем смайлик напрямую из формы
                string selectedEmoji = HttpContext.Request.Form["SelectedEmoji"];
                if (!string.IsNullOrEmpty(selectedEmoji))
                {
                    avatarValue = selectedEmoji;
                }
                else
                {
                    // Если вообще ничего не трогали — берем текущий
                    var oldClaim = claims.FirstOrDefault(c => c.Type == "Avatar")?.Value;
                    avatarValue = (!string.IsNullOrEmpty(oldClaim) && oldClaim != "Avatar" && !oldClaim.Contains("Avatar")) ? oldClaim : "👤";
                }
            }

            // 4. Перезаписываем Claim аватара
            var oldAvatarClaim = claims.FirstOrDefault(c => c.Type == "Avatar");
            if (oldAvatarClaim != null) await _userManager.RemoveClaimAsync(user, oldAvatarClaim);
            await _userManager.AddClaimAsync(user, new Claim("Avatar", avatarValue));

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Профиль успешно обновлен.";
            return RedirectToPage();
        }
    }
}