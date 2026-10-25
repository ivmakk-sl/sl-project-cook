// The icon of the codex mark (the Lucide trophy), for the dish cards, the cell badges, and the dropdown.
import codexIconSvg from './icons/codex.svg?raw';

// The markup without the license comment of the file. trim: a file ends with a line break.
export const CODEX_ICON = codexIconSvg.replace(/<!--[\s\S]*?-->/g, '').trim();
