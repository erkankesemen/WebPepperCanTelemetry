document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const orgFilter = document.getElementById('organizationFilter');
    const statusFilter = document.getElementById('statusFilter');
    const tableTitle = document.getElementById('tableTitle');
    const tableBody = document.querySelector('.vehicle-table tbody');

    function filterVehicles() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedOrg = orgFilter.value;
        const selectedStatus = statusFilter.value;
        let visibleCount = 0;

        const rows = tableBody.querySelectorAll('tr');
        rows.forEach(row => {
            const plate = row.querySelector('td:first-child').textContent.toLowerCase();
            const chassis = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const orgCell = row.querySelector('td[data-organization-id]');
            const statusBadge = row.querySelector('.status-badge');
            const status = statusBadge ? statusBadge.dataset.status : '';
            
            const matchesSearch = plate.includes(searchTerm) || 
                                chassis.includes(searchTerm);
            const matchesOrg = !selectedOrg || 
                             (orgCell && orgCell.dataset.organizationId === selectedOrg);
            const matchesStatus = selectedStatus === '' || status === selectedStatus;
            
            const shouldShow = matchesSearch && matchesOrg && matchesStatus;
            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        // Tablo başlığını güncelle
        const selectedOrgText = selectedOrg ? 
            orgFilter.options[orgFilter.selectedIndex].text : 'Tüm Araçlar';
        tableTitle.textContent = selectedOrgText;
        const countSpan = tableTitle.nextElementSibling;
        if (countSpan) {
            countSpan.textContent = `(${visibleCount} araç)`;
        }
    }

    // Event listeners
    if (searchInput) searchInput.addEventListener('input', filterVehicles);
    if (orgFilter) orgFilter.addEventListener('change', filterVehicles);
    if (statusFilter) statusFilter.addEventListener('change', filterVehicles);
}); 