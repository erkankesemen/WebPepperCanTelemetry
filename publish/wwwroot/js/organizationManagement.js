document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const statusFilter = document.getElementById('statusFilter');
    const tableTitle = document.getElementById('tableTitle');
    const tableBody = document.querySelector('.organization-table tbody');

    function filterOrganizations() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedStatus = statusFilter.value;
        let visibleCount = 0;

        const rows = tableBody.querySelectorAll('tr');
        rows.forEach(row => {
            const orgName = row.querySelector('td:first-child').textContent.toLowerCase();
            const statusBadge = row.querySelector('.status-badge');
            const status = statusBadge ? statusBadge.dataset.status : '';
            
            const matchesSearch = orgName.includes(searchTerm);
            const matchesStatus = selectedStatus === '' || status === selectedStatus;
            
            const shouldShow = matchesSearch && matchesStatus;
            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        // Tablo başlığını güncelle
        const countSpan = tableTitle.nextElementSibling;
        if (countSpan) {
            countSpan.textContent = `(${visibleCount} kurum)`;
        }
    }

    // Event listeners
    if (searchInput) searchInput.addEventListener('input', filterOrganizations);
    if (statusFilter) statusFilter.addEventListener('change', filterOrganizations);
});

// CSRF token alıcı
function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]').value;
}

// API çağrıları için güvenli wrapper
async function apiCall(url, method, data = null) {
    try {
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            credentials: 'include'
        };

        if (data) {
            options.body = JSON.stringify(data);
        }

        const response = await fetch(url, options);
        
        if (!response.ok) {
            throw new Error('API çağrısı başarısız');
        }

        return await response.json();
    } catch (error) {
        console.error('API Hatası:', error);
        throw error;
    }
}

// BaseOperations'ı tekrar tanımlama, sadece özel fonksiyonları ekle
// Örnek:
window.organizationManagement = {
    // Özel fonksiyonlar...
}; 