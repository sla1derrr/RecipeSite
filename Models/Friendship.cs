using System.ComponentModel.DataAnnotations;

namespace RecipeSite.Models
{
    public enum FriendshipStatus
    {
        Pending = 0,
        Accepted = 1
    }

    // Связь дружбы между двумя пользователями.
    // RequesterId — кто отправил запрос, AddresseeId — кому отправлен запрос.
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        public string AddresseeId { get; set; } = string.Empty;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ApplicationUser? Requester { get; set; }
        public ApplicationUser? Addressee { get; set; }
    }
}
