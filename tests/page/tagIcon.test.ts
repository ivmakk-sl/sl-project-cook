// The icon of the cooking tag in the storage window, against a fake Vue component of the #app node. The game page
// keeps its icons in a const of setup(), and its render calls tagIconSvg of the setup state.
import assert from 'node:assert/strict';
import { JSDOM } from 'jsdom';
import { test } from 'vitest';
import { COOK_ICON, installTagIcon } from '../../src/Web/page/tagIcon';

type Win = any;

function storageWindow(withTagIcon: boolean): Win {
  const dom = new JSDOM('<!doctype html><html><body><div id="app"></div></body></html>');
  const w: Win = dom.window;
  const setupState: Record<string, unknown> = {};
  if (withTagIcon) setupState.tagIconSvg = (icon: string) => 'game:' + icon;
  w.document.getElementById('app')._vnode = { component: { setupState } };
  return w;
}

function tagIconSvg(w: Win): (icon: string) => string {
  return w.document.getElementById('app')._vnode.component.setupState.tagIconSvg;
}

test('tagIcon: the cook key gets the mod icon, each other key the game answer', () => {
  const w = storageWindow(true);
  assert.equal(installTagIcon(w), true);

  assert.equal(tagIconSvg(w)('cook'), COOK_ICON);
  assert.equal(tagIconSvg(w)('food'), 'game:food');
  assert.match(COOK_ICON, /^<svg viewBox="0 0 24 24">/);
});

test('tagIcon: a second install keeps one wrapper', () => {
  const w = storageWindow(true);
  installTagIcon(w);
  const first = tagIconSvg(w);
  installTagIcon(w);
  assert.equal(tagIconSvg(w), first);
  assert.equal(tagIconSvg(w)('dish'), 'game:dish');
});

test('tagIcon: no tagIconSvg records a missing part and changes nothing', () => {
  const w = storageWindow(false);
  assert.equal(installTagIcon(w), false);
  assert.ok(w.__cookingErrors && Object.keys(w.__cookingErrors).some((t) => /tagIconSvg/.test(t)), 'no error recorded');
});

test('tagIcon: no Vue component records a missing part', () => {
  const dom = new JSDOM('<!doctype html><html><body><div id="app"></div></body></html>');
  const w: Win = dom.window;
  assert.equal(installTagIcon(w), false);
  assert.ok(w.__cookingErrors && Object.keys(w.__cookingErrors).length === 1);
});
