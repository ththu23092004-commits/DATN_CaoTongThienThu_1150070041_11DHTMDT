
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const dataElement = document.getElementById("bookingSeatData");
    const map = document.getElementById("bookingMap");

    if (!dataElement || !map) return;

    const canvas = document.getElementById("bookingCanvas");
    const message = document.getElementById("bookingSeatMessage");
    const bookingForm = document.getElementById("bookingForm");

    let data;

    try {
        data = JSON.parse(dataElement.textContent || "null");
    } catch (error) {
        console.error("Lỗi đọc sơ đồ:", error);
        data = null;
    }

    if (!data || !Array.isArray(data.ghe) || !data.ghe.length) {
        map.replaceChildren();
        if (message) message.hidden = false;
        return;
    }

    const width = Number(data.chieuRong) || 1000;
    const height = Number(data.chieuCao) || 700;

    const components = Array.isArray(data.thanhPhan)
        ? data.thanhPhan
        : [];

    const selected = new Map();

    let zoom = 1;

    function normalizeText(value) {
        return String(value ?? "")
            .replace(/đ/g, "d")
            .replace(/Đ/g, "D")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .replace(/[^a-z0-9]/g, "");
    }

    function getSeatStatus(seat) {
        return normalizeText(seat.trangThai);
    }

    function formatMoney(value) {
        return new Intl.NumberFormat("vi-VN").format(value) + " đ";
    }

    const componentTypes = components.map(
        item => normalizeText(item.loai)
    );

    const isWorkshop = componentTypes.some(type =>
        type.includes("bantron") ||
        type.includes("banchuu")
    );

    // Giữ tỷ lệ tọa độ của BTC.
    const baseMapWidth = Math.min(
        1000,
        Math.max(280, (canvas?.clientWidth || 760) - 32)
    );

    map.style.aspectRatio = `${width} / ${height}`;
    map.style.width = `${baseMapWidth}px`;
    map.style.height = "auto";
    map.style.maxWidth = "none";
    map.style.margin = "0 auto";

    function position(element, x, y, w, h) {
        element.style.left = `${x / width * 100}%`;
        element.style.top = `${y / height * 100}%`;
        element.style.width = `${w / width * 100}%`;
        element.style.height = `${h / height * 100}%`;
    }

    // =====================================
    // TÍNH KÍCH THƯỚC GHẾ GIỐNG TRANG CHI TIẾT
    // =====================================

    function median(values, fallback) {
        const sorted = values
            .filter(value => Number.isFinite(value) && value > 0)
            .sort((a, b) => a - b);

        if (!sorted.length) return fallback;

        const middle = Math.floor(sorted.length / 2);

        return sorted.length % 2
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    function positiveGaps(values) {
        const result = [];

        for (let i = 1; i < values.length; i++) {
            const gap = values[i] - values[i - 1];

            if (gap > 0 && gap < 150) {
                result.push(gap);
            }
        }

        return result;
    }

    function getSeatSize() {
        // Ghế Workshop tròn, đường kính 48 đơn vị.
        if (isWorkshop) {
            return {
                width: 48,
                height: 48
            };
        }

        const rows = new Map();

        data.ghe.forEach(seat => {
            const row = String(seat.hang ?? "");

            if (!rows.has(row)) {
                rows.set(row, []);
            }

            rows.get(row).push(Number(seat.x));
        });

        const xGaps = [];

        for (const xs of rows.values()) {
            const sorted = [...new Set(xs)]
                .filter(Number.isFinite)
                .sort((a, b) => a - b);

            xGaps.push(...positiveGaps(sorted));
        }

        const ys = [...new Set(
            data.ghe.map(seat => Number(seat.y))
        )]
            .filter(Number.isFinite)
            .sort((a, b) => a - b);

        const cellWidth = median(xGaps, 80);
        const cellHeight = median(positiveGaps(ys), 80);

        return {
            width: Math.max(
                7,
                cellWidth - Math.min(5, cellWidth * 0.12)
            ),
            height: Math.max(
                7,
                cellHeight - Math.min(6, cellHeight * 0.12)
            )
        };
    }

    const seatSize = getSeatSize();

    // =====================================
    // VẼ SÂN KHẤU, BÀN, LỐI ĐI VÀ CỬA
    // =====================================

    function renderComponents() {
        components.forEach(item => {
            const element = document.createElement("div");

            const type = normalizeText(item.loai);
            const name = item.ten || "";

            if (
                type.includes("sankhau") ||
                type.includes("manhinh")
            ) {
                element.className = "customer-stage";
                element.textContent =
                    name || "SÂN KHẤU / MÀN HÌNH";

            } else if (
                type.includes("loidi") ||
                type.includes("aisle")
            ) {
                element.className = "customer-aisle";
                element.dataset.tooltip = name || "Lối đi";
                element.setAttribute(
                    "aria-label",
                    name || "Lối đi"
                );

            } else if (type.includes("cua")) {
                element.className = "customer-door";
                element.textContent = name;

            } else if (type.includes("bantron")) {
                element.className =
                    "customer-component customer-round-table";
                element.textContent = name;

            } else if (type.includes("banchuu")) {
                element.className =
                    "customer-component customer-u-part";

            } else if (type.includes("banchutoa")) {
                element.className =
                    "customer-component customer-chair-table";
                element.textContent = name || "BÀN CHỦ TỌA";

            } else {
                element.className = "customer-component";
                element.textContent = name;
            }

            position(
                element,
                Number(item.x) || 0,
                Number(item.y) || 0,
                Number(item.rong) || 10,
                Number(item.cao) || 10
            );

            map.appendChild(element);
        });
    }

    // =====================================
    // CẬP NHẬT THANH TOÁN
    // =====================================

    function updateSummary() {
        const seats = [...selected.values()];

        const total = seats.reduce(
            (sum, seat) => sum + seat.giaVe,
            0
        );

        const countElement =
            document.getElementById("selectedCount");

        const totalElement =
            document.getElementById("bookingTotal");

        const details =
            document.getElementById("selectedTicketDetails");

        if (countElement) {
            countElement.textContent = seats.length;
        }

        if (totalElement) {
            totalElement.textContent = formatMoney(total);
        }

        if (details) {
            details.replaceChildren();

            if (seats.length === 0) {
                const empty = document.createElement("p");
                empty.className = "ticket-empty";
                empty.textContent = "Chưa có ghế được chọn.";
                details.appendChild(empty);

            } else {
                const codeTitle = document.createElement("div");
                codeTitle.className = "payment-detail-label";
                codeTitle.textContent = "Vị trí ghế";

                const codeList = document.createElement("div");
                codeList.className = "payment-seat-codes";

                seats.forEach(seat => {
                    const tag = document.createElement("span");

                    tag.className = "payment-seat-code";
                    tag.textContent =
                        `${seat.hang}${seat.cot}`;

                    codeList.appendChild(tag);
                });

                details.append(codeTitle, codeList);

                const groups = {};

                seats.forEach(seat => {
                    const name = seat.tenLoai || "Khác";

                    if (!groups[name]) {
                        groups[name] = {
                            count: 0,
                            total: 0
                        };
                    }

                    groups[name].count++;
                    groups[name].total += seat.giaVe;
                });

                const priceTitle =
                    document.createElement("div");

                priceTitle.className =
                    "payment-detail-label payment-price-heading";

                priceTitle.textContent = "Chi tiết giá vé";
                details.appendChild(priceTitle);

                Object.entries(groups).forEach(([name, item]) => {
                    const row = document.createElement("div");
                    row.className = "ticket-detail-row";

                    const left = document.createElement("div");
                    left.className = "ticket-detail-left";

                    const title = document.createElement("strong");
                    title.textContent =
                        `${name} × ${item.count}`;

                    const unitPrice = document.createElement("small");

                    unitPrice.textContent =
                        `${formatMoney(item.total / item.count)} / ghế`;

                    left.append(title, unitPrice);

                    const subtotal = document.createElement("strong");
                    subtotal.className = "ticket-detail-price";
                    subtotal.textContent = formatMoney(item.total);

                    row.append(left, subtotal);
                    details.appendChild(row);
                });
            }
        }

        // Thống kê ghế từ dữ liệu hiện tại.
        const available = data.ghe.filter(
            seat => getSeatStatus(seat) === "trong"
        ).length;

        const holding = data.ghe.filter(
            seat => getSeatStatus(seat) === "danggiu"
        ).length;

        const booked = data.ghe.length - available - holding;

        const totalSeatsElement =
            document.getElementById("totalSeats");

        const availableElement =
            document.getElementById("availableSeats");

        const bookedElement =
            document.getElementById("bookedSeats");

        if (totalSeatsElement) {
            totalSeatsElement.textContent = data.ghe.length;
        }

        if (availableElement) {
            availableElement.textContent = available;
        }

        if (bookedElement) {
            bookedElement.textContent = booked;
        }

        let holdingElement =
            document.getElementById("holdingSeats");

        if (!holdingElement) {
            const countContainer =
                document.querySelector(".booking-seat-count");

            if (countContainer) {
                const span = document.createElement("span");

                span.innerHTML =
                    'Ghế đang giữ: <strong id="holdingSeats">0</strong>';

                countContainer.appendChild(span);

                holdingElement =
                    document.getElementById("holdingSeats");
            }
        }

        if (holdingElement) {
            holdingElement.textContent = holding;
        }
    }

    // =====================================
    // VẼ GHẾ VÀ XỬ LÝ CHỌN GHẾ
    // =====================================

    function renderSeats() {
        data.ghe.forEach(seat => {
            const label = document.createElement("label");
            const input = document.createElement("input");
            const text = document.createElement("span");

            const id = Number(seat.id);
            const status = getSeatStatus(seat);

            const isVip =
                normalizeText(seat.khu).includes("vip");

            const isAvailable = status === "trong";
            const isHolding = status === "danggiu";
            const isBooked = !isAvailable && !isHolding;

            const isSelected = selected.has(id);

            label.className = "customer-seat";

            if (isWorkshop) {
                label.classList.add("workshop-seat");
            }

            if (isBooked) {
                label.classList.add("booked");
                label.title = "Ghế đã được đặt";

            } else if (isHolding) {
                label.classList.add("holding");
                label.title = "Ghế đang được khách khác giữ";

            } else if (isSelected) {
                label.classList.add("selected");
                label.title = "Ghế bạn đang chọn";

            } else {
                label.classList.add(
                    isVip ? "vip" : "normal"
                );

                label.title = isVip
                    ? "Ghế VIP"
                    : "Ghế thường";
            }

            input.type = "checkbox";
            input.name = "GheChonIds";
            input.value = seat.id;
            input.checked = isSelected;
            input.disabled = !isAvailable;
            input.className = "customer-seat-input";

            text.textContent =
                `${seat.hang || ""}${seat.cot || ""}`;

            label.append(input, text);

            position(
                label,
                Number(seat.x) || 0,
                Number(seat.y) || 0,
                seatSize.width,
                seatSize.height
            );

            input.addEventListener("change", () => {
                if (input.checked) {
                    if (selected.size >= 10) {
                        input.checked = false;

                        alert(
                            "Bạn chỉ được chọn tối đa 10 ghế."
                        );

                        return;
                    }

                    selected.set(id, {
                        id,
                        hang: seat.hang,
                        cot: seat.cot,
                        tenLoai:
                            seat.tenLoai || seat.khu || "Thường",
                        giaVe: Number(seat.giaVe) || 0
                    });

                } else {
                    selected.delete(id);
                }

                render();
            });

            map.appendChild(label);
        });
    }

    // =====================================
    // VẼ LẠI SƠ ĐỒ
    // =====================================

    function render() {
        map.replaceChildren();

        renderComponents();
        renderSeats();
        updateSummary();
    }

    // =====================================
    // PHÓNG TO / THU NHỎ
    // =====================================

    function applyZoom() {
        // Thay đổi chiều rộng thực thay vì transform scale.
        // Tránh lỗi vùng cuộn và lệch vị trí.
        map.style.transform = "none";

        map.style.width =
            `${Math.round(baseMapWidth * zoom)}px`;

        map.style.margin =
            zoom > 1 ? "0" : "0 auto";

        const zoomValue =
            document.getElementById("bookingZoomValue");

        if (zoomValue) {
            zoomValue.textContent =
                `${Math.round(zoom * 100)}%`;
        }
    }

    document.getElementById("bookingZoomIn")
        ?.addEventListener("click", () => {
            zoom = Math.min(2, zoom + 0.2);
            applyZoom();
        });

    document.getElementById("bookingZoomOut")
        ?.addEventListener("click", () => {
            zoom = Math.max(0.5, zoom - 0.2);
            applyZoom();
        });

    document.getElementById("bookingZoomReset")
        ?.addEventListener("click", () => {
            zoom = 1;
            applyZoom();
        });

    // =====================================
    // KIỂM TRA TRƯỚC KHI ĐẶT VÉ
    // =====================================

    bookingForm?.addEventListener("submit", event => {
        if (selected.size === 0) {
            event.preventDefault();
            alert("Vui lòng chọn ít nhất một ghế.");
            return;
        }

        if (selected.size > 10) {
            event.preventDefault();

            alert(
                "Bạn chỉ được chọn tối đa 10 ghế."
            );
        }
    });

    render();
    applyZoom();
});
