// The preview lines: a grid below the name line of each dish card, in place of the game's hint line. The
// wrapper keeps the game's render function and only adds lines.
import { addError } from './core';
import { attachCardTip } from './cardTip';
import type { CookingWindow, PredictionEntry } from './types';

// Builds the aligned grid of the card lines from rows of '|'-split cells. The first cell of a row (the quality
// level) gives the color of the name and is not shown. The percent column aligns right. The stat cells start
// with an icon, so they align left and each icon sits below the icon of the line above.
function buildGrid(doc: Document, rows: string[][]): HTMLDivElement {
  const grid = doc.createElement('div');
  grid.className = 'projectcook-grid';
  grid.style.setProperty('--pc-cols', String(rows[0].length - 1));
  rows.forEach((cells) => {
    cells.slice(1).forEach((text, column) => {
      const cell = doc.createElement('span');
      if (column === 1) cell.className = 'projectcook-cell-num';
      if (column === 0) cell.className = 'projectcook-q-' + cells[0];
      cell.textContent = text;
      grid.appendChild(cell);
    });
  });
  return grid;
}

// cardTipReady: the frame has the parts of the dish card tooltip.
export function installPreview(w: CookingWindow, cardTipReady: boolean): void {
  const original = w.renderPredictionList;
  w.renderPredictionList = function (entries: PredictionEntry[]) {
    original(entries);
    try {
      const cards = w.document.querySelectorAll('#predictionList .pot-card');
      let missingBd = false;
      for (let k = 0; k < cards.length && k < entries.length; k++) {
        const preview = entries[k].Preview;
        if (!preview) continue;
        const hint = cards[k].querySelector('.pot-hint');
        if (hint) hint.classList.add('projectcook-hide');
        const rows = preview.split('\n').map((text) => text.split('|'));
        const grid = buildGrid(w.document, rows);
        const bd = cards[k].querySelector('.pot-bd');
        if (bd) bd.appendChild(grid); else missingBd = true;

        const previewTip = entries[k].PreviewTip;
        if (cardTipReady && previewTip) attachCardTip(w, cards[k], previewTip);
      }
      if (missingBd) addError(w, 'preview: no .pot-bd in a prediction card');
    } catch (e) { addError(w, 'preview: ' + e); }
  };
}
