// The codex mark on the Recipe Book rows of the cooking window, against the game's own Cooking.html in jsdom.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import { JSDOM } from 'jsdom';
import { beforeEach, test, type TestContext } from 'vitest';
import { missingText, view } from '../../src/Web/page/core';
import { installCodexBook, redrawBook } from '../../src/Web/page/codexBook';

type Win = any;

const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const gameFilesExist = fs.existsSync(COOKING_HTML);

// Rows as the game's RecipeMsg gives them: a single recipe that can fill, a ready one, a merged row of three recipes
// with its recipe lines, and a craft-locked row.
const FILL = { recipeId: 1203, groupId: 10, variantCount: 1, status: 1, craftLocked: false, name: 'Braised Pork Belly', icon: '', costText: 'Pork|Soy Sauce' };
const READY = { recipeId: 1301, groupId: 11, variantCount: 1, status: 2, craftLocked: false, name: 'Fried Rice with Egg', icon: '', costText: 'Rice|Egg' };
const GROUP = {
  recipeId: 1401, groupId: 12, variantCount: 3, status: 1, craftLocked: false, name: 'Vegetable Soup', icon: '', costText: '',
  variantsJson: JSON.stringify([
    { RecipeId: 1401, Status: 1, CostText: 'Cabbage' },
    { RecipeId: 1402, Status: 0, CostText: 'Spinach' },
    { RecipeId: 1403, Status: 0, CostText: 'Tomato' },
  ]),
};
const LOCKED = { recipeId: 1501, groupId: 13, variantCount: 1, status: 0, craftLocked: true, craftLockText: 'Cooking Lv.3', name: 'Hot Pot', icon: '' };
const ROWS = [READY, FILL, GROUP, LOCKED];

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
  installCodexBook(w);
  return w;
}

const rows = (w: Win) => Array.from(w.document.querySelectorAll('#recipeList > .book-item')) as HTMLElement[];
const names = (w: Win) => rows(w).map((r) => r.querySelector('.book-nm')!.textContent);
// The coin is a sibling of the row icon in a wrapper, as on a dish card.
const coins = (row: Element) => row.querySelectorAll('.projectcook-codex-wrap > .book-ico + .projectcook-codex');
const marked = (w: Win) => rows(w).filter((r) => coins(r).length > 0).map((r) => r.querySelector('.book-nm')!.textContent);

const WORDS = {
  codex: 'Codex', title: 'Dish Codex {0} / {1}', missing: '{0} of the missing dishes are in your Recipe Book.',
  showOnly: 'Click: show only these recipes.', showAll: 'Click: show all recipes.', empty: 'No recipe in your Recipe Book makes a new codex dish.',
};

function book(ids: number[], extra: Record<string, unknown> = {}) {
  return { locked: false, unlocked: 41, total: 88, ids, words: WORDS, ...extra };
}

beforeEach(() => {
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'], book: book([1203, 1401, 1501]) };
  view.codexFilter = false;
});

test.skipIf(!gameFilesExist)('codex book: a coin at the icon of each row whose recipe has no codex entry', async (t) => {
  const w = await cookingWindow(t);
  w.renderRecipeListFromData(ROWS);
  assert.deepEqual(marked(w), ['Braised Pork Belly', 'Vegetable Soup', 'Hot Pot']);
  const coin = coins(rows(w)[1])[0];
  assert.ok(coin.querySelector('svg'), 'the coin holds the trophy icon');
});

test.skipIf(!gameFilesExist)('codex book: one coin on a merged row and none on its opened recipe lines', async (t) => {
  const w = await cookingWindow(t);
  w.renderRecipeListFromData(ROWS);
  rows(w)[2].click();
  const lines = w.document.querySelectorAll('#recipeList .recipe-variants .variant-row');
  assert.equal(lines.length, 3, 'the merged row opened');
  assert.equal(w.document.querySelectorAll('#recipeList .recipe-variants .projectcook-codex').length, 0);
  assert.equal(coins(rows(w)[2]).length, 1);
});

test.skipIf(!gameFilesExist)('codex book: a craft-locked row keeps its lock and gets the coin', async (t) => {
  const w = await cookingWindow(t);
  w.renderRecipeListFromData(ROWS);
  const locked = rows(w)[3];
  assert.ok(locked.classList.contains('craft-locked'));
  assert.equal(coins(locked).length, 1);
});

test.skipIf(!gameFilesExist)('codex book: no coin without the feature, with a locked codex, or with no book data', async (t) => {
  const w = await cookingWindow(t);
  for (const data of [
    { tips: {}, tiers: {}, features: [], book: book([1203]) },
    { tips: {}, tiers: {}, features: ['codexMarks'], book: { locked: true } },
    { tips: {}, tiers: {}, features: ['codexMarks'] },
  ]) {
    view.data = data;
    w.renderRecipeListFromData(ROWS);
    assert.deepEqual(marked(w), [], JSON.stringify(data));
    assert.equal(rows(w).length, ROWS.length);
  }
});

test.skipIf(!gameFilesExist)('codex book: one coin after a second render and after a second install', async (t) => {
  const w = await cookingWindow(t);
  w.renderRecipeListFromData(ROWS);
  w.renderRecipeListFromData(ROWS);
  assert.equal(coins(rows(w)[1]).length, 1);
  installCodexBook(w);
  w.renderRecipeListFromData(ROWS);
  assert.equal(coins(rows(w)[1]).length, 1);
});

test.skipIf(!gameFilesExist)('codex book: book data after the rows draws the rows again with the coins', async (t) => {
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'] };
  const w = await cookingWindow(t);
  w.renderRecipeListFromData(ROWS);
  assert.deepEqual(marked(w), []);
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'], book: book([1301]) };
  view.version++;
  redrawBook(w);
  assert.deepEqual(marked(w), ['Fried Rice with Egg']);
  assert.deepEqual(names(w), ROWS.map((r) => r.name), 'the same rows in the same order');
});

test.skipIf(!gameFilesExist)('codex book: Load and the open of a merged row work as without the mod', async (t) => {
  const w = await cookingWindow(t);
  const clicks: number[] = [];
  w.sendRecipeClick = (id: number) => clicks.push(id);
  w.renderRecipeListFromData(ROWS);
  rows(w)[1].click();
  assert.deepEqual(clicks, [1203], 'a click on a row with Load sends its recipe');
  rows(w)[2].click();
  assert.ok(rows(w)[2].classList.contains('open'));
  (rows(w)[2].querySelector('.st-fill') as HTMLElement).click();
  assert.deepEqual(clicks, [1203, 1401], 'Load of a merged row sends its recipe');
  rows(w)[2].click();
  assert.ok(!rows(w)[2].classList.contains('open'), 'a second click closes the merged row');
});

// The Codex filter in the title of the Recipe Book (design D3, D4).
const filter = (w: Win) => w.document.querySelector('#recipePanelTitle .projectcook-codex-filter') as HTMLButtonElement | null;
const countShown = (w: Win) => w.document.getElementById('recipeCountDisplay').style.display !== 'none';
const tooltip = (w: Win) => w.document.getElementById('recipeTooltip') as HTMLElement;

// The game's row message: the rows, then the counts of the game's "Unlocked A / B".
function rowMessage(w: Win, recipes: unknown[]) {
  w.renderRecipeListFromData(recipes);
  w.eval('State').recipeUnlockedCount = 237;
  w.eval('State').recipeTotalCount = 495;
  w.updateRecipeCount();
}

test.skipIf(!gameFilesExist)('codex filter: the button "Codex A / B" in place of the game count, at the end of the title', async (t) => {
  const w = await cookingWindow(t);
  rowMessage(w, ROWS);
  const button = filter(w)!;
  assert.ok(button, 'the button shows');
  assert.equal(button.textContent, 'Codex 41 / 88');
  assert.equal(button.type, 'button');
  assert.equal(button.getAttribute('aria-pressed'), 'false');
  assert.equal(button.getAttribute('aria-label'), 'Dish Codex 41 / 88');
  assert.ok(button.querySelector('svg'), 'the trophy icon');
  assert.equal(w.document.getElementById('recipePanelTitle').lastElementChild, button);
  assert.ok(!countShown(w), 'the game count is hidden');
});

test.skipIf(!gameFilesExist)('codex filter: the game count without the feature, with a locked codex, or with no book data', async (t) => {
  const w = await cookingWindow(t);
  for (const data of [
    { tips: {}, tiers: {}, features: [], book: book([1203]) },
    { tips: {}, tiers: {}, features: ['codexMarks'], book: { locked: true } },
    { tips: {}, tiers: {}, features: ['codexMarks'] },
  ]) {
    view.data = data;
    view.version++;
    rowMessage(w, ROWS);
    assert.equal(filter(w), null, JSON.stringify(data));
    assert.ok(countShown(w));
    assert.notEqual(w.document.getElementById('recipeCountDisplay').textContent.trim(), '', 'the game count text');
  }
});

test.skipIf(!gameFilesExist)('codex filter: a missing part of the Recipe Book shows in the page check, and no button shows', async (t) => {
  const w = await cookingWindow(t);
  w.document.getElementById('recipeCountDisplay').remove();
  assert.match(missingText(w, w.document, 'cooking'), /codexBook\(#recipeCountDisplay\)/);
  rowMessage(w, ROWS);
  assert.equal(filter(w), null);
});

test.skipIf(!gameFilesExist)('codex filter: a click shows only the marked rows in the game order, a second click all rows', async (t) => {
  const w = await cookingWindow(t);
  rowMessage(w, ROWS);
  filter(w)!.click();
  assert.deepEqual(names(w), ['Braised Pork Belly', 'Vegetable Soup', 'Hot Pot']);
  assert.equal(filter(w)!.getAttribute('aria-pressed'), 'true');
  filter(w)!.click();
  assert.deepEqual(names(w), ROWS.map((r) => r.name));
  assert.equal(filter(w)!.getAttribute('aria-pressed'), 'false');
});

test.skipIf(!gameFilesExist)('codex filter: the empty line when no row is marked', async (t) => {
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'], book: book([9999]) };
  const w = await cookingWindow(t);
  rowMessage(w, ROWS);
  filter(w)!.click();
  assert.equal(rows(w).length, 0);
  const empty = w.document.querySelector('#recipeList .projectcook-codex-empty');
  assert.equal(empty?.textContent, WORDS.empty);
  filter(w)!.click();
  assert.equal(w.document.querySelector('#recipeList .projectcook-codex-empty'), null);
  assert.equal(rows(w).length, ROWS.length);
});

test.skipIf(!gameFilesExist)('codex filter: State.lastRecipes keeps all rows, so the game redraws stay right', async (t) => {
  const w = await cookingWindow(t);
  rowMessage(w, ROWS);
  filter(w)!.click();
  assert.equal(w.eval('State').lastRecipes.length, ROWS.length, 'after a filtered render');
  // A merged row that opens draws again from State.lastRecipes.
  rows(w)[1].click();
  assert.deepEqual(names(w), ['Braised Pork Belly', 'Vegetable Soup', 'Hot Pot']);
  assert.ok(rows(w)[1].classList.contains('open'));
  filter(w)!.click();
  assert.deepEqual(names(w), ROWS.map((r) => r.name), 'all rows after the merged row opened');
  // A language change draws again from State.lastRecipes.
  filter(w)!.click();
  w.renderRecipeListFromData(w.eval('State').lastRecipes);
  assert.deepEqual(names(w), ['Braised Pork Belly', 'Vegetable Soup', 'Hot Pot']);
  // New book data.
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'], book: book([1301]) };
  view.version++;
  redrawBook(w);
  assert.deepEqual(names(w), ['Fried Rice with Egg']);
  filter(w)!.click();
  assert.deepEqual(names(w), ROWS.map((r) => r.name));
  assert.equal(w.eval('State').lastRecipes.length, ROWS.length);
});

test.skipIf(!gameFilesExist)('codex filter: kept across a new row message, a tab switch, and a close and open of the window', async (t) => {
  const w = await cookingWindow(t);
  rowMessage(w, ROWS);
  filter(w)!.click();
  // A tab switch gives a new row message.
  rowMessage(w, [FILL, READY]);
  assert.deepEqual(names(w), ['Braised Pork Belly']);
  // A window opens again as a new frame.
  const reopened = await cookingWindow(t);
  rowMessage(reopened, ROWS);
  assert.equal(filter(reopened)!.getAttribute('aria-pressed'), 'true');
  assert.deepEqual(names(reopened), ['Braised Pork Belly', 'Vegetable Soup', 'Hot Pot']);
});

test.skipIf(!gameFilesExist)('codex filter: the tooltip lines, with the count of the marked dishes', async (t) => {
  const w = await cookingWindow(t);
  const sameDish = { ...FILL, recipeId: 1204, name: 'Braised Pork Belly (spicy)' };
  view.data = { tips: {}, tiers: {}, features: ['codexMarks'], book: book([1203, 1204, 1401]) };
  rowMessage(w, [...ROWS, sameDish]);
  filter(w)!.dispatchEvent(new w.MouseEvent('mouseenter'));
  assert.equal(tooltip(w).textContent, 'Dish Codex 41 / 88\n2 of the missing dishes are in your Recipe Book.\nClick: show only these recipes.');
  filter(w)!.click();
  assert.match(tooltip(w).textContent, /Click: show all recipes\.$/, 'the click line follows the click');
});

// jsdom computes no layout, so the test reads the CSS text of the button: as high as the game's Load button, its
// content centered by flex with a normal line height.
test('codex filter: the button is 22 px high with its content vertically centered', () => {
  const css = fs.readFileSync(path.join(path.dirname(url.fileURLToPath(import.meta.url)), '..', '..', 'src', 'Web', 'page.css'), 'utf8');
  const rule = /\.projectcook-codex-filter\s*\{([^}]*)\}/.exec(css);
  assert.ok(rule, 'a rule for the button');
  assert.match(rule[1], /height:\s*22px/);
  assert.match(rule[1], /line-height:\s*normal/);
  assert.match(rule[1], /align-items:\s*center/);
  assert.match(rule[1], /white-space:\s*nowrap/);
  assert.match(css, /\.projectcook-codex-filter\[aria-pressed="true"\]\s*\{/, 'a pressed look');
});
