
(function () {
    "use strict";

    function initCuonPhimSuKien() {
        const canvas = document.querySelector(
            "#film-event-canvas"
        );

        const images = Array.from(
            document.querySelectorAll(
                "#film-event-images img"
            )
        );

        if (!canvas || !images.length) return;
        if (canvas.dataset.initialized === "true") return;

        const ctx = canvas.getContext("2d", {
            alpha: true
        });

        if (!ctx) return;

        canvas.dataset.initialized = "true";

        const reducedMotion = window.matchMedia(
            "(prefers-reduced-motion: reduce)"
        );

        // Kich thuoc moi: dai phim gon hon
        const FILM_HEIGHT = 162;
        const FRAME_WIDTH = 195;

        // Song uon luon mem
        const WAVE_STRENGTH = 30;
        const WAVE_LENGTH = 760;

        // Toc do chay ngang
        const SPEED = 38;

        // Do rong moi lan ve
        const SLICE_WIDTH = 4;

        const TAU = Math.PI * 2;

        const buffer = document.createElement("canvas");
        const bctx = buffer.getContext("2d", {
            alpha: false
        });

        if (!bctx) return;

        let width = 0;
        let height = 0;

        let offset = 0;
        let lastTime = 0;

        let isVisible = true;
        let isRunning = true;
        let animationId = null;

        // ======================================
        // VE ANH NGANG, KHONG BI BIEN DANG
        // ======================================

        function drawCover(img, x, y, w, h) {
            if (
                !img.complete ||
                img.naturalWidth === 0 ||
                img.naturalHeight === 0
            ) {
                bctx.fillStyle = "#65427f";
                bctx.fillRect(x, y, w, h);
                return;
            }

            const iw = img.naturalWidth;
            const ih = img.naturalHeight;

            const ratio = Math.max(
                w / iw,
                h / ih
            );

            const sw = w / ratio;
            const sh = h / ratio;

            bctx.drawImage(
                img,
                (iw - sw) / 2,
                (ih - sh) / 2,
                sw,
                sh,
                x,
                y,
                w,
                h
            );
        }

        function shortText(value, max) {
            const text = String(value || "");

            if (text.length <= max) {
                return text;
            }

            return text.slice(
                0,
                max - 1
            ).trim() + "…";
        }

        // ======================================
        // VE SAN TOAN BO DAI PHIM
        // ======================================

        function buildFilmBuffer() {
            buffer.width =
                images.length * FRAME_WIDTH;

            buffer.height = FILM_HEIGHT;

            const w = buffer.width;
            const h = buffer.height;

            const bg = bctx.createLinearGradient(
                0, 0, 0, h
            );

            bg.addColorStop(0, "#a279bb");
            bg.addColorStop(0.5, "#8156a3");
            bg.addColorStop(1, "#643b83");

            bctx.fillStyle = bg;
            bctx.fillRect(0, 0, w, h);

            // Vien phim
            bctx.fillStyle =
                "rgba(51, 24, 78, 0.28)";

            bctx.fillRect(0, 0, w, 2);
            bctx.fillRect(0, h - 2, w, 2);

            // Lo phim tren va duoi
            bctx.fillStyle = "#f7e8fc";

            for (let x = 6; x < w; x += 23) {
                bctx.fillRect(
                    x, 6, 14, 8
                );

                bctx.fillRect(
                    x, h - 14, 14, 8
                );
            }

            // Hinh su kien
            images.forEach(function (img, i) {
                const left = i * FRAME_WIDTH;

                // Khung mau lavender nhat
                bctx.fillStyle = "#f8edff";

                bctx.fillRect(
                    left + 7,
                    19,
                    FRAME_WIDTH - 14,
                    124
                );

                // Anh nam ngang
                drawCover(
                    img,
                    left + 11,
                    23,
                    FRAME_WIDTH - 22,
                    96
                );

                // Ten su kien
                bctx.fillStyle = "#382047";
                bctx.font =
                    "bold 9px 'Be Vietnam Pro', Arial";

                bctx.textBaseline = "middle";

                bctx.fillText(
                    shortText(
                        img.dataset.title,
                        25
                    ),
                    left + 11,
                    132,
                    FRAME_WIDTH - 22
                );

                // Vach ngan khung phim
                bctx.fillStyle =
                    "rgba(39,20,60,0.32)";

                bctx.fillRect(
                    left + FRAME_WIDTH - 2,
                    19,
                    2,
                    124
                );
            });
        }

        // ======================================
        // DUONG SONG UON LUON
        // ======================================

        function waveY(x) {
            const mobile = width < 768;

            const strength = mobile
                ? WAVE_STRENGTH * 0.62
                : WAVE_STRENGTH;

            const phase =
                (x + offset * 0.32) *
                TAU / WAVE_LENGTH;

            const wave1 =
                Math.sin(phase) * strength;

            const wave2 =
                Math.sin(
                    (x + offset * 0.14) *
                    TAU / 1250 + 0.7
                ) * (mobile ? 4 : 7);

            return height / 2 +
                wave1 +
                wave2;
        }

        // ======================================
        // VE BONG NHE MOT LAN
        // ======================================

        function drawShadow(stripHeight) {
            ctx.save();

            ctx.beginPath();

            for (
                let x = 0;
                x <= width + 12;
                x += 12
            ) {
                const y =
                    waveY(x) +
                    stripHeight / 2 +
                    5;

                if (x === 0) {
                    ctx.moveTo(x, y);
                } else {
                    ctx.lineTo(x, y);
                }
            }

            ctx.strokeStyle =
                "rgba(100, 49, 132, 0.18)";

            ctx.lineWidth = 10;

            ctx.shadowColor =
                "rgba(126, 62, 156, 0.20)";

            ctx.shadowBlur = 12;
            ctx.shadowOffsetY = 7;

            ctx.stroke();

            ctx.restore();
        }

        // ======================================
        // VE DAI PHIM UON LUON
        // ======================================

        function draw() {
            if (!width || !height || !buffer.width) {
                return;
            }

            ctx.clearRect(
                0, 0, width, height
            );

            const mobile = width < 768;

            const scale = mobile ? 0.72 : 1;

            const stripHeight =
                FILM_HEIGHT * scale;

            const filmWidth =
                buffer.width;

            drawShadow(stripHeight);

            ctx.imageSmoothingEnabled = true;
            ctx.imageSmoothingQuality = "medium";

            const currentOffset =
                ((offset % filmWidth) + filmWidth) %
                filmWidth;

            for (
                let x = 0;
                x < width;
                x += SLICE_WIDTH
            ) {
                const dw = Math.min(
                    SLICE_WIDTH,
                    width - x
                );

                const sx =
                    (x + currentOffset) % filmWidth;

                const y = waveY(x + dw / 2);

                const depth =
                    1 +
                    Math.sin(
                        (x + offset * 0.32) *
                        TAU / WAVE_LENGTH
                    ) * 0.018;

                const localHeight =
                    stripHeight * depth;

                const destY =
                    y - localHeight / 2;

                // Xu ly vi tri lap lai
                const firstWidth = Math.min(
                    dw,
                    filmWidth - sx
                );

                if (firstWidth > 0) {
                    ctx.drawImage(
                        buffer,
                        sx, 0,
                        firstWidth,
                        FILM_HEIGHT,
                        x,
                        destY,
                        firstWidth + 0.5,
                        localHeight
                    );
                }

                if (firstWidth < dw) {
                    const secondWidth =
                        dw - firstWidth;

                    ctx.drawImage(
                        buffer,
                        0, 0,
                        secondWidth,
                        FILM_HEIGHT,
                        x + firstWidth,
                        destY,
                        secondWidth + 0.5,
                        localHeight
                    );
                }
            }
        }

        // ======================================
        // RESPONSIVE
        // ======================================

        function resize() {
            const rect =
                canvas.getBoundingClientRect();

            width = Math.max(
                1,
                rect.width
            );

            height = Math.max(
                1,
                rect.height
            );

            const dpr = Math.min(
                window.devicePixelRatio || 1,
                1.5
            );

            canvas.width = Math.round(
                width * dpr
            );

            canvas.height = Math.round(
                height * dpr
            );

            ctx.setTransform(
                dpr, 0, 0, dpr, 0, 0
            );

            draw();
        }

        // ======================================
        // ANIMATION
        // ======================================

        function animate(time) {
            if (!isRunning) return;

            if (!lastTime) {
                lastTime = time;
            }

            const dt = Math.min(
                (time - lastTime) / 1000,
                0.05
            );

            lastTime = time;

            if (
                isVisible &&
                !document.hidden &&
                !reducedMotion.matches
            ) {
                offset =
                    (offset + SPEED * dt) %
                    Math.max(1, buffer.width);

                draw();
            }

            animationId =
                requestAnimationFrame(animate);
        }

        // ======================================
        // TAI ANH
        // ======================================

        images.forEach(function (img) {
            img.addEventListener(
                "load",
                function () {
                    buildFilmBuffer();
                    draw();
                }
            );

            img.addEventListener(
                "error",
                function () {
                    buildFilmBuffer();
                    draw();
                }
            );
        });

        buildFilmBuffer();

        // Doi kich thuoc man hinh
        if (typeof ResizeObserver !== "undefined") {
            const observer =
                new ResizeObserver(resize);

            observer.observe(canvas);
        } else {
            window.addEventListener(
                "resize",
                resize
            );
        }

        // Chi ve khi cuon phim dang trong man hinh
        if (typeof IntersectionObserver !== "undefined") {
            const observer =
                new IntersectionObserver(
                    function (entries) {
                        isVisible =
                            entries[0].isIntersecting;

                        lastTime = 0;
                    },
                    {
                        threshold: 0.01
                    }
                );

            observer.observe(canvas);
        }

        document.addEventListener(
            "visibilitychange",
            function () {
                lastTime = 0;
            }
        );

        if (reducedMotion.addEventListener) {
            reducedMotion.addEventListener(
                "change",
                function () {
                    lastTime = 0;
                    draw();
                }
            );
        }

        resize();

        animationId =
            requestAnimationFrame(animate);

        window.addEventListener(
            "pagehide",
            function () {
                isRunning = false;

                if (animationId !== null) {
                    cancelAnimationFrame(
                        animationId
                    );
                }
            },
            { once: true }
        );
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initCuonPhimSuKien
        );
    } else {
        initCuonPhimSuKien();
    }
})();
