import { UMB_MODAL_MANAGER_CONTEXT, UmbModalToken } from '@umbraco-cms/backoffice/modal';

export const SUPERTEXT_TRANSLATE_MODAL = new UmbModalToken('Supertext.Modal.Translate', {
  modal: { type: 'sidebar', size: 'small' },
});

/** Opens the dialog for a document; resolves to { translated } or undefined if cancelled. */
export async function openTranslateDialog(host, unique) {
  const modalManager = await host.getContext(UMB_MODAL_MANAGER_CONTEXT);
  return modalManager
    .open(host, SUPERTEXT_TRANSLATE_MODAL, { data: { unique } })
    .onSubmit()
    .catch(() => undefined);
}
