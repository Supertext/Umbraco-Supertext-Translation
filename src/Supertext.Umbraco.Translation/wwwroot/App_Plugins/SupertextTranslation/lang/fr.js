// French UI strings of the Supertext backoffice extension (keys: see en.js).
//   = non-breaking space before ? ! : ; and inside « ».
const SIGNUP = 'https://www.supertext.com/person/en/account/signin';
const API_KEY = 'https://www.supertext.com/en/integrations/api';

export default {
  supertext: {
    translateWithSupertext: 'Traduire avec Supertext',
    translateFrom: 'Traduire depuis',
    into: 'Traduire vers',
    sourceLanguage: 'Langue source',
    translate: 'Traduire',
    replaceAndTranslate: 'Remplacer et traduire',
    hasContent: 'a du contenu',
    translated: 'traduit',
    failed: 'échec',
    hint: 'La page est traduite telle qu’elle est sauvegardée : sauvegardez d’abord vos modifications. Les traductions sont sauvegardées comme brouillons, que vous pouvez relire puis publier.',
    overwriteWarning: (languages, count) => `${languages} ${count === 1 ? 'a' : 'ont'} déjà du contenu.`,
    overwriteExplanation: (source) => `La traduction y remplace le brouillon actuel par une nouvelle traduction de la version ${source}.`,
    noApiKeyHtml:
      'Aucune clé API Supertext n’est configurée. Demandez à votre administrateur de définir <code>SUPERTEXT_API_KEY</code>. ' +
      `Pas encore de compte Supertext ? <a href="${SIGNUP}" target="_blank" rel="noopener">Créez-en un sur supertext.com</a>. ` +
      `Générez votre clé API sur <a href="${API_KEY}" target="_blank" rel="noopener">supertext.com → Integrations → API</a> (rôle Admin requis).`,
    loadLanguagesFailed: 'Impossible de charger les langues de ce document.',
    startFailed: 'La traduction n’a pas pu être lancée.',
    translatedHeadline: 'Traduit avec Supertext',
    translatedMessage: (languages) => `${languages} : sauvegardé comme brouillon. Relisez puis publiez lorsque c’est prêt.`,
    failedHeadline: 'Échec de la traduction Supertext',

    error_chooseLanguages: 'Choisissez une langue source et au moins une langue cible.',
    error_documentNotFound: 'Document introuvable.',
    error_invariantDocumentType: (name) =>
      `Le type de document « ${name} » ne varie pas selon la culture : il n’a donc pas de versions linguistiques distinctes vers lesquelles traduire.`,
    error_noSourceVersion: (culture) => `Le document n’a pas de version ${culture} à partir de laquelle traduire.`,
    error_saveFailed: 'Umbraco n’a pas pu sauvegarder le document traduit.',
    error_noApiKey:
      'Aucune clé API Supertext n’est configurée (Supertext:ApiKey ou SUPERTEXT_API_KEY). ' +
      `Pas encore de compte Supertext ? Créez-en un sur ${SIGNUP}. ` +
      `Générez votre clé API sur ${API_KEY} (supertext.com → Integrations → API, rôle Admin requis).`,
    error_authFailed:
      'Échec de l’authentification. Veuillez vérifier la clé API Supertext. ' +
      `Pas encore de compte Supertext ? Créez-en un sur ${SIGNUP}. ` +
      `Générez votre clé API sur ${API_KEY} (supertext.com → Integrations → API, rôle Admin requis).`,
    error_unreachable: 'Impossible de joindre Supertext.',
    error_notFound: 'La ressource Supertext demandée est introuvable.',
    error_tooLarge: 'Le contenu est trop volumineux pour être traduit par Supertext en une seule fois.',
    error_rateLimited: 'Trop de requêtes envoyées à Supertext. Veuillez réessayer dans un instant.',
    error_unavailable: 'Le service Supertext est actuellement indisponible.',
    error_http: (status) => `Supertext a répondu avec le code HTTP ${status}.`,
    error_noFileId: 'Supertext n’a pas renvoyé d’identifiant de fichier.',
    error_translationFailed: 'Supertext n’a pas pu traduire le document.',
    error_limitExceeded: 'Votre limite de traduction Supertext est dépassée.',
    error_fileDeleted: 'Le fichier Supertext a été supprimé avant de pouvoir être téléchargé.',
    error_timeout: 'Délai dépassé en attendant la traduction Supertext.',
    error_emptyTranslation: 'Le document traduit était vide.',
  },
};
