// The codex mark on the dish cards of the cooking window, against the game's own Cooking.html in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { view } from '../../src/Web/page/core';
import { installCodexMark } from '../../src/Web/page/codexMark';
import { installPreview } from '../../src/Web/page/preview';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);

const KNOWN = { RecipeId: 4062, Name: 'Pork Chops', Tier: 0, Level: 2, IsExact: true, Icon: 'a.png', Preview: '3|Perfect|100%|🍖123' };
const MYSTERY = { RecipeId: 5008, Name: '???', Tier: 0, Level: 0, IsExact: false, Icon: '' };
const DARK = { RecipeId: 0, Name: 'Struggle Meal', Tier: 0, Level: 0, IsExact: false, Icon: '' };

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
  installCodexMark(w);
  return w;
}

const cards = (w: Win) => Array.from(w.document.querySelectorAll('#predictionList .pot-card')) as HTMLElement[];
// The mark is a sibling of the dish icon in a wrapper, so it can hang out of the icon, whose box cuts off its content.
const marks = (card: HTMLElement) => card.querySelectorAll('.projectcook-codex-wrap > .pot-ico + .projectcook-codex');

beforeEach(() => {
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'] };
});

test.skipIf(!gameFilesExist)('codex mark: only the cards whose entry has Codex, at the corner of the dish icon', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }, KNOWN]);
  const [marked, plain] = cards(w);
  assert.equal(marks(marked).length, 1);
  assert.ok(marks(marked)[0].querySelector('svg'), 'the mark holds the trophy icon');
  assert.equal(marks(plain).length, 0);
});

test.skipIf(!gameFilesExist)('codex mark: also on a mystery card and a dark dish card with no preview lines', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...MYSTERY, Codex: 1 }, { ...DARK, Codex: 1 }]);
  const [mystery, dark] = cards(w);
  assert.ok(mystery.classList.contains('mystery'));
  assert.equal(marks(mystery).length, 1);
  assert.equal(mystery.querySelector('.pot-nm')!.textContent, '???');
  assert.equal(marks(dark).length, 1);
});

test.skipIf(!gameFilesExist)('codex mark: no mark without the feature', async (t) => {
  view.data = { tips: {}, tiers: {}, features: [] };
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }]);
  assert.equal(marks(cards(w)[0]).length, 0);
});

test.skipIf(!gameFilesExist)('codex mark: one mark after a second render and after a second install', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }]);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }]);
  assert.equal(marks(cards(w)[0]).length, 1);
  installCodexMark(w);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }]);
  assert.equal(marks(cards(w)[0]).length, 1);
});

// jsdom computes no layout, so the test reads the CSS text: the card mark, the cell badge, and the dropdown icon of
// Codex are one round, opaque coin with a gold ring.
test('codex mark: a round opaque coin with a gold ring on the card, in the cell badge, and in the dropdown', () => {
  const css = fs.readFileSync(path.join(path.dirname(url.fileURLToPath(import.meta.url)), '..', '..', 'src', 'Web', 'page.css'), 'utf8');
  const tokens = fs.readFileSync(path.join(path.dirname(url.fileURLToPath(import.meta.url)), '..', '..', 'src', 'Web', 'tokens.css'), 'utf8');
  for (const selector of ['.pot-card .projectcook-codex-wrap > .projectcook-codex', '.projectcook-sort-badge.projectcook-sort-codex', '.projectcook-sort-ico[data-choice="7"]']) {
    const rule = new RegExp(selector.replace(/[.[\]"=>]/g, '\\$&') + '[^{]*\\{([^}]*)\\}').exec(css);
    assert.ok(rule, `a rule for ${selector}`);
    assert.match(rule[1], /border-radius:\s*50%/, `${selector}: round`);
    assert.match(rule[1], /border:\s*[\d.]+px solid var\(--pc-codex\)/, `${selector}: a gold ring`);
    assert.match(rule[1], /background:\s*var\(--pc-codex-bg\)/, `${selector}: the coin background`);
  }
  const card = /\.pot-card \.projectcook-codex-wrap > \.projectcook-codex\s*\{([^}]*)\}/.exec(css);
  assert.match(card![1], /top:\s*-4px;[\s\S]*left:\s*-4px;/, 'the card coin hangs 4 px out of the icon, as in a cell');
  // The other round icons of the dropdown (Trade value, Expiration Date) are Lucide circles in the 14 px icon box of the
  // dropdown library: a circle of about 13 px with a ring of about 1.2 px.
  const ico = /\.projectcook-sort-ico\[data-choice="7"\]\s*\{([^}]*)\}/.exec(css)![1];
  assert.match(ico, /width:\s*13px;[\s\S]*height:\s*13px;/, 'the dropdown coin is as big as the other round icons');
  assert.match(ico, /border:\s*1\.2px solid/, 'the dropdown coin has the ring of the other round icons');
  const bg = /--pc-codex-bg:\s*([^;]+);/.exec(tokens);
  assert.ok(bg && /^#[0-9a-f]{6}$/i.test(bg[1].trim()), `an opaque coin background: ${bg && bg[1]}`);
});

test.skipIf(!gameFilesExist)('codex mark: the preview lines still show on a marked card', async (t) => {
  const w = await cookingWindow(t);
  w.renderPredictionList([{ ...KNOWN, Codex: 1 }]);
  assert.ok(cards(w)[0].querySelector('.projectcook-grid'));
});
