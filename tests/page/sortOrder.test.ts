// The order and the pack of the food sort, without a page.
import assert from 'node:assert/strict';
import { test } from 'vitest';
import { CHOICE, badgeText, badgeTone, order, pack } from '../../src/Web/page/sortOrder';
import type { SortData } from '../../src/Web/page/types';

const WORDS = { choices: ['Default', 'Satiety', 'Morale', 'Stamina', 'Life', 'Trade value', 'Expiration Date'], expired: 'Expired', sort: 'Sort' };

function data(items: SortData['items']): SortData {
  return { owner: '1', words: WORDS, items };
}

const item = (id: number, w = 1, h = 1) => ({ id, w, h });

test('order: Satiety highest first, no number last and dimmed', () => {
  const d = data({
    1: { n: [5, null, null, null, 3, 2], d: '2d' },
    2: { n: [30, null, null, null, 10, 4], d: '4d' },
    3: { n: [null, null, null, null, 1, null], d: null },
  });
  const r = order([item(1), item(2), item(3)], d, CHOICE.satiety);
  assert.deepEqual(r.ids, [2, 1, 3]);
  assert.deepEqual([...r.dim], [3]);
});

test('order: a negative number comes after the positive numbers and is not dimmed', () => {
  const d = data({
    1: { n: [null, -3, null, null, null, null], d: null },
    2: { n: [null, 4, null, null, null, null], d: null },
    3: { n: [null, null, null, null, null, null], d: null },
  });
  const r = order([item(1), item(2), item(3)], d, CHOICE.morale);
  assert.deepEqual(r.ids, [2, 1, 3]);
  assert.deepEqual([...r.dim], [3]);
});

test('order: Days left, expired first, fewest days next, no spoil last', () => {
  const d = data({
    1: { n: [null, null, null, null, null, 10], d: '10d' },
    2: { n: [null, null, null, null, null, null], d: null },
    3: { n: [null, null, null, null, null, -1], d: 'Expired' },
    4: { n: [null, null, null, null, null, 2], d: '2d' },
  });
  assert.deepEqual(order([item(1), item(2), item(3), item(4)], d, CHOICE.days).ids, [3, 4, 1, 2]);
});

test('order: a tie keeps the real order, an item without data is last', () => {
  const d = data({
    1: { n: [5, null, null, null, null, null], d: null },
    2: { n: [5, null, null, null, null, null], d: null },
  });
  const r = order([item(9), item(1), item(2)], d, CHOICE.satiety);
  assert.deepEqual(r.ids, [1, 2, 9]);
  assert.ok(r.dim.has(9));
});

test('order: a tie groups the same items (config id), then keeps the real order', () => {
  const d = data({
    1: { n: [5, null, null, null, null, null], d: null, c: 20 },
    2: { n: [5, null, null, null, null, null], d: null, c: 10 },
    3: { n: [5, null, null, null, null, null], d: null, c: 20 },
    4: { n: [5, null, null, null, null, null], d: null, c: 10 },
  });
  assert.deepEqual(order([item(1), item(2), item(3), item(4)], d, CHOICE.satiety).ids, [2, 4, 1, 3]);
});

test('pack: first fit row by row in the item sizes', () => {
  const places = pack([item(1, 2, 1), item(2), item(3, 1, 2), item(4)], 3, 3);
  assert.ok(places);
  assert.deepEqual(places.get(1), [0, 0]);
  assert.deepEqual(places.get(2), [2, 0]);
  assert.deepEqual(places.get(3), [0, 1]);
  assert.deepEqual(places.get(4), [1, 1]);
});

test('pack: null when the items do not fit in the order', () => {
  assert.equal(pack([item(1), item(2, 2, 2)], 2, 2), null);
});

test('badgeText: signed stats, plain trade value, days text, none for no number', () => {
  const it = { n: [14, -3, null, 2, 40, 0.5], d: '0.5d' };
  assert.equal(badgeText(it, CHOICE.satiety), '+14');
  assert.equal(badgeText(it, CHOICE.morale), '-3');
  assert.equal(badgeText(it, CHOICE.stamina), null);
  assert.equal(badgeText(it, CHOICE.trade), '40');
  assert.equal(badgeText(it, CHOICE.days), '0.5d');
  assert.equal(badgeText(it, CHOICE.default), null);
  assert.equal(badgeText(undefined, CHOICE.satiety), null);
});

test('badgeTone: a stat is positive or negative, the trade value gold, the days plain or negative when expired', () => {
  const it = { n: [14, -3, null, null, 40, -99997.5], d: '2d' };
  assert.equal(badgeTone(it, CHOICE.satiety), 'pos');
  assert.equal(badgeTone(it, CHOICE.morale), 'neg');
  assert.equal(badgeTone(it, CHOICE.stamina), null);
  assert.equal(badgeTone({ n: [0, null, null, null, null, null], d: null }, CHOICE.satiety), null);
  assert.equal(badgeTone(it, CHOICE.trade), 'gold');
  assert.equal(badgeTone(it, CHOICE.days), 'neg');
  assert.equal(badgeTone({ n: [null, null, null, null, null, 3], d: '3d' }, CHOICE.days), null);
  assert.equal(badgeTone(it, CHOICE.default), null);
});
