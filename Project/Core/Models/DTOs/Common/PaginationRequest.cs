using System.ComponentModel.DataAnnotations;

namespace Core.Models.DTOs.Common
{
    public class PaginationRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше или равен 1")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100")]
        public int PageSize { get; set; } = 10;
    }
}