// The dim of uncookable items in the container tab of the cooking window: the items that the cooking station refuses,
// by the same fields as canAcceptItem of Cooking.html (not food, a product, or an item that needs cutting first). A
// fuel item (burnable, the .is-fuel mark of the page) stays bright at a stove that takes fuel.
import { view } from './core';
import type { BagItemData, GridSortWindow } from './types';

export function dimOn(): boolean {
  return !!view.data.features && view.data.features.indexOf('dimUncookable') >= 0;
}

// The stove takes fuel: not an electric stove (cookType 2, whose fuel section the page hides) and with fuel slots.
export function takesFuel(w: GridSortWindow): boolean {
  const cfg = w.cookingConfig;
  return !!cfg && cfg.cookType !== 2 && (cfg.fuelSlotCount || 0) > 0;
}

export function isUncookable(item: BagItemData, fuel = false): boolean {
  const raw = item._raw;
  if (fuel && raw && raw.burnable) return false;
  return !raw || raw.category !== 1 || raw.canCook === false || !!raw.needCut;
}
