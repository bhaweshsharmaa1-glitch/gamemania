window.ConnectAnimation = {
  drop(disc, distance, done) {
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) {
      disc.style.transform = "translateY(0)";
      done();
      return;
    }
    let position = -distance;
    let velocity = 0;
    let previous = performance.now();
    const gravity = 2300;
    function frame(now) {
      const dt = Math.min((now - previous) / 1000, 0.032);
      previous = now;
      velocity += gravity * dt;
      position += velocity * dt;
      if (position >= 0) {
        position = 0;
        velocity = -velocity * 0.2;
        if (Math.abs(velocity) < 70) {
          disc.style.transform = "translateY(0) scaleY(.91)";
          requestAnimationFrame(() => {
            disc.style.transition = "transform 110ms ease-out";
            disc.style.transform = "translateY(0) scaleY(1)";
            done();
          });
          return;
        }
      }
      disc.style.transform = `translateY(${position}px)`;
      requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  }
};
