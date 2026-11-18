# Fonts for PDF reports

QuestPDF (2026.9+) only uses fonts it can find and stops instead of substituting one (ADR-016).
Any `.ttf`/`.otf` file in this folder is copied next to the app and registered automatically.

- Development on Windows works without files here (`Reports:UseSystemFonts=true`, default): Arial and Tahoma cover Arabic.
- Production (Linux containers) needs an Arabic-capable font here, e.g. Noto Naskh Arabic (SIL Open Font License),
  or a font installed in the image. Add it before go-live (Phase 10).
