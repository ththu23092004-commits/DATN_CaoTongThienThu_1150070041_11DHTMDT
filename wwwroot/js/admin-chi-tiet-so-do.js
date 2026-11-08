
"use strict";

const NS = "http://www.w3.org/2000/svg";

document.addEventListener("DOMContentLoaded", () => {
    const svg = document.getElementById("adminSeatMap");
    const source = document.getElementById("adminSeatMapData");
    const viewport = document.getElementById("adminSeatViewport");
    const empty = document.getElementById("adminSeatEmpty");

    if (!svg || !source) return;

    let data = null;

    try {
        data = JSON.parse(source.textContent || "null");
    } catch (error) {
        console.error("Không đọc được dữ liệu sơ đồ:", error);
    }

    const seats = Array.isArray(data?.seats)
        ? data.seats
        : Array.isArray(data?.ghe)
            ? data.ghe
            : [];

    const components = Array.isArray(data?.components)
        ? data.components
        : Array.isArray(data?.thanhPhan)
            ? data.thanhPhan
            : [];

    if (!data || (!seats.length && !components.length)) {
        svg.hidden = true;

        if (empty) {
            empty.hidden = false;
        }

        return;
    }

    const width = Number(data.width ?? data.chieuRong) || 1000;
    const height = Number(data.height ?? data.chieuCao) || 700;

    let zoom = 1;
    const seatLabels = [];

    function getType(item) {
        return String(item.type ?? item.loai ?? "");
    }

    function getName(item) {
        return String(item.name ?? item.ten ?? getType(item));
    }

    function getValue(item, ...keys) {
        for (const key of keys) {
            if (item[key] !== undefined && item[key] !== null) {
                return Number(item[key]);
            }
        }

        return NaN;
    }

    const hasUShape = components.some(
        item => getType(item) === "BanChuU"
    );

    const hasRoundTable = components.some(
        item => getType(item) === "BanTron"
    );

    // Ưu tiên nhận diện theo thành phần sơ đồ thực tế.
    // Admin hiện tại có thể gán nhầm layout="round" cho sự kiện trực tiếp.
    let layout;

    if (hasUShape) {
        layout = "u";
    } else if (hasRoundTable) {
        layout = "round";
    } else {
        layout = "direct";
    }

    const isWorkshop = layout === "round" || layout === "u";

    const validSeats = seats.filter(seat =>
        seat.x != null &&
        seat.y != null &&
        Number.isFinite(Number(seat.x)) &&
        Number.isFinite(Number(seat.y))
    );

    svg.hidden = false;

    if (empty) {
        empty.hidden = true;
    }

    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    svg.setAttribute("preserveAspectRatio", "xMidYMid meet");
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
        const middle = Math.floor(sorted.length / 2);

        return sorted.length % 2
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    // Tính kích thước ghế trực tiếp dựa trên tọa độ đã lưu.
    const rowGroups = new Map();

    validSeats.forEach(seat => {
        const row = String(seat.row ?? seat.hang ?? "");

        if (!rowGroups.has(row)) {
            rowGroups.set(row, []);
        }

        rowGroups.get(row).push(seat);
    });

    const xSteps = [];

    rowGroups.forEach(row => {
        const xs = [...new Set(
            row.map(seat => Number(seat.x))
        )].sort((a, b) => a - b);

        for (let i = 1; i < xs.length; i++) {
            const step = xs[i] - xs[i - 1];

            if (step > 0 && step < 150) {
                xSteps.push(step);
            }
        }
    });

    const ySteps = [];

    const uniqueY = [...new Set(
        validSeats.map(seat => Number(seat.y))
    )].sort((a, b) => a - b);

    for (let i = 1; i < uniqueY.length; i++) {
        const step = uniqueY[i] - uniqueY[i - 1];

        if (step > 0 && step < 150) {
            ySteps.push(step);
        }
    }

    const xStep = median(xSteps, 64);
    const yStep = median(ySteps, 64);

    const seatWidth = isWorkshop
        ? 48
        : Math.max(12, Math.min(70, xStep - Math.min(5, xStep * 0.12)));

    const seatHeight = isWorkshop
        ? 48
        : Math.max(12, Math.min(70, yStep - Math.min(6, yStep * 0.12)));

    // Tạo họa tiết lối đi chéo giống sơ đồ thiết kế trực tiếp.
    const defs = add("defs", {});

    const pattern = add("pattern", {
        id: "adminSeatAislePattern",
        width: 12,
        height: 12,
        patternUnits: "userSpaceOnUse",
        patternTransform: "rotate(135)"
    }, defs);

    add("rect", {
        width: 12,
        height: 12,
        fill: "#f5f6f7"
    }, pattern);

    add("rect", {
        width: 5,
        height: 12,
        fill: "#e9ecef"
    }, pattern);

    // Nền sơ đồ.
    add("rect", {
        x: 0,
        y: 0,
        width,
        height,
        fill: "#ffffff"
    });

    const partOrder = {
        LoiDi: 1,
        VatCan: 2,
        BanTron: 3,
        BanChuU: 3,
        BanChuToa: 3,
        SanKhau: 4,
        ManHinh: 4,
        CuaVao: 5,
        CuaThoatHiem: 5,
        CuaRaVao: 5
    };

    // Vẽ sân khấu, lối đi, bàn và cửa.
    [...components]
        .sort((a, b) =>
            (partOrder[getType(a)] ?? 10) -
            (partOrder[getType(b)] ?? 10)
        )
        .forEach(part => {
            const x = getValue(part, "x", "viTriX");
            const y = getValue(part, "y", "viTriY");
            const w = getValue(part, "width", "rong", "chieuRong");
            const h = getValue(part, "height", "cao", "chieuCao");

            const type = getType(part);
            const name = getName(part);

            if (![x, y, w, h].every(Number.isFinite)) return;
            if (w <= 0 || h <= 0) return;

            const isRoundTable = type === "BanTron";
            const isUShape = type === "BanChuU";
            const isHeadTable = type === "BanChuToa";

            const isStage =
                type === "SanKhau" ||
                type === "ManHinh";

            const isDoor =
                type === "CuaVao" ||
                type === "CuaThoatHiem" ||
                type === "CuaRaVao";

            const isAisle = type === "LoiDi";
            const isObstacle = type === "VatCan";

            // Bàn chữ U được vẽ chung ở phần dưới.
            if (isUShape) return;

            let fill;
            let stroke;
            let textColor;

            if (isStage && isWorkshop) {
                fill = "#956022";
                stroke = "#956022";
                textColor = "#ffffff";
            } else if (isStage) {
                fill = "#e1eaf6";
                stroke = "#9cb4d3";
                textColor = "#344e71";
            } else if (isDoor) {
                fill = "#e8f5ee";
                stroke = "#80c9a5";
                textColor = "#27694d";
            } else if (isAisle) {
                fill = "url(#adminSeatAislePattern)";
                stroke = "none";
                textColor = "#27694d";
            } else if (isRoundTable || isHeadTable) {
                fill = "#f7e9d4";
                stroke = "#a98552";
                textColor = "#76512c";
            } else {
                fill = "#e7ebef";
                stroke = "#bdc6d2";
                textColor = "#27694d";
            }

            const group = add("g", {});
            let shape;

            if (isRoundTable) {
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

            if (!isAisle && !isObstacle) {
                addText(
                    x + w / 2,
                    y + h / 2,
                    name,
                    isRoundTable ? 12 : isStage ? 13 : isHeadTable ? 12 : 10,
                    textColor,
                    group
                );
            }
        });

    // Vẽ bàn chữ U thành một hình liền mạch.
    if (layout === "u") {
        const uParts = components
            .filter(part => getType(part) === "BanChuU")
            .map(part => ({
                x: getValue(part, "x", "viTriX"),
                y: getValue(part, "y", "viTriY"),
                width: getValue(part, "width", "rong", "chieuRong"),
                height: getValue(part, "height", "cao", "chieuCao")
            }))
            .filter(part =>
                [part.x, part.y, part.width, part.height]
                    .every(Number.isFinite)
            );

        const verticalParts = uParts
            .filter(part => part.height > part.width);

        const horizontalParts = uParts
            .filter(part => part.width >= part.height);

        const leftLeg = verticalParts
            .sort((a, b) => a.x - b.x)[0];

        const rightLeg = verticalParts
            .sort((a, b) => b.x - a.x)[0];

        const bottomBar = horizontalParts
            .sort((a, b) => b.y - a.y)[0];

        if (leftLeg && rightLeg && bottomBar) {
            const left = leftLeg.x;
            const right = rightLeg.x + rightLeg.width;

            const outerTop = Math.min(
                leftLeg.y,
                rightLeg.y
            );

            const innerLeft = leftLeg.x + leftLeg.width;
            const innerRight = rightLeg.x;
            const innerBottom = bottomBar.y;

            const bottom = Math.max(
                leftLeg.y + leftLeg.height,
                rightLeg.y + rightLeg.height,
                bottomBar.y + bottomBar.height
            );

            const path = add("path", {
                d: [
                    `M ${left} ${outerTop}`,
                    `H ${innerLeft}`,
                    `V ${innerBottom}`,
                    `H ${innerRight}`,
                    `V ${outerTop}`,
                    `H ${right}`,
                    `V ${bottom}`,
                    `H ${left}`,
                    "Z"
                ].join(" "),
                fill: "#f7e9d4",
                stroke: "#a98552",
                "stroke-width": 2,
                "stroke-linejoin": "round"
            });

            addTitle(path, "Bàn chủ tọa chữ U");
        }
    }

    // Vẽ ghế.
    let booked = 0;

    const seatGroup = add("g", {});
    const labelGroup = add("g", {});

    validSeats.forEach(seat => {
        const x = Number(seat.x);
        const y = Number(seat.y);

        const row = String(seat.row ?? seat.hang ?? "");
        const col = Number(seat.col ?? seat.cot ?? 0);

        const code = String(
            seat.code ?? `${row}${col}`
        );

        const zone = String(
            seat.zone ??
            seat.khu ??
            seat.tenLoai ??
            "Thường"
        );

        const isVip = zone.toLowerCase().includes("vip");

        const status = String(
            seat.trangThai ??
            seat.status ??
            "Trong"
        ).trim().toLowerCase();

        const isBooked = ![
            "trong",
            "trống",
            "available",
            "con trong",
            "còn trống"
        ].includes(status);

        if (isBooked) booked++;

        const fill = isBooked
            ? "#e0e5eb"
            : "#ffffff";

        const stroke = isBooked
            ? "#a0aab7"
            : isVip
                ? "#d4a51e"
                : "#80c783";

        const textColor = isBooked
            ? "#748092"
            : isVip
                ? "#9a710a"
                : "#32763b";

        let seatShape;

        if (isWorkshop) {
            // Workshop: ghế hình tròn.
            seatShape = add("circle", {
                cx: x + seatWidth / 2,
                cy: y + seatHeight / 2,
                r: Math.min(seatWidth, seatHeight) / 2,
                fill,
                stroke,
                "stroke-width": 1.5
            }, seatGroup);
        } else {
            // Sự kiện trực tiếp: ghế hình chữ nhật.
            seatShape = add("rect", {
                x,
                y,
                width: seatWidth,
                height: seatHeight,
                rx: 4,
                fill,
                stroke,
                "stroke-width": validSeats.length > 500 ? 0.7 : 1.3
            }, seatGroup);
        }

        addTitle(
            seatShape,
            `Ghế ${code} | ${zone} | ${isBooked ? "Đã đặt" : "Còn trống"}`
        );

        const label = addText(
            x + seatWidth / 2,
            y + seatHeight / 2,
            code,
            isWorkshop
                ? 10
                : Math.max(
                    6,
                    Math.min(10, seatWidth * 0.19, seatHeight * 0.22)
                ),
            textColor,
            labelGroup
        );

        seatLabels.push(label);
    });

    // Cập nhật thống kê.
    const totalEl = document.getElementById("adminSeatTotal");
    const bookedEl = document.getElementById("adminSeatBooked");

    if (totalEl) {
        totalEl.textContent = validSeats.length.toLocaleString("vi-VN");
    }

    if (bookedEl) {
        bookedEl.textContent = booked.toLocaleString("vi-VN");
    }

    // Điều khiển phóng to, thu nhỏ.
    const baseWidth = Math.max(
        500,
        Math.min(1100, (viewport?.clientWidth || 940) - 40)
    );

    function updateZoom() {
        const actualWidth = Math.round(baseWidth * zoom);

        svg.style.width = `${actualWidth}px`;
        svg.style.height = "auto";

        const zoomLabel = document.getElementById("adminSeatZoomValue");

        if (zoomLabel) {
            zoomLabel.textContent = `${Math.round(zoom * 100)}%`;
        }

        const showLabels =
            validSeats.length <= 500 ||
            zoom >= 1.5;

        seatLabels.forEach(label => {
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
