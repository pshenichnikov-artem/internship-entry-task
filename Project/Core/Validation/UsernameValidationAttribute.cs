using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Validation
{
    public class UsernameValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string username) return false;

            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,50}$");
        }

        public override string FormatErrorMessage(string name)
        {
            return "Имя пользователя должно содержать от 3 до 50 символов и включать только буквы, цифры и знак подчеркивания";
        }
    }
}