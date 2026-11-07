using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>A file attached to a visit (lab result, scan). The bytes live in file storage under <see cref="StoragePath"/>.</summary>
public class VisitAttachment : TenantOwnedEntity
{
    public long VisitId { get; set; }

    /// <summary>The original file name, for display and download only (never used to build paths).</summary>
    public string FileName { get; set; } = "";

    /// <summary>Built by the server: tenants/{tenantId}/visits/{visitId}/{guid}.{ext}.</summary>
    public string StoragePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public long UploadedByUserId { get; set; }
}
