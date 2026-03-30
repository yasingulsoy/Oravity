namespace Oravity.SharedKernel.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message = "İstenen kayıt bulunamadı.") : base(message, 404) { }
}
