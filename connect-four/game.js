(() => {
  const rows = 6, cols = 7;
  const boardEl = document.querySelector("#board");
  const hints = document.querySelector("#column-hints");
  const status = document.querySelector("#status");
  const turnLabel = document.querySelector("#turn-label");
  let board, player, busy, ended;
  const colors = { 1: "Coral", 2: "Gold" };

  for (let c = 0; c < cols; c++) {
    const hint = document.createElement("button");
    hint.type = "button";
    hint.textContent = "↓";
    hint.setAttribute("aria-label", `Drop a disc in column ${c + 1}`);
    hint.addEventListener("click", () => playColumn(c));
    hints.append(hint);
  }
  for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) {
    const slot = document.createElement("div");
    slot.className = "slot";
    slot.setAttribute("role", "gridcell");
    slot.dataset.row = r;
    slot.dataset.col = c;
    boardEl.append(slot);
  }
  function reset() {
    board = Array.from({ length: rows }, () => Array(cols).fill(0));
    player = 1; busy = false; ended = false;
    document.querySelector("#reset").disabled = false;
    boardEl.querySelectorAll(".disc").forEach(disc => disc.remove());
    hints.querySelectorAll(".preview").forEach(preview => preview.remove());
    status.textContent = "Coral goes first. Choose a column.";
    status.className = "status-line";
    updateTurn();
  }
  function updateTurn() {
    turnLabel.textContent = colors[player];
    turnLabel.style.color = player === 1 ? "#c86c5e" : "#b88e44";
    hints.querySelectorAll(".preview").forEach(preview => preview.remove());
    if (!ended) {
      hints.querySelectorAll("button").forEach((hint, col) => {
        if (board[0][col] === 0) {
          const preview = document.createElement("span");
          preview.className = `preview${player === 2 ? " player-two" : ""}`;
          hint.append(preview);
        }
      });
    }
  }
  function winsAt(r, c, who) {
    return winningLine(r, c, who).length >= 4;
  }
  function playColumn(col) {
    if (busy || ended) return;
    let row = rows - 1;
    while (row >= 0 && board[row][col] !== 0) row--;
    if (row < 0) return;
    busy = true;
    const who = player;
    board[row][col] = who;
    document.querySelector("#reset").disabled = true;
    const slot = boardEl.querySelector(`.slot[data-row="${row}"][data-col="${col}"]`);
    const disc = document.createElement("span");
    disc.className = `disc${who === 2 ? " player-two" : ""}`;
    slot.append(disc);
    updateTurn();
    const slotHeight = slot.getBoundingClientRect().height + 7;
    ConnectAnimation.drop(disc, slotHeight * (row + 1), () => {
      busy = false;
      document.querySelector("#reset").disabled = false;
      if (winsAt(row, col, who)) {
        ended = true;
        status.textContent = `${colors[who]} wins! Lovely move.`;
        winningLine(row, col, who).forEach(([r, c]) => boardEl.querySelector(`.slot[data-row="${r}"][data-col="${c}"] .disc`)?.classList.add("winner"));
        const stats = GameStats.load(); stats.wins = (stats.wins || 0) + 1; GameStats.save(stats);
      } else if (board[0].every(Boolean)) {
        ended = true; status.textContent = "A lovely draw. Shall we play again?";
      } else {
        player = who === 1 ? 2 : 1;
        status.textContent = `${colors[player]}'s turn.`;
        updateTurn();
      }
    });
  }
  function winningLine(r, c, who) {
    for (const [dr, dc] of [[0, 1], [1, 0], [1, 1], [1, -1]]) {
      const line = [[r, c]];
      for (const sign of [-1, 1]) {
        let nr = r + dr * sign, nc = c + dc * sign;
        while (nr >= 0 && nr < rows && nc >= 0 && nc < cols && board[nr][nc] === who) {
          line.push([nr, nc]); nr += dr * sign; nc += dc * sign;
        }
      }
      if (line.length >= 4) return line;
    }
    return [];
  }
  document.querySelector("#reset").addEventListener("click", reset);
  reset();
})();
