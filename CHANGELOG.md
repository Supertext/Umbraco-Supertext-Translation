# Changelog

## Unreleased

- Changed: **languages are no longer guessed.** An Umbraco culture that is a Supertext code (`de-CH`, `fr-CH`, `es`, `nl`, `zh-Hans`, apart from case and `_`/`-`) is used as it is; every other culture needs `Supertext:Languages:<culture>:Code` (the same setting as before, now the language mapping, used for source and target). The default regions are gone: they were wrong for most languages (Supertext wants `es`, `sl`, `nl` without region and `zh-Hans` instead of `zh-CN`). An unmapped language is marked *not mapped* in the dialog, and translating into it says *has no Supertext language yet*; nothing is sent for it. New message for *no formal or informal tone for this language* (English, German, French, Italian).
- Added: **Settings › Supertext** dashboard: package version, API endpoint, whether a key is set, the latest automatic API key check with its time and a *Check API key* button, the version reports setting, and every Umbraco language with its Supertext code (*Not mapped* where one is missing) and tone (with a warning where Supertext offers none). English, German, French, Italian.
- Added: automatic API key check (no cost) when the key changes, on *Check API key* and after Supertext rejects a translation's key.
- Added: **Send version information to Supertext** (`Supertext:SendVersionReports`, on by default; `SUPERTEXT_VERSION_REPORTS=0` turns it off): after an update and when the key check result changes, the site URL, the versions of the package, Umbraco, .NET and the Supertext SDK and the key check result are sent to Supertext, never content, the API key or personal data. Public ping route `GET /umbraco/supertext/ping` (404 while reports are off). See the installation guide.
- Changed: bundled Supertext SDK 0.2.1 (commit `eeba8fa`, `V1` namespace): the API's language list, the key check and the version reports.

- Changed: the package now talks to Supertext through the official [Supertext SDK for .NET](https://github.com/Supertext/Supertext-API-SDK-and-Guide) instead of its own API client, HTML packer and language code rules. The SDK is compiled into the package, so there is nothing extra to install; HtmlAgilityPack is no longer referenced. Same behaviour.
- Added: clear messages when Supertext rejects a language pair or returns an incomplete translation (English, German, French, Italian).
- Fixed: a target language Supertext doesn't know is now reported as an unsupported language pair (was: *The requested Supertext resource was not found*).
- Added: French and Italian interface (and German where it was missing). The button, dialog, notifications and error messages follow the backoffice user's UI culture (English, German, French, Italian); server errors are sent with a code so the dialog can show them in the user's language. The "Authentication failed" message now also links to Supertext account signup and API key generation.

## 0.1.0 — 2026-10-07

- First version for Umbraco 17/18: **Translate with Supertext** in the document workspace and actions menu, with a dialog to choose source and target languages.
- Translates the page name, text boxes, text areas, rich text (incl. its blocks) and text inside block list / block grid blocks; copies all other culture-variant values; saves target languages as drafts.
- One Supertext request per target language; confirmation before replacing languages that already have content; per-language results and notifications.
- Settings: API key (`SUPERTEXT_API_KEY`), endpoint, per-language code / formality / visibility, editors, excluded properties. Retries on rate limiting; bare URLs, e-mails and numbers are never sent.
- Demo container (`demo/`): Umbraco 17 LTS + Clean starter kit made multilingual (de-CH, fr-CH, it-CH), accounts from `DEMO_ADMIN_*` / `DEMO_EDITOR_*`.
- Installation, user and developer guides with screenshots generated from the demo (`Tests/Docs/screenshots.mjs`).
- The "No Supertext API key" warning in the dialog, the missing-key error, the config comments and the docs now link to Supertext account signup and API key generation (supertext.com → Integrations → API, Admin role required).
