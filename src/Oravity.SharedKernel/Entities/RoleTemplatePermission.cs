using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Junction: Rol şablonu ↔ İzin. Dış API'ye açılmaz — PublicId kullanılmaz.
/// </summary>
public class RoleTemplatePermission : BaseEntity
{
    public long RoleTemplateId { get; private set; }
    public long PermissionId { get; private set; }

    public RoleTemplate RoleTemplate { get; private set; } = default!;
    public Permission Permission { get; private set; } = default!;

    private RoleTemplatePermission() { }

    public static RoleTemplatePermission Create(long roleTemplateId, long permissionId)
    {
        return new RoleTemplatePermission
        {
            RoleTemplateId = roleTemplateId,
            PermissionId = permissionId
        };
    }
}
