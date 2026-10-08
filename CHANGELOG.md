# Changelog

## Unreleased

- Added: French and Italian interface (and German where it was missing). The button, dialog, notifications and error messages follow the backoffice user's UI culture (English, German, French, Italian); server errors are sent with a code so the dialog can show them in the user's language. The "Authentication failed" message now also links to Supertext account signup and API key generation.

## 0.1.0 — 2026-10-07

- First version for Umbraco 17/18: **Translate with Supertext** in the document workspace and actions menu, with a dialog to choose source and target languages.
- Translates the page name, text boxes, text areas, rich text (incl. its blocks) and text inside block list / block grid blocks; copies all other culture-variant values; saves target languages as drafts.
- One Supertext request per target language; confirmation before replacing languages that already have content; per-language results and notifications.
- Settings: API key (`SUPERTEXT_API_KEY`), endpoint, per-language code / formality / visibility, editors, excluded properties. Retries on rate limiting; bare URLs, e-mails and numbers are never sent.
- Demo container (`demo/`): Umbraco 17 LTS + Clean starter kit made multilingual (de-CH, fr-CH, it-CH), accounts from `DEMO_ADMIN_*` / `DEMO_EDITOR_*`.
- Installation, user and developer guides with screenshots generated from the demo (`Tests/Docs/screenshots.mjs`).
- The "No Supertext API key" warning in the dialog, the missing-key error, the config comments and the docs now link to Supertext account signup and API key generation (supertext.com → Integrations → API, Admin role required).
