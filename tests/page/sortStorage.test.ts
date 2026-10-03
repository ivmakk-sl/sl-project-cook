// The food sort in the storage window, against a fixture of its parts: the storage side (.bag-panel.is-storage)
// with its grid and toolbar, and a fake Vue component whose watch runs at once, as flush 'sync' does.
import assert from 'node:assert/strict';
import { JSDOM } from 'jsdom';
import { beforeEach, test } from 'vitest';
import { view } from '../../src/Web/page/core';
import { applyStorageSort, storageGrids } from '../../src/Web/page/sortStorage';
import type { SortData } from '../../src/Web/page/types';

type Win = any;

const WORDS = { choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'], expired: 'Expired', sort: 'Sort' };

function sortData(owner: string, bag?: string): SortData {
  return {
    owner,
    bag,
    words: WORDS,
    items: {
      1: { n: [5, null, null, null, 3, 2], d: '2d' },
      2: { n: [30, null, null, null, 10, 4], d: '4d' },
      3: { n: [null, null, null, null, 1, null], d: null },
      11: { n: [-2, null, null, null, 1, 9], d: '9d' },
      12: { n: [8, null, null, null, 2, 1], d: '1d' },
    },
  };
}

// The game's items in their real places, in a 3 x 2 grid.
function realItems() {
  return [
    { id: 1, x: 0, y: 0, w: 1, h: 1 },
    { id: 2, x: 2, y: 1, w: 1, h: 1 },
    { id: 3, x: 1, y: 0, w: 1, h: 1 },
  ];
}

function storageWindow(): Win {
  const dom = new JSDOM(`<!doctype html><html><body><div id="app">
    <div class="bag-panel"><div class="grid-container"><div class="item"></div><div class="item"></div></div>
      <div class="toolbar"><button class="action-btn discard-btn">Discard All</button><button class="action-btn">Auto Organize</button></div></div>
    <div class="bag-panel is-storage"><div class="grid-container"><div class="item"></div><div class="item"></div><div class="item"></div></div>
      <div class="toolbar"><button class="action-btn">Auto Organize</button></div></div>
  </div></body></html>`);
  const w: Win = dom.window;
  const watchers: (() => void)[] = [];
  const bagB = { ownerId: 42, gridCols: 3, gridRows: 2, items: realItems() };
  const bagA = { ownerId: 5, gridCols: 2, gridRows: 1, items: [{ id: 11, x: 0, y: 0, w: 1, h: 1 }, { id: 12, x: 1, y: 0, w: 1, h: 1 }] };
  w.Vue = { watch: (_src: () => unknown, cb: () => void) => { watchers.push(cb); } };
  w.document.getElementById('app')._vnode = { component: { setupState: { bagA, bagB, isDualBag: true } } };
  // A new items array from a game message: the watchers run before the page draws it.
  w.__setItems = (items: unknown[]) => { bagB.items = items as any; watchers.forEach((cb) => cb()); };
  w.__bagB = bagB;
  w.__bagA = bagA;
  return w;
}

const toolbarA = (w: Win) => w.document.querySelector('.bag-panel:not(.is-storage) .toolbar');
const placeA = (w: Win, id: number) => { const it = w.__bagA.items.find((i: any) => i.id === id); return [it.x, it.y]; };

test('storage sort: the Backpack side gets the dropdown and the sort when the sort data names it', () => {
  const w = storageWindow();
  view.sort = sortData('42', '5');
  applyStorageSort(w);
  const root = toolbarA(w).querySelector('.projectcook-sort');
  assert.ok(root, 'no dropdown on the Backpack side');
  // At the left of the line: before the game's Discard All.
  assert.ok(root.nextElementSibling.classList.contains('discard-btn'));

  select(w, 1);
  assert.deepEqual(placeA(w, 12), [0, 0]);
  assert.deepEqual(placeA(w, 11), [1, 0]);
  const badge = w.document.querySelector('.bag-panel:not(.is-storage) .projectcook-sort-layer [data-id="11"]');
  assert.equal(badge.textContent, '-2');
  assert.ok(badge.classList.contains('projectcook-sort-neg'));
  assert.match(toolbarA(w).querySelector('.projectcook-sort-btn').textContent, /Satiety/);

  const grids = storageGrids(w);
  assert.deepEqual(grids.map((g) => g.owner).sort(), ['42', '5']);
  const bagGrid = grids.find((g) => g.owner === '5')!;
  assert.deepEqual(bagGrid.items.find((i) => i.id === '12'), { id: '12', x: 1, y: 0, w: 1, h: 1 });
  assert.deepEqual(bagGrid.sizeOf('2'), [1, 1]);

  select(w, 0);
  assert.deepEqual(placeA(w, 11), [0, 0]);
  assert.equal(storageGrids(w).length, 0);
});

test('storage sort: a storage opened alone (one grid, on side A) gets the dropdown and the sort', () => {
  const w = storageWindow();
  const state = w.document.getElementById('app')._vnode.component.setupState;
  state.isDualBag = false;
  w.__bagA.ownerId = 42;
  view.sort = sortData('42', '5');
  view.choice = 1;
  applyStorageSort(w);
  assert.ok(toolbarA(w).querySelector('.projectcook-sort'), 'no dropdown on the single grid');
  assert.deepEqual(placeA(w, 12), [0, 0]);
  assert.deepEqual(placeA(w, 11), [1, 0]);
  assert.deepEqual(storageGrids(w).map((g) => g.owner), ['42']);

  // A storage that is not sortable, opened alone: the sort data names another owner.
  w.__bagA.ownerId = 77;
  applyStorageSort(w);
  assert.equal(toolbarA(w).querySelector('.projectcook-sort'), null);
  assert.equal(storageGrids(w).length, 0);
});

test('storage sort: an ordinary storage after a sortable one keeps the Backpack side unsorted', () => {
  const w = storageWindow();
  // The sort data of the last sortable storage (42) still names the Backpack, but the open storage is another one.
  view.sort = sortData('42', '5');
  w.__bagB.ownerId = 77;
  view.choice = 1;
  applyStorageSort(w);
  assert.equal(toolbarA(w).querySelector('.projectcook-sort'), null);
  assert.equal(toolbarB(w).querySelector('.projectcook-sort'), null);
  assert.deepEqual(placeA(w, 11), [0, 0]);
  assert.equal(w.document.querySelector('.bag-panel:not(.is-storage) .projectcook-sort-badge'), null);
  assert.equal(storageGrids(w).length, 0);
});

test('storage sort: without the Backpack in the sort data, its side has no dropdown and keeps its places', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  applyStorageSort(w);
  select(w, 1);
  assert.equal(toolbarA(w).querySelector('.projectcook-sort'), null);
  assert.deepEqual(placeA(w, 11), [0, 0]);
  assert.deepEqual(storageGrids(w).map((g) => g.owner), ['42']);
});

