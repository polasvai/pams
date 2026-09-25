// Cricket Auction Manager interactions
(function () {
    const layout = document.querySelector('.admin-layout');
    const toggle = document.getElementById('sidebarCollapse');
    if (!layout || !toggle) return;

    const storageKey = 'poms-sidebar-collapsed';
    const setCollapsed = (collapsed) => {
        layout.classList.toggle('sidebar-collapsed', collapsed);
        toggle.setAttribute('aria-expanded', String(!collapsed));
        toggle.setAttribute('aria-label', collapsed ? 'Expand sidebar' : 'Collapse sidebar');
        localStorage.setItem(storageKey, String(collapsed));
    };

    setCollapsed(localStorage.getItem(storageKey) === 'true');
    toggle.addEventListener('click', () => setCollapsed(!layout.classList.contains('sidebar-collapsed')));
})();

