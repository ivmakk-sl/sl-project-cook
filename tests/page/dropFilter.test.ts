// The drop filter of the food sort: the moves that the pages send to the game while a sort is on.
import assert from 'node:assert/strict';
import { test } from 'vitest';
import { filterMove, firstFree, installDropFilter, type SortedGrid } from '../../src/Web/page/dropFilter';

// A 3 x 2 storage of owner 42 with two items in their real places.
function grid(): SortedGrid {
  return {
    owner: '42',
    cols: 3,
    rows: 2,
    items: [{ id: '1', x: 0, y: 0, w: 1, h: 1 }, { id: '2', x: 1, y: 0, w: 2, h: 1 }],
    sizeOf: (id: string) => (id === '9' ? [2, 1] : id === '8' ? [3, 2] : [1, 1]),
  };
}

test('firstFree: the first real cell that fits, row by row', () => {
  assert.deepEqual(firstFree(grid().items, 3, 2, 1, 1), [0, 1]);
  assert.deepEqual(firstFree(grid().items, 3, 2, 2, 1), [0, 1]);
  assert.equal(firstFree(grid().items, 3, 2, 3, 2), null);
});

test('storage window: a drag inside the sorted storage is dropped', () => {
  const msg = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 1, srcOwnerId: 42, dstOwnerId: 42, x: 2, y: 1 } };
  assert.equal(filterMove(msg, [grid()]), null);
});

test('storage window: a drag from the bag into the sorted storage gets a free real cell', () => {
  const msg = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 9, srcOwnerId: 5, dstOwnerId: 42, x: 2, y: 0, cursorCol: 2, cursorRow: 0 } };
  const out = filterMove(msg, [grid()]) as any;
  assert.deepEqual([out.data.x, out.data.y], [0, 1]);
  assert.equal(out.data.cursorCol, 0);
  assert.equal(out.data.cursorRow, 1);
  assert.equal(out.data.itemId, 9);
});

test('a move into the sorted storage that fits no free cell goes unchanged', () => {
  const msg = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 8, srcOwnerId: 5, dstOwnerId: 42, x: 0, y: 1 } };
  assert.equal(filterMove(msg, [grid()]), msg);
});

test('cooking window: a move inside the sorted tab is dropped, a move from the workbench gets a free cell', () => {
  assert.equal(filterMove({ type: 'ITEM_MOVE', sourcePageId: 'Cooking', data: { itemId: 1, fromOwnerId: 42, toOwnerId: 42, x: 2, y: 1 } }, [grid()]), null);
  const out = filterMove({ type: 'ITEM_MOVE', sourcePageId: 'Cooking', data: { itemId: 7, fromOwnerId: 3, toOwnerId: 42, x: 2, y: 1 } }, [grid()]) as any;
  assert.deepEqual([out.data.x, out.data.y], [0, 1]);
});

test('a ground pickup into the sorted storage gets a free real cell', () => {
  const out = filterMove({ type: 'GROUND_PICK_TO', sourcePageId: 'Cooking', data: { id: 77, ownerId: 42, x: 2, y: 1 } }, [grid()]) as any;
  assert.deepEqual([out.data.x, out.data.y], [0, 1]);
});

test('each other move, each other message, and no sorted grid pass unchanged', () => {
  const toBag = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 1, srcOwnerId: 42, dstOwnerId: 5, x: 0, y: 0 } };
  assert.equal(filterMove(toBag, [grid()]), toBag);
  const other = { type: 'SFX_ITEM_PICKUP', sourcePageId: 'Cooking', data: { itemId: 1 } };
  assert.equal(filterMove(other, [grid()]), other);
  const inside = { type: 'ITEM_MOVE', sourcePageId: 'Cooking', data: { itemId: 1, fromOwnerId: 42, toOwnerId: 42, x: 2, y: 1 } };
  assert.equal(filterMove(inside, []), inside);
});

test('installDropFilter: wraps the postMessage of the root once, drops and rewrites', () => {
  const sent: unknown[] = [];
  const root: any = { postMessage: (m: unknown) => { sent.push(m); } };
  installDropFilter(root, () => [grid()]);
  installDropFilter(root, () => [grid()]);
  root.postMessage({ type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 1, srcOwnerId: 42, dstOwnerId: 42, x: 2, y: 1 } }, '*');
  root.postMessage({ type: 'OTHER', data: {} }, '*');
  assert.equal(sent.length, 1);
  assert.deepEqual(sent[0], { type: 'OTHER', data: {} });
});

test('two sorted grids: a move between them gets a free cell of the target, a move inside one is dropped', () => {
  const bag: SortedGrid = { owner: '5', cols: 2, rows: 1, items: [{ id: '11', x: 0, y: 0, w: 1, h: 1 }], sizeOf: () => [1, 1] };
  const toStorage = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 11, srcOwnerId: 5, dstOwnerId: 42, x: 0, y: 0 } };
  const out = filterMove(toStorage, [bag, grid()]) as any;
  assert.deepEqual([out.data.x, out.data.y], [0, 1]);
  const toBag = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 1, srcOwnerId: 42, dstOwnerId: 5, x: 0, y: 0 } };
  const back = filterMove(toBag, [bag, grid()]) as any;
  assert.deepEqual([back.data.x, back.data.y], [1, 0]);
  const insideBag = { type: 'DRAG_ITEM', sourcePageId: 'Backpack', data: { itemId: 11, srcOwnerId: 5, dstOwnerId: 5, x: 1, y: 0 } };
  assert.equal(filterMove(insideBag, [bag, grid()]), null);
});
