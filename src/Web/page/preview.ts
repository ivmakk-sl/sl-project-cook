// The preview lines: a grid below the name line of each dish card, in place of the game's hint line. The
// wrapper keeps the game's render function and only adds lines.
import { addError, view } from './core';
import { attachCardTip } from './cardTip';
import { FIT_SIZES, fitSize } from './fit';
import { drawPortionSwitch, portionSwitchOn } from './portionSwitch';
import type { CookingWindow, PredictionEntry } from './types';

// Builds the aligned grid of the card lines from rows of '|'-split cells. The first cell of a row (the quality
// level) colors the chance, and the second (the quality name) is not shown but is the label of the chance, so the
// line needs no room for the name; the card tooltip names each level. The chance column aligns right. The stat cells
// start with an icon, so they align left and each icon sits below the icon of the line above.
function buildGrid(doc: Document, rows: string[][]): HTMLDivElement {
  const grid = doc.createElement('div');
  grid.className = 'projectcook-grid';
  grid.style.setProperty('--pc-cols', String(rows[0].length - 2));
  rows.forEach((cells) => {
    cells.slice(2).forEach((text, column) => {
      const cell = doc.createElement('span');
      if (column === 0) {
        cell.className = 'projectcook-cell-num projectcook-q-' + cells[0];
        cell.setAttribute('title', cells[1]);
      }
      cell.textContent = text;
      grid.appendChild(cell);
    });
  });
  return grid;
}

// Sets the largest font size at which the grid fits the text area of the card. A grid that does not fit even at the
// smallest size is cut at the edge of the card, so the card never scrolls. With no layout (a hidden window) the grid
// keeps the normal size.
function fitGrid(grid: HTMLElement, bd: HTMLElement): void {
  const room = bd.clientWidth;
  if (!room) return;
  const r = fitSize(FIT_SIZES, (px) => {
    grid.style.fontSize = px + 'px';
    return grid.offsetWidth <= room;
  });
  grid.style.fontSize = r.size + 'px';
  grid.classList.toggle('projectcook-grid-cut', !r.fits);
}

// cardTipReady: the frame has the parts of the dish card tooltip.
export function installPreview(w: CookingWindow, cardTipReady: boolean): void {
  const original = w.renderPredictionList;
  w.renderPredictionList = function (entries: PredictionEntry[]) {
    original(entries);
    try {
      const cards = w.document.querySelectorAll('#predictionList .pot-card');
      const switchOn = portionSwitchOn();
      let missingBd = false;
      let anyPreview = false;
      for (let k = 0; k < cards.length && k < entries.length; k++) {
        const portion = switchOn && view.portion ? entries[k].PreviewPortion : undefined;
        const preview = portion || entries[k].Preview;
        if (!preview) continue;
        anyPreview = true;
        const hint = cards[k].querySelector('.pot-hint');
        if (hint) hint.classList.add('projectcook-hide');
        const rows = preview.split('\n').map((text) => text.split('|'));
        const grid = buildGrid(w.document, rows);
        const bd = cards[k].querySelector('.pot-bd');
        if (bd) {
          bd.appendChild(grid);
          fitGrid(grid, bd as HTMLElement);
        } else missingBd = true;

        const previewTip = entries[k].PreviewTip;
        if (cardTipReady && previewTip) attachCardTip(w, cards[k], previewTip);
      }
      if (missingBd) addError(w, 'preview: no .pot-bd in a prediction card');
      drawPortionSwitch(w, switchOn && anyPreview, () => w.renderPredictionList(entries));
    } catch (e) { addError(w, 'preview: ' + e); }
  };
}
