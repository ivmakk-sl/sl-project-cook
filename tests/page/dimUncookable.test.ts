// The dim of uncookable items in the container tab of the cooking window, against the game's own Cooking.html and
// webui-bag.js in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { applyCookingSort } from '../../src/Web/page/sortCooking';
import { PREFIX } from '../../src/Web/page/sortUi';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);
const OWNER = 4294977025;
const DIM = PREFIX + '-dim';

// Cabbage, Beef Slices (a product), a Wild Rabbit that needs cutting, and a Crowbar (not food).
function backendItems() {
  return [
    { itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Cabbage', configId: 2001, category: 1, canCook: true, needCut: false },
    { itemId: 2, x: 1, y: 0, w: 1, h: 1, name: 'Beef Slices', configId: 2002, category: 1, canCook: false, needCut: false },
    { itemId: 3, x: 2, y: 0, w: 1, h: 1, name: 'Rabbit', configId: 2003, category: 1, canCook: true, needCut: true },
    { itemId: 4, x: 3, y: 0, w: 1, h: 1, name: 'Crowbar', configId: 3001, category: 3 },
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

const dimmed = (w: Win) => [1, 2, 3, 4].filter((id) => w.document.getElementById(String(id)).classList.contains(DIM));

beforeEach(() => {
  view.sort = null;
  view.choice = 0;
  view.data = { tips: {}, tiers: {}, features: ['dimUncookable'] };
});

test.skipIf(!gameFilesExist)('dim: a product, an item that needs cutting, and an item that is not food are dimmed', async (t) => {
  const w = await cookingWindow(t);
  applyCookingSort(w);
  assert.deepEqual(dimmed(w), [2, 3, 4]);
});

test.skipIf(!gameFilesExist)('dim: an uncookable item stays dimmed with a food sort that gives it a number', async (t) => {
  const w = await cookingWindow(t);
  view.sort = {
    owner: String(OWNER),
    words: { choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'], expired: 'Expired', sort: 'Sort' },
    items: {
      1: { n: [5, null, null, null, 3, 2], d: null },
      2: { n: [30, null, null, null, 10, 4], d: null },
      3: { n: [20, null, null, null, 1, null], d: null },
      4: { n: [null, null, null, null, 1, null], d: null },
    },
  };
  view.choice = 1;
  applyCookingSort(w);
  assert.deepEqual(dimmed(w), [2, 3, 4]);
});

test.skipIf(!gameFilesExist)('dim: with the switch off no item is dimmed', async (t) => {
  view.data = { tips: {}, tiers: {} };
  const w = await cookingWindow(t);
  applyCookingSort(w);
  assert.deepEqual(dimmed(w), []);
});

test.skipIf(!gameFilesExist)('dim: the data with the switch on draws the grid again', async (t) => {
  view.data = { tips: {}, tiers: {} };
  const w = await cookingWindow(t);
  applyCookingSort(w);
  view.data = { tips: {}, tiers: {}, features: ['dimUncookable'] };
  applyCookingSort(w);
  assert.deepEqual(dimmed(w), [2, 3, 4]);
});

test.skipIf(!gameFilesExist)('dim: the cooking station cells are not dimmed', async (t) => {
  const w = await cookingWindow(t);
  w.pot.refresh([{ itemId: 9, x: 0, y: 0, w: 1, h: 1, name: 'Cabbage', configId: 2001, category: 1, canCook: true }]);
  applyCookingSort(w);
  assert.equal(w.document.getElementById('9').classList.contains(DIM), false);
});

// Scrap Paper burns (the fuel of a stove) and is not food.
const scrapPaper = { itemId: 5, x: 4, y: 0, w: 1, h: 1, name: 'Scrap Paper', configId: 4001, category: 9, canCook: false, burnable: true };

test.skipIf(!gameFilesExist)('dim: a fuel item is not dimmed at a stove that burns fuel', async (t) => {
  const w = await cookingWindow(t);
  w.cookingConfig.cookType = 1;
  w.cookingConfig.fuelSlotCount = 5;
  w.backpack.refresh([...backendItems(), scrapPaper]);
  applyCookingSort(w);
  assert.equal(w.document.getElementById('5').classList.contains(DIM), false);
  assert.deepEqual(dimmed(w), [2, 3, 4]);
});

test.skipIf(!gameFilesExist)('dim: a fuel item is dimmed at an electric stove and at a stove with no fuel slot', async (t) => {
  const w = await cookingWindow(t);
  w.cookingConfig.cookType = 2;
  w.cookingConfig.fuelSlotCount = 0;
  w.backpack.refresh([...backendItems(), scrapPaper]);
  applyCookingSort(w);
  assert.equal(w.document.getElementById('5').classList.contains(DIM), true);

  w.cookingConfig.cookType = 3;
  w.backpack.refresh([...backendItems(), scrapPaper]);
  assert.equal(w.document.getElementById('5').classList.contains(DIM), true);
});
