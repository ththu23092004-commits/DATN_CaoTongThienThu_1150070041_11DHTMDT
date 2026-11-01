
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const svg = document.getElementById("adminSeatMap");
    const source = document.getElementById("adminSeatMapData");
    const viewport = document.getElementById("adminSeatViewport");
    const empty = document.getElementById("adminSeatEmpty");

    if (!svg || !source) return;

    let data;

    try {
        data = JSON.parse(source.textContent || "null");
    } catch {
        data = null;
    }

    if (!data) {
        svg.hidden = true;
        if (empty) empty.hidden = false;
        return;
    }

    const NS = "http://www.w3.org/2000/svg";
    const width = Number(data.chieuRong) || 1000;
    const height = Number(data.chieuCao) || 700;
    const seats = Array.isArray(data.ghe) ? data.ghe : [];
    const parts = Array.isArray(data.thanhPhan)
        ? data.thanhPhan
        : [];

    let zoom = 1;
    let seatLabels = [];

    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    svg.replaceChildren();

    function add(tag, attributes, parent = svg) {
        const node = document.createElementNS(NS, tag);

        Object.entries(attributes).forEach(([key, value]) => {
            node.setAttribute(key, String(value));
        });

        parent.appendChild(node);
        return node;
    }

    function addText(x, y, content, fontSize, color, parent = svg) {
        const node = add("text", {
            x,
            y,
            "text-anchor": "middle",
            "dominant-baseline": "middle",
            "font-size": fontSize,
            "font-weight": 700,
            fill: color
        }, parent);

        node.textContent = content || "";
        return node;
    }

    function addTitle(node, content) {
        const title = document.createElementNS(NS, "title");
        title.textContent = content;
        node.appendChild(title);
    }

    function median(values, fallback) {
        if (!values.length) return fallback;

        const sorted = [...values].sort((a, b) => a - b);
        const mid = Math.floor(sorted.length / 2);

        return sorted.length % 2
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    const validSeats = seats.filter(s =>
        s.x != null &&
        s.y != null &&
        Number.isFinite(Number(s.x)) &&
        Number.isFinite(Number(s.y))
    );

    const rowGroups = new Map();

    validSeats.forEach(seat => {
        const key = String(seat.hang || "");

        if (!rowGroups.has(key)) {
            rowGroups.set(key, []);
        }

        rowGroups.get(key).push(seat);
    });

    const xGaps = [];

    rowGroups.forEach(row => {
        const xs = [...new Set(row.map(s => Number(s.x)))]
            .sort((a, b) => a - b);

        for (let i = 1; i < xs.length; i++) {
            const gap = xs[i] - xs[i - 1];

            if (gap > 0 && gap < 150) {
                xGaps.push(gap);
            }
        }
    });

    const ys = [...new Set(validSeats.map(s => Number(s.y)))]
        .sort((a, b) => a - b);

    const yGaps = [];

    for (let i = 1; i < ys.length; i++) {
        const gap = ys[i] - ys[i - 1];

        if (gap > 0 && gap < 150) {
            yGaps.push(gap);
        }
    }

    const columnStep = median(xGaps, 70);
    const rowStep = median(yGaps, 62);

    const seatWidth = Math.max(
        7,
        Math.min(58, columnStep * 0.82)
    );

    const seatHeight = Math.max(
        7,
        Math.min(55, rowStep * 0.78)
    );

    add("rect", {
        x: 0,
        y: 0,
        width,
        height,
        fill: "#ffffff"
    });

    const order = {
        LoiDi: 1,
        VatCan: 2,
        SanKhau: 3,
        ManHinh: 4,
        CuaVao: 5,
        CuaThoatHiem: 6
    };

    [...parts]
        .sort((a, b) =>
            (order[a.loai] || 10) - (order[b.loai] || 10)
        )
        .forEach(part => {
            const x = Number(part.x);
            const y = Number(part.y);
            const w = Number(part.rong);
            const h = Number(part.cao);

            if (![x, y, w, h].every(Number.isFinite)) return;
            if (w <= 0 || h <= 0) return;

            const isStage =
                part.loai === "SanKhau" ||
                part.loai === "ManHinh";

            const isDoor =
                part.loai === "CuaVao" ||
                part.loai === "CuaThoatHiem";

            const isAisle = part.loai === "LoiDi";

            const fill = isStage
                ? "#e1eaf6"
                : isDoor
                    ? "#e8f5ee"
                    : isAisle
                        ? "#f0f3f7"
                        : "#e7ebef";

            const stroke = isStage
                ? "#9cb4d3"
                : isDoor
                    ? "#8ac6a6"
                    : isAisle
                        ? "#e2e7ed"
                        : "#bdc6d2";

            const group = add("g", {});

            const rect = add("rect", {
                x,
                y,
                width: w,
                height: h,
                rx: isAisle ? 2 : 10,
                fill,
                stroke,
                "stroke-width": isAisle ? 0.5 : 1.5
            }, group);

            addTitle(rect, part.ten || part.loai);

            if (!isAisle && part.loai !== "VatCan") {
                addText(
                    x + w / 2,
                    y + h / 2,
                    part.ten || part.loai,
                    isStage ? 13 : 10,
                    isStage ? "#344e71" : "#27694d",
                    group
                );
            }
        });

    let booked = 0;
    const seatGroup = add("g", {});
    const labelGroup = add("g", {});

    validSeats.forEach(seat => {
        const x = Number(seat.x);
        const y = Number(seat.y);
        const code = `${seat.hang || ""}${seat.cot || ""}`;
        const zone = String(seat.khu || "");
        const vip = zone.toLowerCase().includes("vip");

        const status = String(seat.trangThai || "Trong")
            .trim()
            .toLowerCase();

        const isBooked =
            status !== "trong" &&
            status !== "trống";

        if (isBooked) booked++;

        const fill = isBooked
            ? "#e0e5eb"
            : "#ffffff";

        const stroke = isBooked
            ? "#a0aab7"
            : vip
                ? "#d4a51e"
                : "#80c783";

        const textColor = isBooked
            ? "#748092"
            : vip
                ? "#9a710a"
                : "#32763b";

        const seatRect = add("rect", {
            x,
            y,
            width: seatWidth,
            height: seatHeight,
            rx: Math.min(5, seatWidth * 0.14),
            fill,
            stroke,
            "stroke-width": validSeats.length > 500 ? 0.7 : 1.3
        }, seatGroup);

        addTitle(
            seatRect,
            `Ghế ${code} | ${zone || "Thường"} | ${isBooked ? "Đã đặt" : "Còn trống"}`
        );

        const label = addText(
            x + seatWidth / 2,
            y + seatHeight / 2,
            code,
            Math.min(10, seatWidth * 0.24, seatHeight * 0.28),
            textColor,
            labelGroup
        );

        seatLabels.push(label);
    });

    const totalEl = document.getElementById("adminSeatTotal");
    const bookedEl = document.getElementById("adminSeatBooked");

    if (totalEl) {
        totalEl.textContent = validSeats.length.toLocaleString("vi-VN");
    }

    if (bookedEl) {
        bookedEl.textContent = booked.toLocaleString("vi-VN");
    }

    const baseWidth = Math.max(
        500,
        Math.min(1100, viewport?.clientWidth - 40 || 900)
    );

    function updateZoom() {
        const actualWidth = Math.round(baseWidth * zoom);

        svg.style.width = `${actualWidth}px`;
        svg.style.height = "auto";

        const zoomLabel = document.getElementById("adminSeatZoomValue");

        if (zoomLabel) {
            zoomLabel.textContent = `${Math.round(zoom * 100)}%`;
        }

        const showSeatCodes =
            validSeats.length <= 250 ||
            (validSeats.length <= 500 && zoom >= 1.4) ||
            zoom >= 2;

        seatLabels.forEach(label => {
            label.style.display = showSeatCodes ? "" : "none";
        });
    }

    document.getElementById("adminSeatZoomIn")
        ?.addEventListener("click", () => {
            zoom = Math.min(3, zoom + 0.25);
            updateZoom();
        });

    document.getElementById("adminSeatZoomOut")
        ?.addEventListener("click", () => {
            zoom = Math.max(0.5, zoom - 0.25);
            updateZoom();
        });

    document.getElementById("adminSeatZoomReset")
        ?.addEventListener("click", () => {
            zoom = 1;
            updateZoom();

            if (viewport) {
                viewport.scrollLeft = 0;
                viewport.scrollTop = 0;
            }
        });

    updateZoom();
});
