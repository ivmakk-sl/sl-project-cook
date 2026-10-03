// The icon of the cooking tag in the storage window. The page keeps its tag icons in a const of setup(), which a
// script cannot reach, and its render calls tagIconSvg of the setup state with the IconKey of a tag (misc icon for
// an unknown key). So the script wraps tagIconSvg of the #app component for the key of the cooking tag.
import { addError } from './core';
import type { StorageWindow } from './types';

// A cooking pot with steam, in the 24 x 24 filled style and the text color of the game's tag icons.
export const COOK_ICON =
  '<svg viewBox="0 0 24 24"><rect x="7.4" y="2.2" width="1.6" height="3.6" rx=".8" opacity=".6"/>' +
  '<rect x="15" y="2.2" width="1.6" height="3.6" rx=".8" opacity=".6"/><rect x="10.6" y="6.4" width="2.8" height="2.2" rx="1"/>' +
  '<rect x="2" y="9.2" width="20" height="2.2" rx="1.1"/><path d="M4.2 12.2h15.6v5.6a3.2 3.2 0 0 1-3.2 3.2H7.4a3.2 3.2 0 0 1-3.2-3.2z"/></svg>';

// The IconKey of the cooking tag (CookingTagLogic.IconKey in C#).
const COOK_KEY = 'cook';

type IconFn = ((icon: string) => string) & { __projectCook?: boolean };
type AppNode = HTMLElement & { _vnode?: { component?: { setupState?: Record<string, unknown> } } };

// Wraps tagIconSvg once. False, with an error for the result of the pass, when the page has no such function.
export function installTagIcon(w: StorageWindow): boolean {
  const app = w.document.getElementById('app') as AppNode | null;
  const state = app && app._vnode && app._vnode.component && app._vnode.component.setupState;
  if (!state) {
    addError(w, 'tagIcon: no Vue component on #app');
    return false;
  }
  const game = state.tagIconSvg as IconFn | undefined;
  if (typeof game !== 'function') {
    addError(w, 'tagIcon: no tagIconSvg in the setup state');
    return false;
  }
  if (game.__projectCook) return true;
  const wrapped: IconFn = (icon: string) => (icon === COOK_KEY ? COOK_ICON : game(icon));
  wrapped.__projectCook = true;
  state.tagIconSvg = wrapped;
  return true;
}
