(() => {
    const showMessage = ({ type = "info", title = "Informasi Sistem", message = "" }) => {
        if (typeof window.showAppMessage === "function") {
            window.showAppMessage({ type, title, message });
            return;
        }

        window.alert(message || title);
    };

    const employeeList = document.getElementById("employeeDragList");
    const board = document.getElementById("scheduleBoard");
    const searchInput = document.getElementById("employeeSearchInput");
    const employeeVisibleCount = document.getElementById("employeeVisibleCount");
    const employeeCompanyFilter = document.getElementById("employeeCompanyFilter");
    const vehicleSearchInput = document.getElementById("vehicleSearchInput");
    const vehicleCompanyFilter = document.getElementById("vehicleCompanyFilter");
    const vehicleVisibleCount = document.getElementById("vehicleVisibleCount");
    const companyFilterButtons = document.querySelectorAll(".schedule-company-filter");
    const accessModalElement = document.getElementById("scheduleAccessModal");
    const accessEmployeeNode = document.getElementById("scheduleAccessEmployee");
    const accessVehicleNode = document.getElementById("scheduleAccessVehicle");
    const accessLinkNode = document.getElementById("scheduleAccessLink");
    const accessRefIdNode = document.getElementById("scheduleAccessRefId");
    const accessTimeNode = document.getElementById("scheduleAccessTime");
    const accessCopyAgainButton = document.getElementById("scheduleAccessCopyAgain");

    if (!employeeList || !board) {
        return;
    }

    let copiedAccessText = "";
    let accessModal = null;

    const showAccessModal = ({ employeeName, nrp, vehicleName, link, refId, jamTes, copiedText }) => {
        copiedAccessText = copiedText;

        if (!accessModalElement || typeof bootstrap === "undefined") {
            showMessage({ type: "success", title: "Akses Tes Disalin", message: copiedText });
            return;
        }

        if (!accessModal) {
            accessModal = new bootstrap.Modal(accessModalElement);
        }

        if (accessEmployeeNode) {
            accessEmployeeNode.textContent = `${nrp} - ${employeeName}`;
        }

        if (accessVehicleNode) {
            accessVehicleNode.textContent = vehicleName || "Vehicle belum tersedia";
        }

        if (accessLinkNode) {
            accessLinkNode.textContent = link || "-";
            accessLinkNode.title = link || "";
        }

        if (accessRefIdNode) {
            accessRefIdNode.textContent = refId || "-";
        }

        if (accessTimeNode) {
            accessTimeNode.textContent = jamTes || "-";
        }

        accessModal.show();
    };

    if (accessCopyAgainButton) {
        accessCopyAgainButton.addEventListener("click", async () => {
            if (!copiedAccessText) {
                return;
            }

            try {
                await navigator.clipboard.writeText(copiedAccessText);
                if (typeof window.showAppMessage === "function") {
                    window.showAppMessage({
                        type: "success",
                        title: "Clipboard Diperbarui",
                        message: "Teks akses tes berhasil disalin ulang ke clipboard."
                    });
                }
            } catch (_error) {
                window.prompt("Copy manual teks di bawah ini:", copiedAccessText);
            }
        });
    }

    const boardShell = board.closest(".schedule-board-shell");
    const maxParticipantsPerSession = Number(board.dataset.maxParticipants || "4");
    let draggedEmployee = null;
    let draggedSchedule = null;
    let draggedElement = null;
    let isMutating = false;

    const resetDragState = () => {
        draggedEmployee = null;
        draggedSchedule = null;
        draggedElement = null;
    };

    const setMutatingState = (value) => {
        isMutating = value;
        boardShell?.classList.toggle("is-mutating", value);
    };

    const createEmptyStateNode = () => {
        const emptyNode = document.createElement("div");
        emptyNode.className = "schedule-slot-empty";
        emptyNode.textContent = "Drop peserta di sini";
        return emptyNode;
    };

    const getSlotStack = (slot) => slot.querySelector(".schedule-slot-stack");

    const syncSlotState = (slot) => {
        const stack = getSlotStack(slot);
        if (!stack) {
            return;
        }

        const chips = Array.from(stack.querySelectorAll(".scheduled-chip"));
        let capacityNode = stack.querySelector(".schedule-slot-capacity");
        const emptyNode = stack.querySelector(".schedule-slot-empty");

        if (chips.length === 0) {
            capacityNode?.remove();
            if (!emptyNode) {
                stack.appendChild(createEmptyStateNode());
            }
            return;
        }

        if (!capacityNode) {
            capacityNode = document.createElement("div");
            capacityNode.className = "schedule-slot-capacity";
            stack.prepend(capacityNode);
        }

        capacityNode.textContent = `${chips.length} / ${maxParticipantsPerSession} peserta`;
        emptyNode?.remove();
    };

    const buildScheduledChip = ({ scheduleId, employeeId, nrp, name, status }) => {
        const chip = document.createElement("div");
        chip.className = `scheduled-chip schedule-session-card schedule-session-card--theory scheduled-${status} is-entering`;
        chip.dataset.scheduleId = String(scheduleId);
        chip.dataset.employeeId = String(employeeId);
        chip.dataset.nrp = nrp || "";
        chip.dataset.name = name || "";
        chip.draggable = true;
        chip.innerHTML = `
            <div class="schedule-session-card-head">
                <div class="schedule-session-card-code text-truncate d-flex align-items-center gap-2">
                    <span>${nrp || "-"}</span>
                </div>
            </div>
            <div class="schedule-session-card-body">
                <div class="schedule-session-card-title text-truncate">${name || "-"}</div>
            </div>
            <div class="schedule-session-card-foot">
                <span class="schedule-status-pill schedule-status-${String(status || "scheduled").replaceAll(" ", "_")}">${status || "scheduled"}</span>
                <button type="button" class="btn btn-sm btn-outline-danger btn-remove-schedule" data-schedule-id="${scheduleId}" draggable="false">
                    <i class="fas fa-xmark"></i>
                </button>
            </div>`;

        window.setTimeout(() => chip.classList.remove("is-entering"), 260);
        return chip;
    };

    employeeList.addEventListener("dragstart", (event) => {
        const card = event.target instanceof HTMLElement ? event.target.closest(".schedule-employee") : null;
        if (!(card instanceof HTMLElement) || isMutating) {
            return;
        }

        draggedEmployee = {
            employeeId: Number(card.dataset.employeeId),
            nrp: card.dataset.nrp,
            nik: card.dataset.nik,
            name: card.dataset.name
        };
        draggedSchedule = null;
        draggedElement = card;
        card.classList.add("dragging");
        event.dataTransfer?.setData("text/plain", card.dataset.employeeId || "");
        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = "copy";
        }
    });

    employeeList.addEventListener("dragend", () => {
        draggedElement?.classList.remove("dragging");
        resetDragState();
    });

    board.addEventListener("dragstart", (event) => {
        const chip = event.target instanceof HTMLElement ? event.target.closest(".scheduled-chip") : null;
        if (!(chip instanceof HTMLElement) || isMutating) {
            return;
        }

        draggedEmployee = null;
        draggedSchedule = {
            scheduleId: Number(chip.dataset.scheduleId),
            employeeId: Number(chip.dataset.employeeId),
            nrp: chip.dataset.nrp,
            name: chip.dataset.name
        };
        draggedElement = chip;
        chip.classList.add("dragging");
        event.dataTransfer?.setData("text/plain", chip.dataset.scheduleId || "");
        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = "move";
        }
    });

    board.addEventListener("dragend", () => {
        draggedElement?.classList.remove("dragging");
        resetDragState();
    });

    board.addEventListener("contextmenu", async (event) => {
        const chip = event.target instanceof HTMLElement ? event.target.closest(".scheduled-chip") : null;
        if (!(chip instanceof HTMLElement)) {
            return;
        }

        event.preventDefault();

        const slot = chip.closest(".schedule-slot");
        if (!slot) {
            return;
        }

        const employeeId = Number(chip.dataset.employeeId);
        const vehicleId = Number(slot.dataset.vehicleId);
        const scheduledAt = slot.dataset.scheduledAt || "";

        if (!employeeId || !vehicleId) {
            showMessage({ type: "error", title: "Generate Akses Gagal", message: "Data jadwal tidak lengkap untuk generate akses." });
            return;
        }

        const response = await fetch("/Admin/GenerateAccessFromSchedule", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ employeeId, vehicleId })
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: "Gagal generate akses dari jadwal." }));
            showMessage({ type: "error", title: "Generate Akses Gagal", message: error.message || "Gagal generate akses dari jadwal." });
            return;
        }

        const payload = await response.json();
        const localDate = scheduledAt ? new Date(scheduledAt) : null;
        const jamTes = localDate && !Number.isNaN(localDate.getTime())
            ? `${localDate.getDate().toString().padStart(2, "0")}/${(localDate.getMonth() + 1).toString().padStart(2, "0")}/${localDate.getFullYear()} ${localDate.getHours().toString().padStart(2, "0")}:${localDate.getMinutes().toString().padStart(2, "0")}`
            : "-";

        const copiedText = `Link : ${payload.link || "-"}\nRefId : ${payload.refId || "-"}\nJam Tes : ${jamTes}`;

        try {
            await navigator.clipboard.writeText(copiedText);
            showAccessModal({
                employeeName: payload.employee || chip.dataset.name || "-",
                nrp: payload.nrp || chip.dataset.nrp || "-",
                vehicleName: payload.vehicle || "-",
                link: payload.link || "-",
                refId: payload.refId || "-",
                jamTes,
                copiedText
            });
        } catch (_error) {
            window.prompt("Copy manual teks di bawah ini:", copiedText);
        }
    });

    const slots = board.querySelectorAll(".schedule-slot");
    slots.forEach((slot) => {
        slot.addEventListener("dragover", (event) => {
            event.preventDefault();
            slot.classList.add("drop-hover");
        });

        slot.addEventListener("dragleave", () => {
            slot.classList.remove("drop-hover");
        });

        slot.addEventListener("drop", async (event) => {
            event.preventDefault();
            slot.classList.remove("drop-hover");

            if (!draggedEmployee) {
                if (!draggedSchedule) {
                    return;
                }
            }

            if (isMutating) {
                return;
            }

            const activeDraggedEmployee = draggedEmployee ? { ...draggedEmployee } : null;
            const activeDraggedSchedule = draggedSchedule ? { ...draggedSchedule } : null;
            const activeDraggedElement = draggedElement instanceof HTMLElement ? draggedElement : null;
            const sourceSlot = activeDraggedElement?.closest(".schedule-slot") || null;

            let response;
            let payload = null;
            setMutatingState(true);
            slot.classList.add("slot-syncing");

            if (activeDraggedEmployee) {
                const payload = {
                    employeeId: activeDraggedEmployee.employeeId,
                    vehicleId: Number(slot.dataset.vehicleId),
                    scheduledAt: slot.dataset.scheduledAt
                };

                response = await fetch("/Admin/AssignSchedule", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });
            } else {
                const payload = {
                    scheduleId: activeDraggedSchedule.scheduleId,
                    vehicleId: Number(slot.dataset.vehicleId),
                    scheduledAt: slot.dataset.scheduledAt
                };

                response = await fetch("/Admin/MoveSchedule", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });
            }

            try {
                payload = await response.json().catch(() => ({ message: "Penjadwalan diperbarui." }));

                if (!response.ok) {
                    showMessage({ type: "error", title: "Penjadwalan Gagal", message: payload.message || "Gagal menyimpan jadwal." });
                    return;
                }

                const targetStack = getSlotStack(slot);
                if (!targetStack) {
                    return;
                }

                if (activeDraggedEmployee) {
                    const newChip = buildScheduledChip({
                        scheduleId: Number(payload.scheduleId || 0),
                        employeeId: activeDraggedEmployee.employeeId,
                        nrp: activeDraggedEmployee.nrp || activeDraggedEmployee.nik || "-",
                        name: activeDraggedEmployee.name || "-",
                        status: "scheduled"
                    });
                    targetStack.appendChild(newChip);
                } else if (activeDraggedElement instanceof HTMLElement) {
                    activeDraggedElement.classList.add("is-entering");
                    targetStack.appendChild(activeDraggedElement);
                    window.setTimeout(() => activeDraggedElement.classList.remove("is-entering"), 260);
                }

                syncSlotState(slot);
                if (sourceSlot && sourceSlot !== slot) {
                    syncSlotState(sourceSlot);
                }
            } finally {
                slot.classList.remove("slot-syncing");
                setMutatingState(false);
                resetDragState();
            }
        });
    });

    const applyFilters = () => {
        const employeeKey = (searchInput?.value || "").trim().toLowerCase();
        const employeeCompany = employeeCompanyFilter?.value || "all";
        const vehicleKey = (vehicleSearchInput?.value || "").trim().toLowerCase();
        const vehicleCompany = vehicleCompanyFilter?.value || "all";

        let visibleEmployeeCount = 0;
        employeeList.querySelectorAll(".schedule-employee").forEach((card) => {
            const text = `${card.dataset.nrp || ""} ${card.dataset.nik || ""} ${card.dataset.ktp || ""} ${card.dataset.name || ""} ${card.dataset.companyName || ""}`.toLowerCase();
            const companyMatches = employeeCompany === "all" || card.dataset.companyId === employeeCompany;
            const textMatches = text.includes(employeeKey);
            const isVisible = companyMatches && textMatches;
            card.style.display = isVisible ? "grid" : "none";
            if (isVisible) {
                visibleEmployeeCount++;
            }
        });

        if (employeeVisibleCount) {
            employeeVisibleCount.textContent = String(visibleEmployeeCount);
        }

        let visibleVehicleCount = 0;
        board.querySelectorAll(".schedule-board-row").forEach((row) => {
            const text = `${row.dataset.vehicleName || ""} ${row.dataset.simperType || ""} ${row.dataset.companyName || ""}`.toLowerCase();
            const companyMatches = vehicleCompany === "all" || row.dataset.companyId === vehicleCompany;
            const textMatches = text.includes(vehicleKey);
            const isVisible = companyMatches && textMatches;
            row.style.display = isVisible ? "" : "none";
            if (isVisible) {
                visibleVehicleCount++;
            }
        });

        board.querySelectorAll(".schedule-company-row").forEach((row) => {
            const companyId = row.dataset.companyId;
            const hasVisibleChildren = Array.from(board.querySelectorAll(`.schedule-board-row[data-company-id="${companyId}"]`))
                .some((child) => child.style.display !== "none");
            row.style.display = hasVisibleChildren ? "" : "none";
        });

        if (vehicleVisibleCount) {
            vehicleVisibleCount.textContent = String(visibleVehicleCount);
        }
    };

    [searchInput, employeeCompanyFilter, vehicleSearchInput, vehicleCompanyFilter].forEach((element) => {
        if (!element) {
            return;
        }

        element.addEventListener("input", applyFilters);
        element.addEventListener("change", applyFilters);
    });

    companyFilterButtons.forEach((button) => {
        button.addEventListener("click", () => {
            const companyId = button.dataset.companyFilter || "all";
            companyFilterButtons.forEach((item) => item.classList.toggle("active", item === button));

            if (employeeCompanyFilter) {
                employeeCompanyFilter.value = companyId;
            }

            if (vehicleCompanyFilter) {
                vehicleCompanyFilter.value = companyId;
            }

            applyFilters();
        });
    });

    applyFilters();

    board.addEventListener("click", async (event) => {
        const btn = event.target instanceof HTMLElement ? event.target.closest(".btn-remove-schedule") : null;
        if (!(btn instanceof HTMLElement) || isMutating) {
            return;
        }

        const scheduleId = Number(btn.dataset.scheduleId);
        if (!scheduleId) {
            return;
        }

        const ok = confirm("Hapus jadwal ini?");
        if (!ok) {
            return;
        }

        const chip = btn.closest(".scheduled-chip");
        const slot = btn.closest(".schedule-slot");

        setMutatingState(true);
        slot?.classList.add("slot-syncing");

        try {
            const response = await fetch("/Admin/RemoveSchedule", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ scheduleId })
            });

            const payload = await response.json().catch(() => ({ message: "Gagal menghapus jadwal." }));
            if (!response.ok) {
                showMessage({ type: "error", title: "Hapus Jadwal Gagal", message: payload.message || "Gagal menghapus jadwal." });
                return;
            }

            chip?.remove();
            if (slot instanceof HTMLElement) {
                syncSlotState(slot);
            }
        } finally {
            slot?.classList.remove("slot-syncing");
            setMutatingState(false);
        }
    });
})();
