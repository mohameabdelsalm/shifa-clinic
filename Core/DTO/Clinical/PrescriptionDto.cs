namespace Core.DTO.Clinical;

public class PrescriptionDto
{
    public long Id { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = [];
}
