
"use strict";

(() => {
    const NS = "http://www.w3.org/2000/svg";

    document.addEventListener("DOMContentLoaded", init);

    function init() {
        const map =
            document.getElementById("detailSeatMap") ||
            document.getElementById("adminSeatMap") ||
            document.querySelector("[data-seat-map]");

        const source =
            document.getElementById("detailSeatMapData") ||
            document.getElementById("adminSeatMapData") ||
            document.querySelector("[data-seat-map-data]");

        if (!map || !source) return;

        let data;

        try {
            data = JSON.parse(source.textContent || "null");
        } catch (error) {
            console.error("Không đọc được dữ liệu sơ đồ:", error);
            showEmpty();
            return;
        }

        if (!data) {
            showEmpty();
            return;
        }

        const seats = normalizeSeats(
            data.seats ?? data.ghe ?? data.Seats ?? []
        );

        const parts = normalizeParts(
            data.components ??
            data.thanhPhan ??
            data.ThanhPhanSoDos ??
            []
        );

        if (!seats.length && !parts.length) {
            showEmpty();
            return;
        }

        const width = Number(
            data.width ?? data.chieuRong ?? data.ChieuRong
        ) || 1000;

        const height = Number(
            data.height ?? data.chieuCao ?? data.ChieuCao
        ) || 700;

        const layout = String(
            data.layout ?? data.kieuSoDo ?? data.Layout ?? ""
        ).trim().toLowerCase();

        const isU = layout === "u" ||
            parts.some(p => p.type === "BanChuU");

        let zoom = 1;

        const svg = createSvg();

        svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
        svg.setAttribute("preserveAspectRatio", "xMidYMid meet");

        map.replaceChildren(svg);
        map.classList.add("detail-seat-map");

        drawBackground();
        drawComponents();
        drawSeats();

        setupZoom();

        function showEmpty() {
            map.replaceChildren();

            const empty = document.createElement("div");
            empty.className = "detail-seat-empty";
            empty.textContent = "Sự kiện chưa có sơ đồ chỗ ngồi.";

            map.appendChild(empty);
        }

        function createSvg() {
            if (map.namespaceURI === NS && map.tagName.toLowerCase() === "svg") {
                return map;
            }

            const element = document.createElementNS(NS, "svg");
            element.classList.add("detail-seat-svg");
            return element;
        }

        function normalizeSeats(items) {
            if (!Array.isArray(items)) return [];

            return items.map((seat, index) => ({
                x: Number(seat.x ?? seat.viTriX ?? seat.ViTriX),
                y: Number(seat.y ?? seat.viTriY ?? seat.ViTriY),
                row: seat.row ?? seat.hang ?? seat.Hang ?? "",
                col: seat.col ?? seat.cot ?? seat.Cot ?? index + 1,
                code: String(
                    seat.code ??
                    seat.maGhe ??
                    seat.MaGhe ??
                    `${seat.row ?? seat.hang ?? seat.Hang ?? ""}${seat.col ?? seat.cot ?? seat.Cot ?? index + 1}`
                ),
                zone: String(
                    seat.zone ??
                    seat.khu ??
                    seat.KhuVuc ??
                    seat.LoaiVe ??
                    "Thuong"
                ),
                status: String(
                    seat.trangThai ??
                    seat.status ??
                    seat.TrangThai ??
                    "Trong"
                )
            })).filter(s =>
                Number.isFinite(s.x) &&
                Number.isFinite(s.y)
            );
        }

        function normalizeParts(items) {
            if (!Array.isArray(items)) return [];

            return items.map(part => ({
                type: String(
                    part.type ??
                    part.loai ??
                    part.LoaiThanhPhan ??
                    ""
                ),
                name: String(
                    part.name ??
                    part.ten ??
                    part.TenHienThi ??
                    ""
                ),
                x: Number(part.x ?? part.viTriX ?? part.ViTriX),
                y: Number(part.y ?? part.viTriY ?? part.ViTriY),
                width: Number(
                    part.width ??
                    part.rong ??
                    part.chieuRong ??
                    part.ChieuRong
                ),
                height: Number(
                    part.height ??
                    part.cao ??
                    part.chieuCao ??
                    part.ChieuCao
                ),
                rotation: Number(
                    part.rotation ??
                    part.gocXoay ??
                    part.GocXoay ??
                    0
                )
            })).filter(p =>
                [p.x, p.y, p.width, p.height].every(Number.isFinite) &&
                p.width > 0 &&
                p.height > 0
            );
        }

        function add(tag, attributes, parent = svg) {
            const node = document.createElementNS(NS, tag);

            Object.entries(attributes).forEach(([key, value]) => {
                node.setAttribute(key, String(value));
            });

            parent.appendChild(node);
            return node;
        }

        function addText(x, y, content, size, color, parent = svg) {
            const node = add("text", {
                x,
                y,
                "text-anchor": "middle",
                "dominant-baseline": "middle",
                "font-size": size,
                "font-family": "Arial, sans-serif",
                "font-weight": 600,
                fill: color
            }, parent);

            node.textContent = content;
            return node;
        }

        function addTitle(node, content) {
            const title = document.createElementNS(NS, "title");
            title.textContent = content;
            node.appendChild(title);
        }

        function drawBackground() {
            add("rect", {
                x: 0,
                y: 0,
                width,
                height,
                fill: "#ffffff"
            });
        }

        function drawComponents() {
            const uParts = parts.filter(p => p.type === "BanChuU");
            const normalParts = parts.filter(p => p.type !== "BanChuU");

            // Vẽ bàn chữ U thành một khối thống nhất.
            if (uParts.length >= 3) {
                drawUShape(uParts);
            } else {
                uParts.forEach(drawRectangleComponent);
            }

            // Vẽ các thành phần còn lại.
            normalParts
                .sort((a, b) => componentOrder(a.type) - componentOrder(b.type))
                .forEach(part => {
                    if (part.type === "BanTron") {
                        drawRoundTable(part);
                    } else {
                        drawRectangleComponent(part);
                    }
                });
        }

        function componentOrder(type) {
            const order = {
                LoiDi: 1,
                VatCan: 2,
                BanTron: 3,
                BanChuToa: 4,
                SanKhau: 5,
                ManHinh: 6,
                CuaVao: 7,
                CuaRaVao: 7,
                CuaThoatHiem: 8
            };

            return order[type] ?? 10;
        }

        function drawRoundTable(part) {
            const cx = part.x + part.width / 2;
            const cy = part.y + part.height / 2;
            const radius = Math.min(part.width, part.height) / 2;

            const group = add("g", {});

            if (part.rotation) {
                group.setAttribute(
                    "transform",
                    `rotate(${part.rotation} ${cx} ${cy})`
                );
            }

            const circle = add("circle", {
                cx,
                cy,
                r: radius,
                fill: "#f3e3c8",
                stroke: "#a98552",
                "stroke-width": 2.5
            }, group);

            addTitle(circle, part.name || "Bàn tròn");

            addText(
                cx,
                cy,
                part.name || "Bàn",
                13,
                "#76512c",
                group
            );
        }

        function drawUShape(uParts) {
            const left = uParts.find(p =>
                /trái/i.test(p.name)
            );

            const right = uParts.find(p =>
                /phải/i.test(p.name)
            );

            const bottom = uParts.find(p =>
                /dưới/i.test(p.name)
            );

            if (!left || !right || !bottom) {
                uParts.forEach(drawRectangleComponent);
                return;
            }

            const x1 = left.x;
            const x2 = left.x + left.width;
            const x3 = right.x;
            const x4 = right.x + right.width;

            const top = Math.min(left.y, right.y);
            const bottomY = bottom.y + bottom.height;
            const innerY = bottom.y;

            const path = [
                `M ${x1} ${top}`,
                `L ${x2} ${top}`,
                `L ${x2} ${innerY}`,
                `L ${x3} ${innerY}`,
                `L ${x3} ${top}`,
                `L ${x4} ${top}`,
                `L ${x4} ${bottomY}`,
                `L ${x1} ${bottomY}`,
                "Z"
            ].join(" ");

            const group = add("g", {});

            const shape = add("path", {
                d: path,
                fill: "#f3e3c8",
                stroke: "#493727",
                "stroke-width": 2.5,
                "stroke-linejoin": "round"
            }, group);

            addTitle(shape, "Bàn chữ U");

            // Bàn chủ tọa được vẽ riêng.
            const chairTable = parts.find(p => p.type === "BanChuToa");

            if (chairTable) {
                drawRectangleComponent(chairTable);
            }
        }

        function drawRectangleComponent(part) {
            const type = part.type;

            const isStage =
                type === "SanKhau" ||
                type === "ManHinh";

            const isDoor =
                type === "CuaVao" ||
                type === "CuaRaVao" ||
                type === "CuaThoatHiem";

            const isAisle = type === "LoiDi";
            const isObstacle = type === "VatCan";
            const isChairTable = type === "BanChuToa";

            let fill = "#e7ebef";
            let stroke = "#bdc6d2";
            let textColor = "#344e71";

            if (isStage) {
                fill = isU ? "#e1eaf6" : "#926024";
                stroke = isU ? "#9cb4d3" : "#926024";
                textColor = isU ? "#344e71" : "#ffffff";
            } else if (isDoor) {
                fill = "#e5f3ec";
                stroke = "#80bea0";
                textColor = "#28734f";
            } else if (isAisle) {
                fill = "#f0f3f7";
                stroke = "#e2e7ed";
            } else if (isChairTable) {
                fill = "#f3e3c8";
                stroke = "#a98552";
                textColor = "#76512c";
            }

            const cx = part.x + part.width / 2;
            const cy = part.y + part.height / 2;
            const group = add("g", {});

            if (part.rotation) {
                group.setAttribute(
                    "transform",
                    `rotate(${part.rotation} ${cx} ${cy})`
                );
            }

            const rect = add("rect", {
                x: part.x,
                y: part.y,
                width: part.width,
                height: part.height,
                rx: isAisle || isObstacle ? 2 : 9,
                fill,
                stroke,
                "stroke-width": isAisle ? 0.7 : 1.8
            }, group);

            addTitle(rect, part.name || type);

            if (!isAisle && !isObstacle && part.name) {
                addText(
                    cx,
                    cy,
                    part.name,
                    isStage ? 14 : 11,
                    textColor,
                    group
                );
            }
        }

        function drawSeats() {
            let booked = 0;

            const coordinates = seats.map(s => ({
                x: s.x,
                y: s.y
            }));

            const size = getSeatSize(coordinates);

            seats.forEach(seat => {
                const status = seat.status.trim().toLowerCase();

                const isBooked = ![
                    "trong",
                    "trống",
                    "available",
                    "0"
                ].includes(status);

                if (isBooked) booked++;

                const vip = seat.zone.toLowerCase().includes("vip");

                const fill = isBooked
                    ? "#dce2e9"
                    : "#dcecff";

                const stroke = isBooked
                    ? "#9ba8b8"
                    : vip
                        ? "#d4a51e"
                        : "#4e9aff";

                const textColor = isBooked
                    ? "#64748b"
                    : vip
                        ? "#9a710a"
                        : "#245a9b";

                const cx = seat.x + size / 2;
                const cy = seat.y + size / 2;

                const group = add("g", {});

                const circle = add("circle", {
                    cx,
                    cy,
                    r: size / 2,
                    fill,
                    stroke,
                    "stroke-width": 1.5
                }, group);

                addTitle(
                    circle,
                    `Ghế ${seat.code} - ${seat.zone} - ${isBooked ? "Đã đặt" : "Còn trống"}`
                );

                addText(
                    cx,
                    cy,
                    seat.code,
                    Math.max(8, Math.min(11, size * 0.23)),
                    textColor,
                    group
                );
            });

            updateStatistics(seats.length, booked);
        }

        function getSeatSize(coordinates) {
            const xs = [...new Set(coordinates.map(p => p.x))]
                .sort((a, b) => a - b);

            const ys = [...new Set(coordinates.map(p => p.y))]
                .sort((a, b) => a - b);

            const gaps = [];

            [xs, ys].forEach(values => {
                for (let i = 1; i < values.length; i++) {
                    const gap = values[i] - values[i - 1];

                    if (gap >= 35 && gap <= 150) {
                        gaps.push(gap);
                    }
                }
            });

            if (!gaps.length) return 50;

            const sorted = gaps.sort((a, b) => a - b);
            const median = sorted[Math.floor(sorted.length / 2)];

            return Math.max(38, Math.min(56, median * 0.48));
        }

        function updateStatistics(total, booked) {
            const totalElement =
                document.getElementById("adminSeatTotal") ||
                document.getElementById("detailSeatTotal");

            const bookedElement =
                document.getElementById("adminSeatBooked") ||
                document.getElementById("detailSeatBooked");

            if (totalElement) {
                totalElement.textContent = total.toLocaleString("vi-VN");
            }

            if (bookedElement) {
                bookedElement.textContent = booked.toLocaleString("vi-VN");
            }
        }

        function setupZoom() {
            const zoomIn =
                document.getElementById("adminSeatZoomIn") ||
                document.getElementById("detailSeatZoomIn");

            const zoomOut =
                document.getElementById("adminSeatZoomOut") ||
                document.getElementById("detailSeatZoomOut");

            const zoomReset =
                document.getElementById("adminSeatZoomReset") ||
                document.getElementById("detailSeatZoomReset");

            const zoomLabel =
                document.getElementById("adminSeatZoomValue") ||
                document.getElementById("detailSeatZoomValue");

            function applyZoom() {
                svg.style.width = `${zoom * 100}%`;
                svg.style.height = "auto";

                if (zoomLabel) {
                    zoomLabel.textContent = `${Math.round(zoom * 100)}%`;
                }
            }

            zoomIn?.addEventListener("click", () => {
                zoom = Math.min(2.5, zoom + 0.25);
                applyZoom();
            });

            zoomOut?.addEventListener("click", () => {
                zoom = Math.max(0.5, zoom - 0.25);
                applyZoom();
            });

            zoomReset?.addEventListener("click", () => {
                zoom = 1;
                applyZoom();

                map.scrollLeft = 0;
                map.scrollTop = 0;
            });

            applyZoom();
        }
    }
})();
