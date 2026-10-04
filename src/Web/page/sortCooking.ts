// The food sort in the cooking window (design D4): the container grid is createGridBag of webui-bag.js (the frame
// global backpack). A wrapper of refresh gives each new items array of a game message its sorted places and draws
// the grid again; a refresh that comes during a drag waits in the grid, and the wrapper of _flushPendingRefresh
// sorts it when the drag ends. After a drop the drag coordinator puts the cell at the drop place by hand, so the
// wrapper of updateItemPosition draws the sorted places again once the drop is done. The badges go on in
// onItemRendered of the grid, so each draw of the game keeps them. The workbench (the pot grid) shows the numbers of
// the choice on its cells too, so the player can compare them with the container tab; its cells keep their places
// and are not dimmed. The sort data of the container tab also holds the numbers of the workbench items.
// The Rat Cage window has the same shape (design D12): its Food Storage grid (the frame global foodBag) takes the
// place of the workbench, the dropdown goes on a line of the mod under the Leave buttons, Satiety orders by the cage
// satiety, and it has no dim of uncookable items.
import { addError, view } from './core';
import { dimOn, isUncookable, takesFuel } from './dimUncookable';
import type { SortedGrid } from './dropFilter';
import { CHOICE, column, order, pack } from './sortOrder';
import { drawCells, drawDropdown, removeDropdown } from './sortUi';
import type { BagGrid, BagItemData, GridSortWindow } from './types';

type State = NonNullable<GridSortWindow['__projectCookCookSort']>;

// The class of the line of the dropdown in the Rat Cage window.
const LINE = 'projectcook-sort-line';

function isRatCage(w: GridSortWindow): boolean {
  return !w.pot && !!w.foodBag;
}

// The grid that shows the numbers in place: the workbench, or the Food Storage grid of the cage.
function secondGrid(w: GridSortWindow): BagGrid | undefined {
  return w.pot || w.foodBag;
}

// The column of the numbers of the choice in this window.
function choiceOf(w: GridSortWindow): number {
  return column(view.choice, isRatCage(w));
}

function sortOf(grid: BagGrid) {
  const sort = view.sort;
  return sort && sort.owner === String(grid.getOwnerId()) ? sort : null;
}

// Writes the places of the choice into the items of the grid (the real places with Default, with no sort data, or
// when the items do not fit), and draws the grid again when a place or the look changed.
function place(w: GridSortWindow, state: State): void {
  const grid = w.backpack;
  const items = grid.getItems();
  if (state.items !== items) {
    state.items = items;
    state.real = new Map(items.map((it) => [String(it.id), [it.x || 0, it.y || 0] as [number, number]]));
  }
  const sort = sortOf(grid);
  const choice = choiceOf(w);
  let places = state.real;
  let dim = new Set<number | string>();
  state.sorted = false;
  if (sort && choice !== CHOICE.default) {
    const gridItems = items.map((it) => ({ id: String(it.id), w: it.w || 1, h: it.h || 1 }));
    const r = order(gridItems, sort, choice);
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
  const look = (sort ? sort.owner : '') + '|' + choice + '|' + [...dim].join(',') + '|' + dimOn();
  // New sort data can change a number with the same order, so it draws the badges of both grids again.
  const fresh = state.sortSeen !== view.sort;
  state.sortSeen = view.sort;
  if (changed || fresh || look !== state.drawn) {
    state.drawn = look;
    grid.renderItems();
    const second = secondGrid(w);
    if (second) second.renderItems();
  }
}

function install(w: GridSortWindow): State {
  const state: State = { items: null, real: new Map(), dim: new Set(), drawn: '', sorted: false, sortSeen: undefined };
  w.__projectCookCookSort = state;
  const grid = w.backpack;
  // The Rat Cage window has its Food and Rat tags at the top left of a cell, so its badges go to the top right.
  if (isRatCage(w)) w.document.documentElement.classList.add('projectcook-ratcage');

  const cfg = grid.getConfig();
  const rendered = cfg.onItemRendered;
  cfg.onItemRendered = function (el: HTMLElement, itemData: BagItemData) {
    if (rendered) rendered(el, itemData);
    try {
      const id = String(itemData.id);
      const always = !isRatCage(w) && dimOn() && isUncookable(itemData, takesFuel(w)) ? new Set<number | string>([id]) : undefined;
      drawCells(w.document, [el], [id], sortOf(grid), choiceOf(w), state.dim, undefined, always);
    } catch (e) { addError(w, 'cookingSort badge: ' + e); }
  };

  const second = secondGrid(w);
  if (second) {
    const potCfg = second.getConfig();
    const potRendered = potCfg.onItemRendered;
    const noDim = new Set<number | string>();
    potCfg.onItemRendered = function (el: HTMLElement, itemData: BagItemData) {
      if (potRendered) potRendered(el, itemData);
      try { drawCells(w.document, [el], [String(itemData.id)], sortOf(grid), choiceOf(w), noDim); } catch (e) { addError(w, 'cookingSort pot badge: ' + e); }
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
    if (sortOf(grid) && choiceOf(w) !== CHOICE.default) {
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

// The toolbar of the dropdown: the toolbar of the container grid in the cooking window, and in the Rat Cage window
// a line of the mod right after the Leave All and Leave by Type buttons (.fill-row), made when it is missing.
function toolbarOf(w: GridSortWindow): Element | null {
  const doc = w.document;
  if (!isRatCage(w)) return doc.querySelector('.bag-toolbar');
  const row = doc.querySelector('.fill-row');
  if (!row) return null;
  let line = row.nextElementSibling;
  if (!line || !line.classList.contains(LINE)) {
    line = doc.createElement('div');
    line.className = LINE;
    row.after(line);
  }
  return line;
}

// The dropdown in the toolbar of the container grid, for the owner of the open tab.
function drawToolbar(w: GridSortWindow): void {
  const toolbar = toolbarOf(w);
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

// One pass: the wrappers once, the places, and the dropdown. For the cooking window and the Rat Cage window.
export function applyCookingSort(w: GridSortWindow): void {
  const state = w.__projectCookCookSort || install(w);
  place(w, state);
  drawToolbar(w);
}

// The container tab of the frame for the drop filter while it shows the packed places, else null.
export function cookingGrid(w: GridSortWindow): SortedGrid | null {
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
      const second = secondGrid(w);
      const it = second && second.getItems().find((i) => String(i.id) === id);
      return it ? [it.w || 1, it.h || 1] : null;
    },
  };
}
