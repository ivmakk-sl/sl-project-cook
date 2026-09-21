// Runs page.js against the game's own Cooking.html, so a game update that renames or removes a page
// part page.js depends on shows up here instead of only in the game.
//
// Both checks run when the game files are found: a static check (every name of the FEATURES table of
// page.js appears as text in the game files) and a behavior check (page.js installed and run against a
// real Cooking.html loaded in jsdom, with resources:'usable' fetching webui-core.js, webui-bag.js, and
// webui-tabhint.js by their <script src>). The jsdom load needs no stub: Cooking.html never touches
// window.vuplex or window.parent.notifyPageReady, so jsdom's own window.parent (itself, for a page with
// no real parent) and its built-in requestAnimationFrame (on with pretendToBeVisual) are enough.
'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const url = require('node:url');
const vm = require('node:vm');
const { JSDOM } = require('jsdom');

const GAME_DIR = process.env.SL_GAME_DIR ||
  'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const COOKING_HTML = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'UI', 'Cooking', 'Cooking.html');
const BAG_JS = path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI', 'webui-bag.js');
const PAGE_JS_PATH = path.join(__dirname, '..', '..', 'src', 'page.js');

const gameFilesExist = fs.existsSync(COOKING_HTML) && fs.existsSync(BAG_JS);

if (!gameFilesExist) {
  test('page.js against the game page', { skip: `game files not found under SL_GAME_DIR (${GAME_DIR}); set SL_GAME_DIR to the game folder` }, () => {});
} else {
  const pageJs = fs.readFileSync(PAGE_JS_PATH, 'utf8');
  const cookingHtml = fs.readFileSync(COOKING_HTML, 'utf8');
  const bagJs = fs.readFileSync(BAG_JS, 'utf8');
  const haystack = cookingHtml + '\n' + bagJs;

  // Pulls the page part names out of the FEATURES table of page.js, so this test never copies the names.
  function featureNames() {
    const table = pageJs.match(/var FEATURES = \{([\s\S]*?)\};/);
    assert.ok(table, 'FEATURES table not found in page.js');
    const names = [];
    const quoted = /'([^']+)'/g;
    let m;
    while ((m = quoted.exec(table[1]))) names.push(m[1]);
    assert.ok(names.length > 0, 'no part names parsed out of the FEATURES table');
    return names;
  }

  // A '#id' or '.class' part is a CSS selector; an 'a.b' part is a property path. Both count as found
  // when every piece of them appears as text somewhere in the game files.
  function partIsPresent(name) {
    return name.replace(/^[#.]/, '').split('.').every((piece) => haystack.includes(piece));
  }

  test('static: every FEATURES part name of page.js exists in the game files', () => {
    for (const name of featureNames()) {
      assert.ok(partIsPresent(name), `page part "${name}" not found in Cooking.html or webui-bag.js`);
    }
  });

  async function loadCookingWindow() {
    const dom = new JSDOM(cookingHtml, {
      url: url.pathToFileURL(COOKING_HTML).href,
      runScripts: 'dangerously',
      resources: 'usable',
      pretendToBeVisual: true
    });
    await new Promise((resolve, reject) => {
      dom.window.addEventListener('load', resolve);
      setTimeout(() => reject(new Error('Cooking.html did not fire load within 5s')), 5000);
    });
    return dom.window;
  }

  // Evaluates page.js in a root context whose document.querySelectorAll('iframe') hands back the real
  // Cooking page window, the same shape page.js expects from the root page's iframes. __cookingTips and
  // __cookingTiers live on this root window; __cookingErrors lives on the cooking window (see page.js).
  function installPageJs(cookingWindow) {
    const rootDocument = {
      querySelectorAll: (selector) => (selector === 'iframe' ? [{ contentWindow: cookingWindow }] : [])
    };
    const root = { document: rootDocument, setTimeout, console };
    root.window = root;
    vm.createContext(root);
    const result = vm.runInContext(pageJs, root, { filename: 'page.js' });
    return { result, root };
  }

  test('jsdom: the install result has no missing', async () => {
    const cookingWindow = await loadCookingWindow();
    const { result } = installPageJs(cookingWindow);
    assert.doesNotMatch(result, /missing:/, `install result was "${result}"`);
  });

  test('jsdom: a preview entry renders the grid and fills the tooltip on mouseenter', async () => {
    const cookingWindow = await loadCookingWindow();
    installPageJs(cookingWindow);

    const entry = {
      RecipeId: 7008, Level: 2, Icon: '', Name: 'Test Dish',
      Preview: '3|Perfect|72%|x1\n2|Good|28%|x1',
      PreviewTip: 'T2|Tier|Mid-tier\nCooking XP: +5\n|3:Perfect|2:Good\nTrade value|10|8'
    };
    cookingWindow.renderPredictionList([entry]);
    const card = cookingWindow.document.querySelector('#predictionList .pot-card');
    assert.ok(card, 'renderPredictionList did not render a prediction card');
    assert.ok(card.querySelector('.pot-hint'), 'the card has no grid');

    card.dispatchEvent(new cookingWindow.MouseEvent('mouseenter'));
    const tip = cookingWindow.document.getElementById('recipeTooltip');
    assert.equal(tip.style.display, 'block');
    assert.match(tip.textContent, /Trade value/);
    assert.match(tip.textContent, /Perfect/);
    assert.doesNotMatch(tip.textContent, /3:/);
    assert.match(tip.textContent, /Tier: Mid-tier/);

    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  test('jsdom: a card of an entry with no preview gives no error', async () => {
    const cookingWindow = await loadCookingWindow();
    installPageJs(cookingWindow);

    cookingWindow.renderPredictionList([{ RecipeId: 0, Level: 0, Icon: '', Name: 'Unknown' }]);
    assert.ok(cookingWindow.document.querySelector('#predictionList .pot-card'));
    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });

  test('jsdom: a High tier item in a grid render gets a .pc-tier ring', async () => {
    const cookingWindow = await loadCookingWindow();
    const { root } = installPageJs(cookingWindow);

    root.window.__cookingTiers = { 555: 1 };
    cookingWindow.pot.refresh([{ itemId: 1, x: 0, y: 0, w: 1, h: 1, name: 'Test', configId: 555 }]);

    const ring = cookingWindow.document.querySelector('#mainPotGrid .pc-tier');
    assert.ok(ring, 'no .pc-tier ring rendered for the High tier item');
    assert.deepEqual(cookingWindow.__cookingErrors || {}, {}, 'page.js reported an error');
  });
}
