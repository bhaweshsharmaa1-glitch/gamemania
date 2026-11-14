window.MemoryAnimation = {
  entrance(cards) {
    cards.forEach((card, index) => {
      card.style.opacity = "0";
      card.style.transform = "translateY(12px) scale(.96)";
      requestAnimationFrame(() => {
        card.style.transition = `opacity 220ms ease ${index * 22}ms, transform 220ms ease ${index * 22}ms`;
        card.style.opacity = "1";
        card.style.transform = "translateY(0) scale(1)";
      });
    });
  },
  burst(card) {
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const rect = card.getBoundingClientRect();
    const burst = document.createElement("span");
    burst.className = "match-spark";
    burst.setAttribute("aria-hidden", "true");
    burst.style.left = `${rect.left + rect.width / 2}px`;
    burst.style.top = `${rect.top + rect.height / 2}px`;
    document.body.append(burst);
    setTimeout(() => burst.remove(), 480);
  }
};
