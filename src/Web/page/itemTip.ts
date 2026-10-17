// The ingredient tooltip: the lines of setData below the game's own lines of the item tooltip.
//
// The lines go through the shared tooltip lines library, which owns the wrap of the tooltip part of the page, the
// place of the block, and the correction for a tooltip that grew. The mod gives its own nodes and not plain text,
// because a line can hold the tier name in the color of its tier and a value in the color of its sign.
import { addTipLines } from '../../Shared/tooltip-lines/web/tooltipLines';
import type { TipContext } from '../../Shared/tooltip-lines/web/tooltipLines';
import { TIP_PREFIX, view } from './core';
import type { CookingWindow, TipItem } from './types';

const PAGE = 'Cooking';

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

// The nodes of the block for the item under the pointer. The document of the frame is needed to build them, so the
// install closes over it.
export function tipNodes(doc: Document, ctx: TipContext): HTMLDivElement[] {
  const item = ctx.item as TipItem | null;
  if (!item || !item.name || item.canCook === false) return [];
  const tip = view.data && view.data.tips[ctx.configId];
  if (!tip) return [];
  return tip.split('\n').map((text) => tipLine(doc, text));
}

export function installItemTip(w: CookingWindow, root: Window = window): void {
  addTipLines(root, {
    page: PAGE,
    id: 'projectcook-ingredient',
    rank: 10,
    prefix: TIP_PREFIX,
    nodes: (ctx) => tipNodes(w.document, ctx),
  });
}
