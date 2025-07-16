using Core.Models.Enums;

namespace Core.Models.DTOs.Game
{
    public class MoveInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public PlayerSymbol Symbol { get; set; }
    }
}