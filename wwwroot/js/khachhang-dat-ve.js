
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const dataElement = document.getElementById("bookingSeatData");
    const map = document.getElementById("bookingMap");

    if (!dataElement || !map) return;

    let data;

    try {
        data = JSON.parse(dataElement.textContent);
    } catch {
        data = null;
    }

    const canvas = document.getElementById("bookingCanvas");
    const message = document.getElementById("bookingSeatMessage");
    const bookingForm = document.getElementById("bookingForm");

    if (!data || !Array.isArray(data.ghe) || data.ghe.length === 0) {
        map.replaceChildren();

        if (message) message.hidden = false;
        return;
    }

    const width = Number(data.chieuRong) || 1000;
    const height = Number(data.chieuCao) || 700;

    let zoom = 1;

    const selected = new Map();

    const formatMoney = value =>
        new Intl.NumberFormat("vi-VN").format(value) + " đ";

    map.style.aspectRatio = `${width} / ${height}`;
    map.style.width = "100%";
    map.style.height = "auto";
    map.style.maxWidth = "740px";
    map.style.margin = "0 auto";

    function position(element, x, y, w, h) {
        element.style.left = `${x / width * 100}%`;
        element.style.top = `${y / height * 100}%`;
        element.style.width = `${w / width * 100}%`;
        element.style.height = `${h / height * 100}%`;
    }

    function normalizeText(value) {
        return String(value || "")
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

    function getSeatSize(seat) {
        const sameRow = data.ghe
            .filter(s =>
                s.hang === seat.hang &&
                s.id !== seat.id
            )
            .map(s =>
                Math.abs(Number(s.x) - Number(seat.x))
            )
            .filter(v => v > 0)
            .sort((a, b) => a - b);

        const sameColumn = data.ghe
            .filter(s =>
                s.cot === seat.cot &&
                s.id !== seat.id
            )
            .map(s =>
                Math.abs(Number(s.y) - Number(seat.y))
            )
            .filter(v => v > 0)
            .sort((a, b) => a - b);

        const w = sameRow.length ? sameRow[0] * 0.75 : 16;
        const h = sameColumn.length ? sameColumn[0] * 0.75 : 16;

        return {
            width: Math.max(10, Math.min(40, w)),
            height: Math.max(10, Math.min(40, h))
        };
    }

    // =========================
    // VẼ CÁC THÀNH PHẦN SƠ ĐỒ
    // =========================

    function renderComponents() {
        (data.thanhPhan || []).forEach(item => {
            const element = document.createElement("div");
            const type = normalizeText(item.loai);
            const name = item.ten || "";

            if (type.includes("sankhau")) {
                element.className = "customer-stage";
                element.textContent = name;
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

    // =========================
    // THỐNG KÊ VÀ THANH TOÁN
    // =========================

    function updateSummary() {
        const seats = [...selected.values()];

        const total = seats.reduce(
            (sum, seat) => sum + seat.giaVe,
            0
        );

        const countElement = document.getElementById("selectedCount");
        const totalElement = document.getElementById("bookingTotal");
        const details = document.getElementById("selectedTicketDetails");

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
                // Hiển thị mã ghế

                const codeTitle = document.createElement("div");
                codeTitle.className = "payment-detail-label";
                codeTitle.textContent = "Vị trí ghế";

                const codeList = document.createElement("div");
                codeList.className = "payment-seat-codes";

                seats.forEach(seat => {
                    const tag = document.createElement("span");

                    tag.className = "payment-seat-code";
                    tag.textContent = seat.hang + seat.cot;

                    codeList.appendChild(tag);
                });

                details.append(codeTitle, codeList);

                // Thống kê từng loại vé

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

                const priceTitle = document.createElement("div");

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

        // Thống kê trạng thái ghế

        const available = data.ghe.filter(seat =>
            getSeatStatus(seat) === "trong"
        ).length;

        const holding = data.ghe.filter(seat =>
            getSeatStatus(seat) === "danggiu"
        ).length;

        const booked = data.ghe.length - available - holding;

const totalSeatsElement = document.getElementById("totalSeats");
const availableElement = document.getElementById("availableSeats");
const bookedElement = document.getElementById("bookedSeats");

if (totalSeatsElement) {
    totalSeatsElement.textContent = data.ghe.length;
}

        if (availableElement) {
            availableElement.textContent = available;
        }

        if (bookedElement) {
            bookedElement.textContent = booked;
        }

        // Bổ sung số ghế đang giữ

        let holdingElement = document.getElementById("holdingSeats");

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

    // =========================
    // VẼ GHẾ
    // =========================

    function renderSeats() {
        data.ghe.forEach(seat => {
            const label = document.createElement("label");
            const input = document.createElement("input");

            const isVip = normalizeText(seat.khu).includes("vip");
            const status = getSeatStatus(seat);

            const isAvailable = status === "trong";
            const isHolding = status === "danggiu";
            const isBooked = !isAvailable && !isHolding;

            const isSelected = selected.has(Number(seat.id));

            label.className = "customer-seat";

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
                label.classList.add(isVip ? "vip" : "normal");
                label.title = isVip ? "Ghế VIP" : "Ghế thường";
            }

            input.type = "checkbox";
            input.name = "GheChonIds";
            input.value = seat.id;
            input.checked = isSelected;
            input.disabled = !isAvailable;
            input.className = "customer-seat-input";

            const text = document.createElement("span");

            text.textContent =
                `${seat.hang || ""}${seat.cot || ""}`;

            label.append(input, text);

            const size = getSeatSize(seat);

            position(
                label,
                Number(seat.x) || 0,
                Number(seat.y) || 0,
                size.width,
                size.height
            );

            input.addEventListener("change", () => {
                const id = Number(seat.id);

                if (input.checked) {
                    if (selected.size >= 10) {
                        input.checked = false;

                        alert("Bạn chỉ được chọn tối đa 10 ghế.");
                        return;
                    }

                    selected.set(id, {
                        id,
                        hang: seat.hang,
                        cot: seat.cot,
                        tenLoai: seat.tenLoai || seat.khu || "Thường",
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

    // =========================
    // RENDER TOÀN BỘ SƠ ĐỒ
    // =========================

    function render() {
        map.replaceChildren();

        renderComponents();
        renderSeats();
        updateSummary();
    }

    // =========================
    // PHÓNG TO / THU NHỎ
    // =========================

    function applyZoom() {
        map.style.transform = `scale(${zoom})`;
        map.style.transformOrigin = "top left";

        if (canvas) {
            canvas.style.minHeight =
                `${Math.max(350, map.offsetHeight * zoom + 30)}px`;
        }

        const zoomValue =
            document.getElementById("bookingZoomValue");

        if (zoomValue) {
            zoomValue.textContent =
                `${Math.round(zoom * 100)}%`;
        }
    }

    document.getElementById("bookingZoomIn")?.addEventListener("click", () => {
        zoom = Math.min(2, zoom + 0.2);
        applyZoom();
    });

    document.getElementById("bookingZoomOut")?.addEventListener("click", () => {
        zoom = Math.max(0.5, zoom - 0.2);
        applyZoom();
    });

    document.getElementById("bookingZoomReset")?.addEventListener("click", () => {
        zoom = 1;
        applyZoom();
    });

    // =========================
    // KIỂM TRA TRƯỚC KHI ĐẶT VÉ
    // =========================

    bookingForm?.addEventListener("submit", event => {
        if (selected.size === 0) {
            event.preventDefault();

            alert("Vui lòng chọn ít nhất một ghế.");
            return;
        }

        if (selected.size > 10) {
            event.preventDefault();

            alert("Bạn chỉ được chọn tối đa 10 ghế.");
        }
    });

    render();
    applyZoom();
});
