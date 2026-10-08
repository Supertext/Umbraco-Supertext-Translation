# Developer guide — Supertext Translation for Umbraco

How the package is built, how to work on it, and how the demo is deployed.

## Architecture

```
Backoffice: "Translate with Supertext" (workspace action + entity action)
   └─ dialog (translate-modal.js) ── GET  /umbraco/management/api/v1/supertext/status
                                  ── GET  …/supertext/languages?documentId=
                                  ── POST …/supertext/translate {documentId, sourceCulture, targetCultures[], overwrite}
Server: SupertextTranslationController (Management API, SectionAccessContent + Update permission per culture)
   └─ ContentTranslator ── per target culture: collect segments → one HTML document → SupertextClient
                        └─ SetCultureName / SetValue(culture) → IContentService.Save (draft)
```

| Part | Role |
| --- | --- |
| `wwwroot/App_Plugins/SupertextTranslation/` | Backoffice extension, plain ES modules (no build step): `umbraco-package.json` (workspace action on `Umb.Workspace.Document`, entity action for `document`, sidebar modal), `workspace-action.js`, `entity-action.js`, `open-dialog.js` (modal token), `translate-modal.js` (Lit element on `UmbModalBaseElement`, calls the API with `umbHttpClient` and bearer security). |
| `wwwroot/App_Plugins/SupertextTranslation/lang/` | UI strings: `en.js`, `de.js`, `fr.js`, `it.js`, registered as `localization` extensions in `umbraco-package.json`. See *UI strings* below. |
| `Controllers/SupertextTranslationController` | `ManagementApiControllerBase`, `[VersionedApiBackOfficeRoute("supertext")]`. Checks `ContentPermissionResource.WithKeys(ActionUpdate.ActionLetter, document, targetCultures)`. |
| `Services/ContentTranslator` | Copies every culture-variant property from the source culture to each target; registers translatable text as segments with "rebuild" closures; translates one document per target language (chunked above `MaxDocumentCharacters`); writes values and the culture name; saves once. Existing target languages are skipped unless `overwrite` (the dialog asks). |
| `Api/SupertextClient`, `Api/HtmlDocument` | HTTP protocol (429 retries, prefix-tolerant key) and segment packing, as in the PHP plugins; HTML parsing with HtmlAgilityPack (already an Umbraco dependency). |
| `Configuration/SupertextOptions` | `Supertext` section; `SUPERTEXT_API_KEY` / `SUPERTEXT_API_ENDPOINT` win. |
| `Composing/SupertextComposer` | Options, typed `HttpClient`, scoped translator. |

**Value formats** the translator understands:

- Text box / text area: strings (plain text segments; bare URLs, e-mails, paths and numbers are skipped).
- Rich text: `{"markup": "<p>…</p>", "blocks": {…}}` since Umbraco 14 (or legacy plain HTML). The whole markup is **one** HTML segment; blocks inside it are walked like block lists.
- Block list / grid / single block: `{"contentData": [{"contentTypeKey", "values": [{"alias", "value", "editorAlias", "culture"}]}], "settingsData": […], …}`. `editorAlias` is usually `null`, so the element type (looked up by `contentTypeKey`) decides each value's editor. Only `contentData` is translated. Values may arrive **encoded twice** (a JSON string whose content is JSON); `Unwrap` decodes one level and re-encodes the result the same way.
- In `CollectBlockNode`, keep the `(object?)` casts on the switch arms: `JsonNode` has an implicit conversion from `string`, so without them the switch is typed `JsonNode` and strings silently become JSON values again (block text was skipped because of this).

**Multipart:** .NET writes part names unquoted (`name=target_lang`). `SubmitAsync` sets `Content-Disposition` explicitly with quoted names, like browsers do, because stricter parsers reject the unquoted form.

### UI strings

All text the backoffice shows comes from the localization files in `wwwroot/App_Plugins/SupertextTranslation/lang/` (Umbraco's backoffice localization: one `localization` extension per culture in `umbraco-package.json`, `meta.culture` `en`/`de`/`fr`/`it`; Umbraco matches the user's UI culture by language, so `de-CH` gets `de`, and falls back to `en`).

- Keys live in the `supertext` section: `this.localize.term('supertext_translate')` in the modal, `#supertext_translateWithSupertext` for manifest labels. Texts with arguments are functions (`overwriteWarning: (languages, count) => …`). `noApiKeyHtml` is rendered with `unsafeHTML` (our own text, with the two links).
- Server errors: `SupertextException` carries an error `Code`, `Args` and the untranslated `Detail` from Supertext besides the English `Message` (logs and API clients). The controller returns them as `supertextCode`/`supertextArgs`/`supertextDetail` in the problem details (and `errorCode`/`errorArgs`/`errorDetail` per language in the translate result); the modal shows `supertext_error_<code>` and falls back to the English text for unknown codes.
- **New or changed strings need all four languages** in the same commit: formal address (Sie, vous, Lei), Umbraco's own terms in each language (its backoffice language files: *Entwurf*, *brouillon*, *bozza*, *Dokumenttyp*, …), "Supertext", placeholders and URLs untranslated; French uses a non-breaking space (`\u00a0`) before `?`, `!`, `:` and `;`. `node Tests/Localization/check-keys.mjs` (also in CI) fails when a language misses a key, a function takes a different number of arguments, the code uses an undefined key or a server error code has no text.

