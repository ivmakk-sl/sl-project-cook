// The icon tabs of the food windows (the icon tabs library): C# sends the tab list of the newest open of a food window
// (setIconTabs), the root page keeps it, and each pass applies it to the frame of that page. The frame of an open is a
// new frame, so its watch of the tab row and its tooltip are installed once for each frame. A mismatch of the tab row
// (a game update changed the page) goes to C# in the result of the next pass, once for each page.
import { watchIconTabs } from '../../Shared/icon-tabs/web/iconTabs';
import type { IconTabRow, IconTabsWatch } from '../../Shared/icon-tabs/web/iconTabs';
import { installTabTooltip } from '../../Shared/icon-tabs/web/tabTooltip';
import { ICON_TABS_PREFIX, view } from './core';
import type { IconTabList } from './types';

// The tab row of each page of a food window: its containers in order, and the tab element in them. The cooking page
// takes a dragged item on a tab (the other pages do not), so its tabs get the drop margin: 16 px up and down (down to
// the bag grid at most), and half the 4 px gap between two tabs to each side.
export const ROWS: Record<string, IconTabRow> = {
  Cooking: {
    rows: ['#bagTabPinned', '#bagTabsScroll'],
    tab: '.bag-tab',
    drop: { up: 16, down: 16, side: 2, dragging: 'body > .item', below: '#backpackGrid' },
  },
  BrewPanel: { rows: ['#bagTabPinned', '#bagTabsScroll'], tab: '.bag-tab' },
  RatCage: { rows: ['#bagTabPinned', '#bagTabsScroll'], tab: '.bag-tab' },
  Compressor: { rows: ['#tabs'], tab: '.left-tab' },
};

type IconTabsWindow = Window & { __projectCookIconTabs?: IconTabsWatch };

// The pages with a mismatch that the result of a pass has not told C# yet, and those that it told.
const mismatched = new Set<string>();
const told = new Set<string>();
// The open id of the list that a frame of each page took first: a frame that loads later and finds the same list is
// the frame of a new open whose list has not come yet, so it waits for it (its tabs can differ from the old list).
const usedOpen: Record<string, number> = {};

export function setIconTabList(list: IconTabList): void {
  view.iconTabs = list;
}

// Applies the list of the newest open to the frame of page, when the list is of that page. atLoad: the call at the
// load of the frame.
export function runIconTabs(w: Window, page: string, atLoad = false): void {
  const list = view.iconTabs;
  const row = ROWS[page];
  if (!list || list.page !== page || !row) return;
  const iw = w as IconTabsWindow;
  if (!iw.__projectCookIconTabs) {
    if (atLoad && usedOpen[page] === list.open) return;
    usedOpen[page] = list.open;
    iw.__projectCookIconTabs = watchIconTabs(w.document, row, ICON_TABS_PREFIX, () => {
      if (!told.has(page)) mismatched.add(page);
    });
    installTabTooltip(w.document, row, ICON_TABS_PREFIX);
  }
  iw.__projectCookIconTabs.set(list.open, list.tabs);
}

// The part of the result of a pass for the mismatches that C# has not seen: "icontabs: mismatch <page>,<page>", or ''.
export function iconTabsResult(): string {
  if (!mismatched.size) return '';
  const pages = [...mismatched];
  for (const p of pages) told.add(p);
  mismatched.clear();
  return 'icontabs: mismatch ' + pages.join(',');
}
