
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("workshopDesigner");
    if (!root) return;

    const form = root.closest("form");
    const map = document.getElementById("wsMap");
    const tableCount = document.getElementById("wsTableCount");
    const seatsPerTable = document.getElementById("wsSeatsPerTable");
    const layoutType = document.getElementById("wsLayoutType");

    if (!map || !tableCount || !seatsPerTable) return;

    const removed = new Set();

    const WIDTH = 1000;
    const HEIGHT = 700;

    const byId = id => document.getElementById(id);

    const clamp = (value, min, max) =>
        Math.max(min, Math.min(max, value));

function isRoundLayout() {
    return !layoutType ||
        layoutType.value.trim().toLowerCase() !== "u";
}

    function makeNode(tag, className, text) {
        const node = document.createElement(tag);
        node.className = className;

        if (text) {
            node.textContent = text;
        }

        return node;
    }

    function setRect(node, x, y, width, height) {
        node.style.left = `${x / WIDTH * 100}%`;
        node.style.top = `${y / HEIGHT * 100}%`;
        node.style.width = `${width / WIDTH * 100}%`;
        node.style.height = `${height / HEIGHT * 100}%`;
    }

    function setCircle(node, centerX, centerY, size) {
        setRect(
            node,
            centerX - size / 2,
            centerY - size / 2,
            size,
            size
        );
    }

    function getCenters(total) {
        const layouts = {
            1: [
                { x: 500, y: 410 }
            ],
            2: [
                { x: 350, y: 410 },
                { x: 650, y: 410 }
            ],
            3: [
                { x: 500, y: 275 },
                { x: 300, y: 530 },
                { x: 700, y: 530 }
            ],
            4: [
                { x: 330, y: 260 },
                { x: 670, y: 260 },
                { x: 330, y: 535 },
                { x: 670, y: 535 }
            ],
            5: [
                { x: 230, y: 270 },
                { x: 500, y: 270 },
                { x: 770, y: 270 },
                { x: 365, y: 535 },
                { x: 635, y: 535 }
            ],
            6: [
                { x: 220, y: 270 },
                { x: 500, y: 270 },
                { x: 780, y: 270 },
                { x: 220, y: 535 },
                { x: 500, y: 535 },
                { x: 780, y: 535 }
            ]
        };

        return layouts[total] || layouts[3];
    }

    function updateSummary(total) {
        const totalEl = byId("wsTotal");
        const normalEl = byId("wsNormalTotal");

        if (totalEl) {
            totalEl.textContent = total;
        }

        if (normalEl) {
            normalEl.textContent = total;
        }
    }

    function syncTicketInputs(total) {
        const capacity = byId("SoChoNgoiWorkshop");
        const quantity = byId("wsTicketQuantity");
        const ticketName = byId("wsTicketName");

        if (capacity) {
            capacity.value = total;
        }

        if (quantity) {
            quantity.value = total;
        }

        if (ticketName) {
            ticketName.value = "Thường";
        }
    }

    function saveJson(seats, components) {
        const hidden = byId("SoDoJsonWorkshop");

        if (!hidden || !isRoundLayout()) return;

        hidden.value = JSON.stringify({
            width: WIDTH,
            height: HEIGHT,
            layout: "round",
            seats: seats,
            components: components
        });
    }

    function render() {
        if (!isRoundLayout()) {
            return [];
        }

        const tables = clamp(
            parseInt(tableCount.value, 10) || 1,
            1,
            6
        );

        const perTable = clamp(
            parseInt(seatsPerTable.value, 10) || 6,
            2,
            6
        );

        tableCount.value = tables;
        seatsPerTable.value = perTable;

        const centers = getCenters(tables);

        map.replaceChildren();

        const seats = [];
        const components = [];

        const stageWidth = 320;
        const stageHeight = 60;
        const stageX = (WIDTH - stageWidth) / 2;
        const stageY = 45;

        const stage = makeNode(
            "div",
            "ws-stage",
            "KHU VỰC TRÌNH BÀY"
        );

        setRect(
            stage,
            stageX,
            stageY,
            stageWidth,
            stageHeight
        );

        map.appendChild(stage);

        components.push({
            type: "SanKhau",
            name: "Khu vực trình bày",
            x: stageX,
            y: stageY,
            width: stageWidth,
            height: stageHeight
        });

        const tableSize = 132;
        const seatSize = 48;
        const orbit = 108;

        centers.forEach((center, index) => {
            const row = String.fromCharCode(65 + index);

            const table = makeNode(
                "div",
                "ws-table",
                `BÀN ${index + 1}`
            );

            setCircle(
                table,
                center.x,
                center.y,
                tableSize
            );

            map.appendChild(table);

            components.push({
                type: "BanTron",
                name: `Bàn ${index + 1}`,
                x: center.x - tableSize / 2,
                y: center.y - tableSize / 2,
                width: tableSize,
                height: tableSize
            });

            for (let seatIndex = 0; seatIndex < perTable; seatIndex++) {
                const angle =
                    -Math.PI / 2 +
                    (2 * Math.PI * seatIndex / perTable);

                const seatX =
                    center.x + Math.cos(angle) * orbit;

                const seatY =
                    center.y + Math.sin(angle) * orbit;

                const code = `${row}${seatIndex + 1}`;
                const isRemoved = removed.has(code);

                const seat = makeNode(
                    "button",
                    `ws-seat${isRemoved ? " removed" : ""}`,
                    code
                );

                seat.type = "button";

                setCircle(
                    seat,
                    seatX,
                    seatY,
                    seatSize
                );

                seat.title = isRemoved
                    ? `Thêm lại ghế ${code}`
                    : `Loại bỏ ghế ${code}`;

                seat.addEventListener("click", () => {
                    if (!isRoundLayout()) return;

                    if (removed.has(code)) {
                        removed.delete(code);
                    } else {
                        removed.add(code);
                    }

                    render();
                });

                map.appendChild(seat);

                if (!isRemoved) {
                    seats.push({
                        row: row,
                        col: seatIndex + 1,
                        code: code,
                        x: Math.round(
                            (seatX - seatSize / 2) * 100
                        ) / 100,
                        y: Math.round(
                            (seatY - seatSize / 2) * 100
                        ) / 100,
                        zone: "Thuong"
                    });
                }
            }
        });

        updateSummary(seats.length);
        syncTicketInputs(seats.length);
        saveJson(seats, components);

        const warning = byId("wsWarning");

        if (warning) {
            warning.hidden = seats.length > 0;
        }

        return seats;
    }

    function rebuild() {
        if (!isRoundLayout()) return;

        removed.clear();
        render();
    }

    tableCount.addEventListener("change", rebuild);
    seatsPerTable.addEventListener("change", rebuild);

    const restoreButton = byId("wsRestore");

    if (restoreButton) {
        restoreButton.addEventListener("click", () => {
            if (!isRoundLayout()) return;

            removed.clear();
            render();
        });
    }

