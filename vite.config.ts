// Builds the page script: one IIFE file, not minified, that C# embeds and sends to the root page.
// The entry sets window.__projectCook itself, so the bundle exports nothing. Vitest runs the page tests
// against that bundle and the game's own Cooking.html.
import { defineConfig } from 'vitest/config';

export default defineConfig({
  build: {
    lib: {
      entry: 'src/Web/page/main.ts',
      formats: ['iife'],
      name: 'projectCookPage',
      fileName: () => 'page.js',
    },
    outDir: 'obj/page',
    emptyOutDir: true,
    minify: false,
  },
  test: {
    include: ['tests/page/**/*.test.ts'],
    // Each test loads the game's Cooking.html in jsdom, which takes about half a second.
    testTimeout: 20000,
  },
});
