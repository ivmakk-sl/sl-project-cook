// Separate pieces: the game's cooking station grid refuses a drop on a cell with an item. With the switch on, a drop
// of an item with uses on one item of the same kind with free uses is accepted, and the game merges the piece into
// that item (PiecesOnCookingDragMove in C#).
import { view } from './core';
import type { BagItemData, CookingWindow, Slot } from './types';

type Validity = (slots: Slot[], draggedId: string) => boolean;

export function installPieces(w: CookingWindow & { __projectCookPieces?: boolean }): void {
  if (w.__projectCookPieces) return;
  const pot = w.pot;
  const original = pot.checkValidity.bind(pot) as Validity;
  pot.checkValidity = (slots: Slot[], draggedId: string) =>
    original(slots, draggedId) || canMerge(w, slots, draggedId);
  w.__projectCookPieces = true;
}

function canMerge(w: CookingWindow, slots: Slot[], draggedId: string): boolean {
  if (!view.data.features || view.data.features.indexOf('separatePieces') < 0) return false;
  const dragged = find(w.backpack.getItems(), draggedId) || find(w.pot.getItems(), draggedId);
  if (!dragged || !dragged._raw || !((dragged.useTimesMax || 0) >= 2)) return false;
  const cfg = w.pot.getConfig();
  for (const s of slots) if (s.c < 0 || s.c >= cfg.cols || s.r < 0 || s.r >= cfg.rows) return false;
  let target: BagItemData | null = null;
  for (const item of w.pot.getItems()) {
    if (item.id === draggedId) continue;
    const x = item.x || 0, y = item.y || 0, iw = item.w || 1, ih = item.h || 1;
    if (!slots.some((s) => s.c >= x && s.c < x + iw && s.r >= y && s.r < y + ih)) continue;
    if (target) return false;
    target = item;
  }
  return !!target && !!target._raw && target._raw.configId === dragged._raw.configId &&
    (target.useTimes || 0) < (target.useTimesMax || 0);
}

function find(items: BagItemData[], id: string): BagItemData | undefined {
  for (const item of items) if (item.id === id) return item;
  return undefined;
}
