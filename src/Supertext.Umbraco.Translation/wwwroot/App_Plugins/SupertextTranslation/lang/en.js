// English UI strings of the Supertext backoffice extension (also the fallback for every other language).
// Keys are used as `supertext_<key>` (e.g. this.localize.term('supertext_translate')) or `#supertext_<key>` in manifests.
// Every key here must exist in de.js, fr.js and it.js too (checked by Tests/Localization/check-keys.mjs).
const SIGNUP = 'https://www.supertext.com/person/en/account/signin';
const API_KEY = 'https://www.supertext.com/en/integrations/api';

export default {
  supertext: {
    translateWithSupertext: 'Translate with Supertext',
    translateFrom: 'Translate from',
    into: 'Into',
    sourceLanguage: 'Source language',
    translate: 'Translate',
    replaceAndTranslate: 'Replace and translate',
    hasContent: 'has content',
    translated: 'translated',
    failed: 'failed',
    hint: 'The page is translated as saved: save your changes first. Translations are saved as drafts for you to review and publish.',
    overwriteWarning: (languages, count) => `${languages} already ${count === 1 ? 'has' : 'have'} content.`,
    overwriteExplanation: (source) => `Translating replaces the current draft there with a new translation of the ${source} version.`,
    noApiKeyHtml:
      'No Supertext API key is configured. Ask your administrator to set <code>SUPERTEXT_API_KEY</code>. ' +
      `No Supertext account yet? <a href="${SIGNUP}" target="_blank" rel="noopener">Create one at supertext.com</a>. ` +
      `Generate your API key at <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (requires the Admin role).`,
    loadLanguagesFailed: 'Could not load the languages of this document.',
    startFailed: 'The translation could not be started.',
    translatedHeadline: 'Translated with Supertext',
    translatedMessage: (languages) => `${languages} saved as draft. Review and publish when ready.`,
    failedHeadline: 'Supertext translation failed',

    // Errors from the server (code → text); the server also sends the English text as a fallback.
    error_chooseLanguages: 'Choose a source language and at least one target language.',
    error_documentNotFound: 'Document not found.',
    error_invariantDocumentType: (name) =>
      `The document type "${name}" does not vary by culture, so it has no separate language versions to translate into.`,
    error_noSourceVersion: (culture) => `The document has no ${culture} version to translate from.`,
    error_saveFailed: 'Umbraco could not save the translated document.',
    error_noApiKey:
      'No Supertext API key is configured (Supertext:ApiKey or SUPERTEXT_API_KEY). ' +
      `No Supertext account yet? Create one at ${SIGNUP}. ` +
      `Generate your API key at ${API_KEY} (supertext.com → Integrations → API, requires the Admin role).`,
    error_authFailed:
      'Authentication failed. Please check the Supertext API key. ' +
      `No Supertext account yet? Create one at ${SIGNUP}. ` +
      `Generate your API key at ${API_KEY} (supertext.com → Integrations → API, requires the Admin role).`,
    error_unreachable: 'Could not reach Supertext.',
    error_notFound: 'The requested Supertext resource was not found.',
    error_tooLarge: 'The content is too large for Supertext to translate in one go.',
    error_rateLimited: 'Too many requests to Supertext. Please try again shortly.',
    error_unavailable: 'The Supertext service is currently unavailable.',
    error_http: (status) => `Supertext answered with HTTP ${status}.`,
    error_noFileId: 'Supertext did not return a file id.',
    error_translationFailed: 'Supertext failed to translate the document.',
    error_limitExceeded: 'Your Supertext translation limit is exceeded.',
    error_fileDeleted: 'The Supertext file was deleted before it could be downloaded.',
    error_timeout: 'Timed out waiting for the Supertext translation.',
    error_emptyTranslation: 'The translated document was empty.',
  },
};
