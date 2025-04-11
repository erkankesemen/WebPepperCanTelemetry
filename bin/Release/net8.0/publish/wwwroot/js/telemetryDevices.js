document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const orgFilter = document.getElementById('organizationFilter');
    const statusFilter = document.getElementById('statusFilter');
    const tableTitle = document.getElementById('tableTitle');
    const tableBody = document.querySelector('.telemetry-device-table tbody');

    function filterDevices() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedOrg = orgFilter.value;
        const selectedStatus = statusFilter.value;
        let visibleCount = 0;

        const rows = tableBody.querySelectorAll('tr');
        rows.forEach(row => {
            const serialNo = row.querySelector('td:first-child').textContent.toLowerCase();
            const simNo = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const orgCell = row.querySelector('td[data-organization-id]');
            const statusBadge = row.querySelector('.status-badge');
            const status = statusBadge ? statusBadge.dataset.status : '';
            
            const matchesSearch = serialNo.includes(searchTerm) || 
                                simNo.includes(searchTerm);
            const matchesOrg = !selectedOrg || 
                             (orgCell && orgCell.dataset.organizationId === selectedOrg);
            const matchesStatus = selectedStatus === '' || status === selectedStatus;
            
            const shouldShow = matchesSearch && matchesOrg && matchesStatus;
            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        // Tablo başlığını güncelle
        const selectedOrgText = selectedOrg ? 
            orgFilter.options[orgFilter.selectedIndex].text : 'Tüm Cihazlar';
        tableTitle.textContent = selectedOrgText;
        const countSpan = tableTitle.nextElementSibling;
        if (countSpan) {
            countSpan.textContent = `(${visibleCount} cihaz)`;
        }
    }

    // Event listeners
    if (searchInput) searchInput.addEventListener('input', filterDevices);
    if (orgFilter) orgFilter.addEventListener('change', filterDevices);
    if (statusFilter) statusFilter.addEventListener('change', filterDevices);
}); 