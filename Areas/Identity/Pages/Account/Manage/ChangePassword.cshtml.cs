using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSite.Models;

namespace RecipeSite.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите текущий пароль")]
            [DataType(DataType.Password)]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "Введите новый пароль")]
            [StringLength(100, ErrorMessage = "Пароль должен быть от {2} до {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Ваш пароль успешно изменен.";
            return RedirectToPage();
        }
    }
}