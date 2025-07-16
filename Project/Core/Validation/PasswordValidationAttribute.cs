using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Validation
{
    public class PasswordValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string password) return false;

            // Password must contain at least one digit, one lowercase, one uppercase, and be at least 6 characters
            var regex = new Regex(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{6,100}$");
            return regex.IsMatch(password);
        }

        public override string FormatErrorMessage(string name)
        {
            return "Пароль должен содержать минимум 6 символов, включая хотя бы одну цифру, одну строчную и одну заглавную букву";
        }
    }
}