// Filter dropdown
const filterDropdown = document.querySelector('.filter');
const filterHeader = filterDropdown.querySelector('.dropdown-header');
const dropdownOverlay = filterDropdown.querySelector('.dropdown-overlay');

filterHeader.addEventListener('click', () => {
    filterDropdown.classList.toggle('show');
});

// Close when clicking outside the panel
dropdownOverlay.addEventListener('click', (e) => {
    if (e.target === dropdownOverlay) {
        filterDropdown.classList.remove('show');
    }
});

// Optional: Close modal with ESC key
window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        filterDropdown.classList.remove('show');
    }
});

// Carousel auto-scroll
const track = document.getElementById("carouselTrack");
let scrollAmount = 0;

setInterval(() => {
    scrollAmount += 1;
    track.style.transform = `translateX(-${scrollAmount}px)`;

    if (scrollAmount > track.scrollWidth / 2) {
        scrollAmount = 0;
    }
}, 20);