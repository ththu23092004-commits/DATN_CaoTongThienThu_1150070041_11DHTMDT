
(function () {
    "use strict";

    function initVeCarousel() {
        const root = document.querySelector(".ve3d-section");

        if (!root || root.dataset.initialized === "true") {
            return;
        }

        const stage = root.querySelector("#ve3d-stage");
        const cards = root.querySelector("#ve3d-cards");
        const dots = root.querySelector("#ve3d-dots");

        if (!stage || !cards || !dots) {
            return;
        }

        const tickets = Array.from(
            cards.querySelectorAll(".ve3d-ticket")
        );

        if (!tickets.length) {
            return;
        }

        root.dataset.initialized = "true";

        // Giữ nguyên đường cắt vé phiên bản 5:
        // răng cưa mềm đầu/cuối và khuyết tròn hai bên.
        const path = root.querySelector(
            "#ve3d-ticket-shape"
        );

        if (path) {
            let d = "M 18 0 ";

            for (let x = 18; x < 226; x += 26) {
                d += `L ${x + 5} 0 Q ${x + 13} 11 ${x + 21} 0 `;
            }

            d +=
                "L 232 0 Q 250 0 250 18 " +
                "L 250 316 " +
                "Q 237 318 237 331 " +
                "Q 237 344 250 346 " +
                "L 250 426 " +
                "Q 250 444 232 444 ";

            for (let x = 232; x > 18; x -= 26) {
                d +=
                    `L ${x - 5} 444 ` +
                    `Q ${x - 13} 433 ${x - 21} 444 `;
            }

            d +=
                "L 18 444 " +
                "Q 0 444 0 426 " +
                "L 0 346 " +
                "Q 13 344 13 331 " +
                "Q 13 318 0 316 " +
                "L 0 18 " +
                "Q 0 0 18 0 Z";

            path.setAttribute("d", d);
        }

        let current = 0;
        let drag = null;
        let lastWheel = 0;

        function render() {
            const step =
                stage.clientWidth < 530 ? 110 : 174;

            tickets.forEach(function (ticket, i) {
                let dist = i - current;

                if (dist > tickets.length / 2) {
                    dist -= tickets.length;
                }

                if (dist < -tickets.length / 2) {
                    dist += tickets.length;
                }

                const distance = Math.abs(dist);

                ticket.style.transform =
                    `translateX(${dist * step}px) ` +
                    `translateY(${distance * 12}px) ` +
                    `scale(${1 - distance * .12}) ` +
                    `rotateY(${-dist * 12}deg) ` +
                    `rotateZ(${dist * 2.4}deg)`;

                ticket.style.zIndex =
                    String(30 - distance);

                ticket.style.opacity =
                    distance > 2 ? ".4" : "1";

                ticket.style.filter =
                    i === current
                        ? "drop-shadow(0 20px 24px #000b) " +
                          "drop-shadow(0 0 9px #eaa4fb)"
                        : "drop-shadow(0 15px 15px #0008) " +
                          "brightness(.92)";

                ticket.classList.toggle(
                    "active",
                    i === current
                );

                ticket.setAttribute(
                    "aria-label",
                    ticket.getAttribute("aria-label") || "Sự kiện"
                );
            });

            dots.innerHTML = "";

            tickets.forEach(function (_, i) {
                const dot = document.createElement("button");

                dot.type = "button";
                dot.className =
                    "ve3d-dot" +
                    (i === current ? " on" : "");

                dot.setAttribute(
                    "aria-label",
                    "Chọn vé " + (i + 1)
                );

                dot.addEventListener("click", function () {
                    current = i;
                    render();
                });

                dots.appendChild(dot);
            });
        }

        function move(n) {
            current =
                (current + n + tickets.length) %
                tickets.length;

            render();
        }

        tickets.forEach(function (ticket, index) {
            ticket.addEventListener("click", function (e) {
                if (e.target.closest("a")) {
                    return;
                }

                if (index !== current) {
                    current = index;
                    render();
                }
            });

            ticket.addEventListener("keydown", function (e) {
                if (
                    e.target === ticket &&
                    (e.key === "Enter" || e.key === " ")
                ) {
                    e.preventDefault();

                    if (index !== current) {
                        current = index;
                        render();
                    } else {
                        const link =
                            ticket.querySelector(".ve3d-view");

                        if (link) {
                            link.click();
                        }
                    }
                }
            });
        });

        const prev = root.querySelector("#ve3d-prev");
        const next = root.querySelector("#ve3d-next");

        if (prev) {
            prev.addEventListener("click", function () {
                move(-1);
            });
        }

        if (next) {
            next.addEventListener("click", function () {
                move(1);
            });
        }

        stage.addEventListener(
            "wheel",
            function (e) {
                if (tickets.length <= 1) {
                    return;
                }

                e.preventDefault();

                const now = Date.now();

                if (now - lastWheel < 400) {
                    return;
                }

                lastWheel = now;

                move(
                    e.deltaY + e.deltaX > 0 ? 1 : -1
                );
            },
            { passive: false }
        );

        stage.addEventListener("pointerdown", function (e) {
            if (e.target.closest("button, a")) {
                return;
            }

            drag = {
                x: e.clientX,
                y: e.clientY
            };
        });

        stage.addEventListener("pointerup", function (e) {
            if (!drag) {
                return;
            }

            const dx = e.clientX - drag.x;
            const dy = e.clientY - drag.y;

            drag = null;

            if (
                Math.abs(dx) > 35 &&
                Math.abs(dx) > Math.abs(dy)
            ) {
                move(dx < 0 ? 1 : -1);
            }
        });

        stage.addEventListener("pointercancel", function () {
            drag = null;
        });

        if (typeof ResizeObserver !== "undefined") {
            const observer = new ResizeObserver(render);
            observer.observe(stage);
        } else {
            window.addEventListener("resize", render);
        }

        render();
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initVeCarousel
        );
    } else {
        initVeCarousel();
    }
})();
