(() => {
    const showMessage = ({ type = "info", title = "Informasi Sistem", message = "" }) => {
        if (typeof window.showAppMessage === "function") {
            window.showAppMessage({ type, title, message });
            return;
        }

        window.alert(message || title);
    };

    const board = document.getElementById("practicalScheduleBoard");
    const searchInput = document.getElementById("practicalBoardSearch");
    const employeeSelect = document.getElementById("practicalEmployeeSelect");
    const vehicleSelect = document.getElementById("practicalVehicleSelect");
    const templateSelect = document.getElementById("practicalTemplateSelect");
    const instructorSelect = document.getElementById("practicalInstructorSelect");
    const assistNode = document.getElementById("practicalFormAssist");
    const modalElement = document.getElementById("practicalLinkModal");
    const employeeNode = document.getElementById("practicalLinkEmployee");
    const vehicleNode = document.getElementById("practicalLinkVehicle");
    const urlNode = document.getElementById("practicalLinkUrl");
    const instructorNode = document.getElementById("practicalLinkInstructor");
    const timeNode = document.getElementById("practicalLinkTime");
    const copyAgainButton = document.getElementById("practicalLinkCopyAgain");

    if (!board) {
        return;
    }

    let draggedSession = null;
    let draggedElement = null;
    let copiedText = "";
    let modal = null;
    let isMutating = false;

    const boardShell = board.closest(".schedule-board-shell");

    const showLinkModal = ({ employee, nrp, vehicle, instructor, link, time, text }) => {
        copiedText = text;
        if (!modalElement || typeof bootstrap === "undefined") {
            showMessage({ type: "success", title: "Link Penilaian Disalin", message: text });
            return;
        }

        if (!modal) {
            modal = new bootstrap.Modal(modalElement);
        }

        if (employeeNode) {
            employeeNode.textContent = `${nrp} - ${employee}`;
        }
        if (vehicleNode) {
            vehicleNode.textContent = vehicle || "-";
        }
        if (urlNode) {
            urlNode.textContent = link || "-";
            urlNode.title = link || "";
        }
        if (instructorNode) {
            instructorNode.textContent = instructor || "-";
        }
        if (timeNode) {
            timeNode.textContent = time || "-";
        }

        modal.show();
    };

    const resetDragState = () => {
        draggedSession = null;
        draggedElement = null;
    };

    const setMutatingState = (value) => {
        isMutating = value;
        boardShell?.classList.toggle("is-mutating", value);
    };

    const createEmptyStateNode = () => {
        const emptyNode = document.createElement("div");
        emptyNode.className = "schedule-slot-empty";
        emptyNode.textContent = "Belum ada jadwal praktek";
        return emptyNode;
    };

    const getSlotStack = (slot) => slot.querySelector(".schedule-slot-stack");

    const syncSlotState = (slot) => {
        const stack = getSlotStack(slot);
        if (!stack) {
            return;
        }

        const chips = Array.from(stack.querySelectorAll(".practical-scheduled-chip"));
        const emptyNode = stack.querySelector(".schedule-slot-empty");

        if (chips.length === 0) {
            if (!emptyNode) {
                stack.appendChild(createEmptyStateNode());
            }
        } else {
            emptyNode?.remove();
        }
    };

    const applyBoardFilter = () => {
        const keyword = (searchInput?.value || "").trim().toLowerCase();
        const rows = Array.from(board.querySelectorAll(".schedule-board-row"));

        rows.forEach((row) => {
            const rowText = `${row.dataset.vehicleName || ""} ${row.dataset.simperType || ""} ${row.dataset.companyName || ""}`.toLowerCase();
            const chips = Array.from(row.querySelectorAll(".practical-scheduled-chip"));

            let visibleChipCount = 0;
            chips.forEach((chip) => {
                const chipText = `${chip.dataset.nrp || ""} ${chip.dataset.name || ""} ${chip.dataset.instructor || ""} ${chip.dataset.vehicleName || ""} ${chip.dataset.companyName || ""} ${chip.dataset.status || ""}`.toLowerCase();
                const chipVisible = !keyword || chipText.includes(keyword);
                chip.style.display = chipVisible ? "" : "none";
                if (chipVisible) {
                    visibleChipCount++;
                }
            });

            const emptyNode = row.querySelector(".schedule-slot-empty");
            if (emptyNode instanceof HTMLElement && visibleChipCount > 0) {
                emptyNode.style.display = "none";
            } else if (emptyNode instanceof HTMLElement) {
                emptyNode.style.display = "";
            }

            const rowVisible = !keyword || rowText.includes(keyword) || visibleChipCount > 0;
            row.style.display = rowVisible ? "" : "none";
        });

        board.querySelectorAll(".schedule-company-row").forEach((companyRow) => {
            const companyId = companyRow.getAttribute("data-company-id");
            const hasVisibleRows = Array.from(board.querySelectorAll(`.schedule-board-row[data-company-id="${companyId}"]`))
                .some((row) => row instanceof HTMLElement && row.style.display !== "none");
            companyRow.style.display = hasVisibleRows ? "" : "none";
        });
    };

    const syncAssistText = () => {
        if (!assistNode) {
            return;
        }

        const employeeOption = employeeSelect?.selectedOptions?.[0];
        const vehicleOption = vehicleSelect?.selectedOptions?.[0];
        const templateOption = templateSelect?.selectedOptions?.[0];
        const instructorOption = instructorSelect?.selectedOptions?.[0];

        if (!employeeOption || !employeeOption.value) {
            assistNode.textContent = "Pilih peserta terlebih dahulu agar pilihan unit, template, dan instruktur otomatis difokuskan ke company yang benar.";
            return;
        }

        const employeeName = employeeOption.getAttribute("data-name") || employeeOption.textContent || "Peserta";
        const companyName = employeeOption.getAttribute("data-company-name") || "company peserta";
        const unitName = vehicleOption?.value ? (vehicleOption.getAttribute("data-vehicle-name") || vehicleOption.textContent || "unit") : "unit belum dipilih";
        const templateName = templateOption?.value ? (templateOption.textContent || "template") : "template belum dipilih";
        const instructorName = instructorOption?.value ? (instructorOption.textContent || "instruktur") : "instruktur belum dipilih";

        assistNode.textContent = `${employeeName} akan dijadwalkan pada ${companyName}. Unit: ${unitName}. Template: ${templateName}. Instruktur: ${instructorName}.`;
    };

    const syncTemplateOptions = () => {
        const selectedCompanyId = employeeSelect?.selectedOptions?.[0]?.getAttribute("data-company-id") || "";
        const selectedVehicleId = vehicleSelect?.value || "";

        if (!templateSelect) {
            return;
        }

        let hasVisibleSelectedOption = false;
        Array.from(templateSelect.options).forEach((option, index) => {
            if (index === 0) {
                option.hidden = false;
                return;
            }

            const optionCompanyId = option.getAttribute("data-company-id") || "";
            const optionVehicleId = option.getAttribute("data-vehicle-id") || "";
            const companyMatches = !selectedCompanyId || optionCompanyId === selectedCompanyId;
            const vehicleMatches = !selectedVehicleId || optionVehicleId === selectedVehicleId;
            const shouldShow = companyMatches && vehicleMatches;
            option.hidden = !shouldShow;

            if (shouldShow && option.selected) {
                hasVisibleSelectedOption = true;
            }
        });

        if (!hasVisibleSelectedOption) {
            const firstVisible = Array.from(templateSelect.options).find((option, index) => index > 0 && !option.hidden);
            templateSelect.value = firstVisible?.value || "";
        }
    };

    const syncCompanyScopedOptions = () => {
        const selectedCompanyId = employeeSelect?.selectedOptions?.[0]?.getAttribute("data-company-id") || "";

        [vehicleSelect, instructorSelect].forEach((select) => {
            if (!select) {
                return;
            }

            let hasVisibleSelectedOption = false;
            Array.from(select.options).forEach((option, index) => {
                if (index === 0) {
                    option.hidden = false;
                    return;
                }

                const optionCompanyId = option.getAttribute("data-company-id") || "";
                const shouldShow = !selectedCompanyId || optionCompanyId === selectedCompanyId;
                option.hidden = !shouldShow;
                if (shouldShow && option.selected) {
                    hasVisibleSelectedOption = true;
                }
            });

            if (!hasVisibleSelectedOption) {
                const firstVisible = Array.from(select.options).find((option, index) => index > 0 && !option.hidden);
                select.value = firstVisible?.value || "";
            }
        });

        syncTemplateOptions();
        syncAssistText();
    };

    if (copyAgainButton) {
        copyAgainButton.addEventListener("click", async () => {
            if (!copiedText) {
                return;
            }

            try {
                await navigator.clipboard.writeText(copiedText);
                showMessage({ type: "success", title: "Clipboard Diperbarui", message: "Link penilaian berhasil disalin ulang." });
            } catch (_error) {
                window.prompt("Copy manual teks di bawah ini:", copiedText);
            }
        });
    }

    searchInput?.addEventListener("input", applyBoardFilter);
    employeeSelect?.addEventListener("change", syncCompanyScopedOptions);
    vehicleSelect?.addEventListener("change", () => {
        syncTemplateOptions();
        syncAssistText();
    });
    templateSelect?.addEventListener("change", syncAssistText);
    instructorSelect?.addEventListener("change", syncAssistText);

    syncCompanyScopedOptions();
    applyBoardFilter();

    board.addEventListener("dragstart", (event) => {
        const chip = event.target instanceof HTMLElement ? event.target.closest(".practical-scheduled-chip") : null;
        if (!(chip instanceof HTMLElement) || isMutating) {
            return;
        }

        draggedSession = { sessionId: Number(chip.dataset.sessionId) };
        draggedElement = chip;
        chip.classList.add("dragging");
        event.dataTransfer?.setData("text/plain", chip.dataset.sessionId || "");
        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = "move";
        }
    });

    board.addEventListener("dragend", () => {
        draggedElement?.classList.remove("dragging");
        resetDragState();
    });

    board.addEventListener("contextmenu", async (event) => {
        const chip = event.target instanceof HTMLElement ? event.target.closest(".practical-scheduled-chip") : null;
        if (!(chip instanceof HTMLElement)) {
            return;
        }

        event.preventDefault();

        const sessionId = Number(chip.dataset.sessionId);
        if (!sessionId) {
            return;
        }

        const response = await fetch("/Admin/GeneratePracticalLink", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ sessionId })
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: "Gagal menyiapkan link penilaian." }));
            showMessage({ type: "error", title: "Link Penilaian Gagal", message: error.message || "Gagal menyiapkan link penilaian." });
            return;
        }

        const payload = await response.json();
        const localDate = payload.scheduleAt ? new Date(payload.scheduleAt) : null;
        const formattedTime = localDate && !Number.isNaN(localDate.getTime())
            ? `${localDate.getDate().toString().padStart(2, "0")}/${(localDate.getMonth() + 1).toString().padStart(2, "0")}/${localDate.getFullYear()} ${localDate.getHours().toString().padStart(2, "0")}:${localDate.getMinutes().toString().padStart(2, "0")}`
            : "-";

        const text = `Link Penilaian : ${payload.link || "-"}\nInstruktur : ${payload.instructor || "-"}\nPeserta : ${payload.nrp || "-"} - ${payload.employee || "-"}\nUnit : ${payload.vehicle || "-"}\nJam Praktek : ${formattedTime}\nCatatan : Instruktur tetap login menggunakan akunnya sendiri.`;

        try {
            await navigator.clipboard.writeText(text);
            showLinkModal({
                employee: payload.employee || chip.dataset.name || "-",
                nrp: payload.nrp || chip.dataset.nrp || "-",
                vehicle: payload.vehicle || chip.dataset.vehicleName || "-",
                instructor: payload.instructor || chip.dataset.instructor || "-",
                link: payload.link || "-",
                time: formattedTime,
                text
            });
        } catch (_error) {
            window.prompt("Copy manual teks di bawah ini:", text);
        }
    });

    board.querySelectorAll(".practical-slot").forEach((slot) => {
        slot.addEventListener("dragover", (event) => {
            event.preventDefault();
            if (!isMutating) {
                slot.classList.add("drop-hover");
            }
        });

        slot.addEventListener("dragleave", () => {
            slot.classList.remove("drop-hover");
        });

        slot.addEventListener("drop", async (event) => {
            event.preventDefault();
            slot.classList.remove("drop-hover");

            if (!draggedSession || isMutating) {
                return;
            }

            const activeDraggedElement = draggedElement instanceof HTMLElement ? draggedElement : null;
            const sourceSlot = activeDraggedElement?.closest(".practical-slot") || null;

            setMutatingState(true);
            slot.classList.add("slot-syncing");

            try {
                const response = await fetch("/Admin/MovePracticalSession", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        sessionId: draggedSession.sessionId,
                        vehicleId: Number(slot.dataset.vehicleId),
                        scheduledAt: slot.dataset.scheduledAt
                    })
                });

                const payload = await response.json().catch(() => ({ message: "Gagal memindahkan jadwal praktek." }));
                if (!response.ok) {
                    showMessage({ type: "error", title: "Reschedule Gagal", message: payload.message || "Gagal memindahkan jadwal praktek." });
                    return;
                }

                const targetStack = getSlotStack(slot);
                if (targetStack && activeDraggedElement) {
                    activeDraggedElement.classList.add("is-entering");
                    targetStack.appendChild(activeDraggedElement);
                    window.setTimeout(() => activeDraggedElement.classList.remove("is-entering"), 260);
                }

                syncSlotState(slot);
                if (sourceSlot && sourceSlot !== slot) {
                    syncSlotState(sourceSlot);
                }

                applyBoardFilter();
            } finally {
                slot.classList.remove("slot-syncing");
                setMutatingState(false);
                resetDragState();
            }
        });
    });

    board.addEventListener("click", async (event) => {
        const button = event.target instanceof HTMLElement ? event.target.closest(".btn-remove-practical-session") : null;
        if (!(button instanceof HTMLElement) || isMutating) {
            return;
        }

        const sessionId = Number(button.dataset.sessionId);
        if (!sessionId) {
            return;
        }

        if (!window.confirm("Hapus jadwal praktek ini?")) {
            return;
        }

        const chip = button.closest(".practical-scheduled-chip");
        const slot = button.closest(".practical-slot");

        setMutatingState(true);
        slot?.classList.add("slot-syncing");

        try {
            const response = await fetch("/Admin/RemovePracticalSession", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ sessionId })
            });

            const payload = await response.json().catch(() => ({ message: "Gagal menghapus jadwal praktek." }));
            if (!response.ok) {
                showMessage({ type: "error", title: "Hapus Jadwal Gagal", message: payload.message || "Gagal menghapus jadwal praktek." });
                return;
            }

            chip?.remove();
            if (slot instanceof HTMLElement) {
                syncSlotState(slot);
            }

            applyBoardFilter();
        } finally {
            slot?.classList.remove("slot-syncing");
            setMutatingState(false);
        }
    });
})();
