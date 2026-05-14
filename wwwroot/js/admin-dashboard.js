(() => {
    const tableBody = document.querySelector("#activeExamTable tbody");
    const statActive = document.getElementById("statActive");
    const statCameraOff = document.getElementById("statCameraOff");
    const statSuspicious = document.getElementById("statSuspicious");
    const statAvgScore = document.getElementById("statAvgScore");
    const trendCanvas = document.getElementById("liveTrendChart");
    const exportPngBtn = document.getElementById("btnExportPng");
    const exportPdfBtn = document.getElementById("btnExportPdf");
    const cameraOnCount = document.getElementById("cameraOnCount");
    const cameraOffCountEl = document.getElementById("cameraOffCount");
    const cameraTotalCount = document.getElementById("cameraTotalCount");
    const cameraLastRefresh = document.getElementById("cameraLastRefresh");
    const cameraRealtimeTableBody = document.querySelector("#cameraRealtimeTable tbody");
    const cameraOffAlertBadge = document.getElementById("cameraOffAlertBadge");
    const cameraFilterButtons = document.querySelectorAll("[data-camera-filter]");
    const cameraSearchInput = document.getElementById("cameraSearchInput");
    const cameraLiveGrid = document.getElementById("cameraLiveGrid");
    const suspiciousEventFeed = document.getElementById("suspiciousEventFeed");
    const suspiciousEvents = [];

    let cameraRowsCache = [];
    let activeCameraFilter = "all";
    const cameraFrameCache = new Map();
    const cameraFrameTimeCache = new Map();

    const getItemToken = (item) => item.token || item.Token || "";

    const getFilteredCameraRows = () => {
        const searchKey = (cameraSearchInput?.value || "").trim().toLowerCase();

        return cameraRowsCache.filter((item) => {
            const matchStatus = activeCameraFilter === "all"
                ? true
                : activeCameraFilter === "on"
                    ? item.cameraActive
                    : !item.cameraActive;

            const searchable = `${item.nrp || ""} ${item.employeeName || ""}`.toLowerCase();
            const matchSearch = searchKey.length === 0 || searchable.includes(searchKey);

            return matchStatus && matchSearch;
        });
    };

    const renderCameraRealtimeTable = () => {
        if (!cameraRealtimeTableBody) {
            return;
        }

        const filteredRows = getFilteredCameraRows();
        cameraRealtimeTableBody.innerHTML = filteredRows.map((item) => `
            <tr class="${item.cameraActive ? "" : "camera-alert-row"}">
                <td>${item.sessionId}</td>
                <td>${item.companyName || "-"}</td>
                <td>${item.nrp}</td>
                <td>${item.employeeName}</td>
                <td>${item.vehicleName}</td>
                <td>
                    <span class="badge ${item.cameraActive ? "bg-success" : "bg-danger pulse-alert"}">${item.cameraActive ? "On" : "Off"}</span>
                    ${item.cameraActive ? "" : '<span class="camera-alert-text ms-1">Alert</span>'}
                </td>
            </tr>
        `).join("");

        if (filteredRows.length === 0) {
            cameraRealtimeTableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted py-3">Data kamera tidak ditemukan.</td>
                </tr>
            `;
        }
    };

    const renderCameraLiveGrid = () => {
        if (!cameraLiveGrid) {
            return;
        }

        const filteredRows = getFilteredCameraRows();
        if (filteredRows.length === 0) {
            cameraLiveGrid.innerHTML = `<div class="text-muted small">Tidak ada session yang sesuai filter.</div>`;
            return;
        }

        cameraLiveGrid.innerHTML = filteredRows.map((item) => {
            const token = getItemToken(item);
            const frameData = token ? cameraFrameCache.get(token) : null;
            const frameTime = token ? cameraFrameTimeCache.get(token) : null;
            const frameHtml = frameData
                ? `<img src="${frameData}" alt="Live camera ${item.nrp}" class="camera-live-image" />`
                : `<div class="camera-live-frame empty">Menunggu frame kamera...</div>`;

            return `
                <div class="camera-live-card" data-token="${token}">
                    <div class="camera-live-head">
                        <strong>${item.nrp}</strong>
                        <span class="badge ${item.cameraActive ? "bg-success" : "bg-danger"}">${item.cameraActive ? "On" : "Off"}</span>
                    </div>
                    <div class="camera-live-name">${item.employeeName}</div>
                    ${frameHtml}
                    <div class="camera-live-time">${frameTime ? `Updated: ${frameTime}` : "Updated: -"}</div>
                </div>
            `;
        }).join("");
    };

    const renderSuspiciousFeed = () => {
        if (!suspiciousEventFeed) {
            return;
        }

        if (suspiciousEvents.length === 0) {
            suspiciousEventFeed.innerHTML = `<div class="text-muted small">Belum ada aktivitas mencurigakan terbaru.</div>`;
            return;
        }

        suspiciousEventFeed.innerHTML = suspiciousEvents.map((item) => `
            <div class="dashboard-alert-item">
                <div class="dashboard-alert-icon"><i class="fas fa-triangle-exclamation"></i></div>
                <div class="dashboard-alert-copy">
                    <strong>${item.logType}</strong>
                    <div>${item.description}</div>
                    <small>Session ${item.sessionId} • Tab Switch ${item.tabSwitchCount} • ${item.timeLabel}</small>
                </div>
            </div>
        `).join("");
    };

    if (cameraFilterButtons.length > 0) {
        cameraFilterButtons.forEach((button) => {
            button.addEventListener("click", () => {
                activeCameraFilter = button.dataset.cameraFilter || "all";
                cameraFilterButtons.forEach((btn) => btn.classList.remove("active"));
                button.classList.add("active");
                renderCameraRealtimeTable();
                renderCameraLiveGrid();
            });
        });
    }

    if (cameraSearchInput) {
        cameraSearchInput.addEventListener("input", () => {
            renderCameraRealtimeTable();
            renderCameraLiveGrid();
        });
    }

    const trendLabels = [];
    const trendActive = [];
    const trendCameraOff = [];
    const trendSuspicious = [];
    const maxTrendPoints = 20;
    let trendChart;

    if (!tableBody) {
        return;
    }

    const getSnapshotTarget = () => document.querySelector("main[role='main']") || document.body;

    const captureSnapshotCanvas = async () => {
        if (typeof html2canvas === "undefined") {
            throw new Error("Snapshot library is not available.");
        }

        return html2canvas(getSnapshotTarget(), {
            backgroundColor: "#ffffff",
            scale: 2,
            useCORS: true
        });
    };

    const getFileStamp = () => {
        const now = new Date();
        const yyyy = now.getFullYear();
        const mm = `${now.getMonth() + 1}`.padStart(2, "0");
        const dd = `${now.getDate()}`.padStart(2, "0");
        const hh = `${now.getHours()}`.padStart(2, "0");
        const mi = `${now.getMinutes()}`.padStart(2, "0");
        return `${yyyy}${mm}${dd}-${hh}${mi}`;
    };

    if (exportPngBtn) {
        exportPngBtn.addEventListener("click", async () => {
            try {
                exportPngBtn.disabled = true;
                const canvas = await captureSnapshotCanvas();
                const link = document.createElement("a");
                link.href = canvas.toDataURL("image/png");
                link.download = `simper-dashboard-${getFileStamp()}.png`;
                link.click();
            } catch (error) {
                alert("Gagal export PNG.");
            } finally {
                exportPngBtn.disabled = false;
            }
        });
    }

    if (exportPdfBtn) {
        exportPdfBtn.addEventListener("click", async () => {
            try {
                exportPdfBtn.disabled = true;
                const canvas = await captureSnapshotCanvas();
                const imageData = canvas.toDataURL("image/png");

                if (typeof window.jspdf === "undefined") {
                    throw new Error("PDF library is not available.");
                }

                const { jsPDF } = window.jspdf;
                const pdf = new jsPDF({ orientation: "landscape", unit: "mm", format: "a4" });
                const pageWidth = pdf.internal.pageSize.getWidth();
                const pageHeight = pdf.internal.pageSize.getHeight();

                const ratio = Math.min(pageWidth / canvas.width, pageHeight / canvas.height);
                const renderWidth = canvas.width * ratio;
                const renderHeight = canvas.height * ratio;
                const x = (pageWidth - renderWidth) / 2;
                const y = (pageHeight - renderHeight) / 2;

                pdf.addImage(imageData, "PNG", x, y, renderWidth, renderHeight);
                pdf.save(`simper-dashboard-${getFileStamp()}.pdf`);
            } catch (error) {
                alert("Gagal export PDF.");
            } finally {
                exportPdfBtn.disabled = false;
            }
        });
    }

    const ensureChart = () => {
        if (!trendCanvas || typeof Chart === "undefined" || trendChart) {
            return;
        }

        const css = getComputedStyle(document.body);
        const textColor = css.getPropertyValue("--bs-body-color") || "#334155";

        trendChart = new Chart(trendCanvas, {
            type: "line",
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: "Active",
                        data: trendActive,
                        borderColor: "#2563eb",
                        backgroundColor: "rgba(37, 99, 235, 0.15)",
                        tension: 0.35,
                        fill: true,
                        pointRadius: 2
                    },
                    {
                        label: "Camera Off",
                        data: trendCameraOff,
                        borderColor: "#dc2626",
                        backgroundColor: "rgba(220, 38, 38, 0.08)",
                        tension: 0.35,
                        fill: false,
                        pointRadius: 2
                    },
                    {
                        label: "Suspicious",
                        data: trendSuspicious,
                        borderColor: "#d97706",
                        backgroundColor: "rgba(217, 119, 6, 0.08)",
                        tension: 0.35,
                        fill: false,
                        pointRadius: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: {
                            color: textColor.trim() || "#334155"
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: {
                            color: textColor.trim() || "#334155"
                        },
                        grid: {
                            color: "rgba(148, 163, 184, 0.18)"
                        }
                    },
                    y: {
                        ticks: {
                            color: textColor.trim() || "#334155",
                            precision: 0
                        },
                        grid: {
                            color: "rgba(148, 163, 184, 0.18)"
                        }
                    }
                }
            }
        });
    };

    const updateTrend = (activeCount, cameraOffCount, suspiciousCount) => {
        const now = new Date();
        const label = `${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}:${now.getSeconds().toString().padStart(2, "0")}`;

        trendLabels.push(label);
        trendActive.push(activeCount);
        trendCameraOff.push(cameraOffCount);
        trendSuspicious.push(suspiciousCount);

        if (trendLabels.length > maxTrendPoints) {
            trendLabels.shift();
            trendActive.shift();
            trendCameraOff.shift();
            trendSuspicious.shift();
        }

        ensureChart();
        if (trendChart) {
            trendChart.update();
        }
    };

    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/monitoring").build();

    const refresh = async () => {
        const response = await fetch("/Admin/ActiveExams");
        if (!response.ok) {
            return;
        }

        const rows = await response.json();

        const activeCount = rows.length;
        const cameraOffCount = rows.filter((item) => !item.cameraActive).length;
        const suspiciousCount = rows.filter((item) => item.tabSwitchCount > 0).length;
        const avgScore = activeCount === 0
            ? 0
            : rows.reduce((sum, item) => sum + Number(item.score || 0), 0) / activeCount;

        if (statActive) statActive.textContent = `${activeCount}`;
        if (statCameraOff) statCameraOff.textContent = `${cameraOffCount}`;
        if (statSuspicious) statSuspicious.textContent = `${suspiciousCount}`;
        if (statAvgScore) statAvgScore.textContent = `${avgScore.toFixed(2)}`;

        const cameraOn = activeCount - cameraOffCount;
        if (cameraOnCount) cameraOnCount.textContent = `${cameraOn}`;
        if (cameraOffCountEl) cameraOffCountEl.textContent = `${cameraOffCount}`;
        if (cameraTotalCount) cameraTotalCount.textContent = `${activeCount}`;
        if (cameraOffAlertBadge) {
            cameraOffAlertBadge.textContent = `OFF: ${cameraOffCount}`;
            cameraOffAlertBadge.classList.toggle("bg-danger", cameraOffCount > 0);
            cameraOffAlertBadge.classList.toggle("bg-secondary", cameraOffCount === 0);
        }
        if (cameraLastRefresh) {
            const now = new Date();
            cameraLastRefresh.textContent = `${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}:${now.getSeconds().toString().padStart(2, "0")}`;
        }

        updateTrend(activeCount, cameraOffCount, suspiciousCount);

        tableBody.innerHTML = rows.map((item) => `
            <tr>
                <td>${item.sessionId}</td>
                <td>${item.companyName || "-"}</td>
                <td>${item.nrp}</td>
                <td>${item.employeeName}</td>
                <td>${item.vehicleName}</td>
                <td>${item.status}</td>
                <td>${item.answeredCount} / ${item.totalQuestions}</td>
                <td><span class="badge ${item.cameraActive ? "bg-success" : "bg-danger"}">${item.cameraActive ? "On" : "Off"}</span></td>
                <td>${item.tabSwitchCount}</td>
                <td>${item.score}</td>
            </tr>
        `).join("");

        cameraRowsCache = rows;
        renderCameraRealtimeTable();
        renderCameraLiveGrid();
    };

    connection.on("SessionCreated", refresh);
    connection.on("SessionStarted", refresh);
    connection.on("AnswerSaved", refresh);
    connection.on("SessionCompleted", refresh);
    connection.on("SuspiciousEvent", (payload) => {
        if (payload) {
            const createdAt = payload.createdAt || payload.createdAtUtc || new Date().toISOString();
            const dt = new Date(createdAt);
            suspiciousEvents.unshift({
                sessionId: payload.sessionId || "-",
                logType: payload.logType || "Suspicious Event",
                description: payload.description || "Aktivitas mencurigakan terdeteksi.",
                tabSwitchCount: payload.tabSwitchCount || 0,
                timeLabel: `${dt.getHours().toString().padStart(2, "0")}:${dt.getMinutes().toString().padStart(2, "0")}:${dt.getSeconds().toString().padStart(2, "0")}`
            });

            if (suspiciousEvents.length > 8) {
                suspiciousEvents.length = 8;
            }

            renderSuspiciousFeed();

            if (typeof window.showAppMessage === "function") {
                window.showAppMessage({
                    type: "error",
                    title: "Aktivitas Mencurigakan",
                    message: `${payload.description || "Peserta keluar dari layar ujian."}`
                });
            }
        }

        refresh();
    });
    connection.on("CameraStatusChanged", refresh);
    connection.on("CameraFrameUpdated", (payload) => {
        if (!payload) {
            return;
        }

        const token = payload.token || payload.Token;
        const imageDataUrl = payload.imageDataUrl || payload.ImageDataUrl;
        const capturedAt = payload.capturedAtUtc || payload.CapturedAtUtc;
        if (!token || !imageDataUrl) {
            return;
        }

        cameraFrameCache.set(token, imageDataUrl);
        if (capturedAt) {
            const dt = new Date(capturedAt);
            const stamp = `${dt.getHours().toString().padStart(2, "0")}:${dt.getMinutes().toString().padStart(2, "0")}:${dt.getSeconds().toString().padStart(2, "0")}`;
            cameraFrameTimeCache.set(token, stamp);
        }

        renderCameraLiveGrid();
    });

    connection.start()
        .then(() => connection.invoke("JoinAdminGroup"))
        .then(() => {
            renderSuspiciousFeed();
            return refresh();
        });

    setInterval(refresh, 10000);
})();
