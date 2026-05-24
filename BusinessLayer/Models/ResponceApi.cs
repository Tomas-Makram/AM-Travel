namespace BusinessLayer.Models
{
    public class ResponceApi<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ResponceApi<T> Ok(T? data, string message = "") =>
           new() { Success = true, Data = data, Message = message };

        public static ResponceApi<T> Fail(string message, params string[] errors) =>
            new() { Success = false, Message = message, Errors = errors.ToList() };
    }
}
