
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("detailSeatMap");
    const dataNode = document.getElementById("detailSeatMapData");

    if (!root || !dataNode) return;

    let data = null;

    try {
        data = JSON.parse(dataNode.textContent || "null");
    } catch {
        data = null;
    }

    if (!data) {
        root.innerHTML = `<div class="map-empty">Không có dữ liệu sơ đồ.</div>`;
        return;
    }

    const seats = Array.isArray(data.seats)
        ? data.seats
        : Array.isArray(data.ghe)
            ? data.ghe
            : [];

    const components = Array.isArray(data.components)
        ? data.components
        : Array.isArray(data.thanhPhan)
            ? data.thanhPhan
            : [];

    const width = Number(data.width ?? data.chieuRong) || 1000;
    const height = Number(data.height ?? data.chieuCao) || 700;

    root.innerHTML = "";
    root.style.position = "relative";
    root.style.width = "100%";
    root.style.maxWidth = `${width}px`;
    root.style.height = `${height}px`;
    root.style.margin = "0 auto";

    function makeNode(tag, className, text) {
        const node = document.createElement(tag);
        node.className = className;

        if (text) {
            node.textContent = text;
        }

        return node;
    }

    function setRect(node, x, y, w, h) {
        node.style.position = "absolute";
        node.style.left = `${x}px`;
        node.style.top = `${y}px`;
        node.style.width = `${w}px`;
        node.style.height = `${h}px`;
    }

    function getType(item) {
        return String(item.type ?? item.loai ?? "");
    }

    function getName(item) {
        return String(item.name ?? item.ten ?? "");
    }

    function getX(item) {
        return Number(item.x ?? item.viTriX ?? 0);
    }

    function getY(item) {
        return Number(item.y ?? item.viTriY ?? 0);
    }

    function getW(item) {
        return Number(item.width ?? item.rong ?? item.chieuRong ?? 0);
    }

    function getH(item) {
        return Number(item.height ?? item.cao ?? item.chieuCao ?? 0);
    }

    const sortedComponents = [...components];

    sortedComponents.forEach(item => {
        const type = getType(item);
        const name = getName(item);
        const x = getX(item);
        const y = getY(item);
        const w = getW(item);
        const h = getH(item);

        if (!Number.isFinite(x) || !Number.isFinite(y) || !Number.isFinite(w) || !Number.isFinite(h)) {
            return;
        }

        if (type === "SanKhau" || type === "ManHinh") {
            const node = makeNode("div", "detail-stage", name || "KHU VỰC TRÌNH BÀY");
            setRect(node, x, y, w, h);
            root.appendChild(node);
            return;
        }

        if (type === "BanTron") {
            const node = makeNode("div", "detail-round-table", name);
            setRect(node, x, y, w, h);
            root.appendChild(node);
            return;
        }

        if (type === "BanChuToa") {
            const node = makeNode("div", "detail-chair-table", name || "BÀN CHỦ TỌA");
            setRect(node, x, y, w, h);
            root.appendChild(node);
            return;
        }

        if (type === "BanChuU") {
            const node = makeNode("div", "detail-u-part", "");
            setRect(node, x, y, w, h);
            root.appendChild(node);
            return;
        }

        if (type === "CuaVao" || type === "CuaThoatHiem" || type === "CuaRaVao") {
            const node = makeNode("div", "detail-door", name || "CỬA");
            setRect(node, x, y, w, h);
            root.appendChild(node);
            return;
        }

        if (type === "LoiDi") {
            const node = makeNode("div", "detail-aisle", "");
            setRect(node, x, y, w, h);
            root.appendChild(node);
        }
    });

    seats.forEach(seat => {
        const x = Number(seat.x ?? seat.viTriX ?? 0);
        const y = Number(seat.y ?? seat.viTriY ?? 0);

        const code = seat.code ||
            `${seat.row ?? seat.hang ?? ""}${seat.col ?? seat.cot ?? ""}`;

        const zone = String(seat.zone ?? seat.khu ?? "Thuong").toLowerCase();
        const status = String(seat.trangThai ?? seat.status ?? "Trong").toLowerCase();

        const isVip = zone.includes("vip");
        const isBooked = status !== "trong" && status !== "trống" && status !== "available";

        const node = makeNode(
            "div",
            `detail-seat${isVip ? " vip" : ""}${isBooked ? " booked" : ""}`,
            code
        );

        setRect(node, x, y, 56, 40);

        root.appendChild(node);
    });
});
