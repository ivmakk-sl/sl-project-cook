// The data that the page script reads: the fields of the Cooking page, and the data of setData.

// One entry of the prediction list of the Cooking page (State.lastPredictionEntries), with the fields
// that the mod adds (PreviewLogic.AddPreviews in C#).
export interface PredictionEntry {
  RecipeId: number;
  Level: number;
  Name: string;
  Icon: string;
  // The preview lines: one line for each quality level, '|'-split cells, the quality level first.
  Preview?: string;
  // The lines of the dish card tooltip.
  PreviewTip?: string;
  // The preview lines of one portion, for the portion switch.
  PreviewPortion?: string;
}

// An item that showItemTip shows.
export interface TipItem {
  name?: string;
  configId: number;
  canCook?: boolean;
}

// A bag item as the backpack and pot grids give it to onItemRendered and getItems (mapItem of webui-bag.js).
export interface BagItemData {
  id?: string;
  x?: number;
  y?: number;
  w?: number;
  h?: number;
  useTimes?: number;
  useTimesMax?: number;
  // The item of the game message; category 1 is food.
  // burnable: the item is fuel for a stove.
  _raw?: { configId: number; category?: number; canCook?: boolean; needCut?: boolean; burnable?: boolean };
}

// One cell of a grid, as getOccupiedSlots of webui-bag.js gives it.
export interface Slot {
  r: number;
  c: number;
}

export interface GridConfig {
  cols: number;
  rows: number;
  onItemRendered?: (el: HTMLElement, itemData: BagItemData) => void;
}

// The backpack and pot grids of webui-bag.js (createGridBag).
export interface BagGrid {
  getConfig(): GridConfig;
  renderItems(): void;
  refresh(backendItems: unknown[]): void;
  getItems(): BagItemData[];
  getOwnerId(): number | string;
  // The end of a drag: applies a refresh that came during the drag; true when there was one.
  _flushPendingRefresh(): boolean;
  updateItemPosition(itemId: string, containerId: string, col: number, row: number): void;
  // Whether the item of the drag can be dropped on these cells; the drag calls it through the grid object.
  checkValidity(slots: Slot[], draggedId: string): boolean;
  getOccupiedSlots(row: number, col: number, w: number, h: number): Slot[];
}

// A frame that the script visits: the errors of its wrappers, and the ones already reported to C#.
export type ModWindow = Window & typeof globalThis & {
  __cookingErrors?: Record<string, boolean>;
  __cookingErrorsReported?: Record<string, boolean>;
};

// The storage window frame (BackpackUI.html), and the mark of the script.
export type StorageWindow = ModWindow & {
  __projectCookStorage?: boolean;
};

// A window of the food sort of webui-bag.js grids: the container grid (backpack), and the second grid that shows the
// numbers in place: the cooking station (pot) of the cooking window, or the Food Storage grid (foodBag) of the Rat
// Cage window. sorted: the items show the packed places now (not Default, and they fit).
export type GridSortWindow = ModWindow & {
  backpack: BagGrid;
  pot?: BagGrid;
  foodBag?: BagGrid;
  // The stove of the cooking window (the page global cookingConfig): cookType 2 is electric.
  cookingConfig?: { cookType?: number; fuelSlotCount?: number };
  // The state of the food sort of the container grid.
  __projectCookCookSort?: { items: BagItemData[] | null; real: Map<string, [number, number]>; dim: Set<number | string>; drawn: string; sorted: boolean; sortSeen: unknown };
};

// The Rat Cage frame (RatCage.html).
export type RatCageWindow = GridSortWindow & { foodBag: BagGrid };

// The Cooking frame: the page functions that the script wraps or calls, and the marks of the script.
export type CookingWindow = GridSortWindow & {
  renderPredictionList(entries: PredictionEntry[]): void;
  showItemTip(item: TipItem): void;
  moveTooltip(e: MouseEvent): void;
  pot: BagGrid;
  // The wrappers are installed.
  __cookingPreview?: boolean;
  __cookingTierMark?: boolean;
  // The data version of the last draw of the bag items.
  __projectCookDrawn?: number;
};

// The data of setData (PageJson.DataJson in C#), by config ID: the tooltip lines of each ingredient, and
// the tier of each ingredient that the page marks (1 High, 3 Low).
export interface PageData {
  tips: Record<string, string>;
  tiers: Record<string, number>;
  // The page features that are on in the config (PageJson.DataJson): separatePieces, dimUncookable, portionSwitch.
  features?: string[];
  // The labels of the portion switch: the whole dish, then one portion.
  portion?: string[];
}

export interface ProjectCookApi {
  setData(data: PageData, sort?: SortData): string;
  setSortData(sort: SortData): string;
  apply(): string;
  // The sort data of the last send, for the frames and the tests.
  sortData(): SortData | null;
}

declare global {
  interface Window {
    // The interface of the page script in the root page.
    __projectCook?: ProjectCookApi;
  }
}

// The numbers of the food sort for the open storage (PageJson.SortDataJson in C#). "n" of an item, by its logic
// id: Satiety, Morale, Stamina, Life, the trade value, and the sort key of the days left (-1 for an expired item);
// null is no number. "d" is the text of the days badge.
export interface SortItem {
  n: (number | null)[];
  d: string | null;
  // The config id: a tie of the numbers groups the same items.
  c?: number;
}

export interface SortData {
  owner: string;
  // The owner of the Backpack side of the storage window, whose items are in items too (none in the cooking window).
  bag?: string;
  words: { choices: string[]; expired: string; sort: string };
  items: Record<string, SortItem>;
}
