using Microsoft.AspNetCore.Mvc;

namespace Core.Models.DTOs.Common
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public ErrorResponse? Error { get; set; }
        
        public static IActionResult Ok(object? data = null)
        {
            return new OkObjectResult(new ApiResponse
            {
                Success = true,
                Data = data
            });
        }
        
        public static IActionResult BadRequest(string message, Dictionary<string, string[]>? details = null)
        {
            return new BadRequestObjectResult(new ApiResponse
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = 400,
                    Message = message,
                    Details = details
                }
            });
        }
        
        public static IActionResult NotFound(string message)
        {
            return new NotFoundObjectResult(new ApiResponse
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = 404,
                    Message = message
                }
            });
        }
        
        public static IActionResult Conflict(string message)
        {
            return new ConflictObjectResult(new ApiResponse
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = 409,
                    Message = message
                }
            });
        }
        
        public static IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            if (result.ErrorCode == 404)
            {
                return NotFound(result.ErrorMessage);
            }
            else if (result.ErrorCode == 409)
            {
                return Conflict(result.ErrorMessage);
            }
            else
            {
                return new ObjectResult(new ApiResponse
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = result.ErrorCode,
                        Message = result.ErrorMessage,
                        Details = result.ValidationErrors
                    }
                })
                {
                    StatusCode = result.ErrorCode
                };
            }
        }
    }
}