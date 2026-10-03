// The font size of a dish card line: the largest size at which the line fits the card.
import assert from 'node:assert/strict';
import { test } from 'vitest';
import { FIT_SIZES, fitSize } from '../../src/Web/page/fit';

test('fitSize: the largest size that fits, from the normal size down', () => {
  assert.deepEqual(FIT_SIZES, [11, 10, 9]);
  assert.deepEqual(fitSize(FIT_SIZES, () => true), { size: 11, fits: true });
  assert.deepEqual(fitSize(FIT_SIZES, (px) => px <= 10), { size: 10, fits: true });
  assert.deepEqual(fitSize(FIT_SIZES, (px) => px <= 9), { size: 9, fits: true });
});

test('fitSize: a line that does not fit at the smallest size keeps the smallest size', () => {
  const tried: number[] = [];
  assert.deepEqual(fitSize(FIT_SIZES, (px) => { tried.push(px); return false; }), { size: 9, fits: false });
  assert.deepEqual(tried, [11, 10, 9]);
});
