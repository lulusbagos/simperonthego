(() => {
    const storageKey = "simper_adminlte_theme";
    const allowedThemes = [
        "emerald-command",
        "navy-gold",
        "jade-corporate",
        "slate-amber",
        "sage-professional",
        "rose-lilac",
        "pine-night",
        "forest-slate",
        "forest-night"
    ];
    const defaultTheme = "forest-slate";
    const buttonSelector = "[data-admin-theme-btn]";
    const themeLabels = {
        "emerald-command": "Green Blue",
        "navy-gold": "Navy Gold",
        "slate-amber": "Slate Amber",
        "forest-slate": "Steel Blue",
        "jade-corporate": "Charcoal Teal",
        "sage-professional": "Purple Pink",
        "rose-lilac": "Rose Lilac",
        "pine-night": "Magenta Night",
        "forest-night": "Forest Night"
    };

    const readStoredTheme = () => {
        try {
            return localStorage.getItem(storageKey) || defaultTheme;
        } catch {
            return defaultTheme;
        }
    };

    const writeStoredTheme = (theme) => {
        try {
            localStorage.setItem(storageKey, theme);
        } catch {
            // Ignore storage failures and still apply the theme for the current session.
        }
    };

    const applyTheme = (theme) => {
        const body = document.body;
        if (!body || !body.classList.contains("admin-cms")) {
            return;
        }

        const resolvedTheme = allowedThemes.includes(theme) ? theme : defaultTheme;
        body.setAttribute("data-admin-theme", resolvedTheme);

        document.querySelectorAll(buttonSelector).forEach((button) => {
            const isActive = button.getAttribute("data-admin-theme-btn") === resolvedTheme;
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
        });

        const themeLabel = document.getElementById("adminThemeLabel");
        if (themeLabel) {
            themeLabel.textContent = themeLabels[resolvedTheme] || themeLabels[defaultTheme];
        }

        const themeSelect = document.getElementById("adminThemeSelect");
        if (themeSelect instanceof HTMLSelectElement) {
            themeSelect.value = resolvedTheme;
        }
    };

    window.setAdminTheme = (theme) => {
        const resolvedTheme = allowedThemes.includes(theme) ? theme : defaultTheme;
        writeStoredTheme(resolvedTheme);
        applyTheme(resolvedTheme);
    };

    const initThemeControls = () => {
        applyTheme(readStoredTheme());

        const handleThemeSelection = (button) => {
            const selectedTheme = button.getAttribute("data-admin-theme-btn");
            if (!selectedTheme) {
                return;
            }

            window.setAdminTheme(selectedTheme);
        };

        document.querySelectorAll(buttonSelector).forEach((button) => {
            button.addEventListener("click", () => handleThemeSelection(button));
        });

        const themeSelect = document.getElementById("adminThemeSelect");
        if (themeSelect instanceof HTMLSelectElement) {
            themeSelect.addEventListener("change", () => {
                const selectedTheme = themeSelect.value;
                if (allowedThemes.includes(selectedTheme)) {
                    window.setAdminTheme(selectedTheme);
                    return;
                }

                writeStoredTheme(defaultTheme);
                applyTheme(defaultTheme);
            });
        }

        document.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const button = target.closest(buttonSelector);
            if (!(button instanceof HTMLElement)) {
                return;
            }

            handleThemeSelection(button);
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initThemeControls, { once: true });
    } else {
        initThemeControls();
    }
})();
