namespace Oravity.SharedKernel.Exceptions;

public class TooManyRequestsException : AppException
{
    public TooManyRequestsException(string message = "Çok fazla istek. Lütfen bekleyin.") : base(message, 429) { }
}
