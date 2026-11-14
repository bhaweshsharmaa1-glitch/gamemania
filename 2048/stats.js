window.GameStats = {
  load() {
    try { return JSON.parse(localStorage.getItem("playroom-2048") || '{"best":0}'); }
    catch (error) { console.warn("Could not read 2048 stats.", error); return { best: 0 }; }
  },
  save(stats) {
    try { localStorage.setItem("playroom-2048", JSON.stringify(stats)); }
    catch (error) { console.warn("Could not save 2048 stats.", error); }
  }
};
