using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RecipeSite.Models;

namespace RecipeSite.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<PersonalDataModel> _logger;

        public PersonalDataModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<PersonalDataModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Пароль обязателен для удаления аккаунта")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Не удалось загрузить пользователя.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Не удалось загрузить пользователя.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!await _userManager.CheckPasswordAsync(user, Input.Password))
            {
                ModelState.AddModelError(string.Empty, "Неверный пароль.");
                return Page();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new System.InvalidOperationException($"Произошла ошибка при удалении пользователя.");
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("Пользователь удалил свой аккаунт.");

            return Redirect("~/");
        }
    }
}