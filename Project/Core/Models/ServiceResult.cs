namespace Core.Models
{
    public class ServiceResult<T>
    {
        public bool Success { get; private set; }
        public T? Data { get; private set; }
        public int ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public Dictionary<string, string[]>? ValidationErrors { get; private set; }

        public Dictionary<string, object>? Metadata { get; private set; }

        private ServiceResult() { }

        public static ServiceResult<T> SuccessResult(T data, Dictionary<string, object>? metadata = null)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Data = data,
                Metadata = metadata
            };
        }

        public static ServiceResult<T> ErrorResult(int code, string message)
        {
            return new ServiceResult<T>
            {
                Success = false,
                ErrorCode = code,
                ErrorMessage = message
            };
        }

        public static ServiceResult<T> ValidationErrorResult(Dictionary<string, string[]> errors)
        {
            return new ServiceResult<T>
            {
                Success = false,
                ErrorCode = 400,
                ErrorMessage = "Ошибка валидации данных",
                ValidationErrors = errors
            };
        }
    }
}