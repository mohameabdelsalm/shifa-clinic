# ADR-012 — Dynamic features per plan and tenant

- **Status:** Accepted
- **Date:** 2026-11-16
- **Decided by:** product owner, 2026-11-16 ("plan + per-tenant override")

## Context

Roles and permissions are already dynamic per tenant (ADR-007: tenants build their own roles from the permission catalog through `api/roles`). The product owner wants whole features (Clinical, Billing, the patient portal, the dashboard, online payments) switched on or off per customer **without code changes**, so plans can be sold with different feature sets and a single clinic can get an exception.

## Decision

- **Feature codes** (`Core/DTO/Tenancy/FeatureCodes`): `Feature.Clinical`, `Feature.Billing`, `Feature.PatientPortal`, `Feature.Dashboard`, `Feature.OnlinePayments`. Each has a default used when nothing else says: on for all except `Feature.OnlinePayments` (off until a clinic sets up its gateway), so existing plans keep working.
- **Effective value** for a tenant: the tenant override (`tenancy.TenantFeatureOverrides`, set by the Platform Admin) → else the plan's entitlement with that code (`PlanEntitlements`, already used for limits) → else the default. `ITenancyApi.GetFeaturesAsync` / `IsFeatureEnabledAsync`, cached 30 seconds (the override endpoint clears the cache on that instance).
- **Enforcement:** `[RequireFeature(FeatureCodes.X)]` on controllers → `FeatureRequirement` policy → `FeatureRequirementHandler`. A disabled feature returns 403 with "This feature is not enabled for your clinic". Requests without a tenant never pass. Applied to Clinical, Billing, portal and dashboard controllers. The portal OTP request answers like an unknown clinic when the portal is off (no enumeration, no SMS). Check-in creates no invoice when Billing is off.
- **APIs:** `GET api/platform/tenants/{id}/features` (value and source: Override / Plan / Default), `PUT api/platform/tenants/features` (`Enabled` true/false sets an override, null removes it) — Platform Admin, audited. `GET api/features` for the signed-in clinic (the UI hides what is off). Plans accept the feature codes in their entitlements.

## Consequences

- Turning a feature off hides it immediately on the instance that handled the change and within 30 seconds elsewhere; data is kept, so turning it back on restores everything.
- New features need a code in `FeatureCodes` (with its default) and the attribute on their controllers — a small code change once, then on/off is data.
