// The drop filter of the food sort (design D5). Both pages send their moves through sendMessage of
// createWebUICore, which calls window.parent.postMessage({ type, data, sourcePageId }): the postMessage of the root
// page, where this script runs. While a sort is on, the cells of each sorted grid show page places, not real ones:
// - a move inside a sorted grid is dropped, so no item moves in it;
// - a move into it from another owner gets the first free real cell that fits the item; with no such cell it is
//   dropped, because its place is a sorted one and could name another real item.
// Each other message passes unchanged.

export interface GridPlace { id: string; x: number; y: number; w: number; h: number }

// The sorted storage: its owner, its size, its items in their real places, and the size of an item of another grid.
export interface SortedGrid {
  owner: string;
  cols: number;
  rows: number;
  items: GridPlace[];
  sizeOf(id: string): [number, number] | null;
}

export interface PageMessage { type?: string; sourcePageId?: string; data?: Record<string, unknown> }

// The first cell, row by row, where an item of w x h fits between the items (the scan of findEmptySpot).
export function firstFree(items: GridPlace[], cols: number, rows: number, w: number, h: number): [number, number] | null {
  const used: boolean[][] = [];
  for (let y = 0; y < rows; y++) used.push(new Array(cols).fill(false));
  for (const it of items) {
    for (let j = 0; j < it.h; j++) for (let i = 0; i < it.w; i++) {
      if (it.y + j < rows && it.x + i < cols) used[it.y + j][it.x + i] = true;
    }
  }
  for (let y = 0; y + h <= rows; y++) {
    for (let x = 0; x + w <= cols; x++) {
      let free = true;
      for (let j = 0; j < h && free; j++) for (let i = 0; i < w && free; i++) if (used[y + j][x + i]) free = false;
      if (free) return [x, y];
    }
  }
  return null;
}

// The source owner, the target owner, and the item of a move message, or null for another message.
function move(message: PageMessage): { from: string | null; to: string; item: string | null } | null {
  const d = message.data;
  if (!d) return null;
  switch (message.type) {
    case 'DRAG_ITEM': return { from: String(d.srcOwnerId), to: String(d.dstOwnerId), item: String(d.itemId) };
    case 'ITEM_MOVE': return { from: String(d.fromOwnerId), to: String(d.toOwnerId), item: String(d.itemId) };
    case 'GROUND_PICK_TO': return { from: null, to: String(d.ownerId), item: null };
    default: return null;
  }
}

// The message to send, null to send nothing. grids are the sorted grids of the page (none with Default).
export function filterMove(message: PageMessage, grids: SortedGrid[]): PageMessage | null {
  const m = move(message);
  const grid = m && grids.find((g) => g.owner === m.to);
  if (!m || !grid) return message;
  if (m.from === grid.owner) return null;
  // A ground item has no size in the message; the ground dock gives one cell to each item.
  const size = (m.item && grid.sizeOf(m.item)) || [1, 1];
  const cell = firstFree(grid.items, grid.cols, grid.rows, size[0], size[1]);
  if (!cell) return null;
  const data: Record<string, unknown> = { ...message.data, x: cell[0], y: cell[1] };
  if ('cursorCol' in data) data.cursorCol = cell[0];
  if ('cursorRow' in data) data.cursorRow = cell[1];
  return { ...message, data };
}

type FilterWindow = Window & { __projectCookDropFilter?: boolean };

// Wraps the postMessage of the root page once. findGrids gives the sorted grids of the page that sends.
export function installDropFilter(root: Window, findGrids: (sourcePageId: string) => SortedGrid[]): void {
  const w = root as FilterWindow;
  if (w.__projectCookDropFilter || typeof w.postMessage !== 'function') return;
  w.__projectCookDropFilter = true;
  const original = w.postMessage;
  w.postMessage = function (this: Window, message: unknown, ...rest: unknown[]) {
    let out = message;
    try {
      const m = message as PageMessage;
      if (m && typeof m === 'object' && move(m)) out = filterMove(m, findGrids(String(m.sourcePageId)));
    } catch (e) {
      out = message;
    }
    if (out === null) return;
    return (original as (...args: unknown[]) => void).call(this, out, ...rest);
  } as typeof w.postMessage;
}
