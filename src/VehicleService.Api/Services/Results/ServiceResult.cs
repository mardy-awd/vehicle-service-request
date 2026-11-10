namespace VehicleService.Api.Services.Results
{
    public class ServiceResult<T>
    {
        public bool IsSuccess => Error == ServiceError.None;

        public T? Value { get; private init; }

        public ServiceError Error { get; private init; }

        public string? ErrorMessage { get; private init; }

        public static ServiceResult<T> Success(T value) =>
            new()
            {
                Value = value,
                Error = ServiceError.None
            };

        public static ServiceResult<T> Failure(ServiceError error, string message) =>
            new()
            {
                Error = error,
                ErrorMessage = message
            };
    }
}