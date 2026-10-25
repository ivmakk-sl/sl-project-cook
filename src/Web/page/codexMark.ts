// The codex mark on the dish cards of the cooking window: a trophy coin at the top left corner of the dish icon of each
// card whose entry has the Codex flag (the dish has no codex entry yet). The wrapper keeps the game's render function and
// only adds the mark, so it works also with the dish preview off.
import { CODEX_ICON } from './codexIcon';
import { addError, view } from './core';
import type { CookingWindow, PredictionEntry } from './types';

export function codexMarksOn(): boolean {
  return !!view.data.features && view.data.features.indexOf('codexMarks') >= 0;
}

export function installCodexMark(w: CookingWindow): void {
  const original = w.renderPredictionList;
  w.renderPredictionList = function (entries: PredictionEntry[]) {
    original(entries);
    if (!codexMarksOn()) return;
    try {
      const cards = w.document.querySelectorAll('#predictionList .pot-card');
      for (let k = 0; k < cards.length && k < entries.length; k++) {
        if (!entries[k].Codex) continue;
        const ico = cards[k].querySelector('.pot-ico');
        if (!ico) { addError(w, 'codexMark: no .pot-ico in a prediction card'); continue; }
        if (cards[k].querySelector('.projectcook-codex')) continue;
        // The game's icon box cuts off its content (overflow: hidden for its blur and "?"), so the mark goes next to
        // the icon in a wrapper of both, and hangs out of the icon corner as the codex mark of a cell does.
        const wrap = w.document.createElement('span');
        wrap.className = 'projectcook-codex-wrap';
        ico.parentNode!.insertBefore(wrap, ico);
        wrap.appendChild(ico);
        const mark = w.document.createElement('span');
        mark.className = 'projectcook-codex';
        mark.innerHTML = CODEX_ICON;
        wrap.appendChild(mark);
      }
    } catch (e) { addError(w, 'codexMark: ' + e); }
  };
}
