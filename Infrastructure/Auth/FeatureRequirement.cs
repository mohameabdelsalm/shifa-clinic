using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public sealed record FeatureRequirement(string FeatureCode) : IAuthorizationRequirement;
