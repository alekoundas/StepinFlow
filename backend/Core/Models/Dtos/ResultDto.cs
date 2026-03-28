namespace Core.Models.Dtos
{
    public class ResultDto<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public IReadOnlyList<string> Errors { get; private set; } = new List<string>();

        // Success factory
        public static ResultDto<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        // Failure factories
        public static ResultDto<T> Failure(string errorMessage) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };

        public static ResultDto<T> Failure(IEnumerable<string> errors) => new()
        {
            IsSuccess = false,
            Errors = errors.ToList()
        };

    }
}
