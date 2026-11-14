window.GameStats = {
  load() {
    try { return JSON.parse(localStorage.getItem("playroom-connect-four") || '{"wins":0}'); }
    catch (error) { console.warn("Could not read Connect Four stats.", error); return { wins: 0 }; }
  },
  save(stats) {
    try { localStorage.setItem("playroom-connect-four", JSON.stringify(stats)); }
    catch (error) { console.warn("Could not save Connect Four stats.", error); }
  }
};
