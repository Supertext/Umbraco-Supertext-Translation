// German UI strings of the Supertext backoffice extension (keys: see en.js).
const SIGNUP = 'https://www.supertext.com/person/en/account/signin';
const API_KEY = 'https://www.supertext.com/en/integrations/api';

export default {
  supertext: {
    translateWithSupertext: 'Mit Supertext übersetzen',
    translateFrom: 'Übersetzen aus',
    into: 'Übersetzen in',
    sourceLanguage: 'Ausgangssprache',
    translate: 'Übersetzen',
    replaceAndTranslate: 'Ersetzen und übersetzen',
    hasContent: 'hat Inhalt',
    translated: 'übersetzt',
    failed: 'fehlgeschlagen',
    hint: 'Übersetzt wird der gespeicherte Stand der Seite: Speichern Sie Ihre Änderungen zuerst. Die Übersetzungen werden als Entwürfe gespeichert, die Sie prüfen und veröffentlichen.',
    overwriteWarning: (languages, count) => `${languages} ${count === 1 ? 'hat' : 'haben'} bereits Inhalt.`,
    overwriteExplanation: (source) => `Beim Übersetzen wird der aktuelle Entwurf dort durch eine neue Übersetzung der Version ${source} ersetzt.`,
    noApiKeyHtml:
      'Es ist kein Supertext-API-Schlüssel konfiguriert. Bitten Sie Ihre Administratorin oder Ihren Administrator, <code>SUPERTEXT_API_KEY</code> zu setzen. ' +
      `Noch kein Supertext-Konto? <a href="${SIGNUP}" target="_blank" rel="noopener">Erstellen Sie eines auf supertext.com</a>. ` +
      `Generieren Sie Ihren API-Schlüssel auf <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (erfordert die Admin-Rolle).`,
    loadLanguagesFailed: 'Die Sprachen dieses Dokuments konnten nicht geladen werden.',
    startFailed: 'Die Übersetzung konnte nicht gestartet werden.',
    translatedHeadline: 'Mit Supertext übersetzt',
    translatedMessage: (languages) => `${languages} als Entwurf gespeichert. Prüfen Sie die Übersetzung und veröffentlichen Sie sie, wenn sie bereit ist.`,
    failedHeadline: 'Supertext-Übersetzung fehlgeschlagen',

    error_chooseLanguages: 'Wählen Sie eine Ausgangssprache und mindestens eine Zielsprache.',
    error_documentNotFound: 'Dokument nicht gefunden.',
    error_invariantDocumentType: (name) =>
      `Der Dokumenttyp „${name}“ variiert nicht nach Kultur und hat daher keine eigenen Sprachversionen, in die übersetzt werden kann.`,
    error_noSourceVersion: (culture) => `Das Dokument hat keine Version in ${culture}, aus der übersetzt werden kann.`,
    error_saveFailed: 'Umbraco konnte das übersetzte Dokument nicht speichern.',
    error_noApiKey:
      'Es ist kein Supertext-API-Schlüssel konfiguriert (Supertext:ApiKey oder SUPERTEXT_API_KEY). ' +
      `Noch kein Supertext-Konto? Erstellen Sie eines auf ${SIGNUP}. ` +
      `Generieren Sie Ihren API-Schlüssel auf ${API_KEY} (supertext.com → Integrations → API, erfordert die Admin-Rolle).`,
    error_authFailed:
      'Authentifizierung fehlgeschlagen. Bitte prüfen Sie den Supertext-API-Schlüssel. ' +
      `Noch kein Supertext-Konto? Erstellen Sie eines auf ${SIGNUP}. ` +
      `Generieren Sie Ihren API-Schlüssel auf ${API_KEY} (supertext.com → Integrations → API, erfordert die Admin-Rolle).`,
    error_unreachable: 'Supertext ist nicht erreichbar.',
    error_notFound: 'Die angeforderte Supertext-Ressource wurde nicht gefunden.',
    error_tooLarge: 'Der Inhalt ist zu groß, um von Supertext in einem Durchgang übersetzt zu werden.',
    error_rateLimited: 'Zu viele Anfragen an Supertext. Bitte versuchen Sie es gleich noch einmal.',
    error_unavailable: 'Der Supertext-Dienst ist zurzeit nicht verfügbar.',
    error_http: (status) => `Supertext hat mit HTTP ${status} geantwortet.`,
    error_noFileId: 'Supertext hat keine Datei-ID zurückgegeben.',
    error_translationFailed: 'Supertext konnte das Dokument nicht übersetzen.',
    error_limitExceeded: 'Ihr Supertext-Übersetzungslimit ist überschritten.',
    error_fileDeleted: 'Die Supertext-Datei wurde gelöscht, bevor sie heruntergeladen werden konnte.',
    error_timeout: 'Zeitüberschreitung beim Warten auf die Supertext-Übersetzung.',
    error_emptyTranslation: 'Das übersetzte Dokument war leer.',
  },
};
