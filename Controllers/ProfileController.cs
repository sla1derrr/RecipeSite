using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeSite.Models;

namespace RecipeSite.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string? firstName, string? avatarPreset, IFormFile? avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FirstName = firstName?.Trim();
            user.AvatarPreset = avatarPreset;

            if (avatar != null && avatar.Length > 0 && avatar.Length <= 2 * 1024 * 1024)
            {
                var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (allowed.Contains(ext))
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var folder = Path.Combine(webRoot, "avatars");
                    Directory.CreateDirectory(folder);

                    var fileName = $"{user.Id}{ext}";
                    using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                    user.AvatarUrl = $"/avatars/{fileName}?v={DateTime.UtcNow.Ticks}";
                }
            }

            await _userManager.UpdateAsync(user);
            TempData["Saved"] = "Профиль сохранён";
            return RedirectToAction(nameof(Index));
        }
    }
}