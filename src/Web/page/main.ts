// Runs in the root page. Each UI page is an iframe, and a reopened window is a new iframe, so the wrappers are
// installed again when they are missing. Defines window.__projectCook once. C# calls setData(data) when the
// ingredient tooltip lines or tiers change (a language switch) or the root page has no script, and apply() at each
// other prediction refresh, setSortData(sort) when the numbers of the food sort of the open storage change, and
// setIconTabs(list) at each open of a food window with the icon tabs on.
// Each call runs one pass and does not retry: when the Cooking frame is not there, C#
// sends again later. A second full send of the script keeps the first interface and its data.
// The looks are in tokens.css and page.css: the script sets classes, and only the column count of a grid as the
// custom property --pc-cols.
import { view } from './core';
import { installDropFilter } from './dropFilter';
import { iconTabsAtLoad, run, sortedGrids } from './install';
import { setIconTabList } from './iconTabs';
import type { IconTabList, PageData, SortData } from './types';

// Stores the ingredient tooltip lines and tiers, and the numbers of the food sort when C# sends them in the same
// call, and applies them in one pass.
function setData(data: PageData, sort?: SortData): string {
  view.data = { tips: (data && data.tips) || {}, tiers: (data && data.tiers) || {}, features: (data && data.features) || [], portion: data && data.portion, book: data && data.book };
  view.version++;
  if (sort) view.sort = sort;
  return run();
}

// Stores the numbers of the food sort of the open storage, and applies them in one pass.
function setSortData(sort: SortData): string {
  view.sort = sort;
  return run();
}

// Stores the icon tab list of the newest open of a food window, and applies it in one pass.
function setIconTabs(list: IconTabList): string {
  setIconTabList(list);
  return run();
}

// Each frame of the root page, now and later, gets the icon tabs at its load: a window opens as a new frame, and its
// first tab row comes before the next pass.
function watchFrames(): void {
  if (typeof MutationObserver === 'undefined' || !document.body) return;
  const hook = (f: HTMLIFrameElement & { __projectCookLoad?: boolean }): void => {
    if (f.__projectCookLoad) return;
    f.__projectCookLoad = true;
    f.addEventListener('load', () => {
      try { if (f.contentWindow) iconTabsAtLoad(f.contentWindow); } catch { /* the next pass applies the icon tabs */ }
    });
  };
  document.querySelectorAll('iframe').forEach((f) => hook(f as HTMLIFrameElement));
  new MutationObserver((records) => {
    for (const r of records) r.addedNodes.forEach((n) => { if (n instanceof HTMLIFrameElement) hook(n); });
  }).observe(document.body, { childList: true, subtree: true });
}

if (!window.__projectCook) {
  window.__projectCook = { setData, setSortData, setIconTabs, frameLoaded: iconTabsAtLoad, apply: run, sortData: () => view.sort };
  // The pages send their moves through the postMessage of this root page; the filter keeps the sorted grids real.
  installDropFilter(window, sortedGrids);
  watchFrames();
}
