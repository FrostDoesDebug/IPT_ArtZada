document.addEventListener("DOMContentLoaded", () => {

    // =========================
    // FILTER DROPDOWN
    // =========================
    const filterDropdown = document.querySelector('.filter');

    if (filterDropdown) {
        const filterHeader = filterDropdown.querySelector('.dropdown-header');
        const dropdownOverlay = filterDropdown.querySelector('.dropdown-overlay');

        if (filterHeader) {
            filterHeader.addEventListener('click', () => {
                filterDropdown.classList.toggle('show');
            });
        }

        if (dropdownOverlay) {
            dropdownOverlay.addEventListener('click', (e) => {
                if (e.target === dropdownOverlay) {
                    filterDropdown.classList.remove('show');
                }
            });
        }

        window.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                filterDropdown.classList.remove('show');
            }
        });
    }

    // =========================
    // CAROUSEL
    // =========================
    const track = document.getElementById("carouselTrack");

    if (track) {
        let scrollAmount = 0;

        setInterval(() => {
            scrollAmount += 1;
            track.style.transform = `translateX(-${scrollAmount}px)`;

            if (scrollAmount > track.scrollWidth / 2) {
                scrollAmount = 0;
            }
        }, 20);
    }

    // =========================
    // NAVIGATION ARROWS
    // =========================
    const navButtons = document.querySelectorAll(".nav-btn");

    if (navButtons.length >= 2) {

        navButtons[0].addEventListener("click", () => {
            console.log("Back clicked");

            if (window.history.length > 1) {
                window.history.back();
            } else {
                window.location.href = "/Home/Index"; // fallback
            }
        });

        navButtons[1].addEventListener("click", () => {
            console.log("Forward clicked");
            window.history.forward();
        });

    } else {
        console.log("Nav buttons not found");
    }

});

// =========================
// SCROLL NAV HIGHLIGHT
// =========================
document.addEventListener("DOMContentLoaded", () => {

    const homeNav = document.getElementById("navHome");
    const shopNav = document.getElementById("navShop");
    const hotSales = document.getElementById("hotSales");

    if (!homeNav || !shopNav || !hotSales) return;

    window.addEventListener("scroll", () => {

        const hotSalesTop = hotSales.getBoundingClientRect().top;

        // If Hot Sales is visible (scrolled down)
        if (hotSalesTop <= 100) {
            homeNav.classList.remove("active");
            shopNav.classList.add("active");
        }
        else {
            // Back to top
            shopNav.classList.remove("active");
            homeNav.classList.add("active");
        }

    });

});
// =========================
// SMOOTH SCROLL TO HOT SALES
// =========================
document.addEventListener("DOMContentLoaded", () => {

    const shopNav = document.getElementById("navShop");
    const hotSales = document.getElementById("hotSales");

    if (shopNav && hotSales) {
        shopNav.addEventListener("click", (e) => {
            e.preventDefault();

            const offset = 120; // header + navbar height
            const top = hotSales.getBoundingClientRect().top + window.scrollY - offset;

            window.scrollTo({
                top: top,
                behavior: "smooth"
            });
        });
    }

});

