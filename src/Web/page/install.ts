// The pass over the Cooking frame and the storage window frame, and its result for C#.
import { addError, ensureStyle, hasFeature, missingText, newErrorsText, view } from './core';
import { installItemTip } from './itemTip';
import { installPieces } from './pieces';
import { installPreview } from './preview';
import type { SortedGrid } from './dropFilter';
import { applyCookingSort, cookingGrid } from './sortCooking';
import { applyStorageSort, storageGrids } from './sortStorage';
import { installTagIcon } from './tagIcon';
import { installTierMark, redrawTiers } from './tierMark';
import type { CookingWindow, PredictionEntry, RatCageWindow, StorageWindow } from './types';

// The start of the result of a pass with a storage window and no Cooking frame (PageJson.StorageResultPrefix in
// C#): the storage window needs no retry.
const STORAGE = 'storage: ';
// The start of the result part of a Rat Cage window, which needs no retry either.
const RAT_CAGE = 'ratcage: ';

// One pass over the frames: installs the wrappers when a frame does not have them, and draws the bag items of the
// Cooking frame again when the tiers can have changed since the last draw of the frame. The install can come after
// the first render of the items, so the first pass of a frame draws them too. No retry: when the Cooking frame is
// not there, C# sends again. The result of the Cooking frame is "installed", "already installed", "no Cooking
// frame", or "error: ...", with an optional "; missing: ..." and "; errors: ..." part. A storage window adds
// "; storage: <its result>", or gives "storage: <its result>" when no Cooking frame is there, and a Rat Cage window
// the same with "ratcage: ".
export function run(): string {
  const frames = document.querySelectorAll('iframe');
  let result = 'no Cooking frame';
  let cookingFound = false;
  let storage = '';
  let ratCage = '';
  for (let i = 0; i < frames.length; i++) {
    const w = frames[i].contentWindow as Window | null;
    if (!w) continue;
    if (isCooking(w)) {
      const r = runCooking(w as CookingWindow);
      if (r !== null) { result = r; cookingFound = true; }
    } else if (isStorage(w)) {
      const r = runStorage(w as StorageWindow);
      if (r !== null) storage = r;
    } else if (isRatCage(w)) {
      const r = runRatCage(w as RatCageWindow);
      if (r !== null) ratCage = r;
    }
  }
  const parts = [];
  if (storage) parts.push(STORAGE + storage);
  if (ratCage) parts.push(RAT_CAGE + ratCage);
  if (!parts.length) return result;
  return cookingFound ? [result, ...parts].join('; ') : parts.join('; ');
}

// The frame is found by its URL also, so a renamed render function gives a "missing" result, not a silent skip.
function isCooking(w: Window): boolean {
  try {
    return typeof (w as CookingWindow).renderPredictionList === 'function' || /Cooking\.html/i.test(String(w.location));
  } catch { return false; }
}

function isStorage(w: Window): boolean {
  try { return /BackpackUI\.html/i.test(String(w.location)); } catch { return false; }
}

function isRatCage(w: Window): boolean {
  try { return /RatCage\.html/i.test(String(w.location)); } catch { return false; }
}

// The result of the Rat Cage frame, or null when the frame is not ready yet. Only the food sort (design D12).
function runRatCage(w: RatCageWindow): string | null {
  let result: string;
  try {
    const doc = w.document;
    if (doc.readyState !== 'complete') return null;
    ensureStyle(doc);
    result = w.__projectCookCookSort ? 'already installed' : 'installed';
    if (hasFeature(w, doc, 'ratCageSort')) {
      try { applyCookingSort(w); } catch (e) { addError(w, 'ratCageSort: ' + e); }
    }
    const missing = missingText(w, doc, 'ratcage');
    if (missing) result += '; missing: ' + missing;
    const errors = newErrorsText(w);
    if (errors) result += '; errors: ' + errors;
  } catch (e) {
    result = 'failed; errors: ' + e;
  }
  return result;
}

