// The root page of the dev harness: loads the page script, then gives the Cooking window a fake bag and
// workbench through the game's own bag message, the mod data through setData, as the plugin does, and a
// prediction list with the preview fields through the game's own prediction message.
import '../../src/Web/page/main';
import type { PageData } from '../../src/Web/page/types';
import data from '../fixtures/data.json';
import prediction from '../fixtures/prediction.json';

// The items of tests/fixtures/data.json: 555 is a High ingredient and 556 a Low one.
const item = (itemId: number, configId: number, x: number) =>
  ({ itemId, configId, x, y: 0, w: 1, h: 1, name: 'Ingredient ' + configId, icon: '', count: 1, category: 1, canCook: true });

const frame = document.querySelector('iframe')!;
let started = false;

function start(): void {
  if (started) return;
  started = true;
  const cooking = frame.contentWindow as Window & typeof globalThis;
  cooking.postMessage({
    type: 'WebUI_Cooking_BagMsg',
    data: {
      bagCols: 6, bagRows: 4, workbenchCols: 4, workbenchRows: 2,
      bagItems: [item(1, 555, 0), item(2, 556, 1)],
      workbenchItems: [item(3, 555, 0), item(4, 556, 1)],
    },
  }, '*');
  setTimeout(() => {
    console.log('Project Cook setData:', window.__projectCook!.setData(data as PageData));
    cooking.postMessage({ type: 'WebUI_Cooking_PredictionMsg', data: { predictionListJson: JSON.stringify(prediction) } }, '*');
  }, 100);
}

// This module script can run before or after the Cooking frame loads.
frame.addEventListener('load', start);
if (frame.contentDocument?.readyState === 'complete' && /Cooking\.html/.test(frame.contentWindow!.location.href)) start();
