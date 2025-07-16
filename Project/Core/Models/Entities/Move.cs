using Core.Models.Enums;

namespace Core.Models.Entities
{
    public class Move
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public int MoveNumber { get; set; }

        public Guid PlayerId { get; set; }
        public PlayerSymbol Symbol { get; set; }

        public int X { get; set; }
        public int Y { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ClientMoveId { get; set; }
        public bool IsReplacedByOpponentSymbol { get; set; }
    }
}
