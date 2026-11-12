
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("seatDesigner");
    if (!root) return;

    const el = id => document.getElementById(id);

const form = root.closest("form");
const map = el("sdMap");
const removed = new Set();
let currentZoom = 1;

    const controls = {
        preset: el("sdPreset"),
        rows: el("sdRows"),
        cols: el("sdCols"),
        vipRows: el("sdVipRows"),
        verticalAisle: el("sdVerticalAisle"),
        horizontalAisle: el("sdHorizontalAisle"),
        stagePosition: el("sdStagePosition"),
        showDoors: el("sdShowDoors")
    };
function applyZoom() {
    map.style.transform = `scale(${currentZoom})`;
}
    const limit = (number, min, max) =>
        Math.min(max, Math.max(min, number));

    const numberValue = (input, fallback, min, max) => {
        const value = Number.parseInt(input.value, 10);
        return limit(Number.isFinite(value) ? value : fallback, min, max);
    };

    const getOptions = () => ({
        rows: numberValue(controls.rows, 6, 2, 25),
cols: numberValue(controls.cols, 10, 4, 40),
        vipRows: numberValue(controls.vipRows, 2, 0, 20),
        verticalAisle: controls.verticalAisle.checked,
        horizontalAisle: controls.horizontalAisle.checked,
        stagePosition: controls.stagePosition.value,
        showDoors: controls.showDoors.checked
    });

    const makeElement = (className, text) => {
        const div = document.createElement("div");
        div.className = className;
        if (text) div.textContent = text;
        return div;
    };

    const setPosition = (element, x, y, width, height) => {
        element.style.left = `${x / 10}%`;
        element.style.top = `${y / 7}%`;
        element.style.width = `${width / 10}%`;
        element.style.height = `${height / 7}%`;
    };

    function render() {
        const o = getOptions();

        controls.rows.value = o.rows;
        controls.cols.value = o.cols;
        controls.vipRows.value = Math.min(o.vipRows, o.rows);

        map.replaceChildren();

        const stage = makeElement("sd-stage", "SÂN KHẤU / MÀN HÌNH");
        const stageY = o.stagePosition === "top" ? 20 : 620;
        setPosition(stage, 270, stageY, 460, 50);
        map.appendChild(stage);

        const seatTop = o.stagePosition === "top" ? 120 : 70;
        const seatBottom = o.stagePosition === "top" ? 610 : 570;

        const usableHeight = seatBottom - seatTop;
        const horizontalGap = o.horizontalAisle ? 40 : 0;
        const verticalGap = o.verticalAisle ? 58 : 0;

        const gapX = Math.min(
            5,
            (800 - verticalGap) / o.cols * 0.12
        );

        const gapY = Math.min(
            6,
            (usableHeight - horizontalGap) / o.rows * 0.12
        );

        const cellWidth = (800 - verticalGap) / o.cols;
        const cellHeight = (usableHeight - horizontalGap) / o.rows;

        const seatWidth = Math.max(7, cellWidth - gapX);
        const seatHeight = Math.max(7, cellHeight - gapY);

        const middleCol = Math.ceil(o.cols / 2);
        const middleRow = Math.ceil(o.rows / 2);

        const seats = [];
        const components = [];

        components.push({
            type: "SanKhau",
            name: "Sân khấu / Màn hình",
            x: 270,
            y: stageY,
            width: 460,
            height: 50
        });

        if (o.verticalAisle) {
            const aisleX = 100 + middleCol * cellWidth;
            const aisle = makeElement("sd-aisle");
            setPosition(aisle, aisleX, seatTop, verticalGap, usableHeight);
            map.appendChild(aisle);

            components.push({
                type: "LoiDi",
                name: "Lối đi giữa",
                x: aisleX,
                y: seatTop,
                width: verticalGap,
                height: usableHeight
            });
        }

        if (o.horizontalAisle) {
            const aisleY = seatTop + middleRow * cellHeight;

            const aisle = makeElement("sd-aisle");
            setPosition(aisle, 80, aisleY, 840, horizontalGap);
            map.appendChild(aisle);

            components.push({
                type: "LoiDi",
                name: "Lối đi ngang",
                x: 80,
                y: aisleY,
                width: 840,
                height: horizontalGap
            });
        }

        if (o.showDoors) {
            [
                {
                    type: "CuaVao",
                    name: "CỬA VÀO",
                    x: 15,
                    y: 340,
                    width: 100,
                    height: 32
                },
                {
                    type: "CuaThoatHiem",
                    name: "THOÁT HIỂM",
                    x: 865,
                    y: 340,
                    width: 125,
                    height: 32
                }
            ].forEach(doorData => {
                const door = makeElement("sd-door", doorData.name);
                setPosition(
                    door,
                    doorData.x,
                    doorData.y,
                    doorData.width,
                    doorData.height
                );
                map.appendChild(door);
                components.push(doorData);
            });
        }
const isLargeLayout = o.rows >= 20 || o.cols >= 30;
map.classList.toggle("large-layout", isLargeLayout);

for (let r = 0; r < o.rows; r++) {
    const rowName = String.fromCharCode(65 + r);

    const isVip = o.stagePosition === "top"
        ? r < o.vipRows
        : r >= o.rows - o.vipRows;


    for (let c = 0; c < o.cols; c++) {
        const seatCode = `${rowName}${c + 1}`;

        let x;
        let y;

        if (isLargeLayout) {
            const leftCols = 10;
            const centerCols = 20;
            const rightCols = o.cols - leftCols - centerCols;

            const leftStart = 80;
            const leftWidth = 200;

            const centerStart = 330;
            const centerWidth = 340;

            const rightStart = 730;
            const rightWidth = 200;

            if (c < leftCols) {
                const localCol = c;
                const leftCellWidth = leftWidth / leftCols;
                x = leftStart + localCol * leftCellWidth;
            } else if (c < leftCols + centerCols) {
                const localCol = c - leftCols;
                const centerCellWidth = centerWidth / centerCols;
                x = centerStart + localCol * centerCellWidth;
            } else {
                const localCol = c - leftCols - centerCols;
                const rightCellWidth = rightWidth / Math.max(1, rightCols);
                x = rightStart + localCol * rightCellWidth;
            }

            const rowGap = 4;
            const seatRowHeight = (usableHeight - ((o.rows - 1) * rowGap)) / o.rows;
            y = seatTop + r * (seatRowHeight + rowGap);

            const isRemoved = removed.has(seatCode);

            const seat = document.createElement("button");
            seat.type = "button";
            seat.className = `sd-seat ${isVip ? "vip" : "normal"}`;

            if (isRemoved) {
                seat.classList.add("removed");
            }

            if (currentZoom < 1.2) {
                seat.classList.add("hide-label");
            }

            seat.textContent = seatCode;
            seat.title = isRemoved
                ? `Thêm lại ghế ${seatCode}`
                : `Loại bỏ ghế ${seatCode}`;

            setPosition(seat, x, y, 16, 16);

            seat.addEventListener("click", () => {
                if (removed.has(seatCode)) {
                    removed.delete(seatCode);
                } else {
                    removed.add(seatCode);
                }
                render();
            });

            map.appendChild(seat);

            if (!isRemoved) {
                seats.push({
                    row: rowName,
                    col: c + 1,
                    code: seatCode,
                    x: Math.round(x * 100) / 100,
                    y: Math.round(y * 100) / 100,
                    zone: isVip ? "VIP" : "Thuong"
                });
            }

            continue;
        }

        x = 100 + c * cellWidth +
            (o.verticalAisle && c >= middleCol ? verticalGap : 0);

        y = seatTop + r * cellHeight +
            (o.horizontalAisle && r >= middleRow ? horizontalGap : 0);

        const isRemoved = removed.has(seatCode);

        const seat = document.createElement("button");
        seat.type = "button";
        seat.className = `sd-seat ${isVip ? "vip" : "normal"}`;

        if (isRemoved) {
            seat.classList.add("removed");
        }

        seat.textContent = seatCode;
        seat.title = isRemoved
            ? `Thêm lại ghế ${seatCode}`
            : `Loại bỏ ghế ${seatCode}`;

        setPosition(seat, x, y, seatWidth, seatHeight);

        seat.addEventListener("click", () => {
            if (removed.has(seatCode)) {
                removed.delete(seatCode);
            } else {
                removed.add(seatCode);
            }
            render();
        });

        map.appendChild(seat);

        if (!isRemoved) {
            seats.push({
                row: rowName,
                col: c + 1,
                code: seatCode,
                x: Math.round(x * 100) / 100,
                y: Math.round(y * 100) / 100,
                zone: isVip ? "VIP" : "Thuong"
            });
        }
    }
}
if (isLargeLayout) {
    [
        { text: "KHU TRÁI", x: 120, y: 95 },
        { text: "KHU GIỮA", x: 445, y: 95 },
        { text: "KHU PHẢI", x: 785, y: 95 }
    ].forEach(item => {
        const zone = makeElement("sd-zone-label", item.text);
        zone.style.left = `${item.x / 10}%`;
        zone.style.top = `${item.y / 7}%`;
        map.appendChild(zone);
    });
}
        const vipCount = seats.filter(s => s.zone === "VIP").length;
        const normalCount = seats.length - vipCount;

        el("sdTotal").textContent = seats.length;
        el("sdVipTotal").textContent = vipCount;
        el("sdNormalTotal").textContent = normalCount;

        const capacity = document.querySelector(
            '#block-truc-tiep input[name="SoChoNgoi"]'
        );

        const rowInput = document.querySelector(
            '#block-truc-tiep input[name="SoHang"]'
        );

        const colInput = document.querySelector(
            '#block-truc-tiep input[name="SoCotMoiHang"]'
        );

        const vipFrom = document.querySelector(
            '#block-truc-tiep input[name="HangVipTu"]'
        );

        const vipTo = document.querySelector(
            '#block-truc-tiep input[name="HangVipDen"]'
        );

        if (capacity) capacity.value = seats.length;
        if (rowInput) rowInput.value = o.rows;
        if (colInput) colInput.value = o.cols;

        if (vipFrom) vipFrom.value = 1;
        if (vipTo) vipTo.value = Math.max(1, Math.min(o.vipRows, o.rows));

        const tickets1 = document.querySelector(
            '#block-truc-tiep input[name="SoLuongVe1"]'
        );

        const tickets2 = document.querySelector(
            '#block-truc-tiep input[name="SoLuongVe2"]'
        );

        if (tickets1) tickets1.value = vipCount;
        if (tickets2) tickets2.value = normalCount;

        el("SoDoJson").value = JSON.stringify({
            width: 1000,
            height: 700,
            seats,
            components
        });

        el("sdWarning").hidden = seats.length > 0;
applyZoom();
        return { seats, components };
    }

    function applyPreset() {
        removed.clear();

        const preset = controls.preset.value;

if (preset === "small") {
    controls.rows.value = 6;
    controls.cols.value = 10;
    controls.vipRows.value = 2;
} else if (preset === "medium") {
    controls.rows.value = 12;
    controls.cols.value = 20;
    controls.vipRows.value = 4;
} else if (preset === "large") {
    controls.rows.value = 25;
    controls.cols.value = 40;
    controls.vipRows.value = 6;
} else if (preset === "workshop") {
    controls.rows.value = 4;
    controls.cols.value = 8;
    controls.vipRows.value = 1;
}
currentZoom = preset === "large" ? 0.9 : 1;
applyZoom();
        render();
    }

    controls.preset.addEventListener("change", applyPreset);

    Object.entries(controls).forEach(([key, control]) => {
        if (key !== "preset") {
            control.addEventListener("change", render);
            control.addEventListener("input", render);
        }
    });

    el("sdRestore").addEventListener("click", () => {
        removed.clear();
        render();
    });

    form.addEventListener("submit", event => {
        const directBlock = el("block-truc-tiep");

        if (!directBlock || directBlock.style.display === "none") {
            return;
        }

        const data = render();

        if (data.seats.length === 0) {
            event.preventDefault();
            alert("Sơ đồ phải có ít nhất một ghế.");
            return;
        }

        const vip = data.seats.filter(s => s.zone === "VIP").length;
        const normal = data.seats.length - vip;

        const vipPrice = document.querySelector(
            '#block-truc-tiep input[name="GiaVe1"]'
        );

        const normalPrice = document.querySelector(
            '#block-truc-tiep input[name="GiaVe2"]'
        );

        if (
            (vip > 0 && (!vipPrice || Number(vipPrice.value) <= 0)) ||
            (normal > 0 && (!normalPrice || Number(normalPrice.value) <= 0))
        ) {
            event.preventDefault();
            alert("Vui lòng nhập giá vé cho các khu vực có ghế.");
        }
    });
el("sdZoomIn")?.addEventListener("click", () => {
    currentZoom = Math.min(2, currentZoom + 0.2);
    applyZoom();
    render();
});

el("sdZoomOut")?.addEventListener("click", () => {
    currentZoom = Math.max(0.6, currentZoom - 0.2);
    applyZoom();
    render();
});

el("sdZoomReset")?.addEventListener("click", () => {
    currentZoom = controls.preset.value === "large" ? 0.9 : 1;
    applyZoom();
    render();
});
    applyPreset();
});

