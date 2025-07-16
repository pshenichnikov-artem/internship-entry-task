using System.ComponentModel.DataAnnotations;

namespace Core.Models.DTOs.Game
{
    public class GameAddRequest
    {
        [Required(ErrorMessage = "Идентификатор игрока X обязателен")]
        public Guid PlayerXId { get; set; }
        
        [Required(ErrorMessage = "Идентификатор игрока O обязателен")]
        public Guid PlayerOId { get; set; }
    }
}