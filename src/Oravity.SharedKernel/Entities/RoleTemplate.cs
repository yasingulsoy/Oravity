using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class RoleTemplate : BaseEntity
{
    /// <summary>
    /// Sabit rol kodu: BRANCH_MANAGER, DOCTOR, ASSISTANT, RECEPTIONIST, ACCOUNTANT, READONLY
    /// </summary>
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<RoleTemplatePermission> RoleTemplatePermissions { get; private set; } = [];
    public ICollection<UserRoleAssignment> UserRoleAssignments { get; private set; } = [];

    private RoleTemplate() { }

    public static RoleTemplate Create(string code, string name, string? description = null)
    {
        return new RoleTemplate
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            Description = description,
            IsActive = true
        };
    }

    public void SetActive(bool value) => IsActive = value;
}
