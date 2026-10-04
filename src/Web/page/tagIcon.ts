// The icon of the cooking tag in the storage window. The page keeps its tag icons in a const of setup(), which a
// script cannot reach, and its render calls tagIconSvg of the setup state with the IconKey of a tag (misc icon for
// an unknown key). So the script gives the icon of the cooking tag to the registry of the mod tags library, which
// wraps tagIconSvg of the #app component once for all mods.
import { registerTagIcon } from '../../Shared/mod-tags/web/registerTagIcon';
import { addError } from './core';
import type { StorageWindow } from './types';

// A cooking pot with steam, in the 24 x 24 filled style and the text color of the game's tag icons.
export const COOK_ICON =
  '<svg viewBox="0 0 24 24"><rect x="7.4" y="2.2" width="1.6" height="3.6" rx=".8" opacity=".6"/>' +
  '<rect x="15" y="2.2" width="1.6" height="3.6" rx=".8" opacity=".6"/><rect x="10.6" y="6.4" width="2.8" height="2.2" rx="1"/>' +
  '<rect x="2" y="9.2" width="20" height="2.2" rx="1.1"/><path d="M4.2 12.2h15.6v5.6a3.2 3.2 0 0 1-3.2 3.2H7.4a3.2 3.2 0 0 1-3.2-3.2z"/></svg>';

// The IconKey of the cooking tag (CookingTagLogic.IconKey in C#).
const COOK_KEY = 'cook';

type AppNode = HTMLElement & { _vnode?: { component?: { setupState?: Record<string, unknown> } } };

// Adds the icon to the registry. False, with an error for the result of the pass, when the page has no such function.
export function installTagIcon(w: StorageWindow): boolean {
  const app = w.document.getElementById('app') as AppNode | null;
  const state = app && app._vnode && app._vnode.component && app._vnode.component.setupState;
  if (!state) {
    addError(w, 'tagIcon: no Vue component on #app');
    return false;
  }
  if (!registerTagIcon(state, COOK_KEY, COOK_ICON)) {
    addError(w, 'tagIcon: no tagIconSvg in the setup state');
    return false;
  }
  return true;
}
