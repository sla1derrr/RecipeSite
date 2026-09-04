using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env; // Добавлено для точного пути к wwwroot

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string NewUsername { get; set; } = string.Empty;
        }

        // ЖЕСТКАЯ ПРИВЯЗКА ФАЙЛА И СМАЙЛИКА НАПРЯМУЮ К СТРАНИЦЕ
        [BindProperty]
        public IFormFile? UploadedAvatar { get; set; }

        [BindProperty]
        public string? SelectedEmoji { get; set; }

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
            ModelState.Remove("UploadedAvatar");
            ModelState.Remove("SelectedEmoji");
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

            // 2. Бронированный способ обработки файла
            if (UploadedAvatar != null && UploadedAvatar.Length > 0)
            {
                // Защита на случай, если WebRootPath == null (часто бывает на Railway)
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRoot, "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UploadedAvatar.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedAvatar.CopyToAsync(fileStream);
                }
                avatarValue = "/avatars/" + uniqueFileName;
            }
            // 3. Если файла нет, смотрим смайлик
            else if (!string.IsNullOrEmpty(SelectedEmoji))
            {
                avatarValue = SelectedEmoji;
            }
            // 4. Если вообще ничего не трогали
            else
            {
                var oldClaim = claims.FirstOrDefault(c => c.Type == "Avatar")?.Value;
                avatarValue = (!string.IsNullOrEmpty(oldClaim) && oldClaim != "Avatar" && !oldClaim.Contains("Avatar")) ? oldClaim : "👤";
            }

            // ПРАВИЛЬНАЯ ПЕРЕЗАПИСЬ CLAIM: Находим и удаляем ВСЕ старые записи аватара
            var oldAvatarClaims = claims.Where(c => c.Type == "Avatar").ToList();
            if (oldAvatarClaims.Any())
            {
                await _userManager.RemoveClaimsAsync(user, oldAvatarClaims);
            }
            await _userManager.AddClaimAsync(user, new Claim("Avatar", avatarValue));

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Профиль успешно обновлен.";
            return RedirectToPage();
        }
    }
}