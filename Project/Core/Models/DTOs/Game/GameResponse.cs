using Core.Models.Enums;

namespace Core.Models.DTOs.Game
{
    public class GameResponse
    {
        public Guid Id { get; set; }
        public Guid PlayerXId { get; set; }
        public Guid PlayerOId { get; set; }
        public int Size { get; set; }
        public int WinConditionLength { get; set; }
        public GameStatus Status { get; set; }
        public PlayerSymbol CurrentTurn { get; set; }
        public PlayerSymbol? Winner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public List<MoveInfo> Moves { get; set; } = new();
    }
}