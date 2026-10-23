// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
// The tooltip of an icon tab: the full name of the tab and, for a locked tab, the game's lock text below it. One tooltip
// element for each page, appended to the body with position fixed, below the tab and kept inside the window. It takes
// the page zoom from the computed zoom of the body, as the game's own tooltips do, so it needs no function of the page,
// and it takes no pointer, so it blocks no click and no drop. During a drag the same tooltip shows the name of the tab
// under the dragged item, above the tab. Its classes: <prefix>-itab-tip, -itab-tip-name, -itab-tip-lock.
import type { IconTabRow } from './iconTabs';

// The space between the tab and the tooltip, and between the tooltip and the window edge, in layout px.
const GAP = 4;

// The classes of the game's tab drop (webui-tabdrop.js) during a drag: each tab that takes a drop, and the tab under
// the dragged item, which takes it (ok) or not (no).
const ARMED = 'tabdrop-armed';
const UNDER = ['tabdrop-ok', 'tabdrop-no'];

export interface TabTooltip {
  hide(): void;
}

export function installTabTooltip(doc: Document, row: IconTabRow, prefix: string): TabTooltip {
  const p = prefix;
  const existing = doc.querySelector<HTMLElement>('body > .' + p + '-itab-tip');
  const tip = existing ?? doc.createElement('div');
  const hide = (): void => {
    tip.hidden = true;
  };
  if (existing) return { hide };

  tip.className = p + '-itab-tip';
  tip.setAttribute('role', 'tooltip');
  tip.hidden = true;
  doc.body.appendChild(tip);

  const tabOf = (target: EventTarget | null): HTMLElement | null => {
    const el = target instanceof (doc.defaultView as Window & typeof globalThis).Element ? target.closest<HTMLElement>(row.tab) : null;
    return el && el.classList.contains(p + '-itab') ? el : null;
  };

  // below: the hover tooltip; above: the name of the tab under a dragged item, which covers the tab and the space below.
  const show = (tab: HTMLElement, above = false): void => {
    tip.replaceChildren();
    const name = doc.createElement('div');
    name.className = p + '-itab-tip-name';
    name.textContent = tab.getAttribute('data-' + p + '-itab-name') ?? '';
    tip.appendChild(name);
    const lockText = tab.getAttribute('data-' + p + '-itab-lock');
    if (lockText) {
      const lock = doc.createElement('div');
      lock.className = p + '-itab-tip-lock';
      lock.textContent = lockText;
      tip.appendChild(lock);
    }
    tip.hidden = false;
    // The page zoom of the body, as the game's own tooltips read it (webui-core.js): the rectangle is on the screen,
    // the fixed place is in the units of the zoomed body.
    const r = tab.getBoundingClientRect();
    const view = doc.defaultView as Window;
    const zoom = parseFloat(view.getComputedStyle(doc.body).zoom) || 1;
    const maxLeft = doc.documentElement.clientWidth / zoom - tip.offsetWidth - GAP;
    tip.style.left = Math.max(GAP, Math.min(r.left / zoom, maxLeft)) + 'px';
    tip.style.top = (above ? Math.max(GAP, r.top / zoom - GAP - tip.offsetHeight) : r.bottom / zoom + GAP) + 'px';
  };

  // The tab under a dragged item, from the classes of the game's tab drop; its name shows while it is there.
  let dragTab: HTMLElement | null = null;
  const onDrag = (): void => {
    let under: HTMLElement | null = null;
    for (const sel of row.rows)
      for (const tab of doc.querySelectorAll<HTMLElement>(sel + ' > ' + row.tab))
        if (!under && tab.classList.contains(p + '-itab') && UNDER.some((c) => tab.classList.contains(c))) under = tab;
    if (under === dragTab) return;
    dragTab = under;
    if (under) show(under, true);
    else hide();
  };
  const dragging = (): boolean => row.rows.some((sel) => doc.querySelector(sel + ' > .' + ARMED) !== null);

  for (const sel of row.rows) {
    const box = doc.querySelector(sel);
    if (!box) continue;
    box.addEventListener('mouseover', (e) => {
      const tab = tabOf(e.target);
      if (tab && !dragging() && tabOf((e as MouseEvent).relatedTarget) !== tab) show(tab);
    });
    box.addEventListener('mouseout', (e) => {
      const tab = tabOf(e.target);
      // During a drag the classes of the game's tab drop show and hide the name (onDrag): the drop margin keeps a
      // tab under the item after the pointer leaves it.
      if (tab && !dragTab && tabOf((e as MouseEvent).relatedTarget) !== tab) hide();
    });
    box.addEventListener('mousedown', hide);
    box.addEventListener('dragstart', hide);
    new (doc.defaultView as Window & typeof globalThis).MutationObserver(onDrag).observe(box, { subtree: true, attributes: true, attributeFilter: ['class'] });
  }
  return { hide };
}
