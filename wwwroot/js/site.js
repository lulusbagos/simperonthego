(() => {
	const messageModalElement = document.getElementById("appMessageModal");
	let messageModal = null;

	window.showAppMessage = ({ type = "info", title = "Informasi Sistem", message = "" }) => {
		if (!messageModalElement || typeof bootstrap === "undefined") {
			window.alert(message || title);
			return;
		}

		if (!messageModal) {
			messageModal = new bootstrap.Modal(messageModalElement);
		}

		const modalContent = messageModalElement.querySelector(".app-message-modal");
		const kicker = document.getElementById("appMessageKicker");
		const titleNode = document.getElementById("appMessageTitle");
		const bodyNode = document.getElementById("appMessageBody");

		if (modalContent) {
			modalContent.setAttribute("data-app-message-type", type);
		}

		if (kicker) {
			kicker.textContent = type === "error"
				? "System Alert"
				: type === "success"
					? "Process Completed"
					: "System Notice";
		}

		if (titleNode) {
			titleNode.textContent = title;
		}

		if (bodyNode) {
			bodyNode.textContent = message;
		}

		messageModal.show();
	};

	if (window.__appInitialMessage && window.__appInitialMessage.message) {
		window.setTimeout(() => window.showAppMessage(window.__appInitialMessage), 120);
	}

	const initNetworkCanvas = (canvas, scale = "global") => {
		const ctx = canvas.getContext("2d");
		if (!ctx) return;

			const state = {
				width: 0,
				height: 0,
				points: [],
				frameId: 0
			};

			const targetPointCount = () => {
				const area = state.width * state.height;
				if (scale === "exam") {
					return Math.max(36, Math.min(110, Math.floor(area / 12000)));
				}
				return Math.max(72, Math.min(180, Math.floor(area / 13500)));
			};

			const randomPoint = () => ({
				x: Math.random() * state.width,
				y: Math.random() * (state.height * 0.72),
				vx: (Math.random() - 0.5) * 0.42,
				vy: (Math.random() - 0.5) * 0.42,
				r: Math.random() * 2.2 + 2.2
			});

			const resizeCanvas = () => {
				const dpr = window.devicePixelRatio || 1;
				const rect = canvas.getBoundingClientRect();
				state.width = Math.max(1, rect.width || window.innerWidth);
				state.height = Math.max(1, rect.height || window.innerHeight);
				canvas.width = Math.floor(state.width * dpr);
				canvas.height = Math.floor(state.height * dpr);
				ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
				state.points = Array.from({ length: targetPointCount() }, randomPoint);
			};

			const animate = () => {
				ctx.clearRect(0, 0, state.width, state.height);

				for (const p of state.points) {
					p.x += p.vx;
					p.y += p.vy;

					if (p.x < 0 || p.x > state.width) p.vx *= -1;
					if (p.y < 0 || p.y > state.height * 0.75) p.vy *= -1;
				}

				const maxDist = 170;
				for (let i = 0; i < state.points.length; i++) {
					const a = state.points[i];
					for (let j = i + 1; j < state.points.length; j++) {
						const b = state.points[j];
						const dx = a.x - b.x;
						const dy = a.y - b.y;
						const dist = Math.hypot(dx, dy);
						if (dist > maxDist) continue;

						const alpha = 1 - dist / maxDist;
						ctx.strokeStyle = `rgba(54, 105, 224, ${alpha * 0.32})`;
						ctx.lineWidth = 1.15;
						ctx.beginPath();
						ctx.moveTo(a.x, a.y);
						ctx.lineTo(b.x, b.y);
						ctx.stroke();
					}
				}

				for (const p of state.points) {
					ctx.fillStyle = "rgba(57, 112, 235, 0.22)";
					ctx.beginPath();
					ctx.arc(p.x, p.y, p.r * 2.35, 0, Math.PI * 2);
					ctx.fill();

					ctx.fillStyle = "rgba(49, 96, 214, 0.86)";
					ctx.beginPath();
					ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
					ctx.fill();
				}

				state.frameId = window.requestAnimationFrame(animate);
			};

			resizeCanvas();
			animate();
			window.addEventListener("resize", resizeCanvas);
			document.addEventListener("visibilitychange", () => {
				if (document.hidden && state.frameId) {
					window.cancelAnimationFrame(state.frameId);
					state.frameId = 0;
				} else if (!document.hidden && !state.frameId) {
					animate();
				}
			});
	};

	const publicNetworkCanvas = document.getElementById("publicNetworkCanvas");
	if (publicNetworkCanvas instanceof HTMLCanvasElement) {
		initNetworkCanvas(publicNetworkCanvas, "global");
	}

	const canUseCursorFx = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
	if (canUseCursorFx && document.body) {
		document.body.classList.add("cursor-fx-active");

		const dot = document.createElement("div");
		dot.className = "cursor-fx-dot";
		const ring = document.createElement("div");
		ring.className = "cursor-fx-ring";
		document.body.append(dot, ring);

		const trails = Array.from({ length: 8 }, () => {
			const item = document.createElement("div");
			item.className = "cursor-fx-trail";
			document.body.appendChild(item);
			return { el: item, x: -100, y: -100 };
		});

		let mx = -100;
		let my = -100;
		let rx = -100;
		let ry = -100;
		let raf = 0;

		const render = () => {
			rx += (mx - rx) * 0.18;
			ry += (my - ry) * 0.18;
			dot.style.transform = `translate3d(${mx - 4}px, ${my - 4}px, 0)`;
			ring.style.transform = `translate3d(${rx - 4}px, ${ry - 4}px, 0)`;

			let tx = mx;
			let ty = my;
			trails.forEach((t, idx) => {
				t.x += (tx - t.x) * (0.28 - idx * 0.018);
				t.y += (ty - t.y) * (0.28 - idx * 0.018);
				const scale = 1 - idx * 0.08;
				t.el.style.transform = `translate3d(${t.x - 5}px, ${t.y - 5}px, 0) scale(${Math.max(0.3, scale)})`;
				t.el.style.opacity = `${Math.max(0.12, 0.42 - idx * 0.04)}`;
				tx = t.x;
				ty = t.y;
			});

			raf = window.requestAnimationFrame(render);
		};

		window.addEventListener("mousemove", (ev) => {
			mx = ev.clientX;
			my = ev.clientY;
			if (!raf) {
				raf = window.requestAnimationFrame(render);
			}
		});

		document.addEventListener("mouseover", (ev) => {
			const target = ev.target;
			if (!(target instanceof HTMLElement)) return;
			const interactive = target.closest("a, button, input, textarea, select, [role='button']");
			document.body.classList.toggle("cursor-fx-hover", Boolean(interactive));
		});
	}

	const animatedItems = document.querySelectorAll("[data-animate='reveal']");
	if (animatedItems.length > 0) {
		const observer = new IntersectionObserver((entries) => {
			entries.forEach((entry) => {
				if (!entry.isIntersecting) {
					return;
				}

				const delay = Number(entry.target.getAttribute("data-animate-delay") || "0");
				window.setTimeout(() => {
					entry.target.classList.add("is-visible");
				}, delay);
				observer.unobserve(entry.target);
			});
		}, { threshold: 0.12 });

		animatedItems.forEach((item) => observer.observe(item));
	}

	const loadingForms = document.querySelectorAll("form[data-loading-form='true']");
	loadingForms.forEach((form) => {
		form.addEventListener("submit", () => {
			const submitButton = form.querySelector("button[type='submit'][data-loading-button]");
			if (!submitButton) {
				return;
			}

			submitButton.dataset.originalText = submitButton.textContent || "";
			submitButton.classList.add("is-loading");
			submitButton.disabled = true;
		});
	});

	const cameraPermissionButton = document.getElementById("cameraPermissionBtn");
	const cameraPermissionStatus = document.getElementById("cameraPermissionStatus");
	const cameraPreviewMini = document.getElementById("cameraPreviewMini");
	const networkIndicator = document.querySelector("[data-network-indicator]");
	const networkIcon = document.querySelector("[data-network-icon]");
	const networkLabel = document.querySelector("[data-network-label]");
	const networkDetail = document.querySelector("[data-network-detail]");

	if (networkIndicator && networkLabel && networkDetail) {
		const applyNetworkState = () => {
			const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
			const isOnline = navigator.onLine;
			let state = "healthy";
			let label = "Jaringan stabil";
			let detail = "Koneksi memadai untuk membuka halaman.";
			let iconClass = "fas fa-wifi";

			if (!isOnline) {
				state = "offline";
				label = "Jaringan terputus";
				detail = "Periksa koneksi internet sebelum melanjutkan.";
				iconClass = "fas fa-triangle-exclamation";
			} else if (connection) {
				const effectiveType = connection.effectiveType || "";
				const downlink = Number(connection.downlink || 0);
				const rtt = Number(connection.rtt || 0);

				if (effectiveType === "slow-2g" || effectiveType === "2g" || downlink < 0.7 || rtt > 900) {
					state = "poor";
					label = "Jaringan sangat lambat";
					detail = "Cari jaringan lain yang lebih stabil sebelum lanjut membaca atau submit.";
					iconClass = "fas fa-signal";
				} else if (effectiveType === "3g" || downlink < 1.5 || rtt > 450) {
					state = "slow";
					label = "Jaringan lambat";
					detail = "Jika proses terasa lambat, pindah ke jaringan yang lebih baik.";
					iconClass = "fas fa-wifi";
				} else {
					const downlinkText = downlink > 0 ? `${downlink.toFixed(1)} Mbps` : "stabil";
					label = "Jaringan stabil";
					detail = `Koneksi aktif, estimasi ${downlinkText}.`;
				}
			}

			networkIndicator.setAttribute("data-network-state", state);
			networkLabel.textContent = label;
			networkDetail.textContent = detail;
			if (networkIcon) {
				networkIcon.className = iconClass;
			}
		};

		applyNetworkState();
		window.addEventListener("online", applyNetworkState);
		window.addEventListener("offline", applyNetworkState);

		const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
		if (connection && typeof connection.addEventListener === "function") {
			connection.addEventListener("change", applyNetworkState);
		}
	}

	if (cameraPermissionButton && cameraPermissionStatus) {
		cameraPermissionButton.addEventListener("click", async () => {
			if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
				cameraPermissionStatus.textContent = "Browser Tidak Mendukung";
				cameraPermissionStatus.className = "badge text-bg-danger";
				return;
			}

			try {
				const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
				if (cameraPreviewMini) {
					cameraPreviewMini.srcObject = stream;
					cameraPreviewMini.hidden = false;
				}
				cameraPermissionStatus.textContent = "Kamera Aktif";
				cameraPermissionStatus.className = "badge text-bg-success";
				cameraPermissionButton.textContent = "Kamera Sudah Diizinkan";
				cameraPermissionButton.classList.remove("btn-outline-primary");
				cameraPermissionButton.classList.add("btn-success");
			} catch (_error) {
				cameraPermissionStatus.textContent = "Izin Ditolak";
				cameraPermissionStatus.className = "badge text-bg-danger";
			}
		});
	}

	const haulingRoot = document.querySelector("[data-hauling-root]");
	if (haulingRoot instanceof HTMLElement) {
		const haulingSections = Array.from(document.querySelectorAll("[data-hauling-section]"));
		const floatItems = document.querySelectorAll("[data-hauling-float]");
		const staggerItems = document.querySelectorAll("[data-hauling-stagger]");
		const animateIn = (nodeList, className) => {
			nodeList.forEach((item, index) => {
				window.setTimeout(() => {
					item.classList.add(className);
				}, 80 + (index * 70));
			});
		};

		// Mode sertifikat (tanpa section quiz/bacaan) tetap harus tampil.
		if (haulingSections.length === 0) {
			animateIn(floatItems, "is-visible");
			animateIn(staggerItems, "is-visible");
			return;
		}

		const permitReqId = haulingRoot?.getAttribute("data-hauling-reqid") || "default";
		const permitNik = haulingRoot?.getAttribute("data-hauling-nik") || "";
		const permitNama = haulingRoot?.getAttribute("data-hauling-nama") || "";
		const haulingSubmitUrl = haulingRoot?.getAttribute("data-hauling-submit-url") || "";
		const haulingProgressStorageKey = `permit-hauling-progress:${permitReqId}`;
		const progressValue = document.querySelector("[data-hauling-progress-value]");
		const progressBar = document.querySelector("[data-hauling-progress-bar]");
		const nextButton = document.querySelector("[data-hauling-next]");
		const finalStatus = document.querySelector("[data-hauling-final-status]");
		const agreementCheckbox = document.querySelector("[data-hauling-agreement]");
		const agreementButton = document.querySelector("[data-hauling-agree-button]");
		const quizHost = document.querySelector("[data-hauling-quiz]");
		const quizList = document.querySelector("[data-hauling-quiz-list]");
		const quizFeedback = document.querySelector("[data-hauling-quiz-feedback]");
		const minReadSeconds = 6;
		let completedCount = 0;
		let currentIndex = 0;
		let finalApproved = false;
		let quizPassed = false;

		const quizPool = [
			{
				id: "q1",
				question: "Saat ada rambu STOP, operator wajib berhenti berapa detik?",
				options: ["3 detik", "5 detik", "8 detik", "10 detik"],
				answer: "8 detik"
			},
			{
				id: "q2",
				question: "Batas kecepatan maksimum di area road maintenance adalah?",
				options: ["15 km/jam", "20 km/jam", "30 km/jam", "40 km/jam"],
				answer: "20 km/jam"
			},
			{
				id: "q3",
				question: "Mendahului unit lain di jalan hauling harus dilakukan dari sisi mana?",
				options: ["Sebelah kiri", "Sebelah kanan", "Bebas selama aman", "Di jalur tengah"],
				answer: "Sebelah kanan"
			},
			{
				id: "q4",
				question: "Sebelum gerak mundur, pengemudi harus membunyikan klakson berapa kali bunyi pendek?",
				options: ["1 kali", "2 kali", "3 kali", "4 kali"],
				answer: "3 kali"
			},
			{
				id: "q5",
				question: "Jika jarak pandang kurang dari 70 meter, operator harus?",
				options: ["Menyalakan hazard lalu lanjut", "Berhenti mengoperasikan unit", "Tetap jalan pelan", "Minta escort lalu lanjut"],
				answer: "Berhenti mengoperasikan unit"
			},
			{
				id: "q6",
				question: "Jika operator merasa fatigue, tindakan pertama yang benar adalah?",
				options: ["Tetap jalan sampai tujuan", "Masuk ke rest area terdekat", "Parkir di tikungan", "Mengurangi kecepatan tanpa berhenti"],
				answer: "Masuk ke rest area terdekat"
			},
			{
				id: "q7",
				question: "Saat kondisi jalan licin atau emergency, kecepatan maksimal adalah?",
				options: ["10 km/jam", "15 km/jam", "20 km/jam", "25 km/jam"],
				answer: "20 km/jam"
			},
			{
				id: "q8",
				question: "Unit kecil seperti LV, Bus, atau Elf tidak boleh diparkir dalam radius berapa meter dari alat berat?",
				options: ["10 meter", "15 meter", "25 meter", "30 meter"],
				answer: "25 meter"
			},
			{
				id: "q9",
				question: "Saat simpang warga memiliki portal aktif, security memprioritaskan unit apa tetap jalan terlebih dahulu?",
				options: ["LV", "Trailer", "Bus", "Fuel Truck"],
				answer: "Trailer"
			},
			{
				id: "q10",
				question: "Jika unit breakdown di jalan lurus, batas maksimal waktu evakuasi adalah?",
				options: ["2 jam", "4 jam", "6 jam", "8 jam"],
				answer: "6 jam"
			}
		];

		const buildSeed = (value) => {
			let hash = 2166136261;
			for (let i = 0; i < value.length; i += 1) {
				hash ^= value.charCodeAt(i);
				hash += (hash << 1) + (hash << 4) + (hash << 7) + (hash << 8) + (hash << 24);
			}
			return hash >>> 0;
		};

		const mulberry32 = (seed) => {
			let t = seed >>> 0;
			return () => {
				t += 0x6D2B79F5;
				let r = Math.imul(t ^ (t >>> 15), t | 1);
				r ^= r + Math.imul(r ^ (r >>> 7), r | 61);
				return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
			};
		};

		const shuffleArray = (items, randomFn) => {
			const cloned = [...items];
			for (let i = cloned.length - 1; i > 0; i -= 1) {
				const j = Math.floor(randomFn() * (i + 1));
				[cloned[i], cloned[j]] = [cloned[j], cloned[i]];
			}
			return cloned;
		};

		const buildParticipantQuiz = () => {
			const identity = `${permitReqId}|${permitNik}|${permitNama}`.toLowerCase();
			const randomFn = mulberry32(buildSeed(identity));
			const pickedQuestions = shuffleArray(quizPool, randomFn).slice(0, 2);
			return pickedQuestions.map((item) => ({
				...item,
				options: shuffleArray(item.options, randomFn)
			}));
		};

		const selectedQuiz = buildParticipantQuiz();

		const renderQuiz = () => {
			if (!quizList || selectedQuiz.length === 0) {
				return;
			}

			quizList.innerHTML = selectedQuiz.map((item, questionIndex) => `
				<div class="hauling-quiz-item" data-hauling-quiz-item data-question-id="${item.id}">
					<div class="hauling-quiz-question">${questionIndex + 1}. ${item.question}</div>
					<div class="hauling-quiz-options">
						${item.options.map((option, optionIndex) => `
							<label class="hauling-quiz-option">
								<input type="radio" name="haulingQuiz_${item.id}" value="${option.replace(/"/g, "&quot;")}" data-hauling-quiz-input>
								<span>${String.fromCharCode(65 + optionIndex)}. ${option}</span>
							</label>
						`).join("")}
					</div>
				</div>
			`).join("");
		};

		const evaluateQuiz = () => {
			if (!quizList || !quizFeedback) {
				return;
			}

			const answers = selectedQuiz.map((item) => {
				const checked = quizList.querySelector(`input[name="haulingQuiz_${item.id}"]:checked`);
				return checked?.value || "";
			});

			const allAnswered = answers.every((value) => value);
			if (!allAnswered) {
				quizPassed = false;
				quizFeedback.textContent = "Jawab kedua pertanyaan untuk membuka persetujuan akhir.";
				quizFeedback.classList.remove("is-pass", "is-fail");
				syncHaulingState();
				return;
			}

			const allCorrect = answers.every((value, index) => value === selectedQuiz[index].answer);
			quizPassed = allCorrect;
			quizFeedback.textContent = allCorrect
				? "Jawaban benar. Persyaratan quiz sudah terpenuhi."
				: "Masih ada jawaban yang belum tepat. Periksa kembali materi lalu jawab ulang.";
			quizFeedback.classList.toggle("is-pass", allCorrect);
			quizFeedback.classList.toggle("is-fail", !allCorrect);
			syncHaulingState();
		};

		const saveHaulingProgress = () => {
			try {
				const payload = {
					completedSections: haulingSections
						.filter((section) => section.dataset.completed === "true")
						.map((section) => Number(section.dataset.sectionIndex || "0")),
					agreementChecked: Boolean(agreementCheckbox?.checked),
					quizAnswers: selectedQuiz.map((item) => {
						const checked = quizList?.querySelector(`input[name="haulingQuiz_${item.id}"]:checked`);
						return {
							id: item.id,
							answer: checked?.value || ""
						};
					}),
					quizPassed,
					finalApproved,
					savedAt: new Date().toISOString()
				};

				window.localStorage.setItem(haulingProgressStorageKey, JSON.stringify(payload));
			} catch (_error) {
				// Ignore storage failures silently; page should still work without persistence.
			}
		};

		const loadHaulingProgress = () => {
			try {
				const raw = window.localStorage.getItem(haulingProgressStorageKey);
				if (!raw) {
					return null;
				}

				return JSON.parse(raw);
			} catch (_error) {
				return null;
			}
		};

		const updateSectionReadiness = (section) => {
			const completeButton = section.querySelector("[data-hauling-complete]");
			const readHint = section.querySelector("[data-hauling-read-hint]");
			if (!completeButton) {
				return;
			}

			const isCurrent = Number(section.dataset.sectionIndex || "0") - 1 === currentIndex;
			const hasReachedBottom = section.dataset.reachedBottom === "true";
			const readElapsed = Number(section.dataset.readElapsed || "0");
			const isReady = isCurrent && hasReachedBottom && readElapsed >= minReadSeconds && section.dataset.completed !== "true";

			completeButton.disabled = !isReady;

			if (!readHint) {
				return;
			}

			if (section.dataset.completed === "true") {
				readHint.textContent = "Bagian ini sudah selesai dibaca dan dikonfirmasi.";
				readHint.classList.add("is-ready");
				section.classList.add("is-confirmed");
				return;
			}

			section.classList.remove("is-confirmed");

			if (!isCurrent) {
				readHint.textContent = "Selesaikan bagian sebelumnya terlebih dahulu.";
				readHint.classList.remove("is-ready");
				return;
			}

			if (!hasReachedBottom) {
				readHint.textContent = "Scroll perlahan sampai bagian paling bawah untuk membuka konfirmasi.";
				readHint.classList.remove("is-ready");
				return;
			}

			if (readElapsed < minReadSeconds) {
				readHint.textContent = `Bagian sudah sampai bawah. Lanjutkan membaca ${minReadSeconds - readElapsed} detik lagi.`;
				readHint.classList.remove("is-ready");
				return;
			}

			readHint.textContent = "Bacaan bagian ini sudah memenuhi durasi minimum. Tombol konfirmasi aktif.";
			readHint.classList.add("is-ready");
		};

		const syncHaulingState = () => {
			haulingSections.forEach((section, index) => {
				section.classList.toggle("is-current", index === currentIndex && !section.dataset.completed);
				section.classList.toggle("is-complete", section.dataset.completed === "true");
				if (index === currentIndex && section.dataset.completed !== "true") {
					section.__startReadTimer?.();
					section.__checkScrollReady?.();
				}
				updateSectionReadiness(section);
			});

			if (progressValue) {
				progressValue.textContent = String(completedCount);
			}

			if (progressBar) {
				const percent = (completedCount / haulingSections.length) * 100;
				progressBar.style.width = `${percent}%`;
				progressBar.classList.remove("is-pulsing");
				void progressBar.offsetWidth;
				progressBar.classList.add("is-pulsing");
			}

			if (nextButton) {
				const nextIncomplete = haulingSections.find((section) => section.dataset.completed !== "true");
				nextButton.disabled = !nextIncomplete;
			}

			if (finalStatus) {
				if (completedCount === haulingSections.length) {
					finalStatus.textContent = quizPassed
						? "Semua bagian dan quiz verifikasi sudah selesai. Centang pernyataan tanggung jawab untuk membuka persetujuan akhir."
						: "Semua bagian sudah dibaca. Jawab dua pertanyaan verifikasi dengan benar lalu centang pernyataan tanggung jawab.";
					finalStatus.classList.add("is-ready");
					finalStatus.classList.add("is-celebrating");
				} else {
					finalStatus.textContent = `Masih ada ${haulingSections.length - completedCount} bagian yang belum ditandai selesai.`;
					finalStatus.classList.remove("is-ready");
					finalStatus.classList.remove("is-celebrating");
				}
			}

			if (agreementCheckbox) {
				const canAgree = completedCount === haulingSections.length;
				agreementCheckbox.disabled = !canAgree;
				if (!canAgree) {
					agreementCheckbox.checked = false;
				}
			}

			if (agreementButton) {
				const canApprove = completedCount === haulingSections.length && quizPassed && agreementCheckbox?.checked;
				agreementButton.disabled = finalApproved || !canApprove;
			}

			saveHaulingProgress();
		};

		haulingSections.forEach((section, index) => {
			const completeButton = section.querySelector("[data-hauling-complete]");
			const scrollBox = section.querySelector("[data-hauling-scrollbox]");
			if (!completeButton) {
				return;
			}

			section.dataset.readElapsed = "0";
			section.dataset.reachedBottom = "false";

			if (scrollBox) {
				let readTimerStarted = false;
				let elapsedSeconds = 0;
				let readTimer = null;
				const checkScrollReady = () => {
					const hasScrollableOverflow = scrollBox.scrollHeight > scrollBox.clientHeight + 8;
					if (!hasScrollableOverflow) {
						section.dataset.reachedBottom = "true";
						updateSectionReadiness(section);
						return;
					}

					const reachedBottom = scrollBox.scrollTop + scrollBox.clientHeight >= scrollBox.scrollHeight - 8;
					if (reachedBottom) {
						section.dataset.reachedBottom = "true";
					}
					updateSectionReadiness(section);
				};

				const markInteraction = () => {
					if (readTimerStarted || section.dataset.completed === "true") {
						return;
					}

					readTimerStarted = true;
					readTimer = window.setInterval(() => {
						if (section.dataset.completed === "true") {
							window.clearInterval(readTimer);
							return;
						}

						elapsedSeconds += 1;
						section.dataset.readElapsed = String(elapsedSeconds);
						updateSectionReadiness(section);
					}, 1000);
				};

				section.__startReadTimer = markInteraction;
				section.__checkScrollReady = checkScrollReady;

				scrollBox.addEventListener("scroll", () => {
					markInteraction();
					checkScrollReady();
				});

				scrollBox.addEventListener("wheel", markInteraction, { passive: true });
				scrollBox.addEventListener("touchmove", markInteraction, { passive: true });
				scrollBox.addEventListener("mouseenter", markInteraction, { passive: true });
				window.setTimeout(checkScrollReady, 120);
			}

			completeButton.addEventListener("click", () => {
				if (section.dataset.completed === "true") {
					return;
				}

				section.dataset.completed = "true";
				completedCount += 1;
				completeButton.disabled = true;
				completeButton.textContent = "Bagian ini sudah ditinjau";
				section.classList.add("is-confirmed");

				const nextIndex = haulingSections.findIndex((item) => item.dataset.completed !== "true");
				currentIndex = nextIndex === -1 ? haulingSections.length - 1 : nextIndex;

				syncHaulingState();
			});
		});

		const restoredState = loadHaulingProgress();
		renderQuiz();

		if (quizList) {
			quizList.addEventListener("change", (event) => {
				if (!(event.target instanceof HTMLInputElement) || event.target.type !== "radio") {
					return;
				}

				evaluateQuiz();
				saveHaulingProgress();
			});
		}

		if (restoredState && Array.isArray(restoredState.completedSections)) {
			const completedSet = new Set(restoredState.completedSections.map((value) => Number(value)));
			haulingSections.forEach((section) => {
				const sectionIndex = Number(section.dataset.sectionIndex || "0");
				if (!completedSet.has(sectionIndex)) {
					return;
				}

				const completeButton = section.querySelector("[data-hauling-complete]");
				section.dataset.completed = "true";
				section.dataset.reachedBottom = "true";
				section.dataset.readElapsed = String(minReadSeconds);
				section.classList.add("is-confirmed");
				if (completeButton) {
					completeButton.disabled = true;
					completeButton.textContent = "Bagian ini sudah ditinjau";
				}
			});

			completedCount = haulingSections.filter((section) => section.dataset.completed === "true").length;
			const nextIndex = haulingSections.findIndex((section) => section.dataset.completed !== "true");
			currentIndex = nextIndex === -1 ? haulingSections.length - 1 : nextIndex;

			if (agreementCheckbox && restoredState.agreementChecked && completedCount === haulingSections.length) {
				agreementCheckbox.checked = true;
			}

			if (quizList && Array.isArray(restoredState.quizAnswers)) {
				restoredState.quizAnswers.forEach((item) => {
					if (!item?.id || !item.answer) {
						return;
					}

					const escapedAnswer = typeof CSS !== "undefined" && typeof CSS.escape === "function"
						? CSS.escape(item.answer)
						: item.answer.replace(/["\\]/g, "\\$&");
					const target = quizList.querySelector(`input[name="haulingQuiz_${item.id}"][value="${escapedAnswer}"]`);
					if (target instanceof HTMLInputElement) {
						target.checked = true;
					}
				});
				evaluateQuiz();
			}

			if (agreementButton && finalStatus && restoredState.finalApproved) {
				finalApproved = true;
				agreementCheckbox?.setAttribute("disabled", "disabled");
				agreementButton.textContent = "Persetujuan tercatat";
				agreementButton.disabled = true;
				agreementButton.classList.add("is-locked");
				finalStatus.textContent = "Persetujuan akhir telah diberikan. Karyawan dinyatakan telah membaca, memahami, dan menyetujui seluruh ketentuan jalan hauling.";
				finalStatus.classList.add("is-ready", "is-celebrating");
			}
		}

		if (nextButton) {
			nextButton.addEventListener("click", () => {
				const targetSection = haulingSections.find((section) => section.dataset.completed !== "true");
				if (!targetSection) {
					return;
				}

				targetSection.scrollIntoView({ behavior: "smooth", block: "start" });
			});
		}

		if (agreementCheckbox && finalStatus) {
			agreementCheckbox.addEventListener("change", () => {
				if (agreementCheckbox.checked) {
					if (!quizPassed) {
						finalStatus.textContent = "Pernyataan tanggung jawab dicentang, tetapi quiz verifikasi harus dijawab benar terlebih dahulu.";
						finalStatus.classList.remove("is-celebrating");
					} else {
						finalStatus.textContent = "Semua bagian selesai dibaca, dipahami, dan pernyataan tanggung jawab telah disetujui.";
						finalStatus.classList.add("is-celebrating");
					}
				} else if (completedCount === haulingSections.length) {
					finalStatus.textContent = quizPassed
						? "Semua bagian sudah dibaca. Centang pernyataan tanggung jawab untuk menyatakan memahami seluruh ketentuan."
						: "Semua bagian sudah dibaca. Selesaikan quiz verifikasi lalu centang pernyataan tanggung jawab.";
					finalStatus.classList.remove("is-celebrating");
				}
				syncHaulingState();
				saveHaulingProgress();
			});
		}

		if (agreementButton && finalStatus) {
			agreementButton.addEventListener("click", async () => {
				if (agreementButton.disabled) {
					return;
				}

				const defaultText = agreementButton.textContent;
				agreementButton.disabled = true;
				agreementButton.textContent = "Menyimpan...";

				try {
					const response = await fetch(haulingSubmitUrl, {
						method: "POST",
						headers: {
							"X-Requested-With": "XMLHttpRequest"
						}
					});

					const payload = await response.json().catch(() => null);
					if (!response.ok || !payload?.success) {
						throw new Error(payload?.message || "Simpan persetujuan gagal.");
					}

					agreementButton.textContent = "Persetujuan tercatat";
					agreementButton.classList.add("is-locked");
					finalApproved = true;
					if (agreementCheckbox) {
						agreementCheckbox.disabled = true;
					}

					const certificateUrl = typeof payload.certificateUrl === "string" ? payload.certificateUrl : "";
					finalStatus.textContent = certificateUrl
						? `Persetujuan akhir telah diberikan dan tersimpan di database. Sertifikat: ${certificateUrl}`
						: "Persetujuan akhir telah diberikan dan tersimpan di database.";
					finalStatus.classList.add("is-ready", "is-celebrating");
					saveHaulingProgress();

					if (certificateUrl) {
						window.setTimeout(() => {
							window.location.href = certificateUrl;
						}, 700);
					}
				} catch (error) {
					agreementButton.disabled = false;
					agreementButton.textContent = defaultText || "Saya menyetujuinya";
					finalStatus.textContent = error instanceof Error
						? error.message
						: "Gagal menyimpan persetujuan ketentuan hauling.";
					finalStatus.classList.remove("is-ready", "is-celebrating");
				}
			});
		}

		animateIn(floatItems, "is-visible");
		animateIn(staggerItems, "is-visible");
		syncHaulingState();
	}
})();
