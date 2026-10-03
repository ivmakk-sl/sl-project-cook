// The food sort in the cooking window (design D4): the container grid is createGridBag of webui-bag.js (the frame
// global backpack). A wrapper of refresh gives each new items array of a game message its sorted places and draws
// the grid again; a refresh that comes during a drag waits in the grid, and the wrapper of _flushPendingRefresh
// sorts it when the drag ends. After a drop the drag coordinator puts the cell at the drop place by hand, so the
// wrapper of updateItemPosition draws the sorted places again once the drop is done. The badges go on in
// onItemRendered of the grid, so each draw of the game keeps them. The workbench (the pot grid) shows the numbers of
// the choice on its cells too, so the player can compare them with the container tab; its cells keep their places
// and are not dimmed. The sort data of the container tab also holds the numbers of the workbench items.
import { addError, view } from './core';
import type { SortedGrid } from './dropFilter';
import { CHOICE, order, pack } from './sortOrder';
import { drawCells, drawDropdown, removeDropdown } from './sortUi';
import type { BagGrid, BagItemData, CookingWindow } from './types';

type State = NonNullable<CookingWindow['__projectCookCookSort']>;

function sortOf(grid: BagGrid) {
  const sort = view.sort;
  return sort && sort.owner === String(grid.getOwnerId()) ? sort : null;
}

// Writes the places of the choice into the items of the grid (the real places with Default, with no sort data, or
// when the items do not fit), and draws the grid again when a place or the look changed.
function place(w: CookingWindow, state: State): void {
  const grid = w.backpack;
  const items = grid.getItems();
  if (state.items !== items) {
    state.items = items;
    state.real = new Map(items.map((it) => [String(it.id), [it.x || 0, it.y || 0] as [number, number]]));
  }
  const sort = sortOf(grid);
  let places = state.real;
  let dim = new Set<number | string>();
  state.sorted = false;
  if (sort && view.choice !== CHOICE.default) {
    const gridItems = items.map((it) => ({ id: String(it.id), w: it.w || 1, h: it.h || 1 }));
    const r = order(gridItems, sort, view.choice);
    const byId = new Map(gridItems.map((it) => [it.id as number | string, it]));
    const cfg = grid.getConfig();
    const packed = pack(r.ids.map((id) => byId.get(id)!), cfg.cols, cfg.rows);
    if (packed) {
      places = packed as Map<string, [number, number]>;
      dim = r.dim;
      state.sorted = true;
    }
  }
  let changed = false;
  for (const it of items) {
    const p = places.get(String(it.id));
    if (!p) continue;
    if (it.x !== p[0]) { it.x = p[0]; changed = true; }
    if (it.y !== p[1]) { it.y = p[1]; changed = true; }
  }
  state.dim = dim;
  const look = (sort ? sort.owner : '') + '|' + view.choice + '|' + [...dim].join(',');
  // New sort data can change a number with the same order, so it draws the badges of both grids again.
  const fresh = state.sortSeen !== view.sort;
  state.sortSeen = view.sort;
  if (changed || fresh || look !== state.drawn) {
    state.drawn = look;
    grid.renderItems();
    if (w.pot) w.pot.renderItems();
  }
}

function install(w: CookingWindow): State {
  const state: State = { items: null, real: new Map(), dim: new Set(), drawn: '', sorted: false, sortSeen: undefined };
  w.__projectCookCookSort = state;
  const grid = w.backpack;

  const cfg = grid.getConfig();
  const rendered = cfg.onItemRendered;
  cfg.onItemRendered = function (el: HTMLElement, itemData: BagItemData) {
    if (rendered) rendered(el, itemData);
    try { drawCells(w.document, [el], [String(itemData.id)], sortOf(grid), view.choice, state.dim); } catch (e) { addError(w, 'cookingSort badge: ' + e); }
  };

  if (w.pot) {
    const potCfg = w.pot.getConfig();
    const potRendered = potCfg.onItemRendered;
    const noDim = new Set<number | string>();
    potCfg.onItemRendered = function (el: HTMLElement, itemData: BagItemData) {
      if (potRendered) potRendered(el, itemData);
      try { drawCells(w.document, [el], [String(itemData.id)], sortOf(grid), view.choice, noDim); } catch (e) { addError(w, 'cookingSort pot badge: ' + e); }
    };
  }

  const refresh = grid.refresh;
  grid.refresh = function (backendItems: unknown[]) {
    const before = grid.getItems();
    refresh.call(grid, backendItems);
    // During a drag the grid keeps the new items for later, and getItems still gives the old array.
    if (grid.getItems() !== before) {
      try {
        place(w, state);
        drawToolbar(w);
      } catch (e) { addError(w, 'cookingSort refresh: ' + e); }
    }
  };

  const flush = grid._flushPendingRefresh;
  grid._flushPendingRefresh = function () {
    const applied = flush.call(grid);
    if (applied) {
      try {
        place(w, state);
        drawToolbar(w);
      } catch (e) { addError(w, 'cookingSort flush: ' + e); }
    }
    return applied;
  };

  const update = grid.updateItemPosition;
  grid.updateItemPosition = function (itemId: string, containerId: string, col: number, row: number) {
    update.call(grid, itemId, containerId, col, row);
    if (sortOf(grid) && view.choice !== CHOICE.default) {
      w.setTimeout(() => {
        try {
          state.drawn = '';
          place(w, state);
        } catch (e) { addError(w, 'cookingSort drop: ' + e); }
      }, 0);
    }
  };
  return state;
}

// The dropdown in the toolbar of the container grid, for the owner of the open tab.
function drawToolbar(w: CookingWindow): void {
  const toolbar = w.document.querySelector('.bag-toolbar');
  const sort = sortOf(w.backpack);
  if (!sort || !toolbar) {
    removeDropdown(toolbar);
    return;
  }
  drawDropdown(w.document, toolbar, sort, view.choice, (choice) => {
    view.choice = choice;
    applyCookingSort(w);
  });
}

// One pass: the wrappers once, the places, and the dropdown.
export function applyCookingSort(w: CookingWindow): void {
  const state = w.__projectCookCookSort || install(w);
  place(w, state);
  drawToolbar(w);
}

// The container tab of the frame for the drop filter while it shows the packed places, else null.
export function cookingGrid(w: CookingWindow): SortedGrid | null {
  const state = w.__projectCookCookSort;
  const grid = w.backpack;
  if (!state || !state.sorted || !grid || !sortOf(grid)) return null;
  const cfg = grid.getConfig();
  return {
    owner: String(grid.getOwnerId()),
    cols: cfg.cols,
    rows: cfg.rows,
    items: grid.getItems().map((it) => {
      const p = state.real.get(String(it.id)) || [it.x || 0, it.y || 0];
      return { id: String(it.id), x: p[0], y: p[1], w: it.w || 1, h: it.h || 1 };
    }),
    sizeOf: (id) => {
      const it = w.pot && w.pot.getItems().find((i) => String(i.id) === id);
      return it ? [it.w || 1, it.h || 1] : null;
    },
  };
}
