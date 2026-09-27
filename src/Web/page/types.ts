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
}

// An item that showItemTip shows.
export interface TipItem {
  name?: string;
  configId: number;
  canCook?: boolean;
}

// A bag item as the backpack and pot grids give it to onItemRendered.
export interface BagItemData {
  _raw?: { configId: number };
}

export interface GridConfig {
  onItemRendered?: (el: HTMLElement, itemData: BagItemData) => void;
}

// The backpack and pot grids of webui-bag.js.
export interface BagGrid {
  getConfig(): GridConfig;
  renderItems(): void;
}

// The Cooking frame: the page functions that the script wraps or calls, and the marks of the script.
export type CookingWindow = Window & typeof globalThis & {
  renderPredictionList(entries: PredictionEntry[]): void;
  showItemTip(item: TipItem): void;
  moveTooltip(e: MouseEvent): void;
  backpack: BagGrid;
  pot: BagGrid;
  // The wrappers are installed.
  __cookingPreview?: boolean;
  __cookingTierMark?: boolean;
  // The data version of the last draw of the bag items.
  __projectCookDrawn?: number;
  // The errors of the wrappers, and the ones already reported to C#.
  __cookingErrors?: Record<string, boolean>;
  __cookingErrorsReported?: Record<string, boolean>;
};

// The data of setData (PageJson.DataJson in C#), by config ID: the tooltip lines of each ingredient, and
// the tier of each ingredient that the page marks (1 High, 3 Low).
export interface PageData {
  tips: Record<string, string>;
  tiers: Record<string, number>;
}

export interface ProjectCookApi {
  setData(data: PageData): string;
  apply(): string;
}

declare global {
  interface Window {
    // The interface of the page script in the root page.
    __projectCook?: ProjectCookApi;
  }
}
