using Core.Models.Enums;

namespace Core.Models.DTOs.Game
{
    public class GameFilterRequest
    {
        public GameStatus? Status { get; set; }
        public List<Guid>? PlayerIds { get; set; }
    }
}