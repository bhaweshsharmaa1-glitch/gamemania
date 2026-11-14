(() => {
  const grid = document.querySelector("#grid");
  const shell = document.querySelector(".game-shell");
  const status = document.querySelector("#status");
  const scoreEl = document.querySelector("#score");
  const timeEl = document.querySelector("#time");
  const timer = document.querySelector("#timer-ring");
  const startButton = document.querySelector("#start");
  const stats = GameStats.load();
  let score = 0, combo = 0, seconds = 20, active = false, currentHole = null, tickTimer = 0, popTimer = 0, comboTimer = 0;
  const holes = [];
  for (let i = 0; i < 9; i++) {
    const hole = document.createElement("button");
    hole.type = "button"; hole.className = "mole-hole"; hole.setAttribute("aria-label", `Hole ${i + 1}`);
    hole.innerHTML = '<span class="hole-shadow"></span><span class="mole" aria-hidden="true">✦</span><span class="plus-one" aria-hidden="true">+1</span>';
    hole.addEventListener("click", () => hit(hole));
    grid.append(hole); holes.push(hole);
  }
  function updateTimer() {
    timeEl.textContent = seconds;
    timer.style.setProperty("--progress", `${seconds / 20 * 100}%`);
    timer.classList.toggle("low", seconds <= 5);
  }
  function showMole() {
    if (!active) return;
    if (currentHole) currentHole.classList.remove("up");
    let next;
    do { next = holes[Math.floor(Math.random() * holes.length)]; } while (next === currentHole);
    currentHole = next;
    currentHole.classList.remove("hit", "missed");
    currentHole.classList.add("up");
    popTimer = window.setTimeout(() => {
      if (currentHole) currentHole.classList.remove("up");
      combo = 0;
      currentHole = null;
      window.setTimeout(showMole, 100 + Math.random() * 140);
    }, 850);
  }
  function hit(hole) {
    if (!active) return;
    if (hole !== currentHole || !hole.classList.contains("up")) {
      hole.classList.remove("missed"); void hole.offsetWidth; hole.classList.add("missed");
      combo = 0;
      shell.classList.remove("miss-shake"); void shell.offsetWidth; shell.classList.add("miss-shake");
      status.textContent = "Whoops, missed! Keep your eyes peeled.";
      return;
    }
    clearTimeout(popTimer);
    hole.classList.remove("up"); hole.classList.add("hit");
    score++; combo++;
    scoreEl.textContent = score;
    status.textContent = combo > 1 ? `Lovely! x${combo} combo!` : "Nice catch! Keep going.";
    const rect = hole.getBoundingClientRect();
    MoleEffects.burst(rect.left + rect.width / 2, rect.top + rect.height / 2);
    clearTimeout(comboTimer);
    comboTimer = window.setTimeout(() => { combo = 0; }, 1100);
    currentHole = null;
    window.setTimeout(() => hole.classList.remove("hit"), 350);
    popTimer = window.setTimeout(showMole, 150 + Math.random() * 180);
  }
  function finish() {
    active = false; clearTimeout(popTimer); clearInterval(tickTimer);
    if (currentHole) currentHole.classList.remove("up");
    currentHole = null;
    stats.games++; stats.best = Math.max(stats.best, score); GameStats.save(stats);
    document.querySelector("#best").textContent = stats.best;
    status.textContent = `Time! You caught ${score} mole${score === 1 ? "" : "s"}. ${score > 0 ? "Lovely work!" : "Try another round?"}`;
    startButton.disabled = false; startButton.textContent = "Play again";
  }
  function start() {
    if (active) return;
    score = 0; combo = 0; seconds = 20; active = true;
    scoreEl.textContent = "0"; updateTimer();
    status.textContent = "Go, go, go!";
    startButton.disabled = true; startButton.textContent = "Playing…";
    showMole();
    tickTimer = window.setInterval(() => {
      seconds--; updateTimer();
      if (seconds <= 0) finish();
    }, 1000);
  }
  startButton.addEventListener("click", start);
  document.querySelector("#best").textContent = stats.best;
  updateTimer();
})();
