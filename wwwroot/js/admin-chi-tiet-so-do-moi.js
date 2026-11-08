
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const NS = "http://www.w3.org/2000/svg";
    const svg = document.getElementById("adminSeatMap");
    const source = document.getElementById("adminSeatMapData");
    const viewport = document.getElementById("adminSeatViewport");
    const empty = document.getElementById("adminSeatEmpty");

    if (!svg || !source) return;

    let data;

    try {
        data = JSON.parse(source.textContent || "null");
    } catch (error) {
        console.error("Lỗi đọc dữ liệu sơ đồ:", error);
        return;
    }

    if (!data) {
        svg.hidden = true;
        if (empty) empty.hidden = false;
        return;
    }

    const seats = data.seats || data.ghe || [];
    const components = data.components || data.thanhPhan || [];

    const mapWidth = Number(data.width ?? data.chieuRong) || 1000;
    const mapHeight = Number(data.height ?? data.chieuCao) || 700;

    const getType = item => String(
        item.type ?? item.loai ?? item.loaiThanhPhan ?? ""
    );

    const getName = item => String(
        item.name ?? item.ten ?? item.tenHienThi ?? ""
    );

    const getNumber = (item, ...keys) => {
        for (const key of keys) {
            if (item[key] !== undefined && item[key] !== null) {
                return Number(item[key]);
            }
        }
        return NaN;
    };

    const getSeatX = seat => getNumber(seat, "x", "viTriX");
    const getSeatY = seat => getNumber(seat, "y", "viTriY");

    const validSeats = seats.filter(seat =>
        Number.isFinite(getSeatX(seat)) &&
        Number.isFinite(getSeatY(seat))
    );

    const hasU = components.some(p => getType(p) === "BanChuU");
    const hasRound = components.some(p => getType(p) === "BanTron");

    const layout = hasU ? "u" : hasRound ? "round" : "direct";
    const isWorkshop = layout !== "direct";

    let zoom = 1;
    const labels = [];

    svg.replaceChildren();
    svg.hidden = false;

    if (empty) empty.hidden = true;

    svg.setAttribute("viewBox", `0 0 ${mapWidth} ${mapHeight}`);
    svg.setAttribute("preserveAspectRatio", "xMidYMid meet");

    function add(tag, attrs, parent = svg) {
        const node = document.createElementNS(NS, tag);

        Object.entries(attrs).forEach(([key, value]) => {
            node.setAttribute(key, String(value));
        });

        parent.appendChild(node);
        return node;
    }

    function addText(x, y, value, size, color, parent) {
        const node = add("text", {
            x,
            y,
            "text-anchor": "middle",
            "dominant-baseline": "middle",
            "font-size": size,
            "font-weight": 700,
            fill: color
        }, parent);

        node.textContent = value || "";
        return node;
    }

    function addTitle(node, value) {
        const title = document.createElementNS(NS, "title");
        title.textContent = value;
        node.appendChild(title);
    }

    function median(values, fallback) {
        if (!values.length) return fallback;

        const sorted = [...values].sort((a, b) => a - b);
        const middle = Math.floor(sorted.length / 2);

        return sorted.length % 2
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    // Tính kích thước ghế theo đúng cách bộ thiết kế BTC tạo ghế.
    const rows = new Map();

    validSeats.forEach(seat => {
        const row = String(seat.row ?? seat.hang ?? "");

        if (!rows.has(row)) rows.set(row, []);
        rows.get(row).push(seat);
    });

    const columnGaps = [];

    rows.forEach(rowSeats => {
        const xs = [...new Set(rowSeats.map(getSeatX))]
            .sort((a, b) => a - b);

        for (let i = 1; i < xs.length; i++) {
            const gap = xs[i] - xs[i - 1];

            if (gap > 0 && gap < 150) {
                columnGaps.push(gap);
            }
        }
    });

    const rowYs = [...new Set(validSeats.map(getSeatY))]
        .sort((a, b) => a - b);

    const rowGaps = [];

    for (let i = 1; i < rowYs.length; i++) {
        const gap = rowYs[i] - rowYs[i - 1];

        if (gap > 0 && gap < 150) {
            rowGaps.push(gap);
        }
    }

    const cellWidth = median(columnGaps, 80);
    const cellHeight = median(rowGaps, 80);

    const seatWidth = Math.max(
        7,
        cellWidth - Math.min(5, cellWidth * 0.12)
    );

    const seatHeight = Math.max(
        7,
        cellHeight - Math.min(6, cellHeight * 0.12)
    );

    // Nền sơ đồ.
    add("rect", {
        x: 0,
        y: 0,
        width: mapWidth,
        height: mapHeight,
        fill: "#ffffff"
    });

    // Họa tiết lối đi giống bộ thiết kế BTC.
    const defs = add("defs", {});

    const pattern = add("pattern", {
        id: "adminAislePattern",
        width: 12,
        height: 12,
        patternUnits: "userSpaceOnUse",
        patternTransform: "rotate(45)"
    }, defs);

    add("rect", {
        width: 12,
        height: 12,
        fill: "#fafafa"
    }, pattern);

    add("rect", {
        width: 6,
        height: 12,
        fill: "#e9edf1"
    }, pattern);

    // Vẽ thành phần theo đúng tọa độ đã lưu.
    components.forEach(part => {
        const type = getType(part);
        const name = getName(part);

        const x = getNumber(part, "x", "viTriX");
        const y = getNumber(part, "y", "viTriY");
        const w = getNumber(part, "width", "rong", "chieuRong");
        const h = getNumber(part, "height", "cao", "chieuCao");

        if (![x, y, w, h].every(Number.isFinite)) return;
        if (w <= 0 || h <= 0) return;

        // Bàn chữ U xử lý riêng để tạo hình liền mạch.
        if (type === "BanChuU") return;

        const isStage = type === "SanKhau" || type === "ManHinh";
        const isDoor = [
            "CuaVao",
            "CuaThoatHiem",
            "CuaRaVao"
        ].includes(type);

        const isAisle = type === "LoiDi";
        const isRound = type === "BanTron";
        const isChairTable = type === "BanChuToa";

        let fill = "#ffffff";
        let stroke = "#bdc6d2";
        let textColor = "#27694d";

        if (isStage) {
            fill = isWorkshop ? "#956022" : "#e1eaf6";
            stroke = isWorkshop ? "#956022" : "#9cb4d3";
            textColor = isWorkshop ? "#ffffff" : "#344e71";
        } else if (isDoor) {
            fill = "#e4f4ee";
            stroke = "#8cbaab";
            textColor = "#29755e";
        } else if (isAisle) {
            fill = "url(#adminAislePattern)";
            stroke = "none";
        } else if (isRound || isChairTable) {
            fill = "#f7e9d4";
            stroke = "#a98552";
            textColor = "#76512c";
        }

        const group = add("g", {});
        let shape;

        if (isRound) {
            shape = add("circle", {
                cx: x + w / 2,
                cy: y + h / 2,
                r: Math.min(w, h) / 2,
                fill,
                stroke,
                "stroke-width": 2
            }, group);
        } else {
            shape = add("rect", {
                x,
                y,
                width: w,
                height: h,
                rx: isAisle ? 0 : isDoor ? 8 : 10,
                fill,
                stroke,
                "stroke-width": isAisle ? 0 : 1.5
            }, group);
        }

        addTitle(shape, name);

        if (!isAisle && type !== "VatCan") {
            addText(
                x + w / 2,
                y + h / 2,
                name,
                isStage ? 13 : 10,
                textColor,
                group
            );
        }
    });

    // Bàn chữ U: ghép ba cạnh thành một hình thống nhất.
    if (layout === "u") {
        const parts = components
            .filter(p => getType(p) === "BanChuU")
            .map(p => ({
                x: getNumber(p, "x", "viTriX"),
                y: getNumber(p, "y", "viTriY"),
                w: getNumber(p, "width", "rong", "chieuRong"),
                h: getNumber(p, "height", "cao", "chieuCao")
            }))
            .filter(p => [p.x, p.y, p.w, p.h].every(Number.isFinite));

        const vertical = parts
            .filter(p => p.h > p.w)
            .sort((a, b) => a.x - b.x);

        const horizontal = parts
            .filter(p => p.w >= p.h)
            .sort((a, b) => b.y - a.y);

        const left = vertical[0];
        const right = vertical[vertical.length - 1];
        const bottom = horizontal[0];

        if (left && right && bottom) {
            const leftOuter = left.x;
            const leftInner = left.x + left.w;
            const rightInner = right.x;
            const rightOuter = right.x + right.w;

            const top = Math.min(left.y, right.y);
            const innerBottom = bottom.y;

            const outerBottom = Math.max(
                left.y + left.h,
                right.y + right.h,
                bottom.y + bottom.h
            );

            const path = add("path", {
                d: [
                    `M ${leftOuter} ${top}`,
                    `H ${leftInner}`,
                    `V ${innerBottom}`,
                    `H ${rightInner}`,
                    `V ${top}`,
                    `H ${rightOuter}`,
                    `V ${outerBottom}`,
                    `H ${leftOuter}`,
                    "Z"
                ].join(" "),
                fill: "#f7e9d4",
                stroke: "#3d3024",
                "stroke-width": 2,
                "stroke-linejoin": "round"
            });

            addTitle(path, "Bàn chủ tọa chữ U");
        }
    }

    // Vẽ ghế.
    const seatGroup = add("g", {});
    const labelGroup = add("g", {});
    let booked = 0;

    validSeats.forEach(seat => {
        const x = getSeatX(seat);
        const y = getSeatY(seat);

        const row = seat.row ?? seat.hang ?? "";
        const col = seat.col ?? seat.cot ?? "";
        const code = String(seat.code ?? `${row}${col}`);

        const zone = String(
            seat.zone ?? seat.khu ?? seat.tenKhu ?? "Thuong"
        );

        const isVip = zone.toLowerCase().includes("vip");

        const status = String(
            seat.trangThai ?? seat.status ?? "Trong"
        ).trim().toLowerCase();

        const isBooked = ![
            "trong",
            "trống",
            "available",
            "còn trống",
            "con trong"
        ].includes(status);

        if (isBooked) booked++;

        const fill = isBooked ? "#e0e5eb" : "#ffffff";

        const stroke = isBooked
            ? "#a0aab7"
            : isVip
                ? "#d4af37"
                : "#86c98a";

        const textColor = isBooked
            ? "#748092"
            : isVip
                ? "#8b6b08"
                : "#2f6f2f";

        let shape;

        if (isWorkshop) {
            const size = 48;

            shape = add("circle", {
                cx: x + size / 2,
                cy: y + size / 2,
                r: size / 2,
                fill,
                stroke,
                "stroke-width": 1.5
            }, seatGroup);

            const label = addText(
                x + size / 2,
                y + size / 2,
                code,
                10,
                textColor,
                labelGroup
            );

            labels.push(label);
        } else {
            // SỰ KIỆN TRỰC TIẾP: GHẾ CHỮ NHẬT GIỐNG BTC.
            shape = add("rect", {
                x,
                y,
                width: seatWidth,
                height: seatHeight,
                rx: 5,
                fill,
                stroke,
                "stroke-width": 1.5
            }, seatGroup);

            const label = addText(
                x + seatWidth / 2,
                y + seatHeight / 2,
                code,
                Math.min(10, seatWidth * 0.2, seatHeight * 0.22),
                textColor,
                labelGroup
            );

            labels.push(label);
        }

        addTitle(
            shape,
            `Ghế ${code} - ${zone} - ${isBooked ? "Đã đặt" : "Còn trống"}`
        );
    });

    // Thống kê.
    const totalEl = document.getElementById("adminSeatTotal");
    const bookedEl = document.getElementById("adminSeatBooked");

    if (totalEl) {
        totalEl.textContent = validSeats.length.toLocaleString("vi-VN");
    }

    if (bookedEl) {
        bookedEl.textContent = booked.toLocaleString("vi-VN");
    }

    // Phóng to / thu nhỏ.
    const baseWidth = Math.max(
        500,
        Math.min(1100, (viewport?.clientWidth || 940) - 40)
    );

    function updateZoom() {
        svg.style.width = `${Math.round(baseWidth * zoom)}px`;
        svg.style.height = "auto";

        const zoomLabel = document.getElementById("adminSeatZoomValue");

        if (zoomLabel) {
            zoomLabel.textContent = `${Math.round(zoom * 100)}%`;
        }

        const showLabels = validSeats.length <= 500 || zoom >= 1.5;

        labels.forEach(label => {
            label.style.display = showLabels ? "" : "none";
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
