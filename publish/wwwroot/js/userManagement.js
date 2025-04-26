// Arama ve filtreleme fonksiyonları
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const orgFilter = document.getElementById('organizationFilter');
    const statusFilter = document.getElementById('statusFilter');
    const tableTitle = document.getElementById('tableTitle');
    const tableBody = document.querySelector('tbody');

    function filterUsers() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedOrg = orgFilter.value;
        const selectedStatus = statusFilter.value;
        let visibleCount = 0;

        const rows = tableBody.querySelectorAll('tr');
        rows.forEach(row => {
            const userName = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const email = row.querySelector('td:nth-child(3)').textContent.toLowerCase();
            const orgCell = row.querySelector('td:first-child');
            
            // Durum badge'ini al
            const statusBadge = row.querySelector('.status-badge');
            const isActive = statusBadge?.dataset.status === 'true';
            
            const matchesSearch = userName.includes(searchTerm) || 
                                email.includes(searchTerm);
            const matchesOrg = !selectedOrg || row.dataset.organizationId === selectedOrg;
            
            // Durum filtrelemesi
            let matchesStatus = true;
            if (selectedStatus) {
                switch(selectedStatus) {
                    case 'active':
                        matchesStatus = isActive;
                        break;
                    case 'inactive':
                        matchesStatus = !isActive;
                        break;
                }
            }
            
            const shouldShow = matchesSearch && matchesOrg && matchesStatus;
            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        // Tablo başlığını güncelle
        const selectedOrgText = selectedOrg ? 
            orgFilter.options[orgFilter.selectedIndex].text : 'Tüm Kullanıcılar';
        tableTitle.textContent = selectedOrgText;
        const countSpan = tableTitle.nextElementSibling;
        if (countSpan) {
            countSpan.textContent = `(${visibleCount} kullanıcı)`;
        }
    }

    // Event listeners
    if (searchInput) searchInput.addEventListener('input', filterUsers);
    if (orgFilter) orgFilter.addEventListener('change', filterUsers);
    if (statusFilter) statusFilter.addEventListener('change', filterUsers);
});

// Şifre validasyonu için fonksiyon
function initializePasswordValidation() {
    
    // Tüm şifre inputlarını bul
    const passwordInputs = document.querySelectorAll('input[type="password"]');
    
    // Her input için event listener ekle
    passwordInputs.forEach(input => {
        ['input', 'keyup', 'paste'].forEach(eventType => {
            input.addEventListener(eventType, function() {
                // Input ID'sinden prefix'i belirle (edit veya new)
                const prefix = input.id.startsWith('edit') ? 'edit' : 'new';
                validatePassword(prefix);
            });
        });
    });
}

function validatePassword(prefix) {
    const passwordInput = document.getElementById(`${prefix}Password`);
    const confirmInput = document.getElementById(`${prefix}ConfirmPassword`);
    const requirements = document.getElementById(`${prefix}PasswordRequirements`);
    
    if (!passwordInput || !confirmInput || !requirements) {
        console.error('Required elements not found');
        return;
    }

    const password = passwordInput.value || ''; // Boş string olabilir
    const confirmPassword = confirmInput.value || ''; // Boş string olabilir

    // Kuralları kontrol et
    const rules = {
        length: password.length >= 6,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /[0-9]/.test(password),
        special: /[!@#$%^&*.]/.test(password), // Nokta ekledik
        match: password !== '' && password === confirmPassword
    };

    // Her kural için DOM'u güncelle
    Object.entries(rules).forEach(([rule, isValid]) => {
        const requirementDiv = requirements.querySelector(`[data-requirement="${rule}"]`);
        if (requirementDiv) {
            const icon = requirementDiv.querySelector('i');
            if (icon) {
                // Eğer şifre boşsa veya düzenleme modunda ve şifre girilmemişse
                const isEmptyAndEdit = prefix === 'edit' && password === '';
                if (isEmptyAndEdit) {
                    // Düzenleme modunda boş şifre için gri renk kullan
                    icon.style.color = '#9CA3AF'; // gray-400
                    icon.className = 'fas fa-minus-circle';
                    requirementDiv.style.color = '#6B7280'; // gray-500
                } else {
                    // Normal validasyon renkleri
                    icon.style.color = isValid ? '#10B981' : '#EF4444';
                    icon.className = `fas ${isValid ? 'fa-check-circle' : 'fa-times-circle'}`;
                    requirementDiv.style.color = isValid ? '#059669' : '#4B5563';
                }
            }
        }
    });

    // Düzenleme modunda boş şifre geçerli
    if (prefix === 'edit' && password === '') {
        return true;
    }

    return Object.values(rules).every(Boolean);
}

// Modal açıldığında validasyonu başlat
document.addEventListener('modalOpened', function(e) {
    if (e.detail.modalId === 'newUserModal' || e.detail.modalId === 'editUserModal') {
        setTimeout(initializePasswordValidation, 100);
    }
});

// Form submit kontrolü
document.addEventListener('submit', function(e) {
    const form = e.target;
    
    // Sadece kullanıcı formlarını kontrol et
    if (!form.id.includes('userForm') && !form.id.includes('editUserForm')) {
        return true; // Kullanıcı formu değilse validasyonu atla
    }

    const isEdit = form.querySelector('#editPassword') !== null;
    const prefix = isEdit ? 'edit' : 'new';
    
    if (isEdit && !form.querySelector('#editPassword').value) {
        return true; // Düzenleme modunda boş şifre geçerli
    }

    if (!validatePassword(prefix)) {
        e.preventDefault();
        window.services.ui.showToast('error', 'Lütfen şifre gereksinimlerini karşılayın');
        return false;
    }
});

// Sayfa yüklendiğinde validasyonu başlat
document.addEventListener('DOMContentLoaded', function() {
    initializePasswordValidation();
});

// CSRF token ve API çağrıları için yardımcı fonksiyonlar
function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]').value;
}

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