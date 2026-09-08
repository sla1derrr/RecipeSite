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

                // ВАЖНО: UserName сознательно не меняем — вход в систему идёт по Email,
                // который совпадает с UserName при регистрации. Если сменить UserName на
                // никнейм, пользователь перестанет находиться по email при следующем входе
                // (PasswordSignInAsync ищет по UserName), и логин будет падать с
                // "Invalid login attempt", хотя пароль верный. Никнейм хранится только в claim.
            }

            string avatarValue = string.Empty;

            // 2. Сохраняем файл прямо в claim как base64 data-URI (не на диск!),
            // т.к. файловая система контейнера на Railway эфемерна и стирается
            // при каждом рестарте/передеплое — а БД (и claim в ней) сохраняется.
            if (UploadedAvatar != null && UploadedAvatar.Length > 0)
            {
                const long maxSizeBytes = 2 * 1024 * 1024; // 2 МБ, чтобы не раздувать таблицу claim'ов
                if (UploadedAvatar.Length > maxSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, "Файл слишком большой (максимум 2 МБ).");
                    await LoadAsync(user);
                    return Page();
                }

                using var memoryStream = new MemoryStream();
                await UploadedAvatar.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                var contentType = string.IsNullOrEmpty(UploadedAvatar.ContentType)
                    ? "image/png"
                    : UploadedAvatar.ContentType;
                avatarValue = $"data:{contentType};base64,{base64}";
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