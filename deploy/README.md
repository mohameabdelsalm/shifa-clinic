# Deploying Shifa (runbook)

ADR-017 explains the decisions. This page is the checklist an operator follows.

## What runs where

| Piece | Where | Notes |
|---|---|---|
| Shifa API | Docker image built from `Dockerfile` (Linux, .NET 10, non-root, port 8080) | Stateless except `/data` |
| HTTPS | Caddy (`deploy/Caddyfile`), automatic Let's Encrypt | Or any reverse proxy / Azure front door |
| Database | Licensed SQL Server 2019+ or Azure SQL | **Never** Developer Edition in production |
| `/data` volume | Data Protection key ring (`/data/keys`) + visit attachments (`/data/files`) | Must survive redeploys; back it up |
| Jobs | Hangfire inside the API (schema `hangfire`) | `Jobs__Enabled=false` on extra instances if wanted |

## First install

1. **Database.** Create it with the same collation as development (Arabic sorting/search):
   ```sql
   CREATE DATABASE Shifa COLLATE Arabic_CI_AS;
   ```
   Turn on TDE (encryption at rest) and use encrypted backups.
2. **Logins** (least privilege):
   - a *migration* login with `db_owner`, used only by `--migrate`;
   - an *app* login with `db_datareader`, `db_datawriter` and `EXECUTE` (needed for `sp_getapplock`). The API never needs DDL rights.
3. **Settings.** Copy `deploy/.env.example` to `deploy/.env` and fill in every value. Secrets go only here or in a secret store, never in git.
   - `Jwt__SigningKey`: 64+ random characters.
   - `Reports__QuestPdfLicense`: the right license type (Community only under USD 1M annual revenue).
   - `Sms__Provider=Disabled` until a provider is chosen (the portal OTP and SMS reminders are then skipped).
4. **Build and migrate.** Run `--migrate` with the migration login's connection string. It prints each module's pending migrations, applies them and installs the Hangfire schema:
   ```bash
   cd deploy && docker compose build
   ```
   ```bash
   docker compose run --rm -e ConnectionStrings__Shifa="<migration login connection string>" api --migrate
   ```
5. **First Platform Admin.** The password is typed here, not stored in a file:
   ```bash
   docker compose run --rm -e PlatformAdmin__Email=admin@example.com -e PlatformAdmin__Password='<strong password>' api --seed-platform-admin
   ```
6. **Start**, then check:
   ```bash
   docker compose up -d
   ```
   `https://<domain>/health/live` and `/health/ready` must answer 200.

## Updating

1. Back up the database and the `shifa-data` volume.
2. Build, migrate and restart:
   ```bash
   git pull && cd deploy && docker compose build
   ```
   ```bash
   docker compose run --rm -e ConnectionStrings__Shifa="<migration login>" api --migrate
   ```
   ```bash
   docker compose up -d
   ```
3. Check the health endpoints and the logs:
   ```bash
   docker compose logs api --since 10m
   ```

Migrations only add or change. Review the pending list `--migrate` prints before running it on production. Nothing in Shifa drops a database.

## Rotating the JWT key

1. Move the current value to `Jwt__PreviousSigningKeys__0`.
2. Put a new random value in `Jwt__SigningKey`, then restart.
3. Old access tokens keep working until they expire (15 minutes).
4. After that, remove `Jwt__PreviousSigningKeys__0` and restart again.

## Backups and restore

| What | How often | Notes |
|---|---|---|
| Database: full | Daily | Encrypted (backup encryption or TDE). Keep 30+ days, one copy off-site |
| Database: transaction log | Every 15 minutes | Point-in-time restore; target RPO 15 minutes |
| `shifa-data` volume (`/data`) | Daily | Attachments + key ring. **Without the key ring, stored Paymob secrets can't be decrypted** and clinics must re-enter them |

**Restore drill (monthly, on a separate server):**
1. Restore the database to a point in time.
2. Restore `/data`.
3. Run the same image with the restored settings.
4. Sign in, open a patient, view an attachment, and run a report.
5. Write down how long it took (RTO target: 4 hours).

## Monitoring

- **Health.** `/health/live` (process) and `/health/ready` (every module database). Point the host's or the load balancer's health check at them.
- **Logs.** Serilog writes to stdout (`docker compose logs`); add a Seq or other sink in `Serilog:WriteTo` if wanted. Logs never contain patient medical data, passwords, OTPs, tokens or message texts.
- **Things to watch:**
  - poisoned outbox messages (log level Error "poisoned");
  - refunds left `Pending` (check them in Paymob);
  - online attempts in `Refunding`;
  - audit actions `Billing.OnlineRefund` with "NOT SETTLED".
- **Hangfire dashboard.** Not exposed outside Development.

## Go-live checklist

- [ ] Licensed SQL Server or Azure SQL, Arabic collation, TDE, encrypted backups, restore drill done
- [ ] Separate migration login and app login
- [ ] Every `CHANGE_ME` in `.env` replaced; `.env` not in git
- [ ] DNS points to the server; ports 80/443 open; HTTPS works
- [ ] `ReverseProxy__KnownNetworks__0` matches the docker network (the rate limit sees real client IPs)
- [ ] `Cors__AllowedOrigins__*` lists only the real front-ends
- [ ] SMTP tested (forgot password e-mail arrives)
- [ ] SMS provider chosen, or `Sms__Provider=Disabled` accepted (no portal sign-in)
- [ ] QuestPDF license type set; an Arabic PDF report checked by eye
- [ ] Paymob: `Paymob__NotificationBaseUrl` reachable from the internet; one real test payment and refund in Paymob's test mode
- [ ] Platform Admin created; first clinic onboarded
