using Core.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.DTOs.Common
{
    public class SortRequest
    {
        public string Field { get; set; } = string.Empty;
        public SortDirection Direction { get; set; } = SortDirection.Ascending;
    }
}