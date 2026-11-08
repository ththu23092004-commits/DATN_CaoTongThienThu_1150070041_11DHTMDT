
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("workshopDesigner");
    if (!root) return;

    const form = root.closest("form");
    const map = document.getElementById("wsMap");
    const layoutType = document.getElementById("wsLayoutType");

    if (!map || !layoutType) return;

    const WIDTH = 1000;
    const HEIGHT = 700;

    const removed = new Set();
    const byId = id => document.getElementById(id);

    let currentLayout = "round";

    const clamp = (value, min, max) =>
        Math.max(min, Math.min(max, value));

    function isUSelected() {
        return String(layoutType.value || "")
            .trim()
            .toLowerCase() === "u";
    }

    function makeNode(tag, className, content = "") {
        const node = document.createElement(tag);
        node.className = className;
        node.textContent = content;
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

    function evenlySpaced(count, start, end) {
        if (count === 1) {
            return [(start + end) / 2];
        }

        return Array.from(
            { length: count },
            (_, index) =>
                start + (end - start) * index / (count - 1)
        );
    }

    function addComponent(components, type, name, x, y, width, height) {
        components.push({
            type,
            name,
            x,
            y,
            width,
            height
        });
    }

    function addSeat(seats, code, centerX, centerY) {
        const size = 48;
        const isRemoved = removed.has(code);

        const seat = makeNode(
            "button",
            `ws-seat${isRemoved ? " removed" : ""}`,
            code
        );

        seat.type = "button";

        setCircle(seat, centerX, centerY, size);

        seat.title = isRemoved
            ? `Thêm lại ghế ${code}`
            : `Loại bỏ ghế ${code}`;

        seat.addEventListener("click", () => {
            if (currentLayout !== "u") return;

            if (removed.has(code)) {
                removed.delete(code);
            } else {
                removed.add(code);
            }

            renderUShape();
        });

        map.appendChild(seat);

        if (!isRemoved) {
            seats.push({
                row: code.charAt(0),
                col: Number(code.substring(1)),
                code,
                x: Math.round((centerX - size / 2) * 100) / 100,
                y: Math.round((centerY - size / 2) * 100) / 100,
                zone: "Thuong"
            });
        }
    }

    function updateSummary(total) {
        const totalEl = byId("wsTotal");
        const normalEl = byId("wsNormalTotal");
        const capacity = byId("SoChoNgoiWorkshop");
        const quantity = byId("wsTicketQuantity");
        const ticketName = byId("wsTicketName");

        if (totalEl) totalEl.textContent = total;
        if (normalEl) normalEl.textContent = total;

        if (capacity) capacity.value = total;
        if (quantity) quantity.value = total;

        if (ticketName) ticketName.value = "Thường";

        const warning = byId("wsWarning");

        if (warning) {
            warning.hidden = total > 0;
        }
    }

    function saveJson(seats, components) {
        const hidden = byId("SoDoJsonWorkshop");

        if (!hidden) {
            throw new Error("Không tìm thấy ô lưu JSON sơ đồ Workshop.");
        }

        const data = {
            width: WIDTH,
            height: HEIGHT,
            layout: "u",
            seats,
            components
        };

        hidden.value = JSON.stringify(data);
    }

    function renderUShape() {
        if (!isUSelected()) return [];

        currentLayout = "u";
        map.replaceChildren();

        const leftCount = clamp(
            parseInt(byId("wsULeftCount")?.value || "3", 10),
            1,
            5
        );

        const rightCount = clamp(
            parseInt(byId("wsURightCount")?.value || "3", 10),
            1,
            5
        );

        const bottomCount = clamp(
            parseInt(byId("wsUBackCount")?.value || "3", 10),
            1,
            4
        );

        const showDoor = byId("wsUShowDoor")?.checked ?? true;
        const showHeadTable = byId("wsUShowHeadTable")?.checked ?? true;

        const seats = [];
        const components = [];

        // Khu vực trình bày
        const stage = makeNode(
            "div",
            "ws-stage",
            "KHU VỰC TRÌNH BÀY"
        );

        setRect(stage, 340, 20, 320, 55);
        map.appendChild(stage);

        addComponent(
            components,
            "SanKhau",
            "Khu vực trình bày",
            340, 20, 320, 55
        );

        // Ba cạnh bàn chữ U
        const leftX = 230;
        const topY = 190;
        const legWidth = 100;
        const legHeight = 370;
        const rightX = 670;
        const bottomY = 470;
        const bottomWidth = 540;
        const bottomHeight = 90;

        const tableParts = [
            {
                name: "Cạnh trái bàn chữ U",
                className: "ws-u-part ws-u-left",
                x: leftX,
                y: topY,
                width: legWidth,
                height: legHeight
            },
            {
                name: "Cạnh phải bàn chữ U",
                className: "ws-u-part ws-u-right",
                x: rightX,
                y: topY,
                width: legWidth,
                height: legHeight
            },
            {
                name: "Cạnh dưới bàn chữ U",
                className: "ws-u-part ws-u-bottom",
                x: leftX,
                y: bottomY,
                width: bottomWidth,
                height: bottomHeight
            }
        ];

        tableParts.forEach(part => {
            const node = makeNode("div", part.className);

            setRect(
                node,
                part.x,
                part.y,
                part.width,
                part.height
            );

            map.appendChild(node);

            addComponent(
                components,
                "BanChuU",
                part.name,
                part.x,
                part.y,
                part.width,
                part.height
            );
        });

        // Bàn chủ tọa
        if (showHeadTable) {
            const headX = 385;
            const headY = 100;
            const headWidth = 230;
            const headHeight = 55;

            const head = makeNode(
                "div",
                "ws-u-head-table",
                "BÀN CHỦ TỌA"
            );

            setRect(
                head,
                headX,
                headY,
                headWidth,
                headHeight
            );

            map.appendChild(head);

            addComponent(
                components,
                "BanChuToa",
                "Bàn chủ tọa",
                headX,
                headY,
                headWidth,
                headHeight
            );
        }

        // Ghế cạnh trái
        evenlySpaced(leftCount, 235, 445).forEach((y, index) => {
            addSeat(seats, `L${index + 1}`, leftX - 65, y);
        });

        // Ghế cạnh phải
        evenlySpaced(rightCount, 235, 445).forEach((y, index) => {
            addSeat(seats, `R${index + 1}`, rightX + legWidth + 65, y);
        });

        // Ghế cạnh dưới
        evenlySpaced(
            bottomCount,
            leftX + 90,
            rightX + legWidth - 90
        ).forEach((x, index) => {
            addSeat(
                seats,
                `D${index + 1}`,
                x,
                bottomY + bottomHeight + 40
            );
        });

        // Cửa ra vào
        if (showDoor) {
            const door = makeNode(
                "div",
                "ws-u-door",
                "CỬA RA VÀO"
            );

            setRect(door, 415, 660, 170, 32);
            map.appendChild(door);

            addComponent(
                components,
                "CuaVao",
                "Cửa ra vào",
                415, 660, 170, 32
            );
        }

        updateSummary(seats.length);
        saveJson(seats, components);

        return seats;
    }

    function getRoundControlGroups() {
        return [
            byId("wsTableCount")?.closest(".ws-control-group"),
            byId("wsSeatsPerTable")?.closest(".ws-control-group")
        ].filter(Boolean);
    }

    function switchLayout() {
        const isU = isUSelected();

        currentLayout = isU ? "u" : "round";

        const uControls = byId("wsUControls");

        if (uControls) {
            uControls.style.display = isU ? "block" : "none";
        }

        getRoundControlGroups().forEach(group => {
            group.style.display = isU ? "none" : "";
        });

        if (isU) {
            renderUShape();
        }
    }

    [
        "wsULeftCount",
        "wsURightCount",
        "wsUBackCount",
        "wsUShowDoor",
        "wsUShowHeadTable"
    ].forEach(id => {
        byId(id)?.addEventListener("change", () => {
            if (isUSelected()) {
                renderUShape();
            }
        });
    });

    layoutType.addEventListener("change", switchLayout);

    byId("wsRestore")?.addEventListener("click", () => {
        if (!isUSelected()) return;

        removed.clear();
        renderUShape();
    });

    // Kiểm tra và lưu lại JSON ngay trước khi gửi form
    if (form) {
        form.addEventListener("submit", event => {
            if (!isUSelected()) return;

            const workshopBlock = root.closest("#block-workshop");

            if (!workshopBlock ||
                workshopBlock.style.display === "none") {
                return;
            }

            const seats = renderUShape();

            if (!seats.length) {
                event.preventDefault();
                alert("Workshop bàn chữ U phải có ít nhất một ghế.");
                return;
            }

            const price = workshopBlock.querySelector('[name="GiaVe2"]');
            const priceValue = Number(price?.value);

            if (!price ||
                !Number.isFinite(priceValue) ||
                priceValue <= 0) {
                event.preventDefault();
                alert("Vui lòng nhập giá vé Workshop hợp lệ.");
                price?.focus();
                return;
            }

            const hidden = byId("SoDoJsonWorkshop");

            if (!hidden?.value) {
                event.preventDefault();
                alert("Không lưu được sơ đồ bàn chữ U. Vui lòng thiết kế lại.");
                return;
            }

            try {
                const saved = JSON.parse(hidden.value);

                if (
                    saved.layout !== "u" ||
                    !Array.isArray(saved.components) ||
                    saved.components.filter(x => x.type === "BanChuU").length !== 3 ||
                    saved.components.some(x => x.type === "BanTron")
                ) {
                    event.preventDefault();
                    alert("Dữ liệu bàn chữ U chưa đúng. Vui lòng thử thiết kế lại.");
                }
            } catch {
                event.preventDefault();
                alert("Dữ liệu sơ đồ bàn chữ U không hợp lệ.");
            }
        }, true);
    }

    switchLayout();
});
