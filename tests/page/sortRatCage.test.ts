// The food sort in the Rat Cage window, against the game's own RatCage.html and webui-bag.js in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { applyCookingSort, cookingGrid } from '../../src/Web/page/sortCooking';
import type { SortData } from '../../src/Web/page/types';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const RAT_CAGE_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'RatCage', 'RatCage.html');
const gameFilesExist = fs.existsSync(RAT_CAGE_HTML);

const WORDS = {
  choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'],
  expired: 'Expired',
  sort: 'Sort',
};
const OWNER = 4294986608;
const CAGE = 4294993200;

// A Watermelon (eat value 12, cage satiety 60), a Hardtack with 2 of 10 uses left (eat value 20, cage satiety 40), and
// a tool (none). The Food Storage grid of the cage holds a Pumpkin (eat value 7, cage satiety 35). The eat values
// differ from the cage satiety, so a test sees which number the sort uses.
function sortData(owner: number): SortData {
  return {
    owner: String(owner),
    words: WORDS,
    items: {
      1: { n: [12, null, null, null, 6, 9, 60], d: '9d' },
      2: { n: [20, null, null, null, 4, 300, 40], d: '300d' },
      3: { n: [null, null, null, null, 12, null, null], d: null },
      9: { n: [7, null, null, null, 5, 8, 35], d: '8d' },
    },
  };
}

function backendItems() {
  return [
    { itemId: 3, x: 0, y: 0, w: 1, h: 1, name: 'Crowbar', configId: 3001 },
    { itemId: 2, x: 1, y: 0, w: 1, h: 1, name: 'Hardtack', configId: 2002, satiety: 40 },
    { itemId: 1, x: 3, y: 2, w: 1, h: 1, name: 'Watermelon', configId: 2001, satiety: 60 },
  ];
}

async function ratCageWindow(t: TestContext): Promise<Win> {
  const dom = new JSDOM(fs.readFileSync(RAT_CAGE_HTML, 'utf8'), {
    url: url.pathToFileURL(RAT_CAGE_HTML).href,
    runScripts: 'dangerously',
    resources: 'usable',
    pretendToBeVisual: true,
  });
  t.onTestFinished(() => dom.window.close());
  await new Promise<void>((resolve, reject) => {
    dom.window.addEventListener('load', () => resolve());
    setTimeout(() => reject(new Error('RatCage.html did not fire load within 5s')), 5000);
  });
  const w = dom.window as Win;
  w.backpack.setOwnerId(OWNER);
  w.backpack.refresh(backendItems());
  w.foodBag.setOwnerId(CAGE);
  w.foodBag.refresh([{ itemId: 9, x: 2, y: 1, w: 1, h: 1, name: 'Pumpkin', configId: 2003, satiety: 35 }]);
  return w;
}

const line = (w: Win) => w.document.querySelector('.projectcook-sort-line');
const place = (grid: any, id: number) => {
  const it = grid.getItems().find((i: any) => i.id === String(id));
  return [it.x, it.y];
};
const cell = (w: Win, id: number) => w.document.getElementById(String(id)) as HTMLElement;
const badge = (w: Win, id: number) => cell(w, id).querySelector('.projectcook-sort-badge') as HTMLElement | null;

function select(w: Win, index: number): void {
  const opt = line(w).querySelectorAll('.projectcook-sort-opt')[index] as HTMLElement;
  opt.dispatchEvent(new w.MouseEvent('click', { bubbles: true }));
}

beforeEach(() => {
  view.sort = null;
  view.choice = 0;
  view.data = { tips: {}, tiers: {}, features: ['dimUncookable'] };
});

test.skipIf(!gameFilesExist)('rat cage sort: the dropdown is on its own line under the Leave buttons, at the left, with the seven choices', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  applyCookingSort(w);
  const fillRow = w.document.querySelector('.fill-row');
  assert.equal(fillRow.nextElementSibling, line(w), 'the line is not right after the Leave buttons');
  assert.equal(line(w).firstElementChild.className, 'projectcook-sort');
  assert.equal(fillRow.querySelector('.projectcook-sort'), null);
  assert.deepEqual([...fillRow.children].map((c: any) => c.id), ['fillAllBtn', 'fillFoodBtn']);
  const opts = line(w).querySelectorAll('.projectcook-sort-opt');
  assert.equal(opts.length, 7);
});

test.skipIf(!gameFilesExist)('rat cage sort: the window gets the class that puts the badges at the top right, away from the game tags', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  applyCookingSort(w);
  assert.ok(w.document.documentElement.classList.contains('projectcook-ratcage'));
});

test.skipIf(!gameFilesExist)('rat cage sort: no dropdown when the sort data names another owner', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(7);
  applyCookingSort(w);
  assert.equal(w.document.querySelector('.projectcook-sort'), null);
});

test.skipIf(!gameFilesExist)('rat cage sort: Satiety sorts by the cage satiety, highest first, in green with a sign, and the tool last and dimmed', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  applyCookingSort(w);
  select(w, 1);
  assert.equal(view.choice, 1);
  assert.deepEqual(place(w.backpack, 1), [0, 0]);
  assert.deepEqual(place(w.backpack, 2), [1, 0]);
  assert.deepEqual(place(w.backpack, 3), [2, 0]);
  assert.equal(badge(w, 1)!.textContent, '+60');
  assert.ok(badge(w, 1)!.classList.contains('projectcook-sort-pos'));
  assert.equal(badge(w, 2)!.textContent, '+40');
  assert.equal(badge(w, 3), null);
  assert.ok(cell(w, 3).classList.contains('projectcook-sort-dim'));
  assert.match(line(w).querySelector('.projectcook-sort-btn .projectcook-sort-text').textContent, /^Satiety/);
  assert.deepEqual(cookingGrid(w)!.items.find((i) => i.id === '1'), { id: '1', x: 3, y: 2, w: 1, h: 1 });

  select(w, 0);
  assert.deepEqual(place(w.backpack, 1), [3, 2]);
  assert.equal(w.document.querySelector('#backpackGrid .projectcook-sort-badge'), null);
});

test.skipIf(!gameFilesExist)('rat cage sort: the Food Storage grid shows the numbers in place, with no dim', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  view.choice = 1;
  applyCookingSort(w);
  assert.equal(badge(w, 9)!.textContent, '+35');
  assert.deepEqual(place(w.foodBag, 9), [2, 1]);
  assert.ok(!cell(w, 9).classList.contains('projectcook-sort-dim'));

  view.choice = 3;
  applyCookingSort(w);
  assert.equal(badge(w, 9), null);
  assert.ok(!cell(w, 9).classList.contains('projectcook-sort-dim'));
});

test.skipIf(!gameFilesExist)('rat cage sort: the dim of uncookable items stays a cooking window part', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  applyCookingSort(w);
  assert.equal(w.document.querySelector('.projectcook-sort-dim'), null);
});

test.skipIf(!gameFilesExist)('rat cage sort: the other choices use the same numbers as the other windows', async (t) => {
  const w = await ratCageWindow(t);
  view.sort = sortData(OWNER);
  view.choice = 5;
  applyCookingSort(w);
  assert.deepEqual(place(w.backpack, 3), [0, 0]);
  assert.equal(badge(w, 3)!.textContent, '12');
  assert.equal(badge(w, 9)!.textContent, '5');
});
