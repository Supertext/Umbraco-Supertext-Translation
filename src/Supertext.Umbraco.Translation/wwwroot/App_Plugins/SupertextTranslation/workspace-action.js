// "Translate with Supertext" button next to Save / Publish in the document workspace.
import { UmbWorkspaceActionBase } from '@umbraco-cms/backoffice/workspace';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { openTranslateDialog } from './open-dialog.js';

export class SupertextTranslateWorkspaceAction extends UmbWorkspaceActionBase {
  async execute() {
    const workspace = await this.getContext(UMB_DOCUMENT_WORKSPACE_CONTEXT);
    const unique = workspace?.getUnique();
    if (!unique) return;
    const result = await openTranslateDialog(this, unique);
    if (result?.translated > 0) {
      await workspace.reload?.(); // show the new language versions
    }
  }
}

export { SupertextTranslateWorkspaceAction as api };
