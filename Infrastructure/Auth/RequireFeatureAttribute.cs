using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>
/// Requires a feature to be enabled for the caller's tenant (plan entitlement or tenant override), e.g.
/// [RequireFeature(FeatureCodes.Billing)]. Several attributes on one endpoint must all pass.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Feature:";

    public RequireFeatureAttribute(string featureCode) : base(PolicyPrefix + featureCode)
    {
    }
}
