// The row split toggle at the end of the line above the grid of the cooking station (#potCapacityBar): a badge of the
// game with a label and a slide switch. C# holds the state and sends it in the page data. A click tells C#, and the next data send draws the new
// state. The game changes only the badges of the bar, so the toggle stays through a render of the game. During a cook
// the game's overlay covers the toggle.
import { view } from './core';
import type { CookingWindow, RowToggle } from './types';

const CLS = 'projectcook-row-toggle';
// The class of the capacity badges of Cooking.html: the toggle takes their box and their label, so it follows the game.
const GAME_BADGE = 'capacity-badge';
const ROW_SPLIT_MESSAGE = 'slmod|projectcook|rowsplit';

// The selector of the page.css rule that hides the toggle in hot pot mode, where no refresh reaches C#: the game sets
// the class active on #hotPotPanel by itself.
export const ROW_TOGGLE_HIDDEN_IN_HOT_POT = 'html:has(#hotPotPanel.active) .' + CLS;

function current(): RowToggle | null {
  const data = view.data.rowToggle;
  const on = !!view.data.features && view.data.features.indexOf('rowSplitToggle') >= 0;
  return on && data && data.show ? data : null;
}

function create(doc: Document): HTMLElement {
  const box = doc.createElement('span');
  box.className = CLS + ' ' + GAME_BADGE;
  const label = doc.createElement('span');
  label.className = CLS + '-label ' + GAME_BADGE + '-label';
  const track = doc.createElement('span');
  track.className = CLS + '-switch';
  box.append(label, track);
  box.addEventListener('click', (e) => {
    e.stopPropagation();
    window.vuplex?.postMessage(ROW_SPLIT_MESSAGE);
  });
  return box;
}

// Draws the toggle from the page data, or removes it.
export function drawRowToggle(w: CookingWindow): void {
  const bar = w.document.getElementById('potCapacityBar');
  if (!bar) return;
  let box = bar.querySelector('.' + CLS) as HTMLElement | null;
  const data = current();
  if (!data) {
    if (box) box.remove();
    return;
  }
  if (!box) {
    box = create(w.document);
    bar.appendChild(box);
  }
  const label = box.querySelector('.' + CLS + '-label') as HTMLElement;
  if (label.textContent !== data.label) label.textContent = data.label;
  box.classList.toggle(CLS + '-is-on', data.on);
}
