document.addEventListener("DOMContentLoaded", () => {
    const toggleButton = document.querySelector("[data-sidebar-toggle]");
    const body = document.body;

    if (toggleButton) {
        toggleButton.addEventListener("click", () => {
            body.classList.toggle("sidebar-open");
        });
    }

    document.querySelectorAll("[data-dismiss-alert]").forEach((button) => {
        button.addEventListener("click", () => {
            const alert = button.closest("[data-dismissable]");
            if (alert) {
                alert.remove();
            }
        });
    });
});
