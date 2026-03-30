namespace Oravity.SharedKernel.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz bulunmuyor.") : base(message, 403) { }
}
