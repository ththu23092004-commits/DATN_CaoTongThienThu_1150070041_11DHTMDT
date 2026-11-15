
(() => {
    "use strict";

    const root = document.getElementById("kpWidget");
    if (!root) return;

    const launcher = document.getElementById("kpLauncher");
    const panel = document.getElementById("kpPanel");
    const closeButton = document.getElementById("kpClose");
    const messages = document.getElementById("kpMessages");
    const form = document.getElementById("kpForm");
    const input = document.getElementById("kpInput");
    const sendButton = document.getElementById("kpSend");
    const resetButton = document.getElementById("kpReset");
    const conversationInput = document.getElementById("kpHoiThoaiId");

    const historyToggle = document.getElementById("kpHistoryToggle");
    const historyPanel = document.getElementById("kpHistoryPanel");
    const historyNewButton = document.getElementById("kpHistoryNew");
    const historyList = document.getElementById("kpHistoryList");
    const historyLoading = document.getElementById("kpHistoryLoading");
    const historyError = document.getElementById("kpHistoryError");
    const historyEmpty = document.getElementById("kpHistoryEmpty");

    const apiUrl = "/kham-pha-hoi-thoai/hoi";
    const historyUrl = "/kham-pha-hoi-thoai/lich-su";

    let nguCanh = null;
    let dangGui = false;
    let dangTaiHoiThoai = false;
    let daKhoiPhucLanDau = false;
    let hoiThoaiId = null;
    let danhSachLichSu = [];

    const initialMarkup = messages.innerHTML;

    function create(tag, className, text) {
        const el = document.createElement(tag);

        if (className) el.className = className;
        if (text !== undefined && text !== null) {
            el.textContent = String(text);
        }

        return el;
    }

    function addText(parent, tag, className, text) {
        const el = create(tag, className, text);
        parent.appendChild(el);
        return el;
    }

    function scrollBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function setConversationId(value) {
        hoiThoaiId = value ? String(value) : null;

        if (conversationInput) {
            conversationInput.value = hoiThoaiId || "";
        }
    }

    function showHistoryView(show) {
        if (!historyPanel) return;

        historyPanel.hidden = !show;
        messages.hidden = show;

        const bottom = document.getElementById("kpBottom");
        if (bottom) bottom.hidden = show;

        if (historyToggle) {
            historyToggle.setAttribute(
                "aria-expanded",
                show ? "true" : "false"
            );
        }
    }

    function openPanel() {
        panel.hidden = false;
        launcher.setAttribute("aria-expanded", "true");
        launcher.classList.add("kp-open");

        if (!daKhoiPhucLanDau && historyToggle) {
            daKhoiPhucLanDau = true;
            khoiPhucCuocTroChuyenGanNhat();
        } else {
            input.focus();
            scrollBottom();
        }
    }

    function closePanel() {
        panel.hidden = true;
        launcher.setAttribute("aria-expanded", "false");
        launcher.classList.remove("kp-open");
        launcher.focus();
    }

    launcher.addEventListener("click", () => {
        if (panel.hidden) openPanel();
        else closePanel();
    });

    closeButton.addEventListener("click", closePanel);

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && !panel.hidden) {
            if (historyPanel && !historyPanel.hidden) {
                showHistoryView(false);
                input.focus();
            } else {
                closePanel();
            }
        }
    });

    function messageBubble(text, isUser = false) {
        const row = create(
            "div",
            "kp-message " +
            (isUser ? "kp-message-user" : "kp-message-bot")
        );

        if (!isUser) {
            row.appendChild(create("div", "kp-bot-avatar", "✦"));
        }

        const bubble = create("div", "kp-bubble");
        addText(bubble, "div", "", text);

        row.appendChild(bubble);
        messages.appendChild(row);
        scrollBottom();

        return row;
    }

    function showTyping() {
        const row = create("div", "kp-message kp-message-bot");
        row.appendChild(create("div", "kp-bot-avatar", "✦"));

        const bubble = create("div", "kp-bubble");
        const typing = create("div", "kp-typing");

        for (let i = 0; i < 3; i++) {
            typing.appendChild(document.createElement("span"));
        }

        bubble.appendChild(typing);
        row.appendChild(bubble);
        messages.appendChild(row);

        scrollBottom();
        return row;
    }

    function formatDate(value) {
        if (!value) return "Đang cập nhật";

        const match = String(value).match(
            /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/
        );

        if (match) {
            const [, year, month, day, hour, minute] = match;
            return `${day}/${month}/${year} • ${hour}:${minute}`;
        }

        return "Đang cập nhật";
    }

    function formatHistoryDate(value) {
        if (!value) return "Vừa trò chuyện";

        const match = String(value).match(
            /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/
        );

        if (!match) return "Vừa trò chuyện";

        const [, year, month, day, hour, minute] = match;
        const dateText = `${day}/${month}/${year}`;
        const today = new Date();

        const todayText = [
            String(today.getDate()).padStart(2, "0"),
            String(today.getMonth() + 1).padStart(2, "0"),
            today.getFullYear()
        ].join("/");

        if (dateText === todayText) {
            return `Hôm nay • ${hour}:${minute}`;
        }

        return `${dateText} • ${hour}:${minute}`;
    }

    function formatMoney(value) {
        if (value === null ||
            value === undefined ||
            value === "") {
            return "Chưa cập nhật giá vé";
        }

        const number = Number(value);

        if (!Number.isFinite(number) || number < 0) {
            return "Chưa cập nhật giá vé";
        }

        return "Từ " +
            new Intl.NumberFormat("vi-VN").format(number) +
            "đ";
    }

    function safeImagePath(path) {
        if (typeof path !== "string") return null;

        if (!/^\/uploads\/sukien\/[a-zA-Z0-9._-]+\.(png|jpg|jpeg|webp)$/i
            .test(path)) {
            return null;
        }

        return path;
    }

    function addMeta(parent, label, value) {
        if (!value) return;

        const p = create("p", "kp-event-meta");
        const strong = create("strong", "", label + ": ");

        p.appendChild(strong);
        p.appendChild(document.createTextNode(value));
        parent.appendChild(p);
    }

    function renderEvent(event, index) {
        const card = create("article", "kp-event");
        const coverPath = safeImagePath(event.anhSuKien);

        if (coverPath) {
            const img = create("img", "kp-event-cover");
            img.src = coverPath;
            img.alt = event.tenSuKien || "Ảnh sự kiện";
            img.loading = "lazy";
            card.appendChild(img);
        } else {
            card.appendChild(
                create("div", "kp-event-empty", "✦")
            );
        }

        const content = create("div", "kp-event-content");

        addText(
            content,
            "div",
            "kp-event-tag",
            "Gợi ý #" + (index + 1)
        );

        addText(
            content,
            "h3",
            "kp-event-title",
            event.tenSuKien || "Sự kiện"
        );

        addMeta(content, "Chủ đề", event.chuDe);

        addMeta(
            content,
            "Thời gian",
            formatDate(event.thoiGianBatDau)
        );

        const ten = event.tenDiaDiem || "";
        const diaChi = event.diaChi || "";

        if (ten) {
            addMeta(content, "Địa điểm", ten);
        } else {
            addMeta(
                content,
                "Địa điểm",
                event.diaDiem || "Đang cập nhật"
            );
        }

        if (diaChi) {
            addMeta(content, "Địa chỉ", diaChi);
        }

        const moTa = event.moTaChiTiet || "";

        if (moTa.trim()) {
            addText(
                content,
                "div",
                "kp-event-description",
                moTa
            );

            if (moTa.length > 140) {
                const description = content.querySelector(
                    ".kp-event-description"
                );

                const readMore = create(
                    "button",
                    "kp-readmore",
                    "Xem thêm nội dung"
                );

                readMore.type = "button";

                readMore.addEventListener("click", () => {
                    const expanded = description.classList.toggle(
                        "kp-expanded"
                    );

                    readMore.textContent = expanded
                        ? "Thu gọn nội dung"
                        : "Xem thêm nội dung";
                });

                content.appendChild(readMore);
            }
        } else {
            addText(
                content,
                "div",
                "kp-event-meta",
                "Sự kiện chưa có mô tả chi tiết."
            );
        }

        addText(
            content,
            "div",
            "kp-event-price",
            formatMoney(event.giaVeThapNhat)
        );

        if (event.lyDoPhuHop) {
            addText(
                content,
                "div",
                "kp-event-reason",
                "✓ " + event.lyDoPhuHop
            );
        }

        if (event.laGoiYMoRong) {
            const warning = event.dieuKienChuaKhop
                ? "Gợi ý mở rộng: " + event.dieuKienChuaKhop
                : "Gợi ý mở rộng, chưa khớp hoàn toàn.";

            addText(
                content,
                "div",
                "kp-event-warning",
                warning
            );
        }

        const id = Number(event.suKienId);

        if (Number.isSafeInteger(id) && id > 0) {
            const link = create(
                "a",
                "kp-event-link",
                "Xem chi tiết sự kiện →"
            );

           link.href = "/KhachHang/ChiTiet/" + id;
            content.appendChild(link);
        }

        card.appendChild(content);
        return card;
    }

    function renderSearchConditions(nhuCau) {
        if (!nhuCau) return;

        const conditions = [];

        if (nhuCau.chuDe) {
            conditions.push({
                icon: "🎭",
                label: "Chủ đề",
                value: nhuCau.chuDe
            });
        }

        if (nhuCau.diaDiem) {
            conditions.push({
                icon: "📍",
                label: "Địa điểm",
                value: nhuCau.diaDiem
            });
        }

        if (nhuCau.nganSachToiDa !== null &&
            nhuCau.nganSachToiDa !== undefined) {
            conditions.push({
                icon: "💰",
                label: "Ngân sách",
                value: "Tối đa " +
                    new Intl.NumberFormat("vi-VN")
                        .format(nhuCau.nganSachToiDa) +
                    "đ"
            });
        }

        if (nhuCau.tuNgay || nhuCau.denNgay) {
            let dateText = "";

            if (nhuCau.tuNgay && nhuCau.denNgay) {
                dateText = nhuCau.tuNgay === nhuCau.denNgay
                    ? formatSearchDate(nhuCau.tuNgay)
                    : formatSearchDate(nhuCau.tuNgay) +
                      " – " +
                      formatSearchDate(nhuCau.denNgay);
            } else if (nhuCau.tuNgay) {
                dateText = "Từ " +
                    formatSearchDate(nhuCau.tuNgay);
            } else {
                dateText = "Đến " +
                    formatSearchDate(nhuCau.denNgay);
            }

            conditions.push({
                icon: "📅",
                label: "Thời gian",
                value: dateText
            });
        }

        if (nhuCau.hinhThuc) {
            const forms = {
                TrucTiep: "Trực tiếp",
                TrucTuyen: "Trực tuyến",
                Hybrid: "Kết hợp"
            };

            conditions.push({
                icon: "🎟️",
                label: "Hình thức",
                value: forms[nhuCau.hinhThuc] ||
                    nhuCau.hinhThuc
            });
        }

        if (nhuCau.tuKhoa) {
            conditions.push({
                icon: "🔎",
                label: "Từ khóa",
                value: nhuCau.tuKhoa
            });
        }

        if (conditions.length === 0) return;

        const wrapper = create(
            "div",
            "kp-search-context"
        );

        addText(
            wrapper,
            "div",
            "kp-search-context-title",
            "✦ Điều kiện tìm kiếm hiện tại"
        );

        const chips = create(
            "div",
            "kp-context-chips"
        );

        conditions.forEach(item => {
            const chip = create(
                "div",
                "kp-context-chip"
            );

            addText(
                chip,
                "span",
                "kp-context-icon",
                item.icon
            );

            const content = create(
                "div",
                "kp-context-content"
            );

            addText(
                content,
                "span",
                "kp-context-label",
                item.label
            );

            addText(
                content,
                "span",
                "kp-context-value",
                item.value
            );

            chip.appendChild(content);
            chips.appendChild(chip);
        });

        wrapper.appendChild(chips);
        messages.appendChild(wrapper);
        scrollBottom();
    }

    function formatSearchDate(value) {
        if (!value) return "";

        const parts = value.split("-");

        if (parts.length !== 3) {
            return value;
        }

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }

    function renderResults(data) {
        const items = Array.isArray(data.suKien)
            ? data.suKien.slice(0, 3)
            : [];

        renderSearchConditions(data.nhuCau);

        const personalLabel = data.caNhanHoa
            ? "✦ Đã bật xếp hạng theo sở thích tài khoản"
            : "✦ Đang tìm kiếm theo nhu cầu của bạn";

        messages.appendChild(
            create("div", "kp-personal-status", personalLabel)
        );

        const chinhXac = items.filter(
            x => x.laGoiYMoRong !== true
        );

        const moRong = items.filter(
            x => x.laGoiYMoRong === true
        );

        if (items.length === 0) {
            messageBubble(
                data.thongBao ||
                "Chưa tìm thấy sự kiện phù hợp.",
                false
            );

            messageBubble(
                "Bạn có thể thử đổi thời gian, địa điểm " +
                "hoặc chủ đề. Mình sẽ tìm lại giúp bạn nhé!",
                false
            );

            return;
        }

        let thongBao = "";

        if (chinhXac.length > 0 && moRong.length === 0) {
            thongBao =
                `Mình tìm được ${chinhXac.length} sự kiện ` +
                "đáp ứng đầy đủ yêu cầu của bạn.";
        } else if (chinhXac.length > 0) {
            thongBao =
                `Mình tìm được ${chinhXac.length} sự kiện ` +
                "đáp ứng đầy đủ yêu cầu. " +
                `Ngoài ra còn ${moRong.length} gợi ý liên quan ` +
                "để bạn tham khảo.";
        } else {
            thongBao =
                "Hiện chưa tìm được sự kiện đáp ứng đầy đủ " +
                "yêu cầu. Mình có một số sự kiện liên quan, " +
                "nhưng chúng chưa khớp toàn bộ điều kiện.";
        }

        messageBubble(thongBao, false);

        const results = create(
            "div",
            "kp-results kp-grouped-results"
        );

        function addGroup(title, description, list, type) {
            if (list.length === 0) return;

            const section = create(
                "section",
                "kp-result-group kp-group-" + type
            );

            const heading = create(
                "div",
                "kp-group-heading"
            );

            const icon = type === "exact" ? "✓" : "✧";

            addText(
                heading,
                "span",
                "kp-group-icon",
                icon
            );

            addText(
                heading,
                "span",
                "kp-group-title",
                title + " (" + list.length + ")"
            );

            section.appendChild(heading);

            if (description) {
                addText(
                    section,
                    "p",
                    "kp-group-description",
                    description
                );
            }

            list.forEach((event, index) => {
                const card = renderEvent(event, index);

                card.classList.add(
                    type === "exact"
                        ? "kp-event-exact"
                        : "kp-event-related"
                );

                section.appendChild(card);
            });

            results.appendChild(section);
        }

        addGroup(
            "Đáp ứng đầy đủ yêu cầu",
            "Các sự kiện đáp ứng những điều kiện " +
            "bạn đang tìm kiếm.",
            chinhXac,
            "exact"
        );

        addGroup(
            "Có thể bạn cũng quan tâm",
            "Những sự kiện dưới đây có nội dung liên quan " +
            "nhưng chưa đáp ứng đầy đủ yêu cầu. " +
            "Hãy kiểm tra phần lưu ý trên từng sự kiện.",
            moRong,
            "related"
        );

        messages.appendChild(results);
        scrollBottom();
    }

    function setHistoryLoading(show) {
        if (historyLoading) {
            historyLoading.hidden = !show;
        }
    }

    function showHistoryError(message) {
        if (!historyError) return;

        historyError.textContent = message ||
            "Không thể tải lịch sử trò chuyện.";

        historyError.hidden = false;
    }

    function renderHistoryList(list) {
        if (!historyList) return;

        historyList.innerHTML = "";

        if (historyEmpty) {
            historyEmpty.hidden = list.length !== 0;
        }

        list.forEach(item => {
            const id = String(item.hoiThoaiId || "");
            if (!id) return;

            const button = create("button", "kp-history-item");
            button.type = "button";
            button.dataset.id = id;
            button.setAttribute("role", "listitem");

            if (id === hoiThoaiId) {
                button.classList.add("kp-history-item-active");
                button.setAttribute("aria-current", "true");
            }

            const top = create("div", "kp-history-item-top");

            const title = create(
                "div",
                "kp-history-item-title",
                item.tieuDe || "Cuộc trò chuyện mới"
            );

            const date = create(
                "div",
                "kp-history-item-date",
                formatHistoryDate(item.ngayCapNhat || item.ngayTao)
            );

            const preview = create(
                "div",
                "kp-history-item-preview",
                item.tinNhanCuoi || "Chưa có nội dung"
            );

            const meta = create(
                "div",
                "kp-history-item-meta",
                `${Number(item.soTinNhan) || 0} tin nhắn`
            );

            top.appendChild(title);
            top.appendChild(date);

            button.appendChild(top);
            button.appendChild(preview);
            button.appendChild(meta);

            historyList.appendChild(button);
        });

        if (historyEmpty) {
            historyEmpty.hidden = historyList.children.length !== 0;
        }
    }

    async function taiDanhSachLichSu(hienLoi = true) {
        if (!historyList) return [];

        setHistoryLoading(true);

        if (historyError) {
            historyError.hidden = true;
            historyError.textContent = "";
        }

        if (historyEmpty) {
            historyEmpty.hidden = true;
        }

        try {
            const response = await fetch(historyUrl, {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    "Accept": "application/json"
                }
            });

            if (!response.ok) {
                throw new Error(
                    response.status === 401 || response.status === 403
                        ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                        : "Không thể tải lịch sử trò chuyện."
                );
            }

            const data = await response.json();

            if (!data.thanhCong || !Array.isArray(data.danhSach)) {
                throw new Error(
                    data.thongBao || "Dữ liệu lịch sử không hợp lệ."
                );
            }

            danhSachLichSu = data.danhSach;
            renderHistoryList(danhSachLichSu);

            return danhSachLichSu;
        } catch (error) {
            if (hienLoi) {
                showHistoryError(error.message);
            }

            return [];
        } finally {
            setHistoryLoading(false);
        }
    }

    async function taiVaMoHoiThoai(id) {
        if (dangGui || dangTaiHoiThoai || !id) return;

        dangTaiHoiThoai = true;
        setHistoryLoading(true);

        if (historyError) {
            historyError.hidden = true;
            historyError.textContent = "";
        }

        try {
            const response = await fetch(
                historyUrl + "/" + encodeURIComponent(id),
                {
                    method: "GET",
                    credentials: "same-origin",
                    cache: "no-store",
                    headers: {
                        "Accept": "application/json"
                    }
                }
            );

            if (!response.ok) {
                throw new Error(
                    response.status === 404
                        ? "Không tìm thấy cuộc trò chuyện này."
                        : response.status === 401 || response.status === 403
                            ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                            : "Không thể mở cuộc trò chuyện."
                );
            }

            const data = await response.json();

            if (!data.thanhCong) {
                throw new Error(
                    data.thongBao || "Không thể mở cuộc trò chuyện."
                );
            }

            setConversationId(data.hoiThoaiId || id);

            nguCanh = null;

            if (data.nguCanhJson) {
                try {
                    nguCanh = JSON.parse(data.nguCanhJson);
                } catch (_) {
                    nguCanh = null;
                }
            }

            messages.innerHTML = "";

            messages.appendChild(
                create(
                    "div",
                    "kp-date-divider",
                    "Nội dung cuộc trò chuyện"
                )
            );

            const tinNhans = Array.isArray(data.tinNhans)
                ? data.tinNhans
                : [];

            tinNhans.forEach(tinNhan => {
                const vaiTro = String(tinNhan.vaiTro || "")
                    .toLowerCase();

                if (vaiTro === "user" || vaiTro === "khachhang") {
                    messageBubble(tinNhan.noiDung || "", true);
                    return;
                }

                if (
                    vaiTro === "assistant" ||
                    vaiTro === "bot" ||
                    vaiTro === "ai"
                ) {
                    let ketQua = null;

                    if (tinNhan.duLieuKetQuaJson) {
                        try {
                            ketQua = JSON.parse(
                                tinNhan.duLieuKetQuaJson
                            );
                        } catch (_) {
                            ketQua = null;
                        }
                    }

                    if (ketQua && ketQua.thanhCong) {
                        renderResults(ketQua);
                    } else {
                        messageBubble(
                            tinNhan.noiDung ||
                            "Mình đã phản hồi câu hỏi của bạn.",
                            false
                        );
                    }

                    return;
                }

                messageBubble(tinNhan.noiDung || "", false);
            });

            if (tinNhans.length === 0) {
                messages.innerHTML = initialMarkup;
            }

            showHistoryView(false);
            renderHistoryList(danhSachLichSu);

            input.value = "";
            input.focus();
            scrollBottom();
        } catch (error) {
            showHistoryView(true);
            showHistoryError(error.message);
        } finally {
            dangTaiHoiThoai = false;
            setHistoryLoading(false);
        }
    }

    async function khoiPhucCuocTroChuyenGanNhat() {
        const danhSach = await taiDanhSachLichSu(false);

        if (danhSach.length > 0) {
            await taiVaMoHoiThoai(danhSach[0].hoiThoaiId);
        } else {
            input.focus();
            scrollBottom();
        }
    }

    function batDauCuocTroChuyenMoi() {
        if (dangGui || dangTaiHoiThoai) return;

        setConversationId(null);
        nguCanh = null;

        messages.innerHTML = initialMarkup;

        input.value = "";
        input.style.height = "auto";

        showHistoryView(false);

        if (historyList) {
            historyList.querySelectorAll(".kp-history-item-active")
                .forEach(item => {
                    item.classList.remove("kp-history-item-active");
                    item.removeAttribute("aria-current");
                });
        }

        input.focus();
        scrollBottom();
    }

    if (historyToggle && historyPanel) {
        historyToggle.addEventListener("click", async () => {
            if (dangGui || dangTaiHoiThoai) return;

            const shouldShow = historyPanel.hidden;
            showHistoryView(shouldShow);

            if (shouldShow) {
                await taiDanhSachLichSu(true);
            } else {
                input.focus();
            }
        });
    }

    if (historyList) {
        historyList.addEventListener("click", (e) => {
            const item = e.target.closest(".kp-history-item");

            if (!item || dangGui || dangTaiHoiThoai) return;

            taiVaMoHoiThoai(item.dataset.id);
        });
    }

    if (historyNewButton) {
        historyNewButton.addEventListener(
            "click",
            batDauCuocTroChuyenMoi
        );
    }

    async function sendQuestion(question) {
        if (dangGui || dangTaiHoiThoai) return;

        const text = (question || "").trim();

        if (!text || text.length > 500) return;

        dangGui = true;
        sendButton.disabled = true;
        input.disabled = true;

        messageBubble(text, true);

        input.value = "";
        input.style.height = "auto";

        const currentSuggestions = messages.querySelector(
            "#kpSuggestions"
        );

        if (currentSuggestions) {
            currentSuggestions.hidden = true;
        }

        const loading = showTyping();

        const formData = new FormData(form);
        formData.set("noiDung", text);
        formData.set("hoiThoaiId", hoiThoaiId || "");

        if (!hoiThoaiId && nguCanh) {
            formData.set(
                "nguCanhJson",
                JSON.stringify(nguCanh)
            );
        } else {
            formData.delete("nguCanhJson");
        }

        try {
            const response = await fetch(apiUrl, {
                method: "POST",
                body: formData,
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json"
                }
            });

            if (response.status === 429) {
                throw new Error(
                    "Đã có nhiều yêu cầu tìm kiếm. " +
                    "Bạn vui lòng thử lại sau ít phút."
                );
            }

            if (!response.ok) {
                let textError = "Chưa thể tìm sự kiện lúc này.";

                try {
                    const errorData = await response.json();
                    textError = errorData.thongBao || textError;
                } catch (_) { }

                throw new Error(textError);
            }

            const data = await response.json();

            if (!data.thanhCong) {
                throw new Error(
                    data.thongBao || "Không tìm được kết quả."
                );
            }

            nguCanh = data.nhuCau || null;

            if (data.hoiThoaiId) {
                setConversationId(data.hoiThoaiId);
            }

            loading.remove();

            renderResults(data);

            if (historyList) {
                taiDanhSachLichSu(false);
            }
        } catch (error) {
            loading.remove();

            messageBubble(
                error.message ||
                "Có lỗi kết nối. Vui lòng thử lại.",
                false
            );
        } finally {
            dangGui = false;
            sendButton.disabled = false;
            input.disabled = false;
            input.focus();
            scrollBottom();
        }
    }

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        sendQuestion(input.value);
    });

    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();

            if (!dangGui) {
                form.requestSubmit();
            }
        }
    });

    input.addEventListener("input", () => {
        input.style.height = "auto";

        input.style.height = Math.min(
            input.scrollHeight,
            110
        ) + "px";
    });

    if (messages) {
        messages.addEventListener("click", (e) => {
            const btn = e.target.closest("[data-kp-question]");

            if (!btn || dangGui) return;

            sendQuestion(btn.dataset.kpQuestion);
        });
    }

    if (resetButton) {
        resetButton.addEventListener(
            "click",
            batDauCuocTroChuyenMoi
        );
    }
})();