const toolbarB = (w: Win) => w.document.querySelector('.bag-panel.is-storage .toolbar');
const cellsB = (w: Win) => [...w.document.querySelectorAll('.bag-panel.is-storage .grid-container > .item')] as HTMLElement[];
const place = (w: Win, id: number) => { const it = w.__bagB.items.find((i: any) => i.id === id); return [it.x, it.y]; };

function select(w: Win, index: number): void {
  const opt = toolbarB(w).querySelectorAll('.projectcook-sort-opt')[index] as HTMLElement;
  opt.dispatchEvent(new w.MouseEvent('click', { bubbles: true }));
}

beforeEach(() => {
  view.sort = null;
  view.choice = 0;
});

test('storage sort: a dropdown in the toolbar of the storage side only when the sort data names its owner', () => {
  const w = storageWindow();
  view.sort = sortData('7');
  applyStorageSort(w);
  assert.equal(w.document.querySelector('.projectcook-sort'), null);

  view.sort = sortData('42');
  applyStorageSort(w);
  const root = toolbarB(w).querySelector('.projectcook-sort');
  assert.ok(root, 'no dropdown on the storage side');
  assert.equal(w.document.querySelectorAll('.projectcook-sort').length, 1);
  assert.equal(toolbarB(w).querySelectorAll('.projectcook-sort-opt').length, 7);
  const btn = root.querySelector('.projectcook-sort-btn');
  assert.match(btn.textContent, /^Sort/);
  assert.equal(btn.querySelector('.projectcook-sort-ico').dataset.choice, 'sort');
  assert.ok(!btn.classList.contains('projectcook-sort-active'));
  assert.ok(toolbarB(w).querySelector('.action-btn'), 'the game button is gone');
});

