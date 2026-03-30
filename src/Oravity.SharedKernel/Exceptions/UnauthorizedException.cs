namespace Oravity.SharedKernel.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Yetkisiz erişim.") : base(message, 401) { }
}
