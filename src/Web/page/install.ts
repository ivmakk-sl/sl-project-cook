// The pass over the Cooking frame and its result for C#.
import { ensureStyle, hasFeature, missingText, newErrorsText, view } from './core';
import { installItemTip } from './itemTip';
import { installPreview } from './preview';
import { installTierMark, redrawTiers } from './tierMark';
import type { CookingWindow, PredictionEntry } from './types';

// One pass over the Cooking frame: installs the wrappers when the frame does not have them, and draws the bag
// items again when the tiers can have changed since the last draw of the frame. The install can come after the
// first render of the items, so the first pass of a frame draws them too. No retry: when the Cooking frame is not
// there, C# sends again. The result is "installed", "already installed", "no Cooking frame", or "error: ...",
// with an optional "; missing: ..." and "; errors: ..." part.
export function run(): string {
  const frames = document.querySelectorAll('iframe');
  let result = 'no Cooking frame';
  for (let i = 0; i < frames.length; i++) {
    try {
      const w = frames[i].contentWindow as CookingWindow | null;
      // The frame is found by its URL also, so a renamed render function gives a "missing" result, not a silent skip.
      if (!w || !(typeof w.renderPredictionList === 'function' || /Cooking\.html/i.test(String(w.location)))) continue;
      const doc = w.document;
      if (typeof w.renderPredictionList !== 'function' && doc.readyState !== 'complete') continue;
      ensureStyle(doc);

      if (w.__cookingPreview) {
        result = 'already installed';
      } else {
        if (hasFeature(w, doc, 'preview')) installPreview(w, hasFeature(w, doc, 'cardTip'));
        if (hasFeature(w, doc, 'itemTip')) installItemTip(w);
        if (hasFeature(w, doc, 'tierMark')) installTierMark(w);

        w.__cookingPreview = true;
        result = 'installed';
        try { w.renderPredictionList(w.eval('State.lastPredictionEntries') as PredictionEntry[]); } catch (e) { result = 'installed, no redraw: ' + e; }
      }

      if (w.__cookingTierMark && w.__projectCookDrawn !== view.version) redrawTiers(w);

      const missing = missingText(w, doc);
      if (missing) result += '; missing: ' + missing;
      const errors = newErrorsText(w);
      if (errors) result += '; errors: ' + errors;
    } catch (e) { result = 'error: ' + e; }
  }
  return result;
}
