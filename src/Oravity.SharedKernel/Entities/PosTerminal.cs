using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şubeye ait POS cihazı.
/// Kredi kartı / taksit ödemelerinde hangi POS kullanıldığı takip edilir.
/// </summary>
public class PosTerminal : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public string Name { get; private set; } = string.Empty;  // "Akbank POS 1"

    public long? BankId { get; private set; }
    public Bank? Bank { get; private set; }

    public string? TerminalId { get; private set; }           // POS cihaz numarası
    public bool IsActive { get; private set; } = true;

    private PosTerminal() { }

    public static PosTerminal Create(long branchId, string name, long? bankId, string? terminalId = null) =>
        new() { BranchId = branchId, Name = name, BankId = bankId, TerminalId = terminalId };

    public void Update(string name, long? bankId, string? terminalId)
    {
        Name       = name;
        BankId     = bankId;
        TerminalId = terminalId;
        MarkUpdated();
    }

    public void Deactivate() { IsActive = false; MarkUpdated(); }
    public void Activate()   { IsActive = true;  MarkUpdated(); }
}
