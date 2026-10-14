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
    error_languagePair: 'Supertext unterstützt dieses Sprachpaar nicht. Prüfen Sie die Sprachzuordnung (Supertext:Languages:<culture>:Code, siehe Einstellungen › Supertext).',
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
    error_emptyTranslation: 'Die Übersetzung von Supertext war leer oder unvollständig.',

    notMapped: 'nicht zugeordnet',
    notMappedHint: (culture) => `${culture} ist noch keiner Supertext-Sprache zugeordnet. Ihre Administratorin oder Ihr Administrator kann sie unter Einstellungen › Supertext zuordnen.`,
    error_unsupportedLanguage: (language) =>
      `Die Sprache ${language} ist noch keiner Supertext-Sprache zugeordnet. Bitten Sie Ihre Administratorin oder Ihren Administrator, sie zuzuordnen (Supertext:Languages:${language}:Code in appsettings.json, z. B. "de-CH").`,
    error_unsupportedFeature:
      'Supertext bietet für diese Sprache keine formelle oder informelle Anrede an. Bitten Sie Ihre Administratorin oder Ihren Administrator, die Standard-Anrede einzustellen (Supertext:Languages:<culture>:Politeness).',

    dashboardLabel: 'Supertext',
    dashboardHeadline: 'Supertext Translation',
    dashboardLoadFailed: 'Die Supertext-Einstellungen konnten nicht geladen werden.',
    version: 'Version',
    endpoint: 'API-Endpunkt',
    apiKey: 'API-Schlüssel',
    apiKeySet: 'Gesetzt',
    apiKeyNotSet: 'Nicht gesetzt (Supertext:ApiKey oder SUPERTEXT_API_KEY)',
    keyCheck: 'Prüfung des API-Schlüssels',
    checkedAt: (time) => `geprüft ${time}`,
    notChecked: 'Noch nicht geprüft.',
    checkKey: 'API-Schlüssel prüfen',
    checkFailed: 'Der API-Schlüssel konnte nicht geprüft werden.',
    keycheck_ok: 'API-Schlüssel geprüft: Die Verbindung zu Supertext funktioniert.',
    keycheck_invalid:
      `Supertext hat den API-Schlüssel abgelehnt. Prüfen Sie, ob Sie den ganzen Schlüssel kopiert haben, oder erstellen Sie einen neuen unter <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (erfordert die Rolle Admin). ` +
      `Noch kein Supertext-Konto? Erstellen Sie eines auf <a href="${SIGNUP}" target="_blank" rel="noopener">supertext.com</a>.`,
    keycheck_missing:
      `Es ist noch kein API-Schlüssel eingetragen. Noch kein Supertext-Konto? Erstellen Sie eines auf <a href="${SIGNUP}" target="_blank" rel="noopener">supertext.com</a>. ` +
      `Ihren API-Schlüssel erstellen Sie unter <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (erfordert die Rolle Admin).`,
    keycheck_unreachable: 'Der API-Schlüssel konnte nicht geprüft werden, weil Supertext nicht erreichbar ist. Die Einstellungen sind gespeichert; prüfen Sie ihn später erneut.',
    keycheck_error: (status) => `Der API-Schlüssel konnte nicht geprüft werden: Supertext hat mit einem Fehler geantwortet (${status}). Die Einstellungen sind gespeichert; prüfen Sie ihn später erneut.`,
    reports: 'Versionsinformationen an Supertext senden',
    reportsOn: 'Ein',
    reportsOff: 'Aus',
    reportsHelp:
      'Sendet die URL der Website sowie die Versionen dieses Pakets, von Umbraco und der Laufzeitumgebung an Supertext, nach einem Update und wenn sich der API-Schlüssel ändert. So sieht der Supertext-Support, welche Version Sie einsetzen. Es werden keine Inhalte, kein API-Schlüssel und keine Personendaten übertragen.',
    reportsHow: 'Zum Ausschalten setzen Sie Supertext:SendVersionReports in appsettings.json auf false oder die Umgebungsvariable SUPERTEXT_VERSION_REPORTS=0.',
    pingUrl: 'Supertext prüft die Erreichbarkeit der Website unter',
    languagesHeadline: 'Sprachen',
    languagesHint:
      'Jede Umbraco-Sprache braucht eine Supertext-Sprache: ihren ISO-Code, wenn er ein Supertext-Code ist (z. B. de-CH, es), sonst einen Eintrag Supertext:Languages:<culture>:Code in appsettings.json. Sprachen ohne Zuordnung können nicht übersetzt werden.',
    umbracoLanguage: 'Umbraco-Sprache',
    supertextLanguage: 'Supertext-Sprache',
    tone: 'Formell/informell',
    fromMapping: 'aus der Zuordnung',
    exactMatch: 'gleicher Code',
    notMappedLong: (culture) => `Nicht zugeordnet: Supertext:Languages:${culture}:Code ergänzen`,
    formal: 'formell',
    informal: 'informell',
    defaultTone: 'Standard',
    noPoliteness: 'Supertext bietet für diese Sprache keine formelle/informelle Anrede an',
    hiddenInDialog: 'im Dialog ausgeblendet',
  },
};
