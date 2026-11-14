window.GameStats = {
  load() {
    try { return JSON.parse(localStorage.getItem("playroom-rock-paper-scissors") || '{"wins":0,"losses":0,"ties":0}'); }
    catch (error) { console.warn("Could not read Rock Paper Scissors stats.", error); return { wins: 0, losses: 0, ties: 0 }; }
  },
  save(stats) {
    try { localStorage.setItem("playroom-rock-paper-scissors", JSON.stringify(stats)); }
    catch (error) { console.warn("Could not save Rock Paper Scissors stats.", error); }
  }
};
