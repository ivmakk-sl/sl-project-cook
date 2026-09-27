// Runs in the root page. Each UI page is an iframe, and a reopened window is a new iframe, so the wrappers are
// installed again when they are missing. Defines window.__projectCook once. C# calls setData(data) when the
// ingredient tooltip lines or tiers change (a language switch) or the root page has no script, and apply() at each
// other prediction refresh. Each call runs one pass and does not retry: when the Cooking frame is not there, C#
// sends again later. A second full send of the script keeps the first interface and its data.
// The looks are in tokens.css and page.css: the script sets classes, and only the column count of a grid as the
// custom property --pc-cols.
import { view } from './core';
import { run } from './install';
import type { PageData } from './types';

// Stores the ingredient tooltip lines and tiers, and applies them in one pass.
function setData(data: PageData): string {
  view.data = { tips: (data && data.tips) || {}, tiers: (data && data.tiers) || {} };
  view.version++;
  return run();
}

window.__projectCook = window.__projectCook || { setData, apply: run };
