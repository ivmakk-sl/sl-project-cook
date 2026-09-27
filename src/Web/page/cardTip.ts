// The dish card tooltip: the lines of the PreviewTip field of an entry, in the game's #recipeTooltip.
import { addError } from './core';
import { tipLine } from './itemTip';
import type { CookingWindow } from './types';

// Builds the grid of the card tooltip: a label column, then one right-aligned column for each quality level.
// A header cell is '<level>:<quality name>' and gets the quality color of that level.
function buildTipGrid(doc: Document, rows: string[][]): HTMLDivElement {
  let cols = 0;
  rows.forEach((cells) => { if (cells.length > cols) cols = cells.length; });
  const grid = doc.createElement('div');
  grid.className = 'projectcook-tip-grid';
  grid.style.setProperty('--pc-cols', String(cols));
  rows.forEach((cells) => {
    for (let c = 0; c < cols; c++) {
      const cell = doc.createElement('span');
      let text = cells[c] || '';
      const name = text.match(/^(\d):(.*)$/);
      if (c >= 1) cell.classList.add('projectcook-cell-num');
      if (name) { text = name[2]; cell.classList.add('projectcook-q-' + name[1]); }
      cell.textContent = text;
      grid.appendChild(cell);
    }
  });
  return grid;
}

// Fills #recipeTooltip with the tip lines in their order (grid rows that follow each other make one grid), and shows,
// moves, and hides it the same way the game does its own recipe tooltip.
export function attachCardTip(w: CookingWindow, card: Element, tipText: string): void {
  const tip = w.document.getElementById('recipeTooltip')!;
  card.addEventListener('mouseenter', (e) => {
    try {
      tip.textContent = '';
      const lines = tipText.split('\n');
      let rows: string[][] = [];
      const flush = () => { if (rows.length) { tip.appendChild(buildTipGrid(w.document, rows)); rows = []; } };
      for (const line of lines) {
        if (line.indexOf('|') >= 0 && !/^T\d\|/.test(line)) rows.push(line.split('|'));
        else { flush(); tip.appendChild(tipLine(w.document, line, true)); }
      }
      flush();
      tip.style.display = 'block';
      w.moveTooltip(e as MouseEvent);
    } catch (e2) { addError(w, 'cardTip: ' + e2); }
  });
  card.addEventListener('mousemove', (e) => { try { w.moveTooltip(e as MouseEvent); } catch (e2) { addError(w, 'cardTip: ' + e2); } });
  card.addEventListener('mouseleave', () => { tip.style.display = 'none'; });
}
