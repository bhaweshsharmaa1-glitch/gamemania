(() => {
  const size = 4;
  const boardEl = document.querySelector("#board");
  const cellsEl = document.querySelector("#cells");
  const tilesEl = document.querySelector("#tiles");
  const status = document.querySelector("#status");
  const stats = GameStats.load();
  let tiles = [], nextId = 1, score = 0, over = false, resizeFrame = 0;
  for (let i = 0; i < size * size; i++) {
    const cell = document.createElement("span");
    cell.className = "cell"; cellsEl.append(cell);
  }
  function createTile(value, row, col) {
    return { id: nextId++, value, row, col };
  }
  function tileElement(tile) {
    let node = tilesEl.querySelector(`[data-id="${tile.id}"]`);
    if (!node) {
      node = document.createElement("span");
      node.className = "tile"; node.dataset.id = tile.id; tilesEl.append(node);
    }
    node.textContent = tile.value;
    node.className = `tile tile-${tile.value}${tile.value >= 128 ? " tile-large" : ""}`;
    node.setAttribute("aria-label", String(tile.value));
    const gap = parseFloat(getComputedStyle(boardEl).getPropertyValue("--gap")) || 10;
    const step = (tilesEl.clientWidth + gap * (size - 1)) / size;
    TileAnimation.position(node, tile.col * step, tile.row * step);
    return node;
  }
  function render(spawned = null, mergedIds = []) {
    const active = new Set(tiles.map(tile => String(tile.id)));
    tilesEl.querySelectorAll(".tile").forEach(node => { if (!active.has(node.dataset.id)) node.remove(); });
    tiles.forEach(tile => {
      const node = tileElement(tile);
      const gap = parseFloat(getComputedStyle(boardEl).getPropertyValue("--gap")) || 10;
      const step = (tilesEl.clientWidth + gap * (size - 1)) / size;
      if (tile.id === spawned) TileAnimation.spawn(node, tile.col * step, tile.row * step);
      if (mergedIds.includes(tile.id)) {
        node.classList.add("merge");
        node.addEventListener("animationend", () => node.classList.remove("merge"), { once: true });
      }
    });
    document.querySelector("#score").textContent = score;
    if (score > stats.best) { stats.best = score; GameStats.save(stats); }
    document.querySelector("#best").textContent = stats.best;
  }
  function spawn() {
    const empty = [];
    for (let r = 0; r < size; r++) for (let c = 0; c < size; c++) {
      if (!tiles.some(tile => tile.row === r && tile.col === c)) empty.push([r, c]);
    }
    if (!empty.length) return null;
    const [row, col] = empty[Math.floor(Math.random() * empty.length)];
    const tile = createTile(Math.random() < .9 ? 2 : 4, row, col);
    tiles.push(tile);
    return tile.id;
  }
  function reset() {
    tiles = []; nextId = 1; score = 0; over = false;
    tilesEl.replaceChildren();
    status.textContent = "Use arrow keys or swipe to move tiles.";
    status.className = "status-line";
    spawn(); spawn(); render();
  }
  function move(direction) {
    if (over) return;
    const before = tiles.map(t => `${t.id}:${t.row},${t.col},${t.value}`).sort().join("|");
    const next = Array.from({ length: size }, () => Array(size).fill(null));
    const merged = [];
    for (let line = 0; line < size; line++) {
      const positions = [];
      for (let i = 0; i < size; i++) {
        if (direction === "left") positions.push([line, i]);
        if (direction === "right") positions.push([line, size - 1 - i]);
        if (direction === "up") positions.push([i, line]);
        if (direction === "down") positions.push([size - 1 - i, line]);
      }
      const ordered = positions.map(([r, c]) => tiles.find(t => t.row === r && t.col === c)).filter(Boolean);
      const result = [];
      for (let i = 0; i < ordered.length; i++) {
        const tile = ordered[i];
        if (i + 1 < ordered.length && tile.value === ordered[i + 1].value) {
          tile.value *= 2; score += tile.value; merged.push(tile.id); i++;
        }
        result.push(tile);
      }
      result.forEach((tile, index) => {
        const [row, col] = positions[index];
        tile.row = row; tile.col = col; next[row][col] = tile;
      });
    }
    tiles = next.flat().filter(Boolean);
    const after = tiles.map(t => `${t.id}:${t.row},${t.col},${t.value}`).sort().join("|");
    if (before === after) return;
    const spawned = spawn();
    render(spawned, merged);
    if (merged.some(id => tiles.find(tile => tile.id === id)?.value >= 64)) {
      boardEl.classList.remove("shake"); void boardEl.offsetWidth; boardEl.classList.add("shake");
    }
    if (!canMove()) {
      over = true; status.textContent = "No more moves. A new game is waiting whenever you are.";
      status.className = "status-line over";
    }
  }
  function canMove() {
    if (tiles.length < size * size) return true;
    return tiles.some(tile => [[0, 1], [1, 0]].some(([dr, dc]) => {
      const neighbor = tiles.find(other => other.row === tile.row + dr && other.col === tile.col + dc);
      return neighbor && neighbor.value === tile.value;
    }));
  }
  document.addEventListener("keydown", event => {
    const keyMap = { ArrowLeft: "left", ArrowRight: "right", ArrowUp: "up", ArrowDown: "down" };
    if (!keyMap[event.key] || /INPUT|TEXTAREA/.test(document.activeElement.tagName)) return;
    event.preventDefault(); move(keyMap[event.key]);
  });
  let touchStart = null;
  boardEl.addEventListener("pointerdown", event => { touchStart = [event.clientX, event.clientY]; });
  boardEl.addEventListener("pointerup", event => {
    if (!touchStart) return;
    const dx = event.clientX - touchStart[0], dy = event.clientY - touchStart[1];
    touchStart = null;
    if (Math.max(Math.abs(dx), Math.abs(dy)) < 22) return;
    move(Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? "right" : "left") : (dy > 0 ? "down" : "up"));
  });
  window.addEventListener("resize", () => {
    cancelAnimationFrame(resizeFrame);
    resizeFrame = requestAnimationFrame(() => tiles.forEach(tile => tileElement(tile)));
  });
  document.querySelector("#reset").addEventListener("click", reset);
  reset();
})();
