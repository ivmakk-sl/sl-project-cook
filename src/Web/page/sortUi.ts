// The parts of the food sort that both windows draw: the dropdown on the line of the game's Auto Organize button,
// and the badge and the dim class of each cell. The dropdown and the number badge library draw them; this file
// gives them the words, the icons, and the numbers of the sort data.
import { CHOICE, badgeText, badgeTone } from './sortOrder';
import type { SortData } from './types';
import { drawDropdown as drawLibDropdown, removeDropdown as removeLibDropdown, type DropdownIcon } from '../../Shared/dropdown/web/dropdown';
import { drawBadges, type BadgeLook } from '../../Shared/number-badge/web/numberBadge';
import defaultIcon from '../../Shared/icons/web/default.svg?raw';
import sortIcon from '../../Shared/icons/web/sort.svg?raw';
import tradeIcon from '../../Shared/icons/web/trade.svg?raw';
import expirationIcon from './icons/expiration.svg?raw';

export const PREFIX = 'projectcook-sort';

// The icon of each choice, and 'sort' for the button with Default. The stats have the game's own icons: the glyphs
// of its item tooltip (ItemDetailPopup) and of the stat cells of the dish cards. The other choices have drawn
// stroke icons (*.svg), whose color comes from the CSS (data-choice), through currentColor.
// trim: a file ends with a line break, which would be a text node of the button.
const ICONS: Record<string, string> = {
  1: '🍖', 2: '🧠', 3: '⚡', 4: '❤️',
  sort: sortIcon.trim(), 0: defaultIcon.trim(), 5: tradeIcon.trim(), 6: expirationIcon.trim(),
};

function icon(key: string): DropdownIcon {
  return { key, html: ICONS[key] || '' };
}

// The first game button of a toolbar: in the storage window an .action-btn (Discard All on the Backpack side, else
// Auto Organize), in the cooking window .bag-sort-btn (Auto Organize).
const ANCHOR = '.action-btn, .bag-sort-btn';

// Draws the dropdown at the left of the first game button, on its line. select runs when the player picks a choice.
// With Default the button reads Sort with the sort icon; with a sort on, the word and the icon of the choice in the
// active look, so the player sees that the grid is not in its real order.
export function drawDropdown(doc: Document, toolbar: Element, sort: SortData, choice: number, select: (choice: number) => void): void {
  const active = choice !== CHOICE.default && !!sort.words.choices[choice];
  drawLibDropdown(doc, toolbar, {
    prefix: PREFIX,
    anchor: ANCHOR,
    button: active
      ? { icon: icon(String(choice)), text: sort.words.choices[choice] + ' ▾' }
      : { icon: icon('sort'), text: (sort.words.sort || sort.words.choices[0]) + ' ▾' },
    active,
    choices: sort.words.choices.map((text, i) => ({ icon: icon(String(i)), text })),
    current: choice,
    select,
  });
}

export function removeDropdown(toolbar: Element | null): void {
  removeLibDropdown(toolbar, PREFIX);
}

// Writes the badge and the dim class of each cell. ids[i] is the item of cells[i]. With no sort data or Default, the
// cells get no badge and no dim class. An item in always is dimmed also then (an uncookable item). With a layer, the badges go into the layer at the places of their cells (the
// storage window draws the frost above the cells, and the layer is above the frost); else each badge is the last
// child of its cell.
export function drawCells(doc: Document, cells: ArrayLike<Element>, ids: (number | string)[], sort: SortData | null, choice: number, dim: Set<number | string>, layer?: HTMLElement, always?: Set<number | string>): void {
  const on = !!sort && choice !== CHOICE.default;
  const looks: (BadgeLook | null)[] = [];
  for (let i = 0; i < cells.length; i++) {
    const kept = !!always && always.has(ids[i]);
    if (!on) {
      looks.push(kept ? { text: null, dim: true, id: String(ids[i]) } : null);
      continue;
    }
    const item = sort!.items[String(ids[i])];
    looks.push({ text: badgeText(item, choice), tone: badgeTone(item, choice), dim: kept || dim.has(ids[i]), id: String(ids[i]) });
  }
  drawBadges(doc, PREFIX, cells, looks, layer);
}
