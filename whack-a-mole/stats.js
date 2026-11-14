window.GameStats = {
  load() {
    try { return JSON.parse(localStorage.getItem("playroom-whack-a-mole") || '{"best":0,"games":0}'); }
    catch (error) { console.warn("Could not read Whack-a-Mole stats.", error); return { best: 0, games: 0 }; }
  },
  save(stats) {
    try { localStorage.setItem("playroom-whack-a-mole", JSON.stringify(stats)); }
    catch (error) { console.warn("Could not save Whack-a-Mole stats.", error); }
  }
};
