using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;

namespace RecipeSite.Controllers
{
    [Authorize]
    public class RecipeImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecipeImagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("/recipe-image/{id:int}")]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Get(int id)
        {
            var myId = _userManager.GetUserId(User)!;

            var owner = await _context.Recipes
                .Where(r => r.Id == id)
                .Select(r => r.UserId)
                .FirstOrDefaultAsync();
            if (owner == null) return NotFound();

            var allowed = owner == myId || await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == myId && f.AddresseeId == owner) ||
                 (f.RequesterId == owner && f.AddresseeId == myId)));
            if (!allowed) return NotFound();

            var img = await _context.RecipeImages.AsNoTracking()
                .FirstOrDefaultAsync(i => i.RecipeId == id);
            if (img == null) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(img.Data, img.ContentType);
        }
    }
}