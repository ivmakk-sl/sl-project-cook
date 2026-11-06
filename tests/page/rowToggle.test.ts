// The row split toggle in the line above the grid of the cooking station, against the game's own Cooking.html in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { drawRowToggle, ROW_TOGGLE_HIDDEN_IN_HOT_POT } from '../../src/Web/page/rowToggle';
import type { RowToggle } from '../../src/Web/page/types';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);

const toggle = (over: Partial<RowToggle> = {}): RowToggle => ({ show: true, on: true, label: 'Split by row', ...over });

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
  return dom.window as Win;
}

const box = (w: Win) => w.document.querySelector('#potCapacityBar .projectcook-row-toggle') as HTMLElement | null;
const isOn = (w: Win) => box(w)!.classList.contains('projectcook-row-toggle-is-on');

const sent: string[] = [];
beforeEach(() => {
  sent.length = 0;
  (globalThis as Win).window = { vuplex: { postMessage: (m: string) => sent.push(m) } };
  view.data = { tips: {}, tiers: {}, features: ['rowSplitToggle'], rowToggle: toggle() };
});

test.skipIf(!gameFilesExist)('row toggle: the label and the switch at the end of the bar, with no other text', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  assert.equal(w.document.getElementById('potCapacityBar').lastElementChild, box(w));
  assert.equal(box(w)!.textContent, 'Split by row');
  assert.ok(box(w)!.querySelector('.projectcook-row-toggle-switch'));
  assert.equal(isOn(w), true);
});

test.skipIf(!gameFilesExist)('row toggle: it is a capacity badge of the game, with the label of one', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  assert.ok(box(w)!.matches('.capacity-badge'));
  assert.ok(box(w)!.querySelector('.capacity-badge-label'));
});

test.skipIf(!gameFilesExist)('row toggle: off moves the switch', async (t) => {
  const w = await cookingWindow(t);
  view.data.rowToggle = toggle({ on: false });
  drawRowToggle(w);
  assert.equal(isOn(w), false);
});

test.skipIf(!gameFilesExist)('row toggle: a click posts one message to C#', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  box(w)!.dispatchEvent(new w.MouseEvent('click', { bubbles: true }));
  assert.deepEqual(sent, ['slmod|projectcook|rowsplit']);
});

test.skipIf(!gameFilesExist)('row toggle: a new state updates the same node', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  const first = box(w);
  view.data.rowToggle = toggle({ on: false, label: '按行分菜' });
  drawRowToggle(w);
  assert.equal(box(w), first);
  assert.equal(isOn(w), false);
  assert.equal(box(w)!.textContent, '按行分菜');
  assert.equal(w.document.querySelectorAll('.projectcook-row-toggle').length, 1);
});

test.skipIf(!gameFilesExist)('row toggle: gone with show false, with no data, and without the page feature', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  view.data.rowToggle = toggle({ show: false });
  drawRowToggle(w);
  assert.equal(box(w), null);
  view.data.rowToggle = toggle();
  drawRowToggle(w);
  view.data.rowToggle = undefined;
  drawRowToggle(w);
  assert.equal(box(w), null);
  view.data = { tips: {}, tiers: {}, features: [], rowToggle: toggle() };
  drawRowToggle(w);
  assert.equal(box(w), null);
});

test.skipIf(!gameFilesExist)('row toggle: the rule hides it while the hot pot panel is active', async (t) => {
  const w = await cookingWindow(t);
  drawRowToggle(w);
  const css = fs.readFileSync(path.join(path.dirname(url.fileURLToPath(import.meta.url)), '..', '..', 'src', 'Web', 'page.css'), 'utf8');
  assert.match(css, new RegExp(ROW_TOGGLE_HIDDEN_IN_HOT_POT.replace(/[.()#]/g, '\\$&') + '\\s*\\{\\s*display:\\s*none;?\\s*\\}'));
  assert.equal(box(w)!.matches(ROW_TOGGLE_HIDDEN_IN_HOT_POT), false);
  w.document.getElementById('hotPotPanel').classList.add('active');
  assert.equal(box(w)!.matches(ROW_TOGGLE_HIDDEN_IN_HOT_POT), true);
});
