window.GameStats = {
  load() {
    try { return JSON.parse(localStorage.getItem("playroom-memory-match") || '{"bestMoves":null,"games":0}'); }
    catch (error) { console.warn("Could not read Memory Match stats.", error); return { bestMoves: null, games: 0 }; }
  },
  save(stats) {
    try { localStorage.setItem("playroom-memory-match", JSON.stringify(stats)); }
    catch (error) { console.warn("Could not save Memory Match stats.", error); }
  }
};
