(() => {
  const choices = { rock: "✊", paper: "✋", scissors: "✌️" };
  const beats = { rock: "scissors", paper: "rock", scissors: "paper" };
  const stats = GameStats.load();
  const playerHand = document.querySelector("#player-hand");
  const computerHand = document.querySelector("#computer-hand");
  const duel = document.querySelector("#duel");
  const status = document.querySelector("#status");
  const buttons = [...document.querySelectorAll(".choice")];
  const updateScore = () => {
    document.querySelector("#player-score").textContent = stats.wins;
    document.querySelector("#tie-score").textContent = stats.ties;
  };
  function play(mine) {
    if (buttons[0].disabled) return;
    const theirs = Object.keys(choices)[Math.floor(Math.random() * 3)];
    buttons.forEach(button => button.disabled = true);
    duel.classList.remove("result");
    duel.classList.add("playing");
    status.textContent = "Rock... paper... scissors...";
    status.className = "status-line";
    playerHand.textContent = "✊";
    computerHand.textContent = "✊";
    window.setTimeout(() => {
      duel.classList.remove("playing");
      playerHand.textContent = choices[mine];
      computerHand.textContent = choices[theirs];
      const outcome = mine === theirs ? "tie" : beats[mine] === theirs ? "win" : "lose";
      if (outcome === "win") { stats.wins++; status.textContent = "You win! Lovely choice."; status.classList.add("win"); GameEffects.burst(innerWidth / 2, innerHeight / 2, "#c5a65f"); }
      else if (outcome === "lose") { stats.losses++; status.textContent = "Playroom wins this one. Again?"; status.classList.add("lose"); }
      else { stats.ties++; status.textContent = "A perfect tie. Great minds!"; }
      GameStats.save(stats);
      updateScore();
      duel.classList.add("result");
      buttons.forEach(button => button.disabled = false);
    }, matchMedia("(prefers-reduced-motion: reduce)").matches ? 50 : 1300);
  }
  buttons.forEach(button => button.addEventListener("click", () => play(button.dataset.choice)));
  document.querySelector("#reset").addEventListener("click", () => {
    stats.wins = 0; stats.losses = 0; stats.ties = 0;
    GameStats.save(stats); updateScore();
    status.textContent = "Choose your move.";
  });
  updateScore();
})();
