// The order and the pack of the food sort (design D4), with no page part: the same code for the storage window and
// the cooking window. The places that it gives are page places only; the game never sees them.
import type { SortData, SortItem } from './types';
// The places of the items in an order: the first-fit pack of the grid pack library, or null when they do not fit.
export { pack } from '../../Shared/grid-pack/web/gridPack';

// The index of a choice in the words of the sort data. The numbers of an item ("n") have the order of the
// choices 1 to 6: Satiety, Morale, Stamina, Life, Trade value, Expiration Date. cage is not a choice of the
// dropdown: it is the seventh number, the cage satiety, which the Rat Cage window shows for the choice Satiety.
export const CHOICE = { default: 0, satiety: 1, morale: 2, stamina: 3, life: 4, trade: 5, days: 6, cage: 7 } as const;

// The column of the numbers that a choice orders and badges by: in the Rat Cage window, Satiety is the cage
// satiety (design D12); else the choice itself.
export function column(choice: number, ratCage: boolean): number {
  return ratCage && choice === CHOICE.satiety ? CHOICE.cage : choice;
}

export interface GridItem { id: number | string; w: number; h: number }

function key(item: SortItem | undefined, choice: number): number | null {
  if (!item || choice < 1 || choice > CHOICE.cage) return null;
  const v = item.n[choice - 1];
  return typeof v === 'number' ? v : null;
}

// The items in the order of the choice, and the items with no number (dimmed, last). Stats and the trade value go
// highest first; the days left go lowest first, so an expired item (a key below 0) is first. A tie groups the same
// items (config id), then keeps the real order.
export function order(items: GridItem[], sort: SortData, choice: number): { ids: (number | string)[]; dim: Set<number | string> } {
  const sign = choice === CHOICE.days ? 1 : -1;
  const keyed = items.map((it, i) => {
    const s = sort.items[String(it.id)];
    return { id: it.id, i, k: key(s, choice), c: (s && s.c) || 0 };
  });
  keyed.sort((a, b) => {
    if (a.k === null || b.k === null) return (a.k === null ? 1 : 0) - (b.k === null ? 1 : 0) || a.c - b.c || a.i - b.i;
    return sign * (a.k - b.k) || a.c - b.c || a.i - b.i;
  });
  return { ids: keyed.map((k) => k.id), dim: new Set(keyed.filter((k) => k.k === null).map((k) => k.id)) };
}

// The color of a badge, null for the plain (white) text: a stat or the cage satiety above 0 is positive and below 0
// negative, the trade value is gold, and the days are plain or negative for an expired item (its key is below 0).
export function badgeTone(item: SortItem | undefined, choice: number): 'pos' | 'neg' | 'gold' | null {
  const v = key(item, choice);
  if (v === null) return null;
  if (choice === CHOICE.trade) return 'gold';
  if (v < 0) return 'neg';
  return v > 0 && choice !== CHOICE.days ? 'pos' : null;
}

// The text of the badge of a cell: a stat or the cage satiety with its sign, the trade value plain, the days text;
// null for no badge.
export function badgeText(item: SortItem | undefined, choice: number): string | null {
  if (!item) return null;
  if (choice === CHOICE.days) return item.d;
  const v = key(item, choice);
  if (v === null) return null;
  if (choice === CHOICE.trade) return String(v);
  return (v > 0 ? '+' : '') + v;
}
