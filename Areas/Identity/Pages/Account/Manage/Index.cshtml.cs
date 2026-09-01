// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Encodings.Web;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public string Username { get; set; }

        // Текущий путь к аватарке (для отображения на странице)
        public string CurrentAvatarUrl { get; set; }
        public string CurrentAvatarPreset { get; set; }

        // Список готовых иконок на выбор (эмодзи как "пресет")
        public static readonly string[] AvatarPresets = new[]
        {
            "🍳", "🥗", "🍕", "🍰", "🍜", "🍇", "👩‍🍳", "👨‍🍳"
        };

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Имя")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Display(Name = "Фамилия")]
            [StringLength(50)]
            public string LastName { get; set; }

            // Выбранная готовая иконка (если не грузим своё фото)
            [Display(Name = "Готовая иконка")]
            public string AvatarPreset { get; set; }

            // Загружаемый файл своей картинки
            [Display(Name = "Своя картинка")]
            public IFormFile AvatarFile { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            CurrentAvatarUrl = user.AvatarUrl;
            CurrentAvatarPreset = user.AvatarPreset;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarPreset = user.AvatarPreset
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Сохраняем имя и фамилию
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;

            // Если загружен новый файл картинки — сохраняем его и очищаем пресет
            if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(Input.AvatarFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/uploads/avatars/{fileName}";
                user.AvatarPreset = null;
            }
            // Иначе, если выбрана готовая иконка — сохраняем её и очищаем свою картинку
            else if (!string.IsNullOrEmpty(Input.AvatarPreset))
            {
                user.AvatarPreset = Input.AvatarPreset;
                user.AvatarUrl = null;
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Ваш профиль обновлён";
            return RedirectToPage();
        }
    }
}