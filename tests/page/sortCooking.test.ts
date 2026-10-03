// The food sort in the cooking window, against the game's own Cooking.html and webui-bag.js in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { applyCookingSort } from '../../src/Web/page/sortCooking';
import type { SortData } from '../../src/Web/page/types';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');

const WORDS = { choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'], expired: 'Expired', sort: 'Sort' };
const OWNER = 4294986608;

function sortData(owner: number): SortData {
  return {
    owner: String(owner),
    words: WORDS,
    items: {
      1: { n: [5, null, null, null, 3, 2], d: '2d' },
      2: { n: [30, null, null, null, 10, 4], d: '4d' },
      3: { n: [null, null, null, null, 1, null], d: null },
    },
  };
}

// The game's items of the fridge tab in their real places.
function backendItems() {
  return [
    { itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'A', configId: 555 },
    { itemId: 2, x: 4, y: 2, w: 1, h: 1, name: 'B', configId: 556 },
    { itemId: 3, x: 1, y: 0, w: 1, h: 1, name: 'C', configId: 557 },
  ];
}

async function cookingWindow(t: TestContext): Promise<Win> {
  const dom = new JSDOM(fs.readFileSync(COOKING_HTML, 'utf8'), {
    url: url.pathToFileURL(COOKING_HTML).href,
    runScripts: 'dangerously',
    resources: 'usable',
    pretendToBeVisual: true,
  });
  t.onTestFinished(() => dom.window.close());
  await new Promise<void>((resolve, reject) => {
    dom.window.addEventListener('load', () => resolve());
    setTimeout(() => reject(new Error('Cooking.html did not fire load within 5s')), 5000);
  });
  const w = dom.window as Win;
  w.backpack.setOwnerId(OWNER);
  w.backpack.refresh(backendItems());
  return w;
}

const place = (w: Win, id: number) => {
  const it = w.backpack.getItems().find((i: any) => i.id === String(id));
  return [it.x, it.y];
};
const cell = (w: Win, id: number) => w.document.getElementById(String(id)) as HTMLElement;
const toolbar = (w: Win) => w.document.querySelector('.bag-toolbar');

function select(w: Win, index: number): void {
  const opt = toolbar(w).querySelectorAll('.projectcook-sort-opt')[index] as HTMLElement;
  opt.dispatchEvent(new w.MouseEvent('click', { bubbles: true }));
}

const gameFilesExist = fs.existsSync(COOKING_HTML);

beforeEach(() => {
  view.sort = null;
  view.choice = 0;
});

test.skipIf(!gameFilesExist)('cooking sort: the dropdown shows only for the owner of the sort data', async (t) => {
  const w = await cookingWindow(t);
  view.sort = sortData(7);
  applyCookingSort(w);
  assert.equal(toolbar(w).querySelector('.projectcook-sort'), null);

  view.sort = sortData(OWNER);
  applyCookingSort(w);
  assert.ok(toolbar(w).querySelector('.projectcook-sort'));
});

test.skipIf(!gameFilesExist)('cooking sort: Satiety packs the cells, badges them, and Default puts the real places back', async (t) => {
  const w = await cookingWindow(t);
  view.sort = sortData(OWNER);
  applyCookingSort(w);
  select(w, 1);

  assert.deepEqual(place(w, 2), [0, 0]);
  assert.deepEqual(place(w, 1), [1, 0]);
  assert.deepEqual(place(w, 3), [2, 0]);
  assert.equal(cell(w, 2).querySelector('.projectcook-sort-badge')!.textContent, '+30');
  assert.ok(cell(w, 3).classList.contains('projectcook-sort-dim'));

  select(w, 0);
  assert.deepEqual(place(w, 2), [4, 2]);
  assert.equal(w.document.querySelector('#backpackGrid .projectcook-sort-badge'), null);
});

test.skipIf(!gameFilesExist)('cooking sort: a refresh of the game gets the sorted places', async (t) => {
  const w = await cookingWindow(t);
  view.sort = sortData(OWNER);
  view.choice = 5;
  applyCookingSort(w);
  w.backpack.refresh(backendItems());
  assert.deepEqual(place(w, 2), [0, 0]);
  assert.ok(cell(w, 2).querySelector('.projectcook-sort-badge'), 'the new cells have no badge');
});

test.skipIf(!gameFilesExist)('cooking sort: a refresh during a drag is sorted when the drag ends', async (t) => {
  const w = await cookingWindow(t);
  view.sort = sortData(OWNER);
  view.choice = 1;
  applyCookingSort(w);
  w.backpack._beginDragCache();
  w.backpack.refresh(backendItems().slice(0, 2));
  assert.equal(w.backpack.getItems().length, 3, 'the held refresh changed the items');
  w.backpack._flushPendingRefresh();
  assert.equal(w.backpack.getItems().length, 2);
  assert.deepEqual(place(w, 2), [0, 0]);
  assert.deepEqual(place(w, 1), [1, 0]);
});

test.skipIf(!gameFilesExist)('cooking sort: a drop inside the sorted grid gets the packed place back', async (t) => {
  const w = await cookingWindow(t);
  view.sort = sortData(OWNER);
  view.choice = 1;
  applyCookingSort(w);
  w.backpack.updateItemPosition('2', 'backpackGrid', 5, 4);
  await new Promise((r) => setTimeout(r, 10));
  assert.deepEqual(place(w, 2), [0, 0]);
});

test.skipIf(!gameFilesExist)('cooking sort: a tab switch that comes after the sort data draws the dropdown', async (t) => {
  const w = await cookingWindow(t);
  w.backpack.setOwnerId(7);
  view.sort = sortData(OWNER);
  view.choice = 1;
  applyCookingSort(w);
  assert.equal(toolbar(w).querySelector('.projectcook-sort'), null);

  w.backpack.setOwnerId(OWNER);
  w.backpack.refresh(backendItems());
  assert.ok(toolbar(w).querySelector('.projectcook-sort'), 'no dropdown after the tab switch');
  assert.equal(toolbar(w).querySelector('.projectcook-sort').nextElementSibling.id, 'sortBagBtn');
});

test.skipIf(!gameFilesExist)('cooking sort: the workbench shows the numbers of the choice in place, with no sort and no dim', async (t) => {
  const w = await cookingWindow(t);
  w.pot.setOwnerId(99);
  w.pot.refresh([
    { itemId: 9, x: 2, y: 0, w: 1, h: 1, name: 'D', configId: 558 },
    { itemId: 8, x: 0, y: 1, w: 1, h: 1, name: 'E', configId: 559 },
  ]);
  const data = sortData(OWNER);
  data.items[9] = { n: [12, null, null, null, 4, 3], d: '3d' };
  data.items[8] = { n: [null, null, null, null, 2, null], d: null };
  view.sort = data;
  applyCookingSort(w);
  assert.equal(w.document.querySelector('#mainPotGrid .projectcook-sort-badge'), null);

  select(w, 1);
  assert.equal(cell(w, 9).querySelector('.projectcook-sort-badge')!.textContent, '+12');
  assert.equal(cell(w, 8).querySelector('.projectcook-sort-badge'), null);
  assert.ok(!cell(w, 8).classList.contains('projectcook-sort-dim'));
  const potPlace = (id: number) => {
    const it = w.pot.getItems().find((i: any) => i.id === String(id));
    return [it.x, it.y];
  };
  assert.deepEqual(potPlace(9), [2, 0]);
  assert.deepEqual(potPlace(8), [0, 1]);

  // New numbers of the same order draw the new badge.
  const next = sortData(OWNER);
  next.items[9] = { n: [15, null, null, null, 4, 3], d: '3d' };
  view.sort = next;
  applyCookingSort(w);
  assert.equal(cell(w, 9).querySelector('.projectcook-sort-badge')!.textContent, '+15');

  select(w, 0);
  assert.equal(w.document.querySelector('#mainPotGrid .projectcook-sort-badge'), null);
});
