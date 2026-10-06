// The "Translate with Supertext" dialog (sidebar modal).
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';

const API = '/umbraco/management/api/v1/supertext';
const AUTH = [{ scheme: 'bearer', type: 'http' }];

export class SupertextTranslateModalElement extends UmbModalBaseElement {
  static properties = {
    _languages: { state: true },
    _source: { state: true },
    _targets: { state: true },
    _status: { state: true },
    _busy: { state: true },
    _confirmOverwrite: { state: true },
    _results: { state: true },
    _error: { state: true },
  };

  constructor() {
    super();
    this._languages = [];
    this._targets = new Set();
    this._busy = false;
    this._confirmOverwrite = false;
    this._results = undefined;
    this._error = undefined;
  }

  async connectedCallback() {
    super.connectedCallback();
    const unique = this.data?.unique;
    const [status, languages] = await Promise.all([
      umbHttpClient.get({ url: `${API}/status`, security: AUTH }),
      umbHttpClient.get({ url: `${API}/languages`, query: { documentId: unique }, security: AUTH }),
    ]);
    this._status = status.data;
    if (languages.error || !Array.isArray(languages.data)) {
      this._error = languages.error?.title ?? 'Could not load the languages of this document.';
      return;
    }
    this._languages = languages.data;
    const source = this._languages.find((l) => l.isDefault && l.exists) ?? this._languages.find((l) => l.exists);
    this._source = source?.isoCode;
    // Preselect every language that has no version yet.
    this._targets = new Set(this._languages.filter((l) => !l.exists && l.isoCode !== this._source).map((l) => l.isoCode));
  }

  _toggle(isoCode, checked) {
    const targets = new Set(this._targets);
    checked ? targets.add(isoCode) : targets.delete(isoCode);
    this._targets = targets;
    this._confirmOverwrite = false;
  }

  get _existingTargets() {
    return this._languages.filter((l) => l.exists && this._targets.has(l.isoCode));
  }

  async _translate() {
    if (this._existingTargets.length > 0 && !this._confirmOverwrite) {
      this._confirmOverwrite = true; // ask first: translating replaces their content
      return;
    }
    this._busy = true;
    this._error = undefined;
    const { data, error } = await umbHttpClient.post({
      url: `${API}/translate`,
      body: {
        documentId: this.data?.unique,
        sourceCulture: this._source,
        targetCultures: [...this._targets],
        overwrite: this._confirmOverwrite,
      },
      headers: { 'Content-Type': 'application/json' },
      security: AUTH,
    });
    this._busy = false;
    if (error || !Array.isArray(data)) {
      this._error = error?.title ?? 'The translation could not be started.';
      return;
    }
    this._results = data;
    const translated = data.filter((r) => r.status === 'translated');
    const failed = data.filter((r) => r.status === 'failed');
    const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT);
    if (translated.length) {
      notifications.peek('positive', {
        data: {
          headline: 'Translated with Supertext',
          message: `${translated.map((r) => this._name(r.culture)).join(', ')} saved as draft. Review and publish when ready.`,
        },
      });
    }
    if (failed.length) {
      notifications.peek('danger', {
        data: { headline: 'Supertext translation failed', message: failed.map((r) => `${this._name(r.culture)}: ${r.error}`).join(' ') },
      });
    }
    this.value = { translated: translated.length };
    if (!failed.length) this._submitModal();
  }

  _name(isoCode) {
    return this._languages.find((l) => l.isoCode === isoCode)?.name ?? isoCode;
  }

  render() {
    return html`
      <umb-body-layout headline="Translate with Supertext">
        <div id="main">
          ${this._status && !this._status.hasApiKey
            ? html`<div class="warning">
                No Supertext API key is configured. Ask your administrator to set <code>SUPERTEXT_API_KEY</code>.
                No Supertext account yet? <a href="https://www.supertext.com/person/en/account/signin" target="_blank" rel="noopener">Create one at supertext.com</a>.
                Generate your API key at <a href="https://www.supertext.com/en/integrations/api" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (requires the Admin role).
              </div>`
            : nothing}
          ${this._error ? html`<div class="warning">${this._error}</div>` : nothing}
          ${this._languages.length ? this._renderForm() : html`<uui-loader></uui-loader>`}
        </div>
        <div slot="actions">
          <uui-button label="Cancel" @click=${this._rejectModal}></uui-button>
          <uui-button
            look="primary"
            color=${this._confirmOverwrite ? 'danger' : 'positive'}
            label=${this._confirmOverwrite ? 'Replace and translate' : 'Translate'}
            ?disabled=${!this._source || this._targets.size === 0 || this._busy || this._status?.hasApiKey === false}
            .state=${this._busy ? 'waiting' : undefined}
            @click=${this._translate}></uui-button>
        </div>
      </umb-body-layout>
    `;
  }

  _renderForm() {
    const sources = this._languages.filter((l) => l.exists);
    return html`
      <uui-box headline="Translate from">
        <select
          id="source"
          aria-label="Source language"
          .value=${this._source ?? ''}
          @change=${(e) => {
            this._source = e.target.value;
            this._toggle(this._source, false);
          }}>
          ${sources.map((l) => html`<option value=${l.isoCode} ?selected=${l.isoCode === this._source}>${l.name}</option>`)}
        </select>
      </uui-box>
      <uui-box headline="Into">
        ${this._languages
          .filter((l) => l.isoCode !== this._source)
          .map(
            (l) => html`
              <div class="language">
                <uui-checkbox
                  label=${l.name}
                  ?checked=${this._targets.has(l.isoCode)}
                  @change=${(e) => this._toggle(l.isoCode, e.target.checked)}></uui-checkbox>
                ${l.exists ? html`<uui-tag look="secondary">has content</uui-tag>` : nothing}
                ${this._renderResult(l.isoCode)}
              </div>
            `,
          )}
      </uui-box>
      ${this._confirmOverwrite
        ? html`<div class="warning" role="alert">
            <strong>${this._existingTargets.map((l) => l.name).join(', ')} already
            ${this._existingTargets.length === 1 ? 'has' : 'have'} content.</strong>
            Translating replaces the current draft there with a new translation of the ${this._name(this._source)} version.
          </div>`
        : nothing}
      <p class="hint">
        The page is translated as saved: save your changes first. Translations are saved as drafts for you to review and publish.
      </p>
    `;
  }

  _renderResult(isoCode) {
    const r = this._results?.find((x) => x.culture === isoCode);
    if (!r) return nothing;
    if (r.status === 'translated') return html`<uui-tag color="positive">translated</uui-tag>`;
    if (r.status === 'failed') return html`<uui-tag color="danger" title=${r.error ?? ''}>failed</uui-tag>`;
    return nothing;
  }

  static styles = css`
    #main { display: flex; flex-direction: column; gap: var(--uui-size-space-5); }
    .language { display: flex; align-items: center; gap: var(--uui-size-space-3); padding: var(--uui-size-space-2) 0; }
    select { width: 100%; padding: var(--uui-size-space-3); font: inherit; border: 1px solid var(--uui-color-border); border-radius: var(--uui-border-radius); background: var(--uui-color-surface); color: var(--uui-color-text); }
    .warning { padding: var(--uui-size-space-4); border-radius: var(--uui-border-radius); background: var(--uui-color-warning); color: var(--uui-color-warning-contrast); }
    .warning a { color: inherit; text-decoration: underline; }
    .hint { color: var(--uui-color-text-alt); margin: 0; }
  `;
}

customElements.define('supertext-translate-modal', SupertextTranslateModalElement);
export default SupertextTranslateModalElement;
