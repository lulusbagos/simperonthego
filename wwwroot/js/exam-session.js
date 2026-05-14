(() => {
    const init = () => {
        const config = window.examConfig;
        if (!config || !config.token) {
            return;
        }

        let currentOrder = 1;
        let totalQuestions = 1;
        let tabSwitchCount = 0;
        let mediaStream;
        let frameIntervalId;
        let answerMapHydrated = false;
        const answeredOrders = new Set();

        const token = config.token;
        const questionContainer = document.getElementById("questionContainer");
        const prevBtn = document.getElementById("prevBtn");
        const nextBtn = document.getElementById("nextBtn");
        const mobilePrevBtn = document.getElementById("mobilePrevBtn");
        const mobileNextBtn = document.getElementById("mobileNextBtn");
        const questionProgressText = document.getElementById("questionProgressText");
        const questionProgressTextMobile = document.getElementById("questionProgressTextMobile");
        const questionNavGrid = document.getElementById("questionNavGrid");
        const answeredCountLabel = document.getElementById("answeredCountLabel");
        const totalCountLabel = document.getElementById("totalCountLabel");
        const answeredProgressBar = document.getElementById("answeredProgressBar");
        const tabSwitchCountLabel = document.getElementById("tabSwitchCount");
        const cameraStatusLabel = document.getElementById("cameraStatus");
        const cameraStatusLabelMobile = document.getElementById("cameraStatusMobile");
        const cameraPreview = document.getElementById("cameraPreview");
        const retryCameraBtn = document.getElementById("retryCameraBtn");
        const examSecurityOverlay = document.getElementById("examSecurityOverlay");
        const resumeExamBtn = document.getElementById("resumeExamBtn");
        const frameCanvas = document.createElement("canvas");
        const frameContext = frameCanvas.getContext("2d");
        let focusViolationLocked = false;

        if (!questionContainer || !prevBtn || !nextBtn || !questionNavGrid || !cameraStatusLabel || !cameraPreview) {
            return;
        }

        const connection = typeof signalR !== "undefined"
            ? new signalR.HubConnectionBuilder().withUrl("/hubs/monitoring").build()
            : null;

        connection?.start().then(() => connection.invoke("JoinSessionGroup", token)).catch(() => {
        });

        const sendCameraFrame = async () => {
        if (!mediaStream || !frameContext || !cameraPreview || cameraPreview.videoWidth === 0 || cameraPreview.videoHeight === 0) {
            return;
        }

        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        const targetWidth = 320;
        const ratio = cameraPreview.videoHeight / cameraPreview.videoWidth;
        const targetHeight = Math.max(180, Math.round(targetWidth * ratio));

        frameCanvas.width = targetWidth;
        frameCanvas.height = targetHeight;
        frameContext.drawImage(cameraPreview, 0, 0, targetWidth, targetHeight);

        const imageDataUrl = frameCanvas.toDataURL("image/jpeg", 0.55);
            await connection.invoke("SendCameraFrame", token, imageDataUrl);
        };

        const updateCountdown = () => {
            const remaining = new Date(config.endTimeUtc).getTime() - new Date().getTime();
            if (remaining <= 0) {
                document.getElementById("submitForm")?.submit();
            }
        };

    const renderQuestionNavigator = () => {
        if (!questionNavGrid) {
            return;
        }

        const buttons = [];
        for (let i = 1; i <= totalQuestions; i++) {
            const states = ["qnav-btn"];
            if (i === currentOrder) {
                states.push("current");
            } else if (answeredOrders.has(i)) {
                states.push("answered");
            } else {
                states.push("pending");
            }

            buttons.push(`<button type="button" class="${states.join(" ")}" data-jump-order="${i}">${i}</button>`);
        }

        questionNavGrid.innerHTML = buttons.join("");
        questionNavGrid.querySelectorAll("[data-jump-order]").forEach((button) => {
            button.addEventListener("click", () => {
                const targetOrder = Number(button.getAttribute("data-jump-order"));
                if (!targetOrder || targetOrder === currentOrder) {
                    return;
                }

                currentOrder = targetOrder;
                loadQuestion();
            });
        });

        const answeredCount = answeredOrders.size;
        if (answeredCountLabel) {
            answeredCountLabel.textContent = `${answeredCount}`;
        }
        if (totalCountLabel) {
            totalCountLabel.textContent = `${totalQuestions}`;
        }
        if (answeredProgressBar) {
            const percent = totalQuestions === 0 ? 0 : Math.min(100, Math.round((answeredCount / totalQuestions) * 100));
            answeredProgressBar.style.width = `${percent}%`;
        }
    };

    const hydrateAnsweredMap = async () => {
        if (answerMapHydrated || totalQuestions <= 1) {
            return;
        }

        answerMapHydrated = true;
        for (let i = 1; i <= totalQuestions; i++) {
            if (i === currentOrder) {
                continue;
            }

            try {
                const response = await fetch(`/Exam/Question?token=${encodeURIComponent(token)}&order=${i}`);
                if (!response.ok) {
                    continue;
                }

                const data = await response.json();
                if (data.selectedAnswer) {
                    answeredOrders.add(i);
                }
            } catch (_error) {
            }
        }

        renderQuestionNavigator();
    };

        const logEvent = async (logType, description) => {
            await fetch("/Exam/LogEvent", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ token, logType, description })
            });
        };

        const showSecurityOverlay = () => {
            if (examSecurityOverlay) {
                examSecurityOverlay.hidden = false;
            }
        };

        const hideSecurityOverlay = () => {
            if (examSecurityOverlay) {
                examSecurityOverlay.hidden = true;
            }
        };

        const handleFocusViolation = async (logType, description) => {
            if (focusViolationLocked) {
                return;
            }

            focusViolationLocked = true;
            tabSwitchCount += 1;
            if (tabSwitchCountLabel) {
                tabSwitchCountLabel.textContent = tabSwitchCount.toString();
            }

            showSecurityOverlay();
            await logEvent(logType, description);

            window.setTimeout(() => {
                focusViolationLocked = false;
            }, 1200);
        };

    const updateCameraStatus = async (isActive) => {
        cameraStatusLabel.textContent = isActive ? "On" : "Off";
        cameraStatusLabel.className = `badge ${isActive ? "bg-success" : "bg-danger"}`;
        if (cameraStatusLabelMobile) {
            cameraStatusLabelMobile.textContent = isActive ? "On" : "Off";
            cameraStatusLabelMobile.className = isActive ? "text-success" : "text-danger";
        }

        await fetch("/Exam/CameraStatus", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ token, isActive })
        });
    };

        const loadQuestion = async () => {
            const response = await fetch(`/Exam/Question?token=${encodeURIComponent(token)}&order=${currentOrder}`);
            if (!response.ok) {
                questionContainer.innerHTML = `
                    <div class="exam-empty-state">
                        <div class="exam-empty-icon"><i class="fas fa-file-circle-question"></i></div>
                        <h6 class="mb-2">Soal belum tersedia</h6>
                        <p class="mb-0">Sesi ini belum memiliki soal aktif atau token sudah tidak valid. Hubungi admin untuk generate sesi baru.</p>
                    </div>`;
                return;
            }

            const data = await response.json();
            totalQuestions = data.totalQuestions;

        const mediaBlocks = [];
        if (data.imageUrl) {
            mediaBlocks.push(`<img src="${data.imageUrl}" alt="Question image" class="img-fluid rounded border mb-3" />`);
        }
        if (data.videoUrl) {
            mediaBlocks.push(`<video src="${data.videoUrl}" controls class="w-100 rounded border mb-3"></video>`);
        }

        const optionsHtml = data.options.map((opt) => {
            const checked = data.selectedAnswer === opt.key ? "checked" : "";
            const selectedClass = checked ? "option-card selected" : "option-card";
            return `
                <label class="${selectedClass}" for="opt_${opt.key}">
                    <input class="form-check-input d-none" type="radio" name="answer" value="${opt.key}" id="opt_${opt.key}" ${checked} />
                    <span class="option-key">${opt.key}</span>
                    <span class="option-text">${opt.text}</span>
                </label>
            `;
        }).join("");

            questionContainer.innerHTML = `
            <div class="mb-3 text-muted fw-semibold">Question ${data.order} of ${data.totalQuestions}</div>
            <h6 class="mb-3 exam-question-title">${data.questionText}</h6>
            ${mediaBlocks.join("")}
            <div class="option-list">${optionsHtml}</div>
        `;

        if (questionProgressText) {
            questionProgressText.textContent = `Question ${data.order}/${data.totalQuestions}`;
        }
        if (questionProgressTextMobile) {
            questionProgressTextMobile.textContent = `${data.order}/${data.totalQuestions}`;
        }

        if (data.selectedAnswer) {
            answeredOrders.add(data.order);
        }

        renderQuestionNavigator();
        hydrateAnsweredMap().catch(() => {
        });

            document.querySelectorAll("input[name='answer']").forEach((input) => {
            input.addEventListener("change", async () => {
                document.querySelectorAll(".option-card").forEach((card) => card.classList.remove("selected"));
                input.closest(".option-card")?.classList.add("selected");

                await fetch("/Exam/SaveAnswer", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ token, questionId: data.questionId, selectedAnswer: input.value })
                });

                answeredOrders.add(data.order);
                renderQuestionNavigator();
            });
        });

            prevBtn.disabled = currentOrder <= 1;
            nextBtn.disabled = currentOrder >= totalQuestions;
            if (mobilePrevBtn) {
                mobilePrevBtn.disabled = currentOrder <= 1;
            }
            if (mobileNextBtn) {
                mobileNextBtn.disabled = currentOrder >= totalQuestions;
            }
        };

    prevBtn.addEventListener("click", () => {
        if (currentOrder > 1) {
            currentOrder -= 1;
            loadQuestion();
        }
    });

    nextBtn.addEventListener("click", () => {
        if (currentOrder < totalQuestions) {
            currentOrder += 1;
            loadQuestion();
        }
    });

    mobilePrevBtn?.addEventListener("click", () => {
        prevBtn.click();
    });

    mobileNextBtn?.addEventListener("click", () => {
        nextBtn.click();
    });

        document.addEventListener("visibilitychange", async () => {
        if (document.hidden) {
            await handleFocusViolation("tab_switch", "Participant leaving tab.");
        } else {
            hideSecurityOverlay();
            await logEvent("reconnect", "Participant returning to tab.");
        }
        });

        window.addEventListener("blur", async () => {
            await handleFocusViolation("window_blur", "Participant moved focus away from exam window.");
        });

        if (resumeExamBtn) {
            resumeExamBtn.addEventListener("click", () => {
                hideSecurityOverlay();
                window.focus();
            });
        }

    document.addEventListener("contextmenu", (event) => event.preventDefault());
    document.addEventListener("copy", (event) => event.preventDefault());

        const requestCameraAccess = async () => {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            await updateCameraStatus(false);
            return;
        }

        if (frameIntervalId) {
            clearInterval(frameIntervalId);
            frameIntervalId = null;
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: "user",
                    width: { ideal: 960 },
                    height: { ideal: 540 }
                },
                audio: false
            });
            mediaStream = stream;
            cameraPreview.srcObject = stream;
            await updateCameraStatus(true);

            frameIntervalId = window.setInterval(() => {
                sendCameraFrame().catch(() => {
                });
            }, 3000);

            sendCameraFrame().catch(() => {
            });
        } catch (_error) {
            await updateCameraStatus(false);
            await logEvent("camera_off", "Camera permission denied.");
        }
        };

        if (retryCameraBtn) {
            retryCameraBtn.addEventListener("click", async () => {
            if (mediaStream) {
                mediaStream.getTracks().forEach((track) => track.stop());
                mediaStream = null;
            }

            await requestCameraAccess();
            });
        }

        requestCameraAccess();

        window.addEventListener("beforeunload", async () => {
        if (frameIntervalId) {
            clearInterval(frameIntervalId);
        }

        if (mediaStream) {
            mediaStream.getTracks().forEach((track) => track.stop());
        }

            await logEvent("reconnect", "Browser refresh or close detected.");
        });

        window.addEventListener("offline", async () => {
        await logEvent("connection_lost", "Network connection lost.");
        });

        window.addEventListener("online", async () => {
        await logEvent("reconnect", "Network connection restored.");
        });

        setInterval(updateCountdown, 1000);
        updateCountdown();
        loadQuestion();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init, { once: true });
    } else {
        init();
    }
})();
