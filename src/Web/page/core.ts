// The helpers that all features use: the game parts, the stored data, the style node, and the error record.
import tokensCss from '../tokens.css?inline';
import pageCss from '../page.css?inline';
import type { ModWindow, PageData, SortData } from './types';

// Page parts each feature depends on, by feature:
//   preview:  renderPredictionList, #predictionList, .pot-bd
//   cardTip:  #recipeTooltip, moveTooltip
//   itemTip:  showItemTip, #recipeTooltip
//   tierMark: backpack.getConfig, pot.getConfig
//   cookingSort: backpack.refresh, backpack._flushPendingRefresh, backpack.updateItemPosition, .bag-toolbar
//   tagIcon:  #app (storage window)
//   storageSort: Vue.watch, .grid-container, .toolbar (storage window; bagB is in the setup state of #app)
// #predictionList .pot-bd exists only after a render, so it is checked in the render wrapper instead of here.
// tagIconSvg is in the setup state of the #app component, so installTagIcon checks it.
export const FEATURES = {
  preview: ['renderPredictionList', '#predictionList'],
  cardTip: ['#recipeTooltip', 'moveTooltip'],
  itemTip: ['showItemTip', '#recipeTooltip'],
  tierMark: ['backpack.getConfig', 'pot.getConfig'],
  cookingSort: ['backpack.refresh', 'backpack._flushPendingRefresh', 'backpack.updateItemPosition', '.bag-toolbar'],
  tagIcon: ['#app'],
  storageSort: ['Vue.watch', '.grid-container', '.toolbar']
};

export type Feature = keyof typeof FEATURES;

// The window of each feature: the Cooking frame or the storage window frame (BackpackUI).
export type Page = 'cooking' | 'storage';
const PAGE_OF: Record<Feature, Page> = {
  preview: 'cooking',
  cardTip: 'cooking',
  itemTip: 'cooking',
  tierMark: 'cooking',
  cookingSort: 'cooking',
  tagIcon: 'storage',
  storageSort: 'storage'
};

// The data of the last setData; the version counts the setData calls, so a frame draws its bag items again
// when the tiers can have changed since its last draw. sort is the data of the last setSortData, and choice the
// choice of the food sort (an index of CHOICE): the root page keeps it for all frames until a browser rebuild.
export const view: { data: PageData; version: number; sort: SortData | null; choice: number } =
  { data: { tips: {}, tiers: {} }, version: 0, sort: null, choice: 0 };

function partExists(w: Window, doc: Document, name: string): boolean {
  if (name.charAt(0) === '#' || name.charAt(0) === '.') return !!doc.querySelector(name);
  if (name.indexOf('.') > 0) {
    let obj: unknown = w;
    for (const step of name.split('.')) {
      if (!obj) return false;
      obj = (obj as Record<string, unknown>)[step];
    }
    return typeof obj !== 'undefined';
  }
  return typeof (w as unknown as Record<string, unknown>)[name] === 'function';
}

export function hasFeature(w: Window, doc: Document, feature: Feature): boolean {
  return FEATURES[feature].every((part) => partExists(w, doc, part));
}

// The missing parts of the features of one window.
export function missingText(w: Window, doc: Document, page: Page): string {
  let text = '';
  for (const feature of Object.keys(FEATURES) as Feature[]) {
    if (PAGE_OF[feature] !== page) continue;
    const missing = FEATURES[feature].filter((part) => !partExists(w, doc, part));
    if (missing.length) text += (text ? ', ' : '') + feature + '(' + missing.join(',') + ')';
  }
  return text;
}

export function addError(w: ModWindow, text: string): void {
  w.__cookingErrors = w.__cookingErrors || {};
  w.__cookingErrors[text] = true;
}

// The errors that the frame recorded since the last report to C#.
export function newErrorsText(w: ModWindow): string {
  const reported = w.__cookingErrorsReported || (w.__cookingErrorsReported = {});
  const texts: string[] = [];
  for (const text in (w.__cookingErrors || {})) if (!reported[text]) { reported[text] = true; texts.push(text); }
  return texts.join(' || ');
}

// The one style node of the mod in the frame: the tokens, then the rules.
export function ensureStyle(doc: Document): void {
  if (doc.getElementById('projectcook-style')) return;
  const style = doc.createElement('style');
  style.id = 'projectcook-style';
  style.textContent = tokensCss + '\n' + pageCss;
  doc.head.appendChild(style);
}
