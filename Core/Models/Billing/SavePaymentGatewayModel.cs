namespace Core.Models.Billing;

/// <summary>The clinic's Paymob keys (from the Paymob dashboard → Developers).</summary>
public class SavePaymentGatewayModel
{
    public string PublicKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string HmacSecret { get; set; } = "";
    public List<long> IntegrationIds { get; set; } = [];
}
