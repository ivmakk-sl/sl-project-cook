// Runs in the root page. Each UI page is an iframe, and a reopened window is a new iframe, so the wrappers are
// installed again when they are missing. Defines window.__projectCook once. C# calls setData(data) when the
// ingredient tooltip lines or tiers change (a language switch) or the root page has no script, and apply() at each
// other prediction refresh, and setSortData(sort) when the numbers of the food sort of the open storage change.
// Each call runs one pass and does not retry: when the Cooking frame is not there, C#
// sends again later. A second full send of the script keeps the first interface and its data.
// The looks are in tokens.css and page.css: the script sets classes, and only the column count of a grid as the
// custom property --pc-cols.
import { view } from './core';
import { installDropFilter } from './dropFilter';
import { run, sortedGrids } from './install';
import type { PageData, SortData } from './types';

// Stores the ingredient tooltip lines and tiers, and the numbers of the food sort when C# sends them in the same
// call, and applies them in one pass.
function setData(data: PageData, sort?: SortData): string {
  view.data = { tips: (data && data.tips) || {}, tiers: (data && data.tiers) || {} };
  view.version++;
  if (sort) view.sort = sort;
  return run();
}

// Stores the numbers of the food sort of the open storage, and applies them in one pass.
function setSortData(sort: SortData): string {
  view.sort = sort;
  return run();
}

if (!window.__projectCook) {
  window.__projectCook = { setData, setSortData, apply: run, sortData: () => view.sort };
  // The pages send their moves through the postMessage of this root page; the filter keeps the sorted grids real.
  installDropFilter(window, sortedGrids);
}
