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
            [BindProperty(SupportsGet = true)]
            public string NewUsername { get; set; }

            [BindProperty(SupportsGet = true)]
            public string Avatar { get; set; }

            public IFormFile AvatarFile { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            Input = new InputModel
            {
                NewUsername = user.UserName,
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

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Обновляем никнейм
            if (!string.IsNullOrEmpty(Input.NewUsername) && Input.NewUsername != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.NewUsername);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Ошибка: этот никнейм уже занят.";
                    return RedirectToPage();
                }
            }

            string avatarValue = Input.Avatar;

            // Если загружен файл картинки — сохраняем его в wwwroot/avatars
            if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
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

            // Сохраняем аватарку в Claims
            if (!string.IsNullOrEmpty(avatarValue))
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var oldAvatarClaim = claims.FirstOrDefault(c => c.Type == "Avatar");
                if (oldAvatarClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, oldAvatarClaim);
                }

                await _userManager.AddClaimAsync(user, new Claim("Avatar", avatarValue));
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Профиль успешно обновлен.";
            return RedirectToPage();
        }
    }
}