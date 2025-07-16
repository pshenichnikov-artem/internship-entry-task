using System.ComponentModel.DataAnnotations;
using Core.Validation;

namespace Core.Models.DTOs.Player
{
    public class PlayerAddRequest
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [UsernameValidation]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Пароль обязателен")]
        [PasswordValidation]
        public string Password { get; set; } = string.Empty;
    }
}