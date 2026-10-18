// The "Supertext" dashboard in the Settings section: the configuration as the package sees it, the latest
// API key check (with a "Check API key" button), the version reports setting and the language mapping.
// Settings themselves live in appsettings.json / the environment (docs/INSTALLATION.md).
import { html, css, nothing, unsafeHTML } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';

const API = '/umbraco/management/api/v1/supertext/settings';
const AUTH = [{ scheme: 'bearer', type: 'http' }];

export class SupertextSettingsDashboardElement extends UmbLitElement {
  static properties = {
    _settings: { state: true },
    _check: { state: true },
    _checking: { state: true },
    _error: { state: true },
  };

  constructor() {
    super();
    this._checking = false;
  }

  async connectedCallback() {
    super.connectedCallback();
    const { data, error } = await umbHttpClient.get({ url: API, security: AUTH });
    if (error || !data) {
      this._error = this.localize.term('supertext_dashboardLoadFailed');
      return;
    }
    this._settings = data;
    this._check = data.keyCheck;
  }

  async _checkKey() {
    this._checking = true;
    const { data, error } = await umbHttpClient.post({ url: `${API}/check-key`, security: AUTH });
    this._checking = false;
    if (error || !data) {
      this._error = this.localize.term('supertext_checkFailed');
      return;
    }
    this._error = undefined;
    this._check = data;
  }

  _when(iso) {
    const date = new Date(iso);
    return Number.isNaN(date.getTime()) ? iso : this.localize.date(date, { dateStyle: 'medium', timeStyle: 'short' });
  }

  _tone(politeness) {
    if (politeness === 'more') return this.localize.term('supertext_formal');
    if (politeness === 'less') return this.localize.term('supertext_informal');
    return this.localize.term('supertext_defaultTone');
  }

  render() {
    if (this._error && !this._settings) return html`<uui-box><div class="fail">${this._error}</div></uui-box>`;
    if (!this._settings) return html`<uui-loader></uui-loader>`;
    const s = this._settings;
    return html`
      <uui-box headline=${this.localize.term('supertext_dashboardHeadline')}>
        <dl>
          <dt>${this.localize.term('supertext_version')}</dt>
          <dd>
            ${s.releaseUrl ? html`<a href=${s.releaseUrl} target="_blank" rel="noopener">${s.version}</a>` : s.version}
            <span class="hint">· Umbraco ${s.umbracoVersion ?? '?'} · Supertext SDK ${s.sdkVersion}</span>
          </dd>
          <dt>${this.localize.term('supertext_endpoint')}</dt>
          <dd><code>${s.endpoint}</code></dd>
          <dt>${this.localize.term('supertext_apiKey')}</dt>
          <dd>${this.localize.term(s.hasApiKey ? 'supertext_apiKeySet' : 'supertext_apiKeyNotSet')}</dd>
          <dt>${this.localize.term('supertext_keyCheck')}</dt>
          <dd>
            ${this._check
              ? html`<div class=${this._check.status === 'ok' ? 'ok' : 'fail'}>
                    ${unsafeHTML(this.localize.term(`supertext_keycheck_${this._check.status}`, this._check.httpStatus ?? ''))}
                  </div>
                  <div class="hint">${this.localize.term('supertext_checkedAt', this._when(this._check.checkedAt))}</div>`
              : html`<div>${this.localize.term('supertext_notChecked')}</div>`}
            ${this._error ? html`<div class="fail">${this._error}</div>` : nothing}
            <uui-button
              look="secondary"
              label=${this.localize.term('supertext_checkKey')}
              .state=${this._checking ? 'waiting' : undefined}
              ?disabled=${this._checking}
              @click=${this._checkKey}></uui-button>
          </dd>
          <dt>${this.localize.term('supertext_reports')}</dt>
          <dd>
            <div>
              ${this.localize.term(s.reports.enabled ? 'supertext_reportsOn' : 'supertext_reportsOff')}
              ${s.reports.fromEnvironment ? html`<span class="hint">(SUPERTEXT_VERSION_REPORTS)</span>` : nothing}
            </div>
            <div class="hint">${this.localize.term('supertext_reportsHelp')}</div>
            <div class="hint">${this.localize.term('supertext_reportsHow')}</div>
            ${s.reports.pingUrl
              ? html`<div class="hint">${this.localize.term('supertext_pingUrl')} <code>${s.reports.pingUrl}</code></div>`
              : nothing}
          </dd>
        </dl>
      </uui-box>
      <uui-box headline=${this.localize.term('supertext_languagesHeadline')}>
        <p class="hint">${this.localize.term('supertext_languagesHint')}</p>
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell>${this.localize.term('supertext_umbracoLanguage')}</uui-table-head-cell>
            <uui-table-head-cell>${this.localize.term('supertext_supertextLanguage')}</uui-table-head-cell>
            <uui-table-head-cell>${this.localize.term('supertext_tone')}</uui-table-head-cell>
          </uui-table-head>
          ${s.languages.map(
            (l) => html`
              <uui-table-row>
                <uui-table-cell>
                  ${l.name} <code>${l.isoCode}</code>
                  ${l.enabled ? nothing : html`<span class="hint">· ${this.localize.term('supertext_hiddenInDialog')}</span>`}
                </uui-table-cell>
                <uui-table-cell>
                  ${l.supertextCode
                    ? html`<code>${l.supertextCode}</code>
                        <span class="hint">${this.localize.term(l.mapped ? 'supertext_fromMapping' : l.fromDefault ? 'supertext_defaultTarget' : 'supertext_exactMatch')}</span>`
                    : html`<span class="fail">${this.localize.term('supertext_notMappedLong', l.isoCode)}</span>`}
                </uui-table-cell>
                <uui-table-cell>
                  ${this._tone(l.politeness)}
                  ${l.politeness !== 'default' && l.politenessSupported === false
                    ? html`<div class="fail">${this.localize.term('supertext_noPoliteness')}</div>`
                    : nothing}
                </uui-table-cell>
              </uui-table-row>
            `,
          )}
        </uui-table>
      </uui-box>
    `;
  }

  static styles = css`
    :host { display: flex; flex-direction: column; gap: var(--uui-size-layout-1); padding: var(--uui-size-layout-1); }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: var(--uui-size-space-4) var(--uui-size-space-6); margin: 0; }
    dt { font-weight: bold; }
    dd { margin: 0; display: flex; flex-direction: column; align-items: flex-start; gap: var(--uui-size-space-2); }
    .hint { color: var(--uui-color-text-alt); }
    .ok { color: var(--uui-color-positive-standalone); }
    .fail { color: var(--uui-color-danger-standalone); }
    .fail a, .ok a { color: inherit; text-decoration: underline; }
    p { margin-top: 0; }
  `;
}

customElements.define('supertext-settings-dashboard', SupertextSettingsDashboardElement);
export default SupertextSettingsDashboardElement;
