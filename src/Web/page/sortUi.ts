// The parts of the food sort that both windows draw: the dropdown on the line of the game's Auto Organize button,
// and the badge and the dim class of each cell.
import { CHOICE, badgeText, badgeTone } from './sortOrder';
import type { SortData } from './types';
import defaultIcon from './icons/default.svg?raw';
import expirationIcon from './icons/expiration.svg?raw';
import sortIcon from './icons/sort.svg?raw';
import tradeIcon from './icons/trade.svg?raw';

// The icon of each choice, and 'sort' for the button with Default. The stats have the game's own icons: the glyphs
// of its item tooltip (ItemDetailPopup) and of the stat cells of the dish cards. The other choices have drawn
// stroke icons (icons/*.svg), whose color comes from the CSS (data-choice), through currentColor.
const ICON_GLYPHS: Record<string, string> = { 1: '🍖', 2: '🧠', 3: '⚡', 4: '❤️' };
// trim: a file ends with a line break, which would be a text node of the button.
const ICON_SVGS: Record<string, string> = {
  sort: sortIcon.trim(), 0: defaultIcon.trim(), 5: tradeIcon.trim(), 6: expirationIcon.trim(),
};

// Writes the icon and the word of a button or an option, only where they differ.
function drawLabel(doc: Document, el: HTMLElement, icon: string, text: string): void {
  let ico = el.querySelector(':scope > .projectcook-sort-ico') as HTMLElement | null;
  let label = el.querySelector(':scope > .projectcook-sort-text') as HTMLElement | null;
  if (!ico || !label) {
    el.textContent = '';
    ico = doc.createElement('span');
    ico.className = 'projectcook-sort-ico';
    label = doc.createElement('span');
    label.className = 'projectcook-sort-text';
    el.appendChild(ico);
    el.appendChild(label);
  }
  if (ico.dataset.choice !== icon) {
    ico.dataset.choice = icon;
    if (ICON_GLYPHS[icon]) ico.textContent = ICON_GLYPHS[icon];
    else ico.innerHTML = ICON_SVGS[icon] || '';
  }
  if (label.textContent !== text) label.textContent = text;
}

function button(doc: Document, cls: string): HTMLButtonElement {
  const b = doc.createElement('button');
  b.type = 'button';
  b.className = cls + ' unity-interactive';
  b.setAttribute('data-interactive', '');
  return b;
}

// The first game button of a toolbar: in the storage window an .action-btn (Discard All on the Backpack side, else
// Auto Organize), in the cooking window .bag-sort-btn (Auto Organize).
function gameButton(toolbar: Element): Element | null {
  return toolbar.querySelector(':scope > .action-btn, :scope > .bag-sort-btn');
}

