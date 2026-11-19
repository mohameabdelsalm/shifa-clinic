namespace Core.DTO.Billing;

/// <summary>The gateway settings without secrets (only whether they are set).</summary>
public class PaymentGatewayDto
{
    public string Provider { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public List<long> IntegrationIds { get; set; } = [];
    public bool HasSecretKey { get; set; }
    public bool HasHmacSecret { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