// The result of the Cooking frame, or null when the frame is not ready yet.
function runCooking(w: CookingWindow): string | null {
  let result: string;
  try {
    const doc = w.document;
    if (typeof w.renderPredictionList !== 'function' && doc.readyState !== 'complete') return null;
    ensureStyle(doc);

    if (w.__cookingPreview) {
      result = 'already installed';
    } else {
      if (hasFeature(w, doc, 'preview')) installPreview(w, hasFeature(w, doc, 'cardTip'));
      if (hasFeature(w, doc, 'itemTip')) installItemTip(w);
      if (hasFeature(w, doc, 'tierMark')) installTierMark(w);
      // The switch is read at each drop, so the wrapper is installed also before the data comes.
      if (hasFeature(w, doc, 'pieces')) installPieces(w);

      w.__cookingPreview = true;
      result = 'installed';
      try { w.renderPredictionList(w.eval('State.lastPredictionEntries') as PredictionEntry[]); } catch (e) { result = 'installed, no redraw: ' + e; }
    }

    if (w.__cookingTierMark && w.__projectCookDrawn !== view.version) redrawTiers(w);
    if (hasFeature(w, doc, 'cookingSort')) {
      try { applyCookingSort(w); } catch (e) { addError(w, 'cookingSort: ' + e); }
    }

    const missing = missingText(w, doc, 'cooking');
    if (missing) result += '; missing: ' + missing;
    const errors = newErrorsText(w);
    if (errors) result += '; errors: ' + errors;
  } catch (e) { result = 'error: ' + e; }
  return result;
}

// The result of the storage window frame, or null when the frame is not ready yet.
function runStorage(w: StorageWindow): string | null {
  let result: string;
  try {
    const doc = w.document;
    if (doc.readyState !== 'complete') return null;
    ensureStyle(doc);

    if (w.__projectCookStorage) {
      result = 'already installed';
    } else {
      if (hasFeature(w, doc, 'tagIcon')) installTagIcon(w);
      w.__projectCookStorage = true;
      result = 'installed';
    }
    if (hasFeature(w, doc, 'storageSort')) {
      applyStorageSort(w);
      observeStorage(w);
    }

    const missing = missingText(w, doc, 'storage');
    if (missing) result += '; missing: ' + missing;
    const errors = newErrorsText(w);
    if (errors) result += '; errors: ' + errors;
  } catch (e) {
    // The errors part, not "error: ": C# reads "error: " only at the start of the whole result, and this one starts
    // with "storage: ".
    result = 'failed; errors: ' + e;
  }
  return result;
}

// A node that the script wrote: the dropdown, a badge, or a part of them.
function isModNode(node: Node): boolean {
  const el = (node.nodeType === 1 ? node : node.parentElement) as Element | null;
  return !!el && !!el.closest('.projectcook-sort, .projectcook-sort-badge, .projectcook-sort-layer, .projectcook-sort-break');
}

// A record of a change that only the script made: its own nodes, or a class change of only its own class.
function isModRecord(r: MutationRecord): boolean {
  if (r.type === 'attributes' && r.attributeName === 'style') return isModNode(r.target);
  if (r.type === 'attributes') {
    const before = new Set((r.oldValue || '').split(/\s+/).filter(Boolean));
    const after = new Set(((r.target as Element).getAttribute('class') || '').split(/\s+/).filter(Boolean));
    for (const c of before) if (!after.has(c) && !c.startsWith('projectcook-')) return false;
    for (const c of after) if (!before.has(c) && !c.startsWith('projectcook-')) return false;
    return true;
  }
  if (isModNode(r.target)) return true;
  if (r.type === 'characterData') return false;
  const nodes = [...Array.from(r.addedNodes), ...Array.from(r.removedNodes)];
  return nodes.length > 0 && nodes.every(isModNode);
}

// Applies the food sort again after each change of the storage window by the game: a render of the cells loses the
// dim class of the script, and a move of a cell (its style) moves its badge.
function observeStorage(w: StorageWindow & { __projectCookObserver?: MutationObserver }): void {
  if (w.__projectCookObserver) return;
  const app = w.document.getElementById('app');
  if (!app) return;
  const observer = new w.MutationObserver((records) => {
    if (records.every(isModRecord)) return;
    try { applyStorageSort(w); } catch (e) { addError(w, 'storageSort: ' + e); }
  });
  observer.observe(app, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['class', 'style'], attributeOldValue: true });
  w.__projectCookObserver = observer;
}

// The sorted grids of the page that sends a move (sourcePageId Backpack, Cooking, or RatCage), for the drop filter.
export function sortedGrids(sourcePageId: string): SortedGrid[] {
  const frames = document.querySelectorAll('iframe');
  for (let i = 0; i < frames.length; i++) {
    try {
      const w = frames[i].contentWindow;
      if (!w) continue;
      if (sourcePageId === 'Backpack' && isStorage(w)) {
        const grids = storageGrids(w as StorageWindow);
        if (grids.length) return grids;
      } else if ((sourcePageId === 'Cooking' && isCooking(w)) || (sourcePageId === 'RatCage' && isRatCage(w))) {
        const grid = cookingGrid(w as CookingWindow | RatCageWindow);
        if (grid) return [grid];
      }
    } catch { /* a frame of another origin */ }
  }
  return [];
}
