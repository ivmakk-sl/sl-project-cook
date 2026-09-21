// Runs in the root page. Each UI page is an iframe, and a reopened panel is a new iframe, so the hook is
// installed again when it is missing. The wrapper keeps the game's render function and only adds lines.
// A panel that opens with ingredients has no frame yet at the first refresh, so the script tries again for 3 seconds.
// The colors are the game's own: quality colors of the cooking page (--q-perfect, --q-good, --q-fail), and the value
// colors of the item window (pos, neg). The tier colors are the colors of the tier badges (--warm-gold, grey).
(function attempt(retries) {
  var qualityColors = { 3: '#FFC107', 2: '#B988E6', 0: '#707070' };
  var tierColors = { 1: '#fbb034', 3: '#9a9a9a' };

  // Page parts each feature depends on, by feature:
  //   preview:  renderPredictionList, #predictionList, .pot-bd
  //   cardTip:  #recipeTooltip, moveTooltip
  //   itemTip:  showItemTip, #recipeTooltip
  //   tierMark: backpack.getConfig, pot.getConfig
  // #predictionList .pot-bd exists only after a render, so it is checked in the render wrapper instead of here.
  var FEATURES = {
    preview: ['renderPredictionList', '#predictionList'],
    cardTip: ['#recipeTooltip', 'moveTooltip'],
    itemTip: ['showItemTip', '#recipeTooltip'],
    tierMark: ['backpack.getConfig', 'pot.getConfig']
  };

  function partExists(w, doc, name) {
    if (name.charAt(0) === '#' || name.charAt(0) === '.') return !!doc.querySelector(name);
    if (name.indexOf('.') > 0) {
      var obj = w, steps = name.split('.');
      for (var s = 0; s < steps.length; s++) { if (!obj) return false; obj = obj[steps[s]]; }
      return typeof obj !== 'undefined';
    }
    return typeof w[name] === 'function';
  }

  function hasFeature(w, doc, feature) {
    var parts = FEATURES[feature];
    for (var p = 0; p < parts.length; p++) if (!partExists(w, doc, parts[p])) return false;
    return true;
  }

  function missingText(w, doc) {
    var text = '';
    for (var feature in FEATURES) {
      var missing = [];
      var parts = FEATURES[feature];
      for (var p = 0; p < parts.length; p++) if (!partExists(w, doc, parts[p])) missing.push(parts[p]);
      if (missing.length) text += (text ? ', ' : '') + feature + '(' + missing.join(',') + ')';
    }
    return text;
  }

  function addError(w, text) {
    w.__cookingErrors = w.__cookingErrors || {};
    w.__cookingErrors[text] = true;
  }

  function newErrorsText(w) {
    var reported = w.__cookingErrorsReported || (w.__cookingErrorsReported = {});
    var texts = [];
    for (var text in (w.__cookingErrors || {})) if (!reported[text]) { reported[text] = true; texts.push(text); }
    return texts.join(' || ');
  }

  // Builds the aligned grid of the card lines from rows of '|'-split cells. The first cell of a row (the quality
  // level) gives the color of the name and is not shown. The percent column aligns right. The stat cells start
  // with an icon, so they align left and each icon sits below the icon of the line above.
  function buildGrid(doc, rows) {
    var grid = doc.createElement('div');
    grid.style.cssText = 'opacity:1;color:#e8dcc8;display:grid;column-gap:8px;white-space:nowrap;' +
      'grid-template-columns:repeat(' + (rows[0].length - 1) + ',max-content)';
    rows.forEach(function (cells) {
      cells.slice(1).forEach(function (text, column) {
        var cell = doc.createElement('span');
        if (column === 1) cell.style.textAlign = 'right';
        if (column === 0 && qualityColors[cells[0]]) cell.style.color = qualityColors[cells[0]];
        cell.textContent = text;
        grid.appendChild(cell);
      });
    });
    return grid;
  }

  // One tooltip line. 'T<tier>|<label>|<tier name>' gives the tier name in its tier color, and '<label>: <value>'
  // gives a signed value in the value colors of the item window, or in the text color when neutral is set.
  function tipLine(doc, text, neutral) {
    var line = doc.createElement('div'), tier = text.match(/^T(\d)\|(.*)\|(.*)$/), parts = text.match(/^(.*: )(.+)$/);
    if (tier) {
      var tierName = doc.createElement('span');
      tierName.textContent = tier[3];
      tierName.style.color = tierColors[tier[1]] || '';
      line.textContent = tier[2] + ': ';
      line.appendChild(tierName);
    } else if (parts) {
      var value = doc.createElement('span');
      value.textContent = parts[2];
      value.style.color = neutral ? '' : parts[2][0] === '-' ? '#EF5350' : parts[2][0] === '+' ? '#66BB6A' : '';
      line.textContent = parts[1];
      line.appendChild(value);
    } else line.textContent = text;
    return line;
  }

  // Builds the grid of the card tooltip: a label column, then one right-aligned column for each quality level.
  // A header cell is '<level>:<quality name>' and gets the quality color of that level.
  function buildTipGrid(doc, rows) {
    var cols = 0;
    rows.forEach(function (cells) { if (cells.length > cols) cols = cells.length; });
    var grid = doc.createElement('div');
    grid.style.cssText = 'display:grid;column-gap:12px;white-space:nowrap;grid-template-columns:repeat(' + cols + ',max-content)';
    rows.forEach(function (cells) {
      for (var c = 0; c < cols; c++) {
        var cell = doc.createElement('span'), text = cells[c] || '', name = text.match(/^(\d):(.*)$/);
        if (c >= 1) cell.style.textAlign = 'right';
        if (name) { text = name[2]; cell.style.color = qualityColors[name[1]] || ''; }
        cell.textContent = text;
        grid.appendChild(cell);
      }
    });
    return grid;
  }

  // Fills #recipeTooltip with the tip lines in their order (grid rows that follow each other make one grid), and shows,
  // moves, and hides it the same way the game does its own recipe tooltip.
  function attachCardTip(w, card, tipText) {
    var tip = w.document.getElementById('recipeTooltip');
    card.addEventListener('mouseenter', function (e) {
      try {
        tip.textContent = '';
        var lines = tipText.split('\n'), rows = [];
        var flush = function () { if (rows.length) { tip.appendChild(buildTipGrid(w.document, rows)); rows = []; } };
        for (var i = 0; i < lines.length; i++) {
          if (lines[i].indexOf('|') >= 0 && !/^T\d\|/.test(lines[i])) rows.push(lines[i].split('|'));
          else { flush(); tip.appendChild(tipLine(w.document, lines[i], true)); }
        }
        flush();
        tip.style.display = 'block';
        w.moveTooltip(e);
      } catch (e2) { addError(w, 'cardTip: ' + e2); }
    });
    card.addEventListener('mousemove', function (e) { try { w.moveTooltip(e); } catch (e2) { addError(w, 'cardTip: ' + e2); } });
    card.addEventListener('mouseleave', function () { tip.style.display = 'none'; });
  }

  // Wraps onItemRendered of the backpack and pot grids to add a tier mark, and exposes __cookingRedrawTiers
  // so the mod can start a fresh render after it sets window.__cookingTiers (the table is not set yet here).
  function installTierMark(w) {
    function wrapGrid(grid) {
      var cfg = grid.getConfig();
      var original = cfg.onItemRendered;
      cfg.onItemRendered = function (el, itemData) {
        if (original) original(el, itemData);
        try {
          var tiers = window.__cookingTiers || {};
          var tier = itemData._raw && tiers[itemData._raw.configId];
          if (tier === 1 || tier === 3) el.classList.add('pc-tier', 'pc-tier-' + tier);
        } catch (e) { addError(w, 'tierMark: ' + e); }
      };
    }
    // The mark is the game's own food badge (.item.is-food::after) with another picture: a triangle that points up
    // on the gold of the cook button for High, and a triangle that points down on grey for Low. Mid keeps the game's badge.
    function badge(path, fill, from, to) {
      return "background-image:url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'>" +
        "<path d='" + path + "' fill='" + fill + "'/></svg>\"),linear-gradient(135deg," + from + "," + to + ");";
    }
    var style = w.document.createElement('style');
    style.textContent =
      '.item.is-food.pc-tier-1::after{' + badge('M12 3 22 20H2z', 'white', 'var(--warm-gold)', 'var(--warm-orange)') + '}' +
      '.item.is-food.pc-tier-3::after{' + badge('M12 21 2 4h20z', 'white', '#9a9a9a', '#5e5e5e') + '}';
    w.document.head.appendChild(style);

    wrapGrid(w.backpack);
    wrapGrid(w.pot);
    w.__cookingRedrawTiers = function () {
      try { w.backpack.renderItems(); w.pot.renderItems(); w.__cookingTiersDrawn = true; } catch (e) { addError(w, 'tierMark redraw: ' + e); }
    };
    // The install can come after the first render of the items (the retry path), so draw them again now. At the
    // first open of a session the tier table is not set yet, and the mod starts the redraw after it sets the table.
    if (window.__cookingTiers) w.__cookingRedrawTiers();
  }

  var frames = document.querySelectorAll('iframe'), result = 'no cooking frame';
  for (var i = 0; i < frames.length; i++) {
    try {
      var w = frames[i].contentWindow;
      // The frame is found by its URL also, so a renamed render function gives a "missing" result, not a silent skip.
      if (!w || !(typeof w.renderPredictionList === 'function' || /Cooking\.html/i.test(String(w.location)))) continue;
      var doc = w.document;
      if (typeof w.renderPredictionList !== 'function' && doc.readyState !== 'complete') continue;

      if (w.__cookingPreview) {
        result = 'already installed';
      } else {
        var cardTipReady = hasFeature(w, doc, 'cardTip');

        if (hasFeature(w, doc, 'preview')) {
          var original = w.renderPredictionList;
          w.renderPredictionList = function (entries) {
            original(entries);
            try {
              var cards = w.document.querySelectorAll('#predictionList .pot-card'), missingBd = false;
              for (var k = 0; k < cards.length && k < entries.length; k++) {
                if (!entries[k].Preview) continue;
                var hint = cards[k].querySelector('.pot-hint');
                if (hint) hint.style.display = 'none';
                var rows = entries[k].Preview.split('\n').map(function (text) { return text.split('|'); });
                var grid = buildGrid(w.document, rows);
                grid.className = 'pot-hint';
                var bd = cards[k].querySelector('.pot-bd');
                if (bd) bd.appendChild(grid); else missingBd = true;

                if (cardTipReady && entries[k].PreviewTip) attachCardTip(w, cards[k], entries[k].PreviewTip);
              }
              if (missingBd) addError(w, 'preview: no .pot-bd in a prediction card');
            } catch (e) { addError(w, 'preview: ' + e); }
          };
        }

        if (hasFeature(w, doc, 'itemTip')) {
          var originalTip = w.showItemTip;
          w.showItemTip = function (item) {
            originalTip(item);
            try {
              var tip = item && item.name && item.canCook !== false && window.__cookingTips && window.__cookingTips[item.configId];
              if (tip) tip.split('\n').forEach(function (text) {
                w.document.getElementById('recipeTooltip').appendChild(tipLine(w.document, text));
              });
            } catch (e) { addError(w, 'itemTip: ' + e); }
          };
        }

        if (hasFeature(w, doc, 'tierMark')) installTierMark(w);

        w.__cookingPreview = true;
        result = 'installed';
        try { w.renderPredictionList(w.eval('State.lastPredictionEntries')); } catch (e) { result = 'installed, no redraw: ' + e; }
      }

      var missing = missingText(w, doc);
      if (missing) result += '; missing: ' + missing;
      var errors = newErrorsText(w);
      if (errors) result += '; errors: ' + errors;
    } catch (e) { result = 'error: ' + e; }
  }
  if (result === 'no cooking frame' && retries > 0) setTimeout(function () { attempt(retries - 1); }, 300);
  return result;
})(10)