// Draws the dropdown at the left of the first game button, on its line, only where it differs. A toolbar
// with the robot switch (has-rsw) wraps, so a full-width break keeps the switch on its own line and puts the
// dropdown on the line of the button. select runs when the player picks a choice.
export function drawDropdown(doc: Document, toolbar: Element, sort: SortData, choice: number, select: (choice: number) => void): void {
  let root = toolbar.querySelector(':scope > .projectcook-sort') as HTMLElement | null;
  if (!root) {
    root = doc.createElement('div');
    root.className = 'projectcook-sort';
    const btn = button(doc, 'projectcook-sort-btn');
    const menu = doc.createElement('div');
    menu.className = 'projectcook-sort-menu';
    menu.hidden = true;
    for (let i = 0; i < sort.words.choices.length; i++) {
      const opt = button(doc, 'projectcook-sort-opt');
      opt.dataset.choice = String(i);
      opt.addEventListener('click', (e) => {
        e.stopPropagation();
        menu.hidden = true;
        select(i);
      });
      menu.appendChild(opt);
    }
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      menu.hidden = !menu.hidden;
    });
    root.appendChild(btn);
    root.appendChild(menu);
  }
  const anchor = gameButton(toolbar);
  if (anchor ? root.nextElementSibling !== anchor : toolbar.firstElementChild !== root)
    toolbar.insertBefore(root, anchor || toolbar.firstChild);
  let brk = toolbar.querySelector(':scope > .projectcook-sort-break');
  if (toolbar.classList.contains('has-rsw')) {
    if (!brk) {
      brk = doc.createElement('div');
      brk.className = 'projectcook-sort-break';
    }
    if (root.previousElementSibling !== brk) toolbar.insertBefore(brk, root);
  } else if (brk) {
    brk.remove();
  }
  // With Default the button reads Sort with the sort icon; with a sort on, the word and the icon of the choice in
  // the active look, so the player sees that the grid is not in its real order.
  const active = choice !== CHOICE.default && !!sort.words.choices[choice];
  const btn = root.querySelector('.projectcook-sort-btn') as HTMLElement;
  drawLabel(doc, btn, active ? String(choice) : 'sort', (active ? sort.words.choices[choice] : sort.words.sort || sort.words.choices[0]) + ' ▾');
  if (btn.classList.contains('projectcook-sort-active') !== active) btn.classList.toggle('projectcook-sort-active', active);
  root.querySelectorAll('.projectcook-sort-opt').forEach((el) => {
    const opt = el as HTMLElement;
    drawLabel(doc, opt, opt.dataset.choice || '0', sort.words.choices[Number(opt.dataset.choice)] || '');
    opt.classList.toggle('projectcook-sort-on', Number(opt.dataset.choice) === choice);
  });
}

export function removeDropdown(toolbar: Element | null): void {
  if (!toolbar) return;
  toolbar.querySelectorAll(':scope > .projectcook-sort, :scope > .projectcook-sort-break').forEach((el) => el.remove());
}

// Writes the badge and the dim class of each cell, only where they differ. ids[i] is the item of cells[i]. With no
// sort data or Default, the cells get no badge and no dim class. With a layer, the badges go into the layer at the
// places of their cells (the storage window draws the frost above the cells, and the layer is above the frost);
// else each badge is the last child of its cell.
export function drawCells(doc: Document, cells: ArrayLike<Element>, ids: (number | string)[], sort: SortData | null, choice: number, dim: Set<number | string>, layer?: HTMLElement): void {
  const on = !!sort && choice !== CHOICE.default;
  const kept = new Set<string>();
  for (let i = 0; i < cells.length; i++) {
    const cell = cells[i] as HTMLElement;
    const id = String(ids[i]);
    const item = on ? sort!.items[id] : undefined;
    const text = on ? badgeText(item, choice) : null;
    let badge = (layer
      ? layer.querySelector(':scope > .projectcook-sort-badge[data-id="' + id + '"]')
      : cell.querySelector(':scope > .projectcook-sort-badge')) as HTMLElement | null;
    if (text === null) {
      if (badge) badge.remove();
    } else {
      if (!badge) {
        badge = doc.createElement('span');
        badge.className = 'projectcook-sort-badge';
        badge.dataset.id = id;
      }
      if (badge.textContent !== text) badge.textContent = text;
      const tone = badgeTone(item, choice);
      for (const t of ['pos', 'neg', 'gold']) {
        const has = tone === t;
        if (badge.classList.contains('projectcook-sort-' + t) !== has) badge.classList.toggle('projectcook-sort-' + t, has);
      }
      if (layer) {
        if (badge.style.left !== cell.style.left) badge.style.left = cell.style.left;
        if (badge.style.top !== cell.style.top) badge.style.top = cell.style.top;
        if (badge.parentElement !== layer) layer.appendChild(badge);
        kept.add(id);
      } else if (cell.lastElementChild !== badge) {
        cell.appendChild(badge);
      }
    }
    const dimmed = on && dim.has(ids[i]);
    if (cell.classList.contains('projectcook-sort-dim') !== dimmed) cell.classList.toggle('projectcook-sort-dim', dimmed);
  }
  if (layer) {
    layer.querySelectorAll(':scope > .projectcook-sort-badge').forEach((el) => {
      if (!kept.has((el as HTMLElement).dataset.id || '')) el.remove();
    });
  }
}
