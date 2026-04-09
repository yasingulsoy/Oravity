namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şube + yıl bazlı protokol sıra no sayacı.
/// PK: (branch_id, year) — composite.
/// </summary>
public class ProtocolSequence
{
    public long BranchId { get; private set; }
    public int  Year     { get; private set; }
    public int  LastSeq  { get; private set; }

    private ProtocolSequence() { }

    public static ProtocolSequence Create(long branchId, int year) =>
        new() { BranchId = branchId, Year = year, LastSeq = 0 };

    /// <summary>Sayacı 1 artırır ve yeni değeri döner.</summary>
    public int Increment()
    {
        LastSeq++;
        return LastSeq;
    }
}
