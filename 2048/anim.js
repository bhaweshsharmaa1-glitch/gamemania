window.TileAnimation = {
  position(element, x, y) {
    element.style.setProperty("--x", `${x}px`);
    element.style.setProperty("--y", `${y}px`);
    element.style.transform = `translate(${x}px, ${y}px)`;
  },
  spawn(element, x, y) {
    this.position(element, x, y);
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    element.classList.add("spawn");
    element.addEventListener("animationend", () => element.classList.remove("spawn"), { once: true });
  }
};
