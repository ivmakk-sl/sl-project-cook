// The tier marks: a class on each High and Low bag item, which page.css draws in place of the game's food badge.
import { addError, view } from './core';
import type { BagGrid, BagItemData, CookingWindow } from './types';

// Draws the bag items of the frame again, so each gets the mark of its current tier.
export function redrawTiers(w: CookingWindow): void {
  try {
    w.backpack.renderItems();
    w.pot.renderItems();
    w.__projectCookDrawn = view.version;
  } catch (e) { addError(w, 'tierMark redraw: ' + e); }
}

// Wraps onItemRendered of the backpack and pot grids to add a tier mark.
export function installTierMark(w: CookingWindow): void {
  function wrapGrid(grid: BagGrid) {
    const cfg = grid.getConfig();
    const original = cfg.onItemRendered;
    cfg.onItemRendered = function (el: HTMLElement, itemData: BagItemData) {
      if (original) original(el, itemData);
      try {
        const tier = itemData._raw && view.data.tiers[itemData._raw.configId];
        if (tier === 1 || tier === 3) el.classList.add('projectcook-bag-tier', 'projectcook-bag-tier-' + tier);
      } catch (e) { addError(w, 'tierMark: ' + e); }
    };
  }
  wrapGrid(w.backpack);
  wrapGrid(w.pot);
  w.__cookingTierMark = true;
}