if (form) {
    form.addEventListener("submit", event => {
        const selectedLayout = String(layoutType?.value || "")
            .trim()
            .toLowerCase();

        if (selectedLayout === "u") {
            return;
        }

        const hidden = byId("SoDoJsonWorkshop");

        if (!hidden || !hidden.value) {
            event.preventDefault();
            alert("Không tìm thấy dữ liệu sơ đồ bàn tròn.");
            return;
        }

        let savedData;

        try {
            savedData = JSON.parse(hidden.value);
        } catch {
            event.preventDefault();
            alert("Dữ liệu sơ đồ bàn tròn không hợp lệ.");
            return;
        }

        if (savedData.layout !== "round") {
            event.preventDefault();
            alert("Kiểu sơ đồ không khớp với lựa chọn bàn tròn.");
            return;
        }

            const workshopBlock = root.closest("#block-workshop");

            if (!workshopBlock ||
                workshopBlock.style.display === "none") {
                return;
            }

            const tableInput = workshopBlock.querySelector("#wsTableCount");

            if (!tableInput || tableInput.disabled) {
                return;
            }

            const seats = render();

            if (seats.length === 0) {
                event.preventDefault();
                alert("Workshop phải có ít nhất một ghế.");
                return;
            }

            const price = workshopBlock.querySelector('[name="GiaVe2"]');

            if (!price || Number(price.value) <= 0) {
                event.preventDefault();

                alert("Vui lòng nhập giá vé Workshop hợp lệ.");

                if (price) {
                    price.focus();
                }

                return;
            }
        });
    }

    if (layoutType) {
        layoutType.addEventListener("change", () => {
            if (isRoundLayout()) {
                render();
            }
        });
    }

    if (isRoundLayout()) {
        render();
    }
});
