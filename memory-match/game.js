(() => {
  const symbols = ["✦", "●", "◆", "♥", "☀", "☘", "☾", "✿"];
  const grid = document.querySelector("#grid");
  const status = document.querySelector("#status");
  let first = null, locked = false, moves = 0, matches = 0, turnId = 0;
  function shuffle(items) {
    for (let i = items.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [items[i], items[j]] = [items[j], items[i]];
    }
    return items;
  }
  function reset() {
    turnId++; first = null; locked = false; moves = 0; matches = 0;
    document.querySelector("#moves").textContent = "0";
    document.querySelector("#pairs").textContent = "0 / 8";
    status.textContent = "Turn over two cards to find a pair.";
    grid.replaceChildren();
    const cards = shuffle([...symbols, ...symbols]).map((symbol, index) => {
      const button = document.createElement("button");
      button.type = "button"; button.className = "memory-card";
      button.setAttribute("aria-label", `Face-down card ${index + 1}`);
      button.innerHTML = `<span class="card-inner"><span class="card-face card-back">✳</span><span class="card-face card-front" aria-hidden="true">${symbol}</span></span>`;
      button.dataset.symbol = symbol;
      button.addEventListener("click", () => flip(button));
      grid.append(button);
      return button;
    });
    MemoryAnimation.entrance(cards);
  }
  function flip(card) {
    if (locked || card.classList.contains("flipped") || card.classList.contains("matched")) return;
    card.classList.add("flipped");
    card.setAttribute("aria-label", `${card.dataset.symbol} card`);
    if (!first) { first = card; return; }
    locked = true; moves++;
    document.querySelector("#moves").textContent = moves;
    const second = card, firstCard = first, thisTurn = turnId;
    first = null;
    if (second.dataset.symbol === firstCard.dataset.symbol) {
      second.classList.add("matched"); firstCard.classList.add("matched");
      MemoryAnimation.burst(second);
      matches++;
      document.querySelector("#pairs").textContent = `${matches} / 8`;
      locked = false;
      if (matches === 8) {
        status.textContent = `All matched in ${moves} moves. Beautifully done!`;
        const stats = GameStats.load();
        stats.games++;
        if (stats.bestMoves === null || moves < stats.bestMoves) stats.bestMoves = moves;
        GameStats.save(stats);
      } else status.textContent = "A match! Keep going.";
    } else {
      status.textContent = "Not quite. Have another look.";
      window.setTimeout(() => {
        if (thisTurn !== turnId) return;
        firstCard.classList.add("wrong"); second.classList.add("wrong");
        window.setTimeout(() => {
          firstCard.classList.remove("flipped", "wrong");
          second.classList.remove("flipped", "wrong");
          firstCard.setAttribute("aria-label", "Face-down card");
          second.setAttribute("aria-label", "Face-down card");
          locked = false;
          status.textContent = "Turn over two cards to find a pair.";
        }, 330);
      }, 520);
    }
  }
  document.querySelector("#reset").addEventListener("click", reset);
  reset();
})();
