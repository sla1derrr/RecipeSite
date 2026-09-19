using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;

namespace RecipeSite.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FriendsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Список друзей + входящие/исходящие заявки
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var accepted = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.Status == FriendshipStatus.Accepted &&
                            (f.RequesterId == userId || f.AddresseeId == userId))
                .ToListAsync();

            var friends = accepted
                .Select(f => f.RequesterId == userId ? f.Addressee : f.Requester)
                .Where(u => u != null)
                .ToList();

            var incomingRequests = await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.Status == FriendshipStatus.Pending && f.AddresseeId == userId)
                .ToListAsync();

            var outgoingRequests = await _context.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.Status == FriendshipStatus.Pending && f.RequesterId == userId)
                .ToListAsync();

            ViewBag.IncomingRequests = incomingRequests;
            ViewBag.OutgoingRequests = outgoingRequests;
            return View(friends);
        }

        // Поиск по нику
        public async Task<IActionResult> Search(string? query)
        {
            var userId = _userManager.GetUserId(User)!;
            ViewBag.Query = query;

            List<ApplicationUser> users = new();
            if (!string.IsNullOrWhiteSpace(query))
            {
                users = await _context.Users
                    .Where(u => u.Id != userId && u.UserName != null && u.UserName.Contains(query))
                    .Take(30)
                    .ToListAsync();
            }

            var relevantIds = users.Select(u => u.Id).ToList();
            var relations = await _context.Friendships
                .Where(f => (f.RequesterId == userId && relevantIds.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == userId && relevantIds.Contains(f.RequesterId)))
                .ToListAsync();

            var statusByUserId = new Dictionary<string, string>();
            foreach (var otherId in relevantIds)
            {
                var rel = relations.FirstOrDefault(f =>
                    (f.RequesterId == userId && f.AddresseeId == otherId) ||
                    (f.AddresseeId == userId && f.RequesterId == otherId));

                if (rel == null)
                {
                    statusByUserId[otherId] = "none";
                }
                else if (rel.Status == FriendshipStatus.Accepted)
                {
                    statusByUserId[otherId] = "friends";
                }
                else if (rel.RequesterId == userId)
                {
                    statusByUserId[otherId] = "pending_out";
                }
                else
                {
                    statusByUserId[otherId] = "pending_in";
                }
            }

            ViewBag.StatusByUserId = statusByUserId;
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string userId, string? returnQuery)
        {
            var myId = _userManager.GetUserId(User)!;

            if (userId != myId)
            {
                var exists = await _context.Friendships.AnyAsync(f =>
                    (f.RequesterId == myId && f.AddresseeId == userId) ||
                    (f.RequesterId == userId && f.AddresseeId == myId));

                if (!exists)
                {
                    _context.Friendships.Add(new Friendship
                    {
                        RequesterId = myId,
                        AddresseeId = userId,
                        Status = FriendshipStatus.Pending
                    });
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(returnQuery))
            {
                return RedirectToAction("Search", new { query = returnQuery });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var myId = _userManager.GetUserId(User)!;
            var request = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == id && f.AddresseeId == myId && f.Status == FriendshipStatus.Pending);

            if (request != null)
            {
                request.Status = FriendshipStatus.Accepted;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(int id)
        {
            var myId = _userManager.GetUserId(User)!;
            var request = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == id && (f.AddresseeId == myId || f.RequesterId == myId));

            if (request != null)
            {
                _context.Friendships.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(string userId)
        {
            var myId = _userManager.GetUserId(User)!;
            var request = await _context.Friendships.FirstOrDefaultAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == myId && f.AddresseeId == userId) ||
                 (f.RequesterId == userId && f.AddresseeId == myId)));

            if (request != null)
            {
                _context.Friendships.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Профиль друга + его рецепты (видны только если вы друзья)
        public async Task<IActionResult> Profile(string userId)
        {
            var myId = _userManager.GetUserId(User)!;

            var profileUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (profileUser == null)
            {
                return NotFound();
            }

            var areFriends = userId == myId || await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == myId && f.AddresseeId == userId) ||
                 (f.RequesterId == userId && f.AddresseeId == myId)));

            ViewBag.AreFriends = areFriends;
            ViewBag.ProfileUser = profileUser;

            if (!areFriends)
            {
                return View(new List<Recipe>());
            }

            var recipes = await _context.Recipes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(recipes);
        }
    }
}
