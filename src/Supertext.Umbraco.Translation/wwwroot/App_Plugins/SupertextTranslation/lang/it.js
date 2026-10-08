// Italian UI strings of the Supertext backoffice extension (keys: see en.js).
const SIGNUP = 'https://www.supertext.com/person/en/account/signin';
const API_KEY = 'https://www.supertext.com/en/integrations/api';

export default {
  supertext: {
    translateWithSupertext: 'Traduci con Supertext',
    translateFrom: 'Traduci da',
    into: 'Traduci in',
    sourceLanguage: 'Lingua di origine',
    translate: 'Traduci',
    replaceAndTranslate: 'Sostituisci e traduci',
    hasContent: 'ha contenuto',
    translated: 'tradotto',
    failed: 'non riuscito',
    hint: 'La pagina viene tradotta così come è salvata: salvi prima le Sue modifiche. Le traduzioni vengono salvate come bozze, che potrà rivedere e pubblicare.',
    overwriteWarning: (languages, count) => `${languages} ${count === 1 ? 'ha' : 'hanno'} già contenuto.`,
    overwriteExplanation: (source) => `La traduzione sostituisce la bozza attuale con una nuova traduzione della versione ${source}.`,
    noApiKeyHtml:
      'Non è configurata alcuna chiave API Supertext. Chieda al Suo amministratore di impostare <code>SUPERTEXT_API_KEY</code>. ' +
      `Non ha ancora un account Supertext? <a href="${SIGNUP}" target="_blank" rel="noopener">Ne crei uno su supertext.com</a>. ` +
      `Generi la Sua chiave API su <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (richiede il ruolo Admin).`,
    loadLanguagesFailed: 'Impossibile caricare le lingue di questo documento.',
    startFailed: 'Non è stato possibile avviare la traduzione.',
    translatedHeadline: 'Tradotto con Supertext',
    translatedMessage: (languages) => `${languages}: salvato come bozza. Lo riveda e lo pubblichi quando è pronto.`,
    failedHeadline: 'Traduzione Supertext non riuscita',

    error_chooseLanguages: 'Scelga una lingua di origine e almeno una lingua di destinazione.',
    error_documentNotFound: 'Documento non trovato.',
    error_invariantDocumentType: (name) =>
      `Il tipo di documento «${name}» non varia in base alla lingua, quindi non ha versioni linguistiche separate in cui tradurre.`,
    error_noSourceVersion: (culture) => `Il documento non ha una versione ${culture} da cui tradurre.`,
    error_saveFailed: 'Umbraco non è riuscito a salvare il documento tradotto.',
    error_noApiKey:
      'Non è configurata alcuna chiave API Supertext (Supertext:ApiKey o SUPERTEXT_API_KEY). ' +
      `Non ha ancora un account Supertext? Ne crei uno su ${SIGNUP}. ` +
      `Generi la Sua chiave API su ${API_KEY} (supertext.com → Integrations → API, richiede il ruolo Admin).`,
    error_authFailed:
      'Autenticazione non riuscita. Verifichi la chiave API Supertext. ' +
      `Non ha ancora un account Supertext? Ne crei uno su ${SIGNUP}. ` +
      `Generi la Sua chiave API su ${API_KEY} (supertext.com → Integrations → API, richiede il ruolo Admin).`,
    error_unreachable: 'Impossibile raggiungere Supertext.',
    error_notFound: 'La risorsa Supertext richiesta non è stata trovata.',
    error_tooLarge: 'Il contenuto è troppo grande per essere tradotto da Supertext in una sola volta.',
    error_rateLimited: 'Troppe richieste a Supertext. Riprovi tra poco.',
    error_unavailable: 'Il servizio Supertext non è al momento disponibile.',
    error_http: (status) => `Supertext ha risposto con HTTP ${status}.`,
    error_noFileId: 'Supertext non ha restituito un ID file.',
    error_translationFailed: 'Supertext non è riuscito a tradurre il documento.',
    error_limitExceeded: 'Il Suo limite di traduzione Supertext è stato superato.',
    error_fileDeleted: 'Il file Supertext è stato eliminato prima di poter essere scaricato.',
    error_timeout: 'Tempo scaduto in attesa della traduzione Supertext.',
    error_emptyTranslation: 'Il documento tradotto era vuoto.',
  },
};
