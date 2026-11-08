
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
    const suggestions = document.getElementById("kpSuggestions");

    const apiUrl = "/kham-pha-hoi-thoai/hoi";

    // Ngữ cảnh chỉ giữ trong trang hiện tại.
    // Không ghi vào database hoặc localStorage.
    let nguCanh = null;
    let dangGui = false;

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

    function openPanel() {
        panel.hidden = false;
        launcher.setAttribute("aria-expanded", "true");
        launcher.classList.add("kp-open");
        input.focus();
        scrollBottom();
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
            closePanel();
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
    if (!value) {
        return "Đang cập nhật";
    }

    // ASP.NET trả DateTime dạng:
    // 2027-01-31T18:30:00
    // Lấy trực tiếp giờ địa phương của sự kiện,
    // tránh trình duyệt tự chuyển múi giờ.
    const match = String(value).match(
        /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/
    );

    if (match) {
        const [, year, month, day, hour, minute] = match;

        return `${day}/${month}/${year} • ${hour}:${minute}`;
    }

    return "Đang cập nhật";
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

        // Chỉ cho phép ảnh thuộc thư mục upload của website.
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

            link.href = "/SuKien/ChiTiet/" + id;
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

    // 1. Hiển thị điều kiện hiện tại từ Gemini.
    renderSearchConditions(data.nhuCau);

    // 2. Trạng thái cá nhân hóa.
    const personalLabel = data.caNhanHoa
        ? "✦ Đã bật xếp hạng theo sở thích tài khoản"
        : "✦ Đang tìm kiếm theo nhu cầu của bạn";

    messages.appendChild(
        create("div", "kp-personal-status", personalLabel)
    );

    // 3. Phân loại theo kết quả từ SQL Server.
    const chinhXac = items.filter(
        x => x.laGoiYMoRong !== true
    );

    const moRong = items.filter(
        x => x.laGoiYMoRong === true
    );

    // 4. Thông báo rõ số lượng từng nhóm.
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
    }
    else if (chinhXac.length > 0) {
        thongBao =
            `Mình tìm được ${chinhXac.length} sự kiện ` +
            "đáp ứng đầy đủ yêu cầu. " +
            `Ngoài ra còn ${moRong.length} gợi ý liên quan ` +
            "để bạn tham khảo.";
    }
    else {
        thongBao =
            "Hiện chưa tìm được sự kiện đáp ứng đầy đủ " +
            "yêu cầu. Mình có một số sự kiện liên quan, " +
            "nhưng chúng chưa khớp toàn bộ điều kiện.";
    }

    messageBubble(thongBao, false);

    // 5. Tạo vùng chứa kết quả.
    const results = create(
        "div",
        "kp-results kp-grouped-results"
    );

    // Hàm tạo một nhóm sự kiện.
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

        // Giữ nguyên thứ tự do service xếp hạng.
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

    // 6. Nhóm chính xác luôn xuất hiện trước.
    addGroup(
        "Đáp ứng đầy đủ yêu cầu",
        "Các sự kiện đáp ứng những điều kiện " +
        "bạn đang tìm kiếm.",
        chinhXac,
        "exact"
    );

    // 7. Nhóm mở rộng nằm riêng.
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



    async function sendQuestion(question) {
        if (dangGui) return;

        const text = (question || "").trim();

        if (!text || text.length > 500) return;

        dangGui = true;
        sendButton.disabled = true;
        input.disabled = true;

        messageBubble(text, true);
        input.value = "";
        input.style.height = "auto";

const currentSuggestions = messages.querySelector("#kpSuggestions");

if (currentSuggestions) {
    currentSuggestions.hidden = true;
}

        const loading = showTyping();

        const formData = new FormData(form);
        formData.set("noiDung", text);

        if (nguCanh) {
            formData.set(
                "nguCanhJson",
                JSON.stringify(nguCanh)
            );
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

            // Chỉ cập nhật ngữ cảnh khi request thành công.
            nguCanh = data.nhuCau || null;

            loading.remove();
            renderResults(data);

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
            const btn = e.target.closest(
                "[data-kp-question]"
            );

            if (!btn || dangGui) return;

            sendQuestion(btn.dataset.kpQuestion);
        });
    }

    resetButton.addEventListener("click", () => {
        if (dangGui) return;

        nguCanh = null;
        messages.innerHTML = initialMarkup;
        input.value = "";
        input.focus();
        scrollBottom();
    });
})();

