# ADR-014 — Password reset and change, staff invitations, branch-scoped staff list, subscription expiry

- **Status:** Accepted
- **Date:** 2026-11-16
- **Decided by:** development, under the product owner's instruction to finish the deferred identity work (2026-11-16). The expiry rule is assumption A6.

## Context

ADR-007 deferred password reset and change and the enforcement of subscription expiry (A6). Staff could only be added with a password typed by the admin, and a branch-limited member with `Staff.Read` saw every member of the clinic.

## Decision

- **E-mail.** `IEmailSender`:
  - SMTP when `Email:SmtpHost` is set (Mailpit locally).
  - Otherwise an in-memory fake, allowed only in Development (`GET /dev/email`). Any other environment refuses to start without SMTP, so a password link never silently goes nowhere.
  - Links point to `App:StaffAppUrl/set-password?token=…`; without that setting the e-mail carries the bare token.
- **One-time password tokens** (`identity.PasswordTokens`):
  - 256-bit random, stored only as SHA-256.
  - Purpose `Reset` (30 minutes) or `Invitation` (72 hours).
  - A new token invalidates the user's earlier unused ones of the same purpose.
  - Using a token claims it atomically; it works once.
- **Forgot password.** `POST api/auth/password/forgot` always gives the same answer. Only an active account with that e-mail gets a link, at most 3 per hour.
- **Setting a password.** `POST api/auth/password/reset` takes the token and a strong password. Then:
  - every refresh token and every other unused password token of the user is revoked;
  - a lockout is cleared, since the e-mail proved ownership;
  - the invitation flag is cleared.
- **Change password.** `PUT api/auth/password` is for a signed-in staff member or Platform Admin; portal accounts have no password, so a portal token gets 403. The current password is required, and a wrong one counts toward lockout. The new password must differ from the current one. All sessions are revoked, and access tokens expire on their own within minutes.
- **Invitations.** `POST api/staff` without a password creates the account with an unusable random password and `InvitationPending = true`, then e-mails a 72-hour link. `POST api/staff/{id}/resend-invitation` (`Staff.Manage`, same power rules as editing) issues a new link; the old one stops working. An admin may still set a password directly.
- **Branch-scoped staff list.** A branch-limited caller sees and manages only staff who work in at least one of their branches. All-branches staff are included because they work everywhere. Others are 404 for view, update, suspend and activate.
- **Subscription expiry (A6).** The latest subscription ends at:
  - `TrialEndsAt` for a trial;
  - `EndsAt` for an active or past-due one (none = runs until changed);
  - `EndsAt`, else `StartsAt`, for a cancelled or expired one.
- **The 30 days after it ends.**
  - The clinic is **read-only**: GET, routes ending in `/search` and endpoints marked `[ReadOperation]` work. Those endpoints are dashboard, availability, switch tenant and changing one's own password.
  - Every other change gets 403 "subscription ended, read-only" for staff and portal users. Gateway callbacks are anonymous and still record money already paid.
- **After 30 days.** Sign-in and token use stop (`TenantInfo.IsActive = false`).
- **Renewal.** Recording a payment or changing the plan restores full access at once on that instance; the 30-second cache applies elsewhere.
- **Suspension.** Suspension by the Platform Admin still blocks immediately: it is a separate, deliberate action.
- `GET api/subscription` lets the apps show the plan, the end date, read-only state and the block date.
- **Plan change tie-break.** When the plan changes, the cancelled and the new subscription start at the same moment. "Latest" is therefore ordered by `StartsAt`, then id.

## Consequences

- Production needs `Email:*` (SMTP) and `App:StaffAppUrl`.
- An e-mailed link proves control of the mailbox; a compromised mailbox can reset the password (as in most systems). MFA is not built.
- Access tokens stay valid until they expire (minutes) after a password change; refresh tokens stop at once.
- In read-only mode clinics also can't record visits or take desk payments; that follows A6 as written and can be relaxed per endpoint with `[ReadOperation]` if the product owner decides so.
