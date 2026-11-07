namespace Core.DTO.Clinical;

/// <summary>An attachment opened for download. The caller disposes <see cref="Content"/>.</summary>
public sealed record AttachmentFile(Stream Content, string ContentType, string FileName);
