window.GameEffects = {
  burst(x, y, color = "#d5a467") {
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const canvas = document.querySelector("#effects");
    const ctx = canvas.getContext("2d");
    const ratio = window.devicePixelRatio || 1;
    canvas.width = innerWidth * ratio; canvas.height = innerHeight * ratio;
    ctx.scale(ratio, ratio);
    const particles = Array.from({ length: 28 }, () => ({
      x, y, vx: (Math.random() - .5) * 260, vy: -Math.random() * 240 - 30,
      life: 0.7 + Math.random() * .35, size: 2 + Math.random() * 3
    }));
    let last = performance.now();
    function draw(now) {
      const dt = Math.min((now - last) / 1000, .03); last = now;
      ctx.clearRect(0, 0, innerWidth, innerHeight);
      particles.forEach(p => {
        p.life -= dt; p.vy += 520 * dt; p.x += p.vx * dt; p.y += p.vy * dt;
        ctx.globalAlpha = Math.max(0, p.life); ctx.fillStyle = color;
        ctx.beginPath(); ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2); ctx.fill();
      });
      if (particles.some(p => p.life > 0)) requestAnimationFrame(draw);
      else ctx.clearRect(0, 0, innerWidth, innerHeight);
    }
    requestAnimationFrame(draw);
  }
};
