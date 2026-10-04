// The portion switch after the "THIS POT" title of the cooking window, against the game's own Cooking.html in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { installPreview } from '../../src/Web/page/preview';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);

const ENTRY = {
  RecipeId: 4062, Name: 'Pork Chops', Tier: 0, Level: 2, IsExact: true, Icon: '',
  Preview: '3|Perfect|100%|🍖123|🧠68|x2',
  PreviewPortion: '3|Perfect|100%|🍖62|🧠34|x2',
};

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
  installPreview(w, false);
  return w;
}

const title = (w: Win) => w.document.getElementById('potSectionTitle');
const options = (w: Win) => Array.from(title(w).querySelectorAll('.projectcook-portion-opt')) as HTMLElement[];
const cells = (w: Win) => Array.from(w.document.querySelectorAll('#predictionList .projectcook-grid span')).map((c: any) => c.textContent);
const click = (w: Win, el: HTMLElement) => el.dispatchEvent(new w.MouseEvent('click', { bubbles: true }));

beforeEach(() => {
  view.portion = false;
  view.data = { tips: {}, tiers: {}, features: ['portionSwitch'], portion: ['Whole dish', 'Per portion'] };
});

test.skipIf(!gameFilesExist)('portion switch: shows Whole dish at the start and the lines of the whole dish', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([ENTRY]);
  assert.deepEqual(options(w).map((o) => o.textContent), ['Whole dish', 'Per portion']);
  assert.ok(options(w)[0].classList.contains('projectcook-portion-on'));
  assert.deepEqual(cells(w), ['100%', '🍖123', '🧠68', 'x2']);
});

test.skipIf(!gameFilesExist)('portion switch: Per portion shows the lines of one portion, and the choice stays for a new window', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([ENTRY]);
  click(w, options(w)[1]);
  assert.equal(view.portion, true);
  assert.deepEqual(cells(w), ['100%', '🍖62', '🧠34', 'x2']);
  assert.ok(options(w)[1].classList.contains('projectcook-portion-on'));

  const again = await cookingWindow(t);
  again.renderPredictionList([ENTRY]);
  assert.ok(options(again)[1].classList.contains('projectcook-portion-on'));
  assert.deepEqual(cells(again), ['100%', '🍖62', '🧠34', 'x2']);
});

test.skipIf(!gameFilesExist)('portion switch: no switch without a preview line', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...ENTRY, Preview: undefined, PreviewPortion: undefined }]);
  assert.equal(options(w).length, 0);
  w.renderPredictionList([ENTRY]);
  w.renderPredictionList([]);
  assert.equal(options(w).length, 0);
});

test.skipIf(!gameFilesExist)('portion switch: with the switch off, no switch and the lines of the whole dish', async (t) => {
  view.portion = true;
  view.data = { tips: {}, tiers: {} };
  const w = await cookingWindow(t);
  w.renderPredictionList([ENTRY]);
  assert.equal(options(w).length, 0);
  assert.deepEqual(cells(w), ['100%', '🍖123', '🧠68', 'x2']);
});

test.skipIf(!gameFilesExist)('portion switch: comes back after the game writes the title text', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([ENTRY]);
  title(w).textContent = 'THIS POT';
  w.renderPredictionList([ENTRY]);
  assert.equal(options(w).length, 2);
  assert.ok(title(w).textContent.startsWith('THIS POT'));
});

test.skipIf(!gameFilesExist)('portion switch: a choice after a change of the list draws the new list', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([ENTRY]);
  w.renderPredictionList([{ ...ENTRY, Preview: '3|Perfect|100%|🍖40|x1', PreviewPortion: '3|Perfect|100%|🍖40|x1' }]);
  click(w, options(w)[1]);
  assert.deepEqual(cells(w), ['100%', '🍖40', 'x1']);
});
