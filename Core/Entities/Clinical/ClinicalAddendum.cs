using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>An append-only correction or addition to a completed visit: who, when (CreationDate), why and what.</summary>
public class ClinicalAddendum : TenantOwnedEntity
{
    public long VisitId { get; set; }
    public long AuthorUserId { get; set; }
    public string Reason { get; set; } = "";
    public string Content { get; set; } = "";
}
