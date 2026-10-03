// The food sort in the storage window (design D4): a Vue watch on the items of each side with flush 'sync' gives
// each new items array of a game message its sorted places before the page draws it, as the value view of Better
// Trade does in the trade window. The storage side (bagB) sorts when the sort data names its owner, and the Backpack
// side (bagA) sorts with it when the sort data names the Backpack too. The places are page places only: a drag inside
// a sorted grid is dropped by the drop filter, and each other action names the item by its id.
import { addError, view } from './core';
import type { SortedGrid } from './dropFilter';
import { CHOICE, order, pack } from './sortOrder';
import { drawCells, drawDropdown, removeDropdown } from './sortUi';
import type { StorageWindow } from './types';

interface BagItem { id: number; x: number; y: number; w: number; h: number }
interface Bag { ownerId: number; gridCols: number; gridRows: number; items: BagItem[] }
interface SetupState { bagA?: Bag; bagB?: Bag; isDualBag?: boolean }

type Side = 'A' | 'B';

// sorted: the items show the packed places now (not Default, and they fit).
interface SideState { items: BagItem[] | null; real: Map<number, [number, number]>; dim: Set<number | string>; sorted: boolean }

type SortWindow = StorageWindow & {
  Vue?: {
    watch(source: () => unknown, cb: () => void, options?: { flush?: 'pre' | 'post' | 'sync' }): void;
    nextTick?(fn: () => void): void;
  };
  __projectCookSort?: Record<Side, SideState>;
};

const SIDES: Side[] = ['B', 'A'];
const PANEL: Record<Side, string> = { A: '.bag-panel:not(.is-storage)', B: '.bag-panel.is-storage' };

function setupState(w: SortWindow): SetupState | null {
  const app = w.document.getElementById('app') as (HTMLElement & { _vnode?: { component?: { setupState?: SetupState } } }) | null;
  return (app && app._vnode && app._vnode.component && app._vnode.component.setupState) || null;
}

function bagOf(state: SetupState, side: Side): Bag | undefined {
  return side === 'A' ? state.bagA : state.bagB;
}

// The sort data of a side, or null when the side is not sortable. The data must name the open storage: after an
// ordinary storage C# sends no data, and the data of the last sortable storage still names the Backpack. A storage
// opened alone (the Use of a storage, isDualBag false) is the one grid of the window, on side A.
function sortOf(state: SetupState, side: Side) {
  const sort = view.sort;
  const bag = bagOf(state, side);
  if (!sort || !bag) return null;
  if (!state.isDualBag) return side === 'A' && sort.owner === String(bag.ownerId) ? sort : null;
  if (!state.bagB || sort.owner !== String(state.bagB.ownerId)) return null;
  return side === 'B' || sort.bag === String(bag.ownerId) ? sort : null;
}

// Gives the items of a side the places of the choice: the packed places, or the real places with Default, with no
// sort data, or when the items do not fit. A new items array of the page holds the real places.
function place(w: SortWindow, state: SetupState, side: Side): void {
  const fs = w.__projectCookSort![side];
  const bag = bagOf(state, side);
  if (!bag) return;
  const items = bag.items;
  if (fs.items !== items) {
    fs.items = items;
    fs.real = new Map(items.map((it) => [it.id, [it.x, it.y] as [number, number]]));
  }
  const sort = sortOf(state, side);
  let places = fs.real;
  fs.dim = new Set();
  fs.sorted = false;
  if (sort && view.choice !== CHOICE.default) {
    const r = order(items, sort, view.choice);
    const byId = new Map(items.map((it) => [it.id as number | string, it]));
    const packed = pack(r.ids.map((id) => byId.get(id)!), bag.gridCols, bag.gridRows);
    if (packed) {
      places = packed as Map<number, [number, number]>;
      fs.dim = r.dim;
      fs.sorted = true;
    }
  }
  for (const it of items) {
    const p = places.get(it.id);
    if (!p) continue;
    if (it.x !== p[0]) it.x = p[0];
    if (it.y !== p[1]) it.y = p[1];
  }
}

// The dropdown and the badges of one side.
function draw(w: SortWindow, state: SetupState, side: Side): void {
  const doc = w.document;
  const bag = bagOf(state, side);
  const panel = doc.querySelector(PANEL[side]);
  const toolbar = panel && panel.querySelector(':scope .toolbar');
  const sort = sortOf(state, side);
  if (!sort || !toolbar) {
    removeDropdown(toolbar);
  } else {
    drawDropdown(doc, toolbar, sort, view.choice, (choice) => {
      view.choice = choice;
      applyStorageSort(w);
      // Vue draws the new places of the cells on its next tick; the badges take the places of the cells.
      if (w.Vue && w.Vue.nextTick) w.Vue.nextTick(() => applyStorageSort(w));
    });
  }
  const grid = panel ? panel.querySelector('.grid-container') as HTMLElement | null : null;
  if (!grid || !bag) return;
  // The frost of a freezer is an element next to the cells, above them, so the badges go into a layer above it.
  let layer = grid.querySelector(':scope > .projectcook-sort-layer') as HTMLElement | null;
  if (!layer) {
    layer = doc.createElement('div');
    layer.className = 'projectcook-sort-layer';
  }
  if (grid.lastElementChild !== layer) grid.appendChild(layer);
  const cells = grid.querySelectorAll(':scope > .item');
  // The cells are in the order of the items (a keyed v-for); a count that differs (a drag) is left as it is.
  if (cells.length === bag.items.length) drawCells(doc, cells, bag.items.map((it) => it.id), sort, view.choice, w.__projectCookSort![side].dim, layer);
}

// One pass: the watches once, the places, the dropdowns, and the cells. Records a missing part and does nothing when
// the storage side is not there.
export function applyStorageSort(w: SortWindow): void {
  const state = setupState(w);
  if (!state || !state.bagB || !w.Vue) {
    addError(w, 'storageSort: no bagB or Vue');
    return;
  }
  if (!w.__projectCookSort) {
    const fresh = (): SideState => ({ items: null, real: new Map(), dim: new Set(), sorted: false });
    w.__projectCookSort = { A: fresh(), B: fresh() };
    for (const side of SIDES) {
      // The page can give a side a new bag object (the tabs of side A), so the watch reads the bag each time.
      w.Vue.watch(() => { const bag = bagOf(state, side); return bag && bag.items; }, () => place(w, state, side), { flush: 'sync' });
    }
  }
  for (const side of SIDES) place(w, state, side);
  for (const side of SIDES) draw(w, state, side);
}

// The sorted grids of the frame for the drop filter, each while it shows the packed places.
export function storageGrids(w: SortWindow): SortedGrid[] {
  const fs = w.__projectCookSort;
  const state = setupState(w);
  const grids: SortedGrid[] = [];
  if (!fs || !state) return grids;
  for (const side of SIDES) {
    const bag = bagOf(state, side);
    const other = bagOf(state, side === 'A' ? 'B' : 'A');
    const s = fs[side];
    if (!s.sorted || !bag || !sortOf(state, side)) continue;
    grids.push({
      owner: String(bag.ownerId),
      cols: bag.gridCols,
      rows: bag.gridRows,
      items: bag.items.map((it) => {
        const p = s.real.get(it.id) || [it.x, it.y];
        return { id: String(it.id), x: p[0], y: p[1], w: it.w || 1, h: it.h || 1 };
      }),
      sizeOf: (id) => {
        const it = other && other.items.find((i) => String(i.id) === id);
        return it ? [it.w || 1, it.h || 1] : null;
      },
    });
  }
  return grids;
}
