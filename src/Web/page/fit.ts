// The font size of the preview lines of a dish card: the normal size of the game's hint line, and smaller sizes for a
// line that is too wide for the card (all four stats, big numbers, portions).
export const FIT_SIZES = [11, 10, 9];

// The largest of sizes (largest first) at which fits says the line fits; the smallest when none fits.
export function fitSize(sizes: number[], fits: (px: number) => boolean): { size: number; fits: boolean } {
  for (const px of sizes) if (fits(px)) return { size: px, fits: true };
  return { size: sizes[sizes.length - 1], fits: false };
}
