// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
// The icon tabs of a food window: each container tab of the page shows the icon of its container in place of its name.
// The tab element of the page stays (its click, its classes, its lock badge, a disabled button, the owner that the
// page keeps on it), and the library changes only its content: an icon (an image, the briefcase of the bag, or a short
// text) before the name, and the name text in a hidden span. The full name stays on the tab in data-<prefix>-itab-name,
// and the game's lock text of a locked tab in data-<prefix>-itab-lock, for the tooltip (tabTooltip.ts).
// Each class name comes from the prefix of the mod: <prefix>-itab (the tab), -itab-icon, -itab-text, -itab-name.
import css from './iconTabs.css?inline';
import { briefcase } from './briefcase';
import { installDropMargin } from './dropMargin';
import type { DropMargin } from './dropMargin';

// One tab of the tab list of the window, in the order of the page reducer (sent by the mod from C#).
export interface IconTabEntry {
  // The name that the game gives the tab; empty for a bag tab whose page shows its own bag title.
  name: string;
  // The icon path that the page loads as it is; none for the bag and for a tab with no icon.
  icon?: string;
  // The bag tab: its text when the mod gives one (a short word of the game), else the briefcase.
  bag?: boolean;
  // The short text of a tab with no icon, and of a tab whose icon does not load.
  text: string;
  // The game's lock text of a locked tab, for the tooltip.
  lock?: string;
}

// The tab row of a page: its containers in order (selectors) and the tab element in them, and for a page with a drop
// of a dragged item on a tab, the drop margin of its tabs (dropMargin.ts).
export interface IconTabRow {
  rows: string[];
  tab: string;
  drop?: DropMargin;
}

// 'pending': the page has not built its tab row yet (no tab element), so the list waits for the build.
export type ApplyResult = 'ok' | 'mismatch' | 'pending';

// The CSS of the library with the prefix of the mod. The mod puts it between its tokens and its own rules.
export function iconTabsCss(prefix: string): string {
  return css.replace(/PFX/g, prefix);
}

export function tabsOf(doc: Document, row: IconTabRow): HTMLElement[] {
  return row.rows.flatMap((sel) => [...doc.querySelectorAll<HTMLElement>(sel + ' > ' + row.tab)]);
}

function iconOf(doc: Document, entry: IconTabEntry, p: string): Element {
  if (entry.bag && !entry.text) return briefcase(doc, p + '-itab-icon');
  const text = (): HTMLElement => {
    const span = doc.createElement('span');
    span.className = p + '-itab-text';
    span.textContent = entry.text;
    return span;
  };
  if (!entry.icon) return text();
  const img = doc.createElement('img');
  img.className = p + '-itab-icon';
  img.alt = '';
  img.draggable = false;
  // The page tries the other image types of a /Res/ path and stops each error until the last one fails, then fires
  // res-image-missing (webui-core.js tryNextResImageExt); an error that reaches the image is the end for another path.
  const missing = (): void => {
    if (img.isConnected) img.replaceWith(text());
  };
  img.addEventListener('error', missing);
  img.addEventListener('res-image-missing', missing);
  img.src = entry.icon;
  return img;
}

// The content of one tab: the icon first, then the text nodes of the name in a hidden span.
function applyTab(doc: Document, el: HTMLElement, entry: IconTabEntry, p: string): void {
  const name = doc.createElement('span');
  name.className = p + '-itab-name';
  for (const node of [...el.childNodes]) if (node.nodeType === 3) name.appendChild(node);
  el.insertBefore(name, el.firstChild);
  el.insertBefore(iconOf(doc, entry, p), name);
  el.setAttribute('data-' + p + '-itab-name', entry.name || name.textContent || '');
  if (entry.lock && el.classList.contains('locked')) el.setAttribute('data-' + p + '-itab-lock', entry.lock);
  el.classList.add(p + '-itab');
}

// The name of a tab: the full name of an icon tab, else the own text of the element (a lock badge or a dot of the page
// is a child element with no text).
function nameOf(el: HTMLElement, p: string): string {
  const kept = el.getAttribute('data-' + p + '-itab-name');
  if (kept !== null) return kept;
  return [...el.childNodes].filter((n) => n.nodeType === 3).map((n) => n.textContent).join('');
}

// Applies the entries to the tabs of the row when the row matches them: the same count, and the same name on each tab
// after the bag (the bag tab can show a bag title of the page in place of its name). On a mismatch no tab changes.
export function applyIconTabs(doc: Document, row: IconTabRow, entries: IconTabEntry[], prefix: string): ApplyResult {
  const tabs = tabsOf(doc, row);
  if (tabs.length === 0) return 'pending';
  if (tabs.length !== entries.length) return 'mismatch';
  for (let i = 1; i < tabs.length; i++) if (nameOf(tabs[i], prefix) !== entries[i].name) return 'mismatch';
  let changed = false;
  tabs.forEach((el, i) => {
    if (el.classList.contains(prefix + '-itab')) return;
    applyTab(doc, el, entries[i], prefix);
    changed = true;
  });
  if (row.drop) installDropMargin(doc, row, tabs, row.drop);
  // The row is shorter now; the page sets its scroll buttons again on a scroll event of the row (each page of a food
  // window listens to it), as when the player scrolls.
  if (changed) {
    const view = doc.defaultView as Window & typeof globalThis;
    for (const sel of row.rows) doc.querySelector(sel)?.dispatchEvent(new view.Event('scroll'));
  }
  return 'ok';
}

export interface IconTabsWatch {
  // The list of an open of the window: applied now and after each rebuild of the tab row. Gives 'stale' for a list of
  // an older open than the newest one seen, which is ignored.
  set(openId: number, entries: IconTabEntry[]): ApplyResult | 'stale';
  // The window hides: the list is dropped, so a rebuild of the next open waits for its own list.
  drop(): void;
}

// Watches the containers of the tab row (childList): the page builds the tab row again at each of its messages, and
// each new tab element gets the icon of the list. A rebuild that no longer matches the list is reported once for each
// open through onMismatch.
export function watchIconTabs(doc: Document, row: IconTabRow, prefix: string, onMismatch: (openId: number) => void): IconTabsWatch {
  let entries: IconTabEntry[] | null = null;
  let openId = -1;
  let reported = -1;
  const apply = (): ApplyResult => {
    const result = applyIconTabs(doc, row, entries!, prefix);
    if (result === 'mismatch' && reported !== openId) {
      reported = openId;
      onMismatch(openId);
    }
    return result;
  };
  const observer = new (doc.defaultView as Window & typeof globalThis).MutationObserver(() => {
    if (entries) apply();
  });
  for (const sel of row.rows) {
    const el = doc.querySelector(sel);
    if (el) observer.observe(el, { childList: true });
  }
  return {
    set(id, list) {
      if (id < openId) return 'stale';
      openId = id;
      entries = list;
      return apply();
    },
    drop() {
      entries = null;
    },
  };
}

