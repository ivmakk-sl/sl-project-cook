// The codex part of the Recipe Book of the cooking window:
// - the trophy coin of the dish cards at the icon of each row whose recipe is in the book data (its dish has no codex
//   entry);
// - the Codex filter, a button "Codex A / B" in the title in place of the game's "Unlocked A / B": a click shows only
//   the marked rows, in the game's order, and a second click all rows.
// The wrappers keep the game's render and count: the filter gives the game's render only the marked rows, so their
// clicks, their merged rows, and their tooltips work as without the mod. The rows that the game gave stay the source
// of each draw, and State.lastRecipes keeps them, so a draw of the game itself (a merged row that opens, a language
// change) goes through the wrapper with all rows.
import { CODEX_ICON } from './codexIcon';
import { codexMarksOn } from './codexMark';
import { addError, view } from './core';
import type { BookData, BookWords, CookingWindow, RecipeRow } from './types';

// The book data when the Recipe Book shows its codex part, or null: the feature off, no book data, or a locked codex.
function activeBook(): (BookData & { ids: number[] }) | null {
  const book = view.data.book;
  if (!codexMarksOn() || !book || book.locked || !book.ids) return null;
  return book as BookData & { ids: number[] };
}

export function installCodexBook(w: CookingWindow): void {
  // A second install keeps the game's functions of the first one, so the wrappers do not stack.
  const first = w.__projectCookBook;
  w.__projectCookBook = {
    render: first ? first.render : w.renderRecipeListFromData,
    count: first ? first.count : w.updateRecipeCount,
    rows: first ? first.rows : [],
    drawn: -1,
  };
  w.renderRecipeListFromData = function (recipes: RecipeRow[]) {
    w.__projectCookBook!.rows = recipes || [];
    draw(w);
  };
  w.updateRecipeCount = function () {
    w.__projectCookBook!.count();
    try { drawFilter(w); } catch (e) { addError(w, 'codexBook: filter: ' + e); }
  };
}

// Draws the rows again from the last rows that the game gave, when the data came after the last draw (new book data).
// With the codex marks off a draw is the game's own, so a new data version needs none.
export function redrawBook(w: CookingWindow): void {
  if (w.__projectCookBook && w.__projectCookBook.drawn !== view.version && codexMarksOn()) draw(w);
}

function draw(w: CookingWindow): void {
  const state = w.__projectCookBook!;
  const rows = state.rows;
  state.drawn = view.version;
  const book = activeBook();
  const ids = book ? new Set(book.ids) : null;
  const shown = ids && view.codexFilter ? rows.filter((r) => ids.has(r.recipeId)) : rows;
  state.render(shown);
  try { (w.eval('State') as { lastRecipes: RecipeRow[] }).lastRecipes = rows; } catch (e) { addError(w, 'codexBook: no State: ' + e); }
  try {
    if (ids) markRows(w, shown, ids);
    if (ids && view.codexFilter && !shown.length && book!.words) {
      const empty = w.document.createElement('div');
      empty.className = 'projectcook-codex-empty';
      empty.textContent = book!.words.empty;
      w.document.getElementById('recipeList')?.appendChild(empty);
    }
    drawFilter(w);
  } catch (e) { addError(w, 'codexBook: ' + e); }
}

function markRows(w: CookingWindow, shown: RecipeRow[], ids: Set<number>): void {
  const items = w.document.querySelectorAll('#recipeList > .book-item');
  for (let k = 0; k < items.length && k < shown.length; k++) {
    if (!ids.has(shown[k].recipeId)) continue;
    const ico = items[k].querySelector('.book-ico');
    if (!ico) { addError(w, 'codexBook: no .book-ico in a Recipe Book row'); continue; }
    if (items[k].querySelector('.projectcook-codex')) continue;
    // The same coin as on a dish card, in a wrapper of the icon and the coin.
    const wrap = w.document.createElement('span');
    wrap.className = 'projectcook-codex-wrap';
    ico.parentNode!.insertBefore(wrap, ico);
    wrap.appendChild(ico);
    const mark = w.document.createElement('span');
    mark.className = 'projectcook-codex';
    mark.innerHTML = CODEX_ICON;
    wrap.appendChild(mark);
  }
}

function fmt(text: string, ...args: (string | number)[]): string {
  return text.replace(/\{(\d)\}/g, (all, i) => (args[+i] !== undefined ? String(args[+i]) : all));
}

// The three lines of the tooltip: the counts, the dishes of the marked rows, and what a click does.
function tipText(book: BookData, words: BookWords, rows: RecipeRow[]): string {
  const ids = new Set(book.ids);
  const dishes = new Set(rows.filter((r) => ids.has(r.recipeId)).map((r) => r.groupId));
  return [fmt(words.title, book.unlocked ?? 0, book.total ?? 0), fmt(words.missing, dishes.size),
    view.codexFilter ? words.showAll : words.showOnly].join('\n');
}

// The button in the title, or the game's count when the Recipe Book shows no codex part or a part of the title is
// missing.
function drawFilter(w: CookingWindow): void {
  const doc = w.document;
  const title = doc.getElementById('recipePanelTitle');
  const count = doc.getElementById('recipeCountDisplay');
  let button = title?.querySelector<HTMLButtonElement>('.projectcook-codex-filter') ?? null;
  const book = activeBook();
  if (!title || !count || !book || !book.words) {
    button?.remove();
    if (count) count.style.display = '';
    return;
  }
  const words = book.words;
  count.style.display = 'none';
  if (!button) {
    button = doc.createElement('button');
    button.type = 'button';
    button.className = 'projectcook-codex-filter';
    button.innerHTML = CODEX_ICON + '<span></span>';
    button.addEventListener('click', (e) => {
      e.stopPropagation();
      view.codexFilter = !view.codexFilter;
      draw(w);
      // The game's tooltip shows the text of its last mouseenter, so the click line changes here.
      const tip = doc.getElementById('recipeTooltip');
      const current = activeBook();
      if (tip && tip.style.display === 'block' && current && current.words) tip.textContent = tipText(current, current.words, w.__projectCookBook!.rows);
    });
    const tipOf = (): string => {
      const current = activeBook();
      return current && current.words ? tipText(current, current.words, w.__projectCookBook!.rows) : '';
    };
    if (typeof w.bindRecipeTooltip === 'function') w.bindRecipeTooltip(button, tipOf);
    else button.addEventListener('mouseenter', () => { button!.title = tipOf(); });
  }
  if (button.parentNode !== title || title.lastElementChild !== button) title.appendChild(button);
  button.querySelector('span')!.textContent = words.codex + ' ' + (book.unlocked ?? 0) + ' / ' + (book.total ?? 0);
  button.setAttribute('aria-pressed', String(view.codexFilter));
  button.setAttribute('aria-label', fmt(words.title, book.unlocked ?? 0, book.total ?? 0));
}
