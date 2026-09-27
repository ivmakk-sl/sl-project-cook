// The ingredient tooltip: the lines of setData below the game's own lines of the item tooltip.
import { addError, view } from './core';
import type { CookingWindow, TipItem } from './types';

// One tooltip line. 'T<tier>|<label>|<tier name>' gives the tier name in its tier color, and '<label>: <value>'
// gives a signed value in the value colors of the item window, or in the text color when neutral is set.
export function tipLine(doc: Document, text: string, neutral?: boolean): HTMLDivElement {
  const line = doc.createElement('div');
  const tier = text.match(/^T(\d)\|(.*)\|(.*)$/);
  const parts = text.match(/^(.*: )(.+)$/);
  if (tier) {
    const tierName = doc.createElement('span');
    tierName.textContent = tier[3];
    tierName.className = 'projectcook-tier-' + tier[1];
    line.textContent = tier[2] + ': ';
    line.appendChild(tierName);
  } else if (parts) {
    const value = doc.createElement('span');
    value.textContent = parts[2];
    if (!neutral && parts[2][0] === '-') value.className = 'projectcook-value-neg';
    if (!neutral && parts[2][0] === '+') value.className = 'projectcook-value-pos';
    line.textContent = parts[1];
    line.appendChild(value);
  } else line.textContent = text;
  return line;
}

export function installItemTip(w: CookingWindow): void {
  const originalTip = w.showItemTip;
  w.showItemTip = function (item: TipItem) {
    originalTip(item);
    try {
      const tip = item && item.name && item.canCook !== false && view.data.tips[item.configId];
      if (tip) tip.split('\n').forEach((text) => {
        w.document.getElementById('recipeTooltip')!.appendChild(tipLine(w.document, text));
      });
    } catch (e) { addError(w, 'itemTip: ' + e); }
  };
}
