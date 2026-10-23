// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
// The drop margin of the icon tabs: an icon tab is small, and the dragged item covers it, so a drop on it is hard to aim.
// The game's tab drop (webui-tabdrop.js createTabDrop) takes the rectangle of each tab and of the tab strip around it
// (which clips the tab rectangles) from getBoundingClientRect when the drag starts, and drops into the tab whose
// rectangle holds the cursor. During a drag, the rectangles of the tabs and of the containers of the tab row grow by
// the margin: up, down (to the top of the element below the row at most, so a drop into its first row stays a drop
// into it), and to each side (half the gap between two tabs, so no two tabs share a point). With no drag, each
// rectangle is the element's own, for the layout code of the page.
import type { IconTabRow } from './iconTabs';

export interface DropMargin {
  up: number;
  down: number;
  side: number;
  // The selector of the dragged item while a drag runs (the game moves it to the body before it arms the tab drop).
  dragging: string;
  // The selector of the element below the tab row: the margin below the tabs stops at its top.
  below?: string;
}

type Grown = HTMLElement & { __itabDropMargin?: boolean };

function grow(doc: Document, el: Grown, m: DropMargin, side: number): void {
  if (el.__itabDropMargin) return;
  el.__itabDropMargin = true;
  const view = doc.defaultView as Window & typeof globalThis;
  const own = (e: Element): DOMRect => view.Element.prototype.getBoundingClientRect.call(e);
  el.getBoundingClientRect = function (this: Element): DOMRect {
    const r = own(this);
    if (!doc.querySelector(m.dragging)) return r;
    const below = m.below ? doc.querySelector(m.below) : null;
    const down = below ? Math.max(0, Math.min(m.down, own(below).top - r.bottom)) : m.down;
    const left = r.left - side, top = r.top - m.up, right = r.right + side, bottom = r.bottom + down;
    return { left, top, right, bottom, x: left, y: top, width: right - left, height: bottom - top, toJSON() {} } as DOMRect;
  };
}

// Gives the margin to the containers of the row and to each tab in them; a tab that the page builds again gets it at
// the next call.
export function installDropMargin(doc: Document, row: IconTabRow, tabs: HTMLElement[], m: DropMargin): void {
  for (const sel of row.rows) {
    const box = doc.querySelector<HTMLElement>(sel);
    if (box) grow(doc, box, m, 0);
  }
  for (const tab of tabs) grow(doc, tab, m, m.side);
}
