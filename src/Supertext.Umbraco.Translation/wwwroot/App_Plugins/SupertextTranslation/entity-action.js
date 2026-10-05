// "Translate with Supertext" in the document actions menu (tree "…" and workspace "Actions").
import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { openTranslateDialog } from './open-dialog.js';

export class SupertextTranslateEntityAction extends UmbEntityActionBase {
  async execute() {
    if (!this.args.unique) return;
    await openTranslateDialog(this, this.args.unique);
  }
}

export { SupertextTranslateEntityAction as api };
