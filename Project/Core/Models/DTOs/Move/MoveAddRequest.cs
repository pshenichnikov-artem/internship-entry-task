using System.ComponentModel.DataAnnotations;

namespace Core.Models.DTOs.Move
{
    public class MoveAddRequest
    {
        [Required(ErrorMessage = "Идентификатор игры обязателен")]
        public Guid GameId { get; set; }
        
        public Guid? PlayerId { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Координата X должна быть неотрицательной")]
        public int X { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Координата Y должна быть неотрицательной")]
        public int Y { get; set; }
        
        [Required(ErrorMessage = "Идентификатор хода клиента обязателен")]
        public string ClientMoveId { get; set; }
    }
}