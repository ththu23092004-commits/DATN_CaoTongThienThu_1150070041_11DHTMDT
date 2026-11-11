"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const config = window.favoriteConfig;
    const widget = document.getElementById("favoriteWidget");

    if (!config || !widget) return;

    const floatingButton = document.getElementById("favoriteFloatingButton");
    const countElement = document.getElementById("favoriteCount");
    const overlay = document.getElementById("favoriteOverlay");
    const modal = document.getElementById("favoriteModal");
    const closeButton = document.getElementById("favoriteCloseButton");
    const listElement = document.getElementById("favoriteModalList");

    const token = widget.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    const likedIds = new Set();

    let authenticated = false;
    let lastFocus = null;

    function getId(link) {
        try {
            const url = new URL(link.href, window.location.origin);
            const queryId = url.searchParams.get("id");

            if (queryId && /^\d+$/.test(queryId)) {
                return Number(queryId);
            }

            const match = url.pathname.match(/\/ChiTiet\/(\d+)\/?$/i);

            return match ? Number(match[1]) : null;
        } catch {
            return null;
        }
    }

    function updateHeart(button, id) {
        const liked = likedIds.has(id);

        button.classList.toggle("is-liked", liked);
        button.setAttribute("aria-pressed", String(liked));

        button.setAttribute(
            "aria-label",
            liked ? "Bỏ yêu thích sự kiện" : "Yêu thích sự kiện"
        );

        const icon = button.querySelector("i");

        if (icon) {
            icon.className = liked
                ? "bi bi-heart-fill"
                : "bi bi-heart";
        }
    }

    function updateCount() {
        if (!countElement) return;

        countElement.textContent = likedIds.size;
        countElement.hidden = likedIds.size === 0;
    }

    function syncAllHearts() {
        document.querySelectorAll(
            ".favorite-heart-button[data-event-id]"
        ).forEach(button => {
            updateHeart(
                button,
                Number(button.dataset.eventId)
            );
        });

        updateCount();
    }

    function showMessage(message) {
        listElement.replaceChildren();

        const box = document.createElement("div");
        box.className = "favorite-message";
        box.textContent = message;

        listElement.appendChild(box);
    }

    async function requestList() {
        const response = await fetch(config.listUrl, {
            method: "GET",
            credentials: "same-origin",
            headers: {
                "Accept": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error(
                "Không thể tải danh sách yêu thích."
            );
        }

        return response.json();
    }

    function applyData(data) {
        authenticated = data.authenticated === true;

        likedIds.clear();

        for (const id of data.ids || []) {
            likedIds.add(Number(id));
        }

        syncAllHearts();
    }

    async function refreshFavorites(renderModal = false) {
        const data = await requestList();

        applyData(data);

        if (renderModal) {
            renderList(data);
        }

        return data;
    }

    async function toggleFavorite(id, button) {
        if (!authenticated) {
            const nextUrl = encodeURIComponent(
                window.location.pathname +
                window.location.search
            );

            window.location.href =
                config.loginUrl +
                (config.loginUrl.includes("?") ? "&" : "?") +
                "returnUrl=" + nextUrl;

            return;
        }

        if (!token) {
            alert(
                "Không tìm thấy mã xác thực. Vui lòng tải lại trang."
            );

            return;
        }

        if (button) {
            button.disabled = true;
        }

        try {
            const body = new URLSearchParams();

            body.set("suKienId", String(id));
            body.set("__RequestVerificationToken", token);

            const response = await fetch(config.toggleUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type":
                        "application/x-www-form-urlencoded;charset=UTF-8",
                    "Accept": "application/json"
                },
                body: body.toString()
            });

            if (!response.ok) {
                throw new Error(
                    "Không thể cập nhật yêu thích. Vui lòng thử lại."
                );
            }

            const result = await response.json();

            if (result.liked) {
                likedIds.add(id);
            } else {
                likedIds.delete(id);
            }

            syncAllHearts();

            if (!modal.hidden) {
                await refreshFavorites(true);
            }

        } catch (error) {
            alert(error.message);

        } finally {
            if (button) {
                button.disabled = false;
            }
        }
    }

    // Gắn nút yêu thích vào thẻ sự kiện.
    // Hỗ trợ cả wrapper Bootstrap .col và wrapper div thông thường.
    function attachHearts() {
        const links = document.querySelectorAll(
            "a.event-link, a.event-card-link"
        );

        links.forEach(link => {
            const id = getId(link);

            if (!id) return;

            // Tìm wrapper phù hợp với cấu trúc từng trang.
            const host =
                link.closest(".favorite-card-host, .col") ||
                link.parentElement;

            if (!host) return;

            host.classList.add("favorite-card-host");

            // Bỏ chữ Chi tiết, thay bằng nút trái tim.
            link.querySelectorAll(
                ".event-view, .event-view-pro"
            ).forEach(node => node.remove());

            // Tránh tạo trùng nút tim.
            if (host.querySelector(
                `.favorite-heart-button[data-event-id="${id}"]`
            )) {
                return;
            }

            const button = document.createElement("button");

            button.type = "button";
            button.className = "favorite-heart-button";
            button.dataset.eventId = String(id);

            const icon = document.createElement("i");

            button.appendChild(icon);

            updateHeart(button, id);

            button.addEventListener("click", event => {
                event.preventDefault();
                event.stopPropagation();

                toggleFavorite(id, button);
            });

            // Nút tim nằm ngoài thẻ a.
            host.appendChild(button);
        });
    }

    function renderList(data) {
        listElement.replaceChildren();

        if (!data.authenticated) {
            const box = document.createElement("div");

            box.className = "favorite-empty";

            box.textContent =
                "Vui lòng đăng nhập tài khoản khách hàng để xem yêu thích.";

            listElement.appendChild(box);

            return;
        }

        if (!data.items || data.items.length === 0) {
            const box = document.createElement("div");

            box.className = "favorite-empty";

            const icon = document.createElement("i");

            icon.className = "bi bi-heart";

            const message = document.createElement("div");

            message.textContent =
                "Bạn chưa có sự kiện yêu thích nào.";

            box.append(icon, message);

            listElement.appendChild(box);

            return;
        }

        data.items.forEach(item => {
            const row = document.createElement("div");

            row.className = "favorite-item";

            const img = document.createElement("img");

            img.className = "favorite-item-image";
            img.src = item.image;
            img.alt = item.name;
            img.loading = "lazy";

            const info = document.createElement("div");

            info.className = "favorite-item-info";

            const link = document.createElement("a");

            link.className = "favorite-item-title";
            link.href = item.url;
            link.textContent = item.name;

            const date = document.createElement("div");

            date.className = "favorite-item-date";
            date.textContent = item.date;

            info.append(link, date);

            if (!item.available) {
                const unavailable = document.createElement("span");

                unavailable.className =
                    "favorite-item-unavailable";

                unavailable.textContent =
                    "Sự kiện đã kết thúc hoặc không còn mở";

                info.appendChild(unavailable);
            }

            const remove = document.createElement("button");

            remove.type = "button";
            remove.className = "favorite-remove-button";
            remove.title = "Bỏ yêu thích";

            remove.setAttribute(
                "aria-label",
                "Bỏ yêu thích " + item.name
            );

            const heart = document.createElement("i");

            heart.className = "bi bi-heart-fill";

            remove.appendChild(heart);

            remove.addEventListener("click", () => {
                toggleFavorite(
                    Number(item.id),
                    remove
                );
            });

            row.append(img, info, remove);

            listElement.appendChild(row);
        });
    }

    async function openModal() {
        lastFocus = document.activeElement;

        overlay.hidden = false;
        modal.hidden = false;

        document.body.style.overflow = "hidden";

        closeButton.focus();

        showMessage("Đang tải danh sách yêu thích...");

        try {
            await refreshFavorites(true);

        } catch {
            showMessage(
                "Không tải được danh sách. Vui lòng thử lại."
            );
        }
    }

    function closeModal() {
        modal.hidden = true;
        overlay.hidden = true;

        document.body.style.overflow = "";

        if (
            lastFocus &&
            typeof lastFocus.focus === "function"
        ) {
            lastFocus.focus();
        }
    }

    floatingButton?.addEventListener(
        "click",
        openModal
    );

    closeButton?.addEventListener(
        "click",
        closeModal
    );

    overlay?.addEventListener(
        "click",
        closeModal
    );

    document.addEventListener("keydown", event => {
        if (modal.hidden) return;

        if (event.key === "Escape") {
            closeModal();
        }

        if (event.key === "Tab") {
            const focusables = [
                ...modal.querySelectorAll(
                    "button:not(:disabled), a[href]"
                )
            ];

            if (!focusables.length) return;

            const first = focusables[0];
            const last = focusables[focusables.length - 1];

            if (
                event.shiftKey &&
                document.activeElement === first
            ) {
                event.preventDefault();
                last.focus();

            } else if (
                !event.shiftKey &&
                document.activeElement === last
            ) {
                event.preventDefault();
                first.focus();
            }
        }
    });

    attachHearts();

    refreshFavorites().catch(error => {
        console.error(
            "Lỗi tải yêu thích:",
            error
        );
    });
});