test('storage sort: with a sort on, the button shows the icon and the word of the choice in its active look', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  applyStorageSort(w);
  select(w, 2);
  const btn = toolbarB(w).querySelector('.projectcook-sort-btn');
  assert.match(btn.querySelector('.projectcook-sort-text').textContent, /^Morale/);
  assert.equal(btn.querySelector('.projectcook-sort-ico').dataset.choice, '2');
  // The stats have the game's own icons, the glyphs of its item tooltip; the other choices a drawn icon.
  assert.equal(btn.querySelector('.projectcook-sort-ico').textContent, '🧠');
  assert.ok(btn.classList.contains('projectcook-sort-active'));
  const opts = toolbarB(w).querySelectorAll('.projectcook-sort-opt');
  assert.equal(opts[6].textContent, 'Expiration Date');
  assert.equal(opts[6].querySelector('.projectcook-sort-ico').dataset.choice, '6');
  assert.equal(opts[0].querySelector('.projectcook-sort-ico').dataset.choice, '0');
  assert.deepEqual([1, 2, 3, 4].map((i) => opts[i].querySelector('.projectcook-sort-ico').textContent), ['🍖', '🧠', '⚡', '❤️']);
  assert.ok(opts[6].querySelector('.projectcook-sort-ico svg'));

  select(w, 0);
  assert.match(btn.textContent, /^Sort/);
  assert.ok(!btn.classList.contains('projectcook-sort-active'));
});

test('storage sort: Satiety packs the items in its order, with badges and a dim class, and Default puts the real places back', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  applyStorageSort(w);
  select(w, 1);

  assert.deepEqual(place(w, 2), [0, 0]);
  assert.deepEqual(place(w, 1), [1, 0]);
  assert.deepEqual(place(w, 3), [2, 0]);
  const cells = cellsB(w);
  assert.equal(w.document.querySelector('.projectcook-sort-layer [data-id="1"]').textContent, '+5');
  assert.equal(w.document.querySelector('.projectcook-sort-layer [data-id="2"]').textContent, '+30');
  assert.ok(cells[2].classList.contains('projectcook-sort-dim'));
  assert.equal(view.choice, 1);

  select(w, 0);
  assert.deepEqual(place(w, 1), [0, 0]);
  assert.deepEqual(place(w, 2), [2, 1]);
  assert.deepEqual(place(w, 3), [1, 0]);
  assert.equal(w.document.querySelector('.projectcook-sort-badge'), null);
  assert.equal(w.document.querySelector('.projectcook-sort-dim'), null);
});

test('storage sort: a new items array of the game gets the sorted places at once', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  view.choice = 5;
  applyStorageSort(w);
  w.__setItems(realItems());
  assert.deepEqual(place(w, 2), [0, 0]);
  assert.deepEqual(place(w, 1), [1, 0]);
});

test('storage sort: the choice of the root page applies in a new window', () => {
  const first = storageWindow();
  view.sort = sortData('42');
  applyStorageSort(first);
  select(first, 6);

  const second = storageWindow();
  applyStorageSort(second);
  assert.match(toolbarB(second).querySelector('.projectcook-sort-btn').textContent, /^Expiration Date/);
  assert.deepEqual(place(second, 1), [0, 0]);
  assert.deepEqual(place(second, 2), [1, 0]);
});

test('storage sort: items that do not fit show the real places', () => {
  const w = storageWindow();
  w.__bagB.gridCols = 1;
  w.__bagB.gridRows = 1;
  view.sort = sortData('42');
  view.choice = 1;
  applyStorageSort(w);
  assert.deepEqual(place(w, 2), [2, 1]);
});

