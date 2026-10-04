// Separate pieces in the cooking window: a drop on an item of the same kind on the cooking station, against the
// game's own Cooking.html and webui-bag.js in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { installPieces } from '../../src/Web/page/pieces';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);

const RABBIT = 30014, CABBAGE = 2001;

const piece = (itemId: number, x: number, y: number, useTimes: number, configId = RABBIT, useTimesMax = 3) =>
  ({ itemId, x, y, w: 1, h: 1, name: 'R', configId, useTimes, useTimesMax, category: 1 });

async function cookingWindow(t: TestContext, potItems: unknown[], bagItems: unknown[]): Promise<Win> {
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
  w.pot.refresh(potItems);
  w.backpack.refresh(bagItems);
  installPieces(w);
  return w;
}

// The check of the drop of the dragged item at one cell of the cooking station, as the drag of webui-bag.js calls it.
const drop = (w: Win, col: number, row: number, draggedId: string, size = [1, 1]) =>
  w.pot.checkValidity(w.pot.getOccupiedSlots(row, col, size[0], size[1]), draggedId);

beforeEach(() => {
  view.data = { tips: {}, tiers: {}, features: ['separatePieces'] };
});

test.skipIf(!gameFilesExist)('pieces: a drop on a piece of the same kind with free uses is accepted', async (t) => {
  const w = await cookingWindow(t, [piece(10, 0, 0, 1)], [piece(20, 0, 0, 2)]);
  assert.equal(drop(w, 0, 0, '20'), true);
});

test.skipIf(!gameFilesExist)('pieces: a drop on another kind, a full item, or two items is refused', async (t) => {
  const w = await cookingWindow(t,
    [piece(10, 0, 0, 1, CABBAGE), piece(11, 1, 0, 3), piece(12, 2, 0, 1), piece(13, 3, 0, 1)],
    [piece(20, 0, 0, 2)]);
  assert.equal(drop(w, 0, 0, '20'), false);
  assert.equal(drop(w, 1, 0, '20'), false);
  assert.equal(drop(w, 2, 0, '20', [2, 1]), false);
});

test.skipIf(!gameFilesExist)('pieces: an item with no uses keeps the game rule', async (t) => {
  const w = await cookingWindow(t, [piece(10, 0, 0, 0, CABBAGE, 0)], [piece(20, 0, 0, 0, CABBAGE, 0)]);
  assert.equal(drop(w, 0, 0, '20'), false);
});

test.skipIf(!gameFilesExist)('pieces: an empty cell, a cell outside the grid, and a call with no dragged item keep the game rule', async (t) => {
  const w = await cookingWindow(t, [piece(10, 0, 0, 1)], [piece(20, 0, 0, 2)]);
  assert.equal(drop(w, 1, 0, '20'), true);
  assert.equal(drop(w, -1, 0, '20'), false);
  assert.equal(drop(w, 0, 0, '-1'), false);
});

test.skipIf(!gameFilesExist)('pieces: with the switch off, a drop on a piece is refused', async (t) => {
  view.data = { tips: {}, tiers: {} };
  const w = await cookingWindow(t, [piece(10, 0, 0, 1)], [piece(20, 0, 0, 2)]);
  assert.equal(drop(w, 0, 0, '20'), false);
});

test.skipIf(!gameFilesExist)('pieces: a second install keeps one wrapper', async (t) => {
  const w = await cookingWindow(t, [piece(10, 0, 0, 1)], [piece(20, 0, 0, 2)]);
  const wrapped = w.pot.checkValidity;
  installPieces(w);
  assert.equal(w.pot.checkValidity, wrapped);
});
