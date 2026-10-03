// Runs the page script bundle against the game's own Cooking.html, so a game update that renames or removes a
// page part the script depends on shows up here instead of only in the game.
//
// Both checks run when the game files are found: a static check (every name of the FEATURES table of the page
// source appears as text in the game files) and a behavior check (the bundle run against a real Cooking.html
// loaded in jsdom, with resources:'usable' fetching webui-core.js, webui-bag.js, and webui-tabhint.js by their
// <script src>). The jsdom load needs no stub: Cooking.html never touches window.vuplex or
// window.parent.notifyPageReady, so jsdom's own window.parent (itself, for a page with no real parent) and its
// built-in requestAnimationFrame (on with pretendToBeVisual) are enough.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import vm from 'node:vm';
import { JSDOM } from 'jsdom';
import { test, type TestContext } from 'vitest';

// The jsdom windows carry the game's page globals (renderPredictionList, pot, showItemTip) and the root page
// stub carries the page script's interface, which have no types.
type Win = any;

const HERE = path.dirname(url.fileURLToPath(import.meta.url));

const GAME_DIR = process.env.SL_GAME_DIR ||
  'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const BAG_JS = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'webui-bag.js');
const STORAGE_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'BackpackUI', 'BackpackUI.html');
// The bundle that Vite builds from src/Web/page/ (npm test builds it first).
const PAGE_JS_PATH = path.join(HERE, '..', '..', 'obj', 'page', 'page.js');
// The source file with the FEATURES table, in the quotes that the static test parses.
const FEATURES_PATH = path.join(HERE, '..', '..', 'src', 'Web', 'page', 'core.ts');
const DATA_JSON_PATH = path.join(HERE, '..', 'fixtures', 'data.json');

const gameFilesExist = fs.existsSync(COOKING_HTML) && fs.existsSync(BAG_JS) && fs.existsSync(STORAGE_HTML);