test('storage sort: the dropdown is at the left of the Auto Organize button, on its line', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  applyStorageSort(w);
  const button = toolbarB(w).querySelector('.action-btn');
  assert.equal(button.previousElementSibling.className, 'projectcook-sort');
  assert.equal(toolbarB(w).querySelector('.projectcook-sort-break'), null);
});

test('storage sort: with the robot switch, a break puts the dropdown on the line of the button', () => {
  const w = storageWindow();
  toolbarB(w).classList.add('has-rsw');
  toolbarB(w).insertAdjacentHTML('afterbegin', '<div class="rsw-group"></div>');
  view.sort = sortData('42');
  applyStorageSort(w);
  applyStorageSort(w);
  const root = toolbarB(w).querySelector('.projectcook-sort');
  assert.equal(root.previousElementSibling.className, 'projectcook-sort-break');
  assert.equal(root.nextElementSibling.className, 'action-btn');
  assert.equal(toolbarB(w).querySelectorAll('.projectcook-sort-break').length, 1);
});

test('storage sort: the badges are in a layer above the frost, at the places of their cells', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  view.choice = 1;
  applyStorageSort(w);
  const layer = w.document.querySelector('.bag-panel.is-storage .grid-container > .projectcook-sort-layer');
  assert.ok(layer, 'no badge layer');
  assert.equal(w.document.querySelector('.bag-panel.is-storage .item .projectcook-sort-badge'), null);
  const cells = cellsB(w);
  cells[1].style.left = '57px';
  cells[1].style.top = '3px';
  applyStorageSort(w);
  const badge = layer.querySelector('[data-id="2"]');
  assert.equal(badge.textContent, '+30');
  assert.equal(badge.style.left, '57px');
  assert.equal(badge.style.top, '3px');

  view.choice = 0;
  applyStorageSort(w);
  assert.equal(layer.querySelectorAll('.projectcook-sort-badge').length, 0);
});

test('storage sort: the badge tones: a positive stat green, the trade value gold, the days plain', () => {
  const w = storageWindow();
  view.sort = sortData('42');
  view.choice = 1;
  applyStorageSort(w);
  const badge = (id: number) => w.document.querySelector('.projectcook-sort-layer [data-id="' + id + '"]');
  assert.ok(badge(2).classList.contains('projectcook-sort-pos'));
  view.choice = 5;
  applyStorageSort(w);
  assert.ok(badge(2).classList.contains('projectcook-sort-gold'));
  assert.ok(!badge(2).classList.contains('projectcook-sort-pos'));
  view.choice = 6;
  applyStorageSort(w);
  assert.equal(badge(2).className, 'projectcook-sort-badge');
});

test('storage sort: an expired item has a red badge with Expiration Date', () => {
  const w = storageWindow();
  const data = sortData('42');
  data.items[1].n[5] = -99998;
  view.sort = data;
  view.choice = 6;
  applyStorageSort(w);
  const badge = w.document.querySelector('.projectcook-sort-layer [data-id="1"]');
  assert.ok(badge.classList.contains('projectcook-sort-neg'));
  assert.ok(!w.document.querySelector('.projectcook-sort-layer [data-id="2"]').classList.contains('projectcook-sort-neg'));
});

test('storage sort: after a choice change the badges follow the cells that Vue moves on its next tick', () => {
  const w = storageWindow();
  const ticks: (() => void)[] = [];
  w.Vue.nextTick = (fn: () => void) => { ticks.push(fn); };
  view.sort = sortData('42');
  applyStorageSort(w);
  select(w, 1);
  // Vue draws the new places of the items on its next tick.
  const cells = cellsB(w);
  w.__bagB.items.forEach((it: any, i: number) => { cells[i].style.left = (it.x * 50) + 'px'; cells[i].style.top = (it.y * 50) + 'px'; });
  ticks.forEach((fn) => fn());
  const badge = w.document.querySelector('.projectcook-sort-layer [data-id="2"]');
  assert.equal(badge.style.left, '0px');
  assert.equal(w.document.querySelector('.projectcook-sort-layer [data-id="1"]').style.left, '50px');
});
