// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
// The briefcase of the bag button of the HUD (CoreUI1.html), the icon of the bag tab.
const PATH =
  'M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z';
const SVG = 'http://www.w3.org/2000/svg';

export function briefcase(doc: Document, cls: string): SVGSVGElement {
  const svg = doc.createElementNS(SVG, 'svg');
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('class', cls);
  svg.setAttribute('aria-hidden', 'true');
  const path = doc.createElementNS(SVG, 'path');
  path.setAttribute('d', PATH);
  path.setAttribute('fill', 'currentColor');
  svg.appendChild(path);
  return svg;
}