## Supertext API protocol

AI file translation API v1, shared with all Supertext plugins:

1. `POST translate/ai/file` — multipart: `file` (`content.html`, part `Content-Type: text/html` exactly), `target_lang` (`de-CH`), `source_lang` (primary subtag, `en`), optional `politeness` (`more`/`less`) → `{file_id}`
2. `GET translate/ai/file/{id}/status` until `done` (`error`, `limit_exceeded`, `deleted` fail)
3. `GET translate/ai/file/{id}/translation` → translated HTML
4. `DELETE translate/ai/file/{id}` (files also expire after 24 h)

Header `Authorization: Supertext-Auth-Key <key>`. HTTP 429 is retried up to 4 times (`Retry-After`, else 1/2/4/8 s with jitter). Target languages are translated one after the other to stay under the per-second limit.

## Local development

.NET 10 SDK and Node 20+ (stand-in API, screenshots).

```bash
cd Tests/Docs && npm install && STAND_IN_PREFIX=1 node stand-in.mjs &   # untranslated text comes back as "[de-CH] …"
cd demo/SupertextDemo
export DEMO_ADMIN_EMAIL=admin@example.com DEMO_ADMIN_PASSWORD='choose-10+chars' \
       DEMO_EDITOR_EMAIL=editor@example.com DEMO_EDITOR_PASSWORD='choose-10+chars' \
       Umbraco__CMS__Unattended__UnattendedUserEmail=$DEMO_ADMIN_EMAIL \
       Umbraco__CMS__Unattended__UnattendedUserPassword=$DEMO_ADMIN_PASSWORD \
       Umbraco__CMS__WebRouting__UmbracoApplicationUrl=http://127.0.0.1:8095 \
       Umbraco__CMS__Global__UseHttps=false \
       SUPERTEXT_API_KEY=test SUPERTEXT_API_ENDPOINT=http://127.0.0.1:8765/v1/
dotnet run --urls http://127.0.0.1:8095
```

The first start installs Umbraco (SQLite in `umbraco/Data`), imports Clean, and `DemoSetupService` makes it multilingual (watch for `[demo] setup complete` in `umbraco/Logs`). Delete `umbraco/Data` for a fresh start.

Two Umbraco 17 settings matter for local HTTP:

- `UmbracoApplicationUrl`: Umbraco 17 doesn't auto-detect its URL; without it the backoffice login fails (the OAuth redirect address is unknown).
- `Global:UseHttps=false`: otherwise OpenIddict refuses the login over plain HTTP ("This server only accepts HTTPS requests"), also in Development. On Railway, HTTPS comes through the proxy instead (see Demo).

## Tests

```bash
dotnet test tests/Supertext.Umbraco.Translation.Tests    # HTML packing round trip
node Tests/Localization/check-keys.mjs                   # UI strings complete in en, de, fr, it
```

CI (`.github/workflows/ci.yml`) builds the package and the demo, runs the tests, syntax-checks the backoffice modules (and their language files), checks the UI strings and the demo entrypoint.

