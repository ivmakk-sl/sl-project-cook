// The portion switch after the "THIS POT" title: the dish cards show the stats of the whole dish or of one portion.
// The game writes the title with textContent in its language message (at each window open, and on a language change),
// after its render of the list, which takes the switch away. So an observer of the title draws the switch again from
// the last call. The choice lives in the root page (view.portion).
import { view } from './core';
import type { CookingWindow } from './types';

const CLS = 'projectcook-portion';

export function portionSwitchOn(): boolean {
  return !!view.data.features && view.data.features.indexOf('portionSwitch') >= 0 && !!view.data.portion;
}

// Shows the switch in the title when show is true, else removes it. redraw draws the list again after a choice.
export function drawPortionSwitch(w: CookingWindow, show: boolean, redraw: () => void): void {
  const doc = w.document;
  const title = doc.getElementById('potSectionTitle');
  if (!title) return;
  if (!w.__projectCookPortion) observeTitle(w, title);
  w.__projectCookPortion = { show, redraw };
  let box = title.querySelector('.' + CLS) as (HTMLElement & { __redraw?: () => void }) | null;
  if (!show) {
    if (box) box.remove();
    return;
  }
  const labels = view.data.portion!;
  if (!box) {
    box = doc.createElement('span');
    box.className = CLS;
    title.appendChild(box);
  }
  // The handlers of the choices call the redraw of the last render, which has the current list.
  box.__redraw = redraw;
  const current = box;
  const opts = box.querySelectorAll('.' + CLS + '-opt');
  for (let i = 0; i < labels.length; i++) {
    let opt = opts[i] as HTMLElement | undefined;
    if (!opt) {
      opt = doc.createElement('span');
      opt.className = CLS + '-opt';
      const perPortion = i === 1;
      opt.addEventListener('click', (e) => {
        e.stopPropagation();
        if (view.portion === perPortion) return;
        view.portion = perPortion;
        if (current.__redraw) current.__redraw();
      });
      box.appendChild(opt);
    }
    if (opt.textContent !== labels[i]) opt.textContent = labels[i];
    opt.classList.toggle(CLS + '-on', view.portion === (i === 1));
  }
}

// One observer for each frame (a new open is a new frame). The add of the switch makes one more record, which finds
// the switch there, so the observer does not loop.
function observeTitle(w: CookingWindow, title: HTMLElement): void {
  new w.MutationObserver(() => {
    const last = w.__projectCookPortion;
    if (last && last.show && portionSwitchOn() && !title.querySelector('.' + CLS)) drawPortionSwitch(w, true, last.redraw);
  }).observe(title, { childList: true });
}
