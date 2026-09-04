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
        public InputModel Input { get; set; }

        public class InputModel
        {
            [BindProperty]
            public string NewUsername { get; set; }

            [BindProperty]
            public string Avatar { get; set; }

            public IFormFile AvatarFile { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var nicknameClaim = claims.FirstOrDefault(c => c.Type == "Nickname")?.Value;

            Input = new InputModel
            {
                NewUsername = !string.IsNullOrEmpty(nicknameClaim) ? nicknameClaim : user.UserName,
                Avatar = claims.FirstOrDefault(c => c.Type == "Avatar")?.Value ?? "👤"
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

            // Сохраняем кастомный никнейм в Claims
            if (!string.IsNullOrEmpty(Input?.NewUsername))
            {
                var oldNicknameClaim = claims.FirstOrDefault(c => c.Type == "Nickname");
                if (oldNicknameClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, oldNicknameClaim);
                }
                await _userManager.AddClaimAsync(user, new Claim("Nickname", Input.NewUsername));
            }

            // Считываем выбранный смайлик из формы или модели
            string selectedEmoji = Request.Form["Input.Avatar"];
            string avatarValue = !string.IsNullOrEmpty(selectedEmoji) ? selectedEmoji : Input?.Avatar;

            // Если загружен файл картинки — сохраняем его в wwwroot/avatars
            if (Input?.AvatarFile != null && Input.AvatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.AvatarFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(fileStream);
                }
                avatarValue = "/avatars/" + uniqueFileName;
            }

            // Если аватар не выбран, берем старый из claims или оставляем дефолтный
            if (string.IsNullOrEmpty(avatarValue))
            {
                avatarValue = claims.FirstOrDefault(c => c.Type == "Avatar")?.Value ?? "👤";
            }

            // Перезаписываем Claim аватара
            var oldAvatarClaim = claims.FirstOrDefault(c => c.Type == "Avatar");
            if (oldAvatarClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, oldAvatarClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("Avatar", avatarValue));

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Профиль успешно обновлен.";
            return RedirectToPage();
        }
    }
}