End to end (manual, before a release): fresh demo with the stand-in (`STAND_IN_PREFIX=1`), translate *Features* into all three languages as the editor, check every text field and block text carries the marker (and URLs and code don't), translate again (overwrite warning), publish German and open `/de/funktionen/`. `Tests/Docs/screenshots.mjs` runs most of this automatically.

## Docs screenshots

`docs/images/` is generated by `Tests/Docs/screenshots.mjs` from a **fresh** local demo whose package talks to the stand-in (without `STAND_IN_PREFIX`). The stand-in returns real German for the texts the flow touches (`Tests/Docs/sample-de.json`: *Home* and *Features*), so the guides never show placeholder text. The flow translates and publishes *Home* in German first, because German subpages need a published German home page to get a URL.

```bash
cd Tests/Docs && npm install && node stand-in.mjs &
# start the demo fresh (see Local development), then:
BASE_URL=http://127.0.0.1:8095 DEMO_EDITOR_EMAIL=… DEMO_EDITOR_PASSWORD=… DEMO_ADMIN_EMAIL=… DEMO_ADMIN_PASSWORD=… npm run screenshots
```

New texts in the flow: run the stand-in with `STAND_IN_DUMP=<dir>` and translate what it writes to `<dir>/de-CH.json` into `sample-de.json`. `CHROMIUM_PATH` points Playwright at an installed Chromium.

## Demo (Railway)

The public demo is a container built from `demo/Dockerfile`: Umbraco 17 LTS with the **Clean** starter kit (English) made multilingual — German, French and Italian (Switzerland) — and this package by project reference. It runs on Railway in the `supertext-cms-demos` project, service `umbraco`, region EU West (Amsterdam): <https://umbraco-production.up.railway.app/> (backoffice: `/umbraco`). German, French and Italian URLs (`/de`, `/fr`, `/it`) return 404 until a page is translated and published in that language.

**Deploys:** Railway watches `main` and rebuilds on every push.

| File | Purpose |
| --- | --- |
| `demo/Dockerfile` | `dotnet publish` of `demo/SupertextDemo` (SDK image), run on the ASP.NET image with `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` |
| `demo/entrypoint.sh` | Links `umbraco/Data` (SQLite), `umbraco/Logs` and `wwwroot/media` to the volume at `/data`; maps `DEMO_ADMIN_*` to the unattended install; sets `UmbracoApplicationUrl` from `APP_URL` or `RAILWAY_PUBLIC_DOMAIN` |
| `demo/SupertextDemo/` | The Umbraco project: Umbraco 17.7, Clean 7.0.8, the package, `appsettings.json` (SQLite, unattended install, Supertext language codes, `ExcludedProperties: ["code"]`) |
| `demo/SupertextDemo/DemoSetup/DemoSetupService.cs` | On every start, once Umbraco runs: adds de-CH/fr-CH/it-CH, makes non-element document types and their text/rich-text/block-list properties vary by culture, moves composition values to en-US (Umbraco only moves values of the type's own properties), sets `/de` `/fr` `/it` on the home page, creates the demo accounts |
| `demo/SupertextDemo/Views/` | Clean's views, **committed**: Clean writes them only during its first-boot package migration, so a container restart would otherwise lose them |
| `demo/.env.example` | All variables |

**State:** the SQLite database, logs and media live on a Railway volume at `/data`. Everything else comes from the image. To reset the demo, delete the files on the volume and redeploy.

**Accounts** (created on every start if missing; existing ones are never changed; the e-mail address is the username; Umbraco requires at least 10 characters, shorter passwords skip the account with a warning in the log):

| Variables | Account |
| --- | --- |
| `DEMO_ADMIN_EMAIL`, `DEMO_ADMIN_PASSWORD` | *Administrators* group; also the unattended-install user (required on first boot) |
| `DEMO_EDITOR_EMAIL`, `DEMO_EDITOR_PASSWORD` | *Editors* group — can update, translate and publish in all languages |

Umbraco's installer screen never appears: the install is unattended.

**Service variables:** `DEMO_*` (above), `SUPERTEXT_API_KEY`, optional `SUPERTEXT_API_ENDPOINT`, `APP_URL` (public URL; defaults to `https://$RAILWAY_PUBLIC_DOMAIN`), `PORT=8080` (the domain's target port), `RAILWAY_DOCKERFILE_PATH=demo/Dockerfile`.

**Run it locally:**

```bash
docker build -f demo/Dockerfile -t supertext-umbraco-demo .
docker run --rm -p 8080:8080 -v umbracodemo:/data \
  -e DEMO_ADMIN_EMAIL=admin@example.com -e DEMO_ADMIN_PASSWORD='choose-10+chars' \
  -e APP_URL=http://localhost:8080 -e Umbraco__CMS__Global__UseHttps=false \
  -e SUPERTEXT_API_KEY=... supertext-umbraco-demo
```

## Releasing

Releases are published by `.github/workflows/release.yml` when the version is officially bumped; nobody tags or creates releases by hand.

1. Move the *Unreleased* entries in `CHANGELOG.md` under a new `## X.Y.Z — YYYY-MM-DD` section, and keep an empty *Unreleased* above it.
2. Set the same version in:
   - `src/Supertext.Umbraco.Translation/Supertext.Umbraco.Translation.csproj`: `Version`, the NuGet package version
   - `src/Supertext.Umbraco.Translation/wwwroot/App_Plugins/SupertextTranslation/umbraco-package.json`: `version`, shown under Settings → Packages
3. Push to `main`. The workflow checks that the version files match `CHANGELOG.md`, then tags `vX.Y.Z` and creates the GitHub release with the CHANGELOG section as notes (0.x versions as pre-releases). A push that adds no new version does nothing, and a version that is already released is skipped. After fixing a failed run, start it again with *Run workflow* on the *Release* workflow.

Publishing to NuGet stays manual: `dotnet pack src/Supertext.Umbraco.Translation -c Release`, then push the package (the `umbraco-marketplace` tag lists it on the Umbraco Marketplace).
## Known limitations / roadmap

- Translation runs inside the request (up to `PollTimeoutSeconds` per language). Planned: a background job with progress for very large pages and many languages.
- Unsaved changes in the open page are not translated (the dialog says so).
- No sync of later source changes; translate again to refresh a language.
- Segment-level variants and block-level variance (element types that vary by culture inside an invariant block list) are not handled yet.
- Media alt texts, dictionary items and tags are not translated.
- Human (professional) translation orders are not supported yet.
- Tested on Umbraco 17.7 with the stand-in API; not yet against the live API or on Umbraco 18.
