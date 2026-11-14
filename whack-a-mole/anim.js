window.MoleEffects = {
  burst(x, y) {
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const canvas = document.querySelector("#effects");
    const ctx = canvas.getContext("2d");
    const ratio = window.devicePixelRatio || 1;
    canvas.width = innerWidth * ratio; canvas.height = innerHeight * ratio;
    ctx.scale(ratio, ratio);
    const colors = ["#d29d5e", "#8d9c7d", "#d47e6f", "#e7c77f"];
    const particles = Array.from({ length: 14 }, () => ({
      x, y, vx: (Math.random() - .5) * 260, vy: (Math.random() - .8) * 240,
      life: .35 + Math.random() * .25, color: colors[Math.floor(Math.random() * colors.length)]
    }));
    let last = performance.now();
    function frame(now) {
      const dt = Math.min((now - last) / 1000, .03); last = now;
      ctx.clearRect(0, 0, innerWidth, innerHeight);
      particles.forEach(p => {
        p.life -= dt; p.vy += 420 * dt; p.x += p.vx * dt; p.y += p.vy * dt;
        ctx.globalAlpha = Math.max(0, p.life / .6); ctx.fillStyle = p.color;
        ctx.beginPath(); ctx.arc(p.x, p.y, 3, 0, Math.PI * 2); ctx.fill();
      });
      if (particles.some(p => p.life > 0)) requestAnimationFrame(frame);
      else ctx.clearRect(0, 0, innerWidth, innerHeight);
    }
    requestAnimationFrame(frame);
  }
};
