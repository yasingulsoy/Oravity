namespace Oravity.SharedKernel.Interfaces;

/// <summary>
/// Hasta portalı JWT'inden çözümlenen kimlik bilgileri.
/// Klinik personel <see cref="ICurrentUser"/> ile karışmaması için ayrı interface.
/// </summary>
public interface ICurrentPortalUser
{
    bool IsAuthenticated { get; }
    long AccountId { get; }
    long PatientId { get; }
    long BranchId { get; }
    long CompanyId { get; }
}
