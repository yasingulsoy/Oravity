namespace Oravity.SharedKernel.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message = "Kayıt zaten mevcut.") : base(message, 409) { }
}
