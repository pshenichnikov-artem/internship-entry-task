using System.ComponentModel.DataAnnotations;
using Core.Models.DTOs.Common;

namespace Core.Validation
{
    public class AllowedSortFieldsAttribute : ValidationAttribute
    {
        private readonly HashSet<string> _allowedFields;

        public AllowedSortFieldsAttribute(params string[] allowedFields)
        {
            _allowedFields = allowedFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IEnumerable<SortRequest> sortRequests)
                return ValidationResult.Success;

            foreach (var sort in sortRequests)
            {
                if (!_allowedFields.Contains(sort.Field))
                {
                    var allowed = string.Join(", ", _allowedFields);
                    return new ValidationResult($"Недопустимое поле сортировки: '{sort.Field}'. Допустимые поля: {allowed}");
                }
            }

            return ValidationResult.Success;
        }
    }
}