if (!gameFilesExist) {
  test.skip(`page.js against the game page: game files not found under SL_GAME_DIR (${GAME_DIR}); set SL_GAME_DIR to the game folder`, () => {});
} else {
  const pageJs = fs.readFileSync(PAGE_JS_PATH, 'utf8');
  const featuresSource = fs.readFileSync(FEATURES_PATH, 'utf8');
  const cookingHtml = fs.readFileSync(COOKING_HTML, 'utf8');
  const bagJs = fs.readFileSync(BAG_JS, 'utf8');
  const storageHtml = fs.readFileSync(STORAGE_HTML, 'utf8');
  const haystack = cookingHtml + '\n' + bagJs + '\n' + storageHtml;

  // Pulls the page part names out of the FEATURES table of the page source, so this test never copies the names.
  function featureNames(): string[] {
    const table = featuresSource.match(/export const FEATURES = \{([\s\S]*?)\};/);
    assert.ok(table, 'FEATURES table not found in core.ts');
    const names: string[] = [];
    const quoted = /'([^']+)'/g;
    let m;
    while ((m = quoted.exec(table[1]))) names.push(m[1]);
    assert.ok(names.length > 0, 'no part names parsed out of the FEATURES table');
    return names;
  }

  // A '#id' or '.class' part is a CSS selector; an 'a.b' part is a property path. Both count as found
  // when every piece of them appears as text somewhere in the game files.
  function partIsPresent(name: string): boolean {
    return name.replace(/^[#.]/, '').split('.').every((piece) => haystack.includes(piece));
  }

  test('static: every FEATURES part name of page.js exists in the game files', () => {
    for (const name of featureNames()) {
      assert.ok(partIsPresent(name), `page part "${name}" not found in Cooking.html, webui-bag.js, or BackpackUI.html`);
    }
  });

  // t.onTestFinished(...) closes each window once its test is done, so no page timer keeps the test
  // process alive.
  async function loadCookingWindow(t: TestContext): Promise<Win> {
    const dom = new JSDOM(cookingHtml, {
      url: url.pathToFileURL(COOKING_HTML).href,
      runScripts: 'dangerously',
      resources: 'usable',
      pretendToBeVisual: true
    });
    t.onTestFinished(() => dom.window.close());
    await new Promise<void>((resolve, reject) => {
      dom.window.addEventListener('load', () => resolve());
      setTimeout(() => reject(new Error('Cooking.html did not fire load within 5s')), 5000);
    });
    return dom.window;
  }

  // Evaluates the bundle in a root context whose document.querySelectorAll('iframe') hands back the real
  // Cooking page window, the same shape the script expects from the root page's iframes, then runs one pass as
  // the apply call of C#. __projectCook lives on this root window; __cookingErrors lives on the cooking window.
  function installPageJs(cookingWindow: Win): { result: string; root: Win } {
    const rootDocument = {
      querySelectorAll: (selector: string) => (selector === 'iframe' && cookingWindow ? [{ contentWindow: cookingWindow }] : [])
    };
    const root: Win = { document: rootDocument, setTimeout, console };
    root.window = root;
    vm.createContext(root);
    vm.runInContext(pageJs, root, { filename: 'page.js' });
    const result: string = root.__projectCook.apply();
    return { result, root };
  }

  // A second run of the whole script in the same root page, as a second full send.
  function runPageJsAgain(root: Win): void {
    vm.runInContext(pageJs, root, { filename: 'page.js' });
  }

  function fixtureData(): Win {
    return JSON.parse(fs.readFileSync(DATA_JSON_PATH, 'utf8'));
  }

  // A root page with the page script and no Cooking frame yet, so the interface has no pass to run
  // when the test calls it first.
  function rootWithoutFrame(): Win {
    return installPageJs(null).root;
  }

  function withFrame(root: Win, cookingWindow: Win): void {
    root.document.querySelectorAll = (selector: string) => (selector === 'iframe' ? [{ contentWindow: cookingWindow }] : []);
  }

  function withFrames(root: Win, windows: Win[]): void {
    root.document.querySelectorAll = (selector: string) => (selector === 'iframe' ? windows.map((w) => ({ contentWindow: w })) : []);
  }

  // A storage window frame: a page at the URL of BackpackUI.html with the #app node of the game page. The game
  // page needs the game to render, so the tests of the storage window features give their own Vue component.
  async function storageWindow(t: TestContext): Promise<Win> {
    const dom = new JSDOM('<!doctype html><html><head></head><body><div id="app"></div></body></html>', {
      url: url.pathToFileURL(STORAGE_HTML).href,
      runScripts: 'dangerously',
      pretendToBeVisual: true
    });
    t.onTestFinished(() => dom.window.close());
    if (dom.window.document.readyState !== 'complete') {
      await new Promise<void>((resolve) => dom.window.addEventListener('load', () => resolve()));
    }
    return dom.window;
  }

  test('interface: setData stores the data and runs one pass, so the item tooltip and the tier mark use it', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const root = rootWithoutFrame();
    withFrame(root, cookingWindow);

    const result = root.__projectCook.setData(fixtureData());
    assert.match(result, /^(installed|already installed)/, `setData result was "${result}"`);

    cookingWindow.showItemTip({ name: 'Test', configId: 556, canCook: true });
    const tip = cookingWindow.document.getElementById('recipeTooltip');
    assert.match(tip.textContent, /Low "raw" \\ tier/);

    cookingWindow.pot.refresh([{ itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Test', configId: 555 }]);
    assert.ok(cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier-1'), 'no .projectcook-bag-tier-1 mark for the High item');
    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  test('interface: a second apply answers already installed and adds no second grid to a card', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const root = rootWithoutFrame();
    withFrame(root, cookingWindow);
    root.__projectCook.setData(fixtureData());

    cookingWindow.renderPredictionList([{ RecipeId: 7008, Level: 2, Icon: '', Name: 'Test Dish', Preview: '3|Perfect|72%|x1\n2|Good|28%|x1' }]);
    const card = cookingWindow.document.querySelector('#predictionList .pot-card');
    const before = card.querySelectorAll('.projectcook-grid').length;

    assert.equal(root.__projectCook.apply(), 'already installed');
    assert.equal(card.querySelectorAll('.projectcook-grid').length, before);
  });

  test('interface: setData with changed tiers draws the bag items again', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const root = rootWithoutFrame();
    withFrame(root, cookingWindow);
    root.__projectCook.setData(fixtureData());
    cookingWindow.pot.refresh([{ itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Test', configId: 555 }]);
    assert.ok(cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier-1'));

    const data = fixtureData();
    data.tiers = { 555: 3 };
    root.__projectCook.setData(data);
    assert.ok(cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier-3'), 'the item was not drawn again with its new tier');
    assert.equal(cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier-1'), null);
  });

  test('interface: apply with no Cooking frame answers no Cooking frame and schedules no timer', () => {
    const root = rootWithoutFrame();
    let timers = 0;
    root.setTimeout = () => { timers++; };

    assert.equal(root.__projectCook.apply(), 'no Cooking frame');
    assert.equal(timers, 0);
  });

  test('interface: a pass with only a storage window answers its own result, not no Cooking frame', async (t) => {
    const root = rootWithoutFrame();
    const storage = await storageWindow(t);
    withFrames(root, [storage]);

    assert.match(root.__projectCook.apply(), /^storage: installed/);
    assert.match(root.__projectCook.apply(), /^storage: already installed/);
    assert.ok(storage.document.getElementById('projectcook-style'), 'the storage window has no style node');
  });

  test('interface: an error of the storage pass goes to the errors part of the result, which C# logs', async (t) => {
    const root = rootWithoutFrame();
    const storage = await storageWindow(t);
    storage.document.head.appendChild = () => { throw new Error('boom'); };
    withFrames(root, [storage]);

    const result = root.__projectCook.apply();
    assert.match(result, /^storage: failed; errors: .*boom/);
  });

  test('interface: a pass with both windows answers the cooking result first', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const root = rootWithoutFrame();
    withFrames(root, [await storageWindow(t), cookingWindow]);

    const result = root.__projectCook.apply();
    assert.match(result, /^installed/);
    assert.match(result, /; storage: installed/);
  });

  const SORT = {
    owner: '42',
    words: { choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'], expired: 'Expired', sort: 'Sort' },
    items: { '7': { n: [14, null, null, null, 40, 0.5], d: '0.5d' } }
  };

  test('interface: setSortData stores the sort data and runs one pass', async (t) => {
    const root = rootWithoutFrame();
    withFrames(root, [await storageWindow(t)]);

    assert.match(root.__projectCook.setSortData(SORT), /^storage: installed/);
    assert.equal(root.__projectCook.sortData().owner, '42');
    assert.match(root.__projectCook.apply(), /^storage: already installed/);
  });

  test('interface: setData with sort data stores both', () => {
    const root = rootWithoutFrame();
    root.__projectCook.setData(fixtureData(), SORT);
    assert.equal(root.__projectCook.sortData().items['7'].d, '0.5d');
  });

  test('interface: a second run of the script keeps the first interface and its data', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const root = rootWithoutFrame();
    const first = root.__projectCook;
    first.setData(fixtureData());
    runPageJsAgain(root);
    assert.equal(root.__projectCook, first);

    withFrame(root, cookingWindow);
    root.__projectCook.apply();
    cookingWindow.showItemTip({ name: 'Test', configId: 555, canCook: true });
    assert.match(cookingWindow.document.getElementById('recipeTooltip').textContent, /High-tier/);
  });

  test('jsdom: the result of the first pass has no missing', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const { result } = installPageJs(cookingWindow);
    assert.doesNotMatch(result, /missing:/, `install result was "${result}"`);
  });

  test('jsdom: a preview entry renders the grid and fills the tooltip on mouseenter', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    installPageJs(cookingWindow);

    const entry = {
      RecipeId: 7008, Level: 2, Icon: '', Name: 'Test Dish',
      Preview: '3|Perfect|72%|x1\n2|Good|28%|x1',
      PreviewTip: 'T2|Tier|Mid-tier\nCooking XP: +5\n|3:Perfect|2:Good\nTrade value|10|8'
    };
    cookingWindow.renderPredictionList([entry]);
    const card = cookingWindow.document.querySelector('#predictionList .pot-card');
    assert.ok(card, 'renderPredictionList did not render a prediction card');
    assert.ok(card.querySelector('.projectcook-grid'), 'the card has no grid');

    card.dispatchEvent(new cookingWindow.MouseEvent('mouseenter'));
    const tip = cookingWindow.document.getElementById('recipeTooltip');
    assert.equal(tip.style.display, 'block');
    assert.match(tip.textContent, /Trade value/);
    assert.match(tip.textContent, /Perfect/);
    assert.doesNotMatch(tip.textContent, /3:/);
    assert.match(tip.textContent, /Tier: Mid-tier/);

    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  test('jsdom: a card of an entry with no preview gives no error', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    installPageJs(cookingWindow);

    cookingWindow.renderPredictionList([{ RecipeId: 0, Level: 0, Icon: '', Name: 'Unknown' }]);
    assert.ok(cookingWindow.document.querySelector('#predictionList .pot-card'));
    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  test('jsdom: a High tier item in a grid render gets a .projectcook-bag-tier mark', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const { root } = installPageJs(cookingWindow);

    root.__projectCook.setData({ tips: {}, tiers: { 555: 1 } });
    cookingWindow.pot.refresh([{ itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Test', configId: 555 }]);

    const ring = cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier');
    assert.ok(ring, 'no .projectcook-bag-tier mark rendered for the High tier item');
    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  const PREVIEW_ENTRY = { RecipeId: 7008, Level: 2, Icon: '', Name: 'Test Dish', Preview: '3|Perfect|72%|x1\n2|Good|28%|x1' };

  test('css: one style node with the tokens and the rules, also after a second pass', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const { root } = installPageJs(cookingWindow);
    root.__projectCook.apply();

    const styles = cookingWindow.document.querySelectorAll('#projectcook-style');
    assert.equal(styles.length, 1);
    assert.match(styles[0].textContent, /--pc-q-perfect/);
    assert.match(styles[0].textContent, /\.projectcook-grid/);
  });

  test('css: the preview grid has its own class and its column count, and the game hint is hidden', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    installPageJs(cookingWindow);

    cookingWindow.renderPredictionList([PREVIEW_ENTRY]);
    const card = cookingWindow.document.querySelector('#predictionList .pot-card');
    const grid = card.querySelector('.projectcook-grid');
    assert.ok(grid, 'the card has no .projectcook-grid');
    assert.equal(grid.classList.contains('pot-hint'), false);
    assert.equal(grid.style.getPropertyValue('--pc-cols'), '2');
    const cells = grid.children;
    // No quality name: the chance is the first cell, in the color of its quality, with the name as its label.
    assert.equal(cells[0].textContent, '72%');
    assert.ok(cells[0].classList.contains('projectcook-cell-num'), 'the percent cell has no projectcook-cell-num');
    assert.ok(cells[0].classList.contains('projectcook-q-3'), 'the chance of Perfect has no projectcook-q-3');
    assert.equal(cells[0].getAttribute('title'), 'Perfect');
    assert.equal(cells[2].textContent, '28%');
    assert.ok(cells[2].classList.contains('projectcook-q-2'), 'the chance of Good has no projectcook-q-2');
    assert.equal(cells[2].getAttribute('title'), 'Good');
    assert.ok(![...cells].some((c) => c.textContent === 'Perfect' || c.textContent === 'Good'), 'a quality name shows');
    for (const hint of card.querySelectorAll('.pot-hint')) assert.ok(hint.classList.contains('projectcook-hide'));
  });

  test('css: the item tooltip marks signed values and the tier name with classes', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const { root } = installPageJs(cookingWindow);
    root.__projectCook.setData({ tips: { 557: 'T1|Tier|High-tier\nSatiety: +8\nHealth: -2' }, tiers: {} });

    cookingWindow.showItemTip({ name: 'Test', configId: 557, canCook: true });
    const tip = cookingWindow.document.getElementById('recipeTooltip');
    assert.ok(tip.querySelector('.projectcook-tier-1'), 'the tier name has no projectcook-tier-1');
    assert.equal(tip.querySelector('.projectcook-value-pos').textContent, '+8');
    assert.equal(tip.querySelector('.projectcook-value-neg').textContent, '-2');
  });

  test('css: a High bag item gets the bag tier classes that the badge rule selects', async (t) => {
    const cookingWindow = await loadCookingWindow(t);
    const { root } = installPageJs(cookingWindow);
    root.__projectCook.setData(fixtureData());

    cookingWindow.pot.refresh([{ itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Test', configId: 555, category: 1 }]);
    const el = cookingWindow.document.querySelector('#mainPotGrid .projectcook-bag-tier');
    assert.ok(el, 'no .projectcook-bag-tier item');
    assert.ok(el.classList.contains('projectcook-bag-tier-1'));
    // The badge rule is '.item.is-food.projectcook-bag-tier-1::after'; a pseudo-element selector matches no element.
    assert.ok(el.matches('.item.is-food.projectcook-bag-tier-1'));
  });
}
