namespace Core.Models.DTOs.Move
{
    public class MoveFilterRequest
    {
        public Guid? GameId { get; set; }
        public Guid? PlayerId { get; set; }
    }
}