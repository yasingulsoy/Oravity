using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class Permission : BaseEntity
{
    /// <summary>
    /// Benzersiz izin kodu, örn: "patient.create", "invoice.delete"
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Hangi kaynak üzerinde etki eder, örn: "patient", "invoice"
    /// </summary>
    public string Resource { get; private set; } = default!;

    /// <summary>
    /// Eylem türü: "create", "read", "update", "delete", "export"
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>
    /// Kritik izin — UI'da ekstra onay gerektirir.
    /// </summary>
    public bool IsDangerous { get; private set; }

    public ICollection<RoleTemplatePermission> RoleTemplatePermissions { get; private set; } = [];
    public ICollection<UserPermissionOverride> UserPermissionOverrides { get; private set; } = [];

    private Permission() { }

    public static Permission Create(string resource, string action, bool isDangerous = false)
    {
        return new Permission
        {
            Resource = resource.ToLowerInvariant(),
            Action = action.ToLowerInvariant(),
            Code = $"{resource.ToLowerInvariant()}.{action.ToLowerInvariant()}",
            IsDangerous = isDangerous
        };
    }
}
