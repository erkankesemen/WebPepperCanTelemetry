// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function() {
    // Aktif menü öğesini işaretle
    const currentPath = window.location.pathname;
    const menuItems = document.querySelectorAll('.sidebar-menu-item');
    
    menuItems.forEach(item => {
        if (item.getAttribute('href') === currentPath) {
            item.classList.add('active-menu-item');
        }
    });
});

// Genel UI işlemleri
function showLoading() {
    // Loading gösterici...
}

function hideLoading() {
    // Loading gizleyici...
}

// Genel yardımcı fonksiyonlar
function formatDate(date) {
    // Tarih formatlama...
}

// Hata gösterici
function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    if (errorDiv) {
        errorDiv.textContent = message;
        errorDiv.classList.remove('hidden');
    }
}

// Kullanıcı modalı için
function openModal() {
    const modal = document.getElementById('userModal');
    if (modal) {
        modal.classList.remove('hidden');
        document.getElementById('userForm').reset();
    }
}

function closeModal() {
    const modal = document.getElementById('userModal');
    if (modal) {
        modal.classList.add('hidden');
    }
}

// Kurum modalı için
function openOrganizationModal() {
    const modal = document.getElementById('organizationModal');
    if (modal) {
        modal.classList.remove('hidden');
        document.getElementById('organizationForm').reset();
    }
}

function closeOrganizationModal() {
    const modal = document.getElementById('organizationModal');
    if (modal) {
        modal.classList.add('hidden');
    }
}

// Dropdown menüler için
function toggleDropdown(id) {
    const dropdown = document.getElementById(id);
    const allDropdowns = document.querySelectorAll('[id$="Management"],[id="systemSettings"]');
    const button = dropdown.previousElementSibling;
    const arrow = button.querySelector('svg:last-child');
    
    allDropdowns.forEach(d => {
        if (d.id !== id) {
            d.classList.add('hidden');
            const btn = d.previousElementSibling;
            const arr = btn.querySelector('svg:last-child');
            arr.classList.remove('rotate-180');
        }
    });

    dropdown.classList.toggle('hidden');
    arrow.classList.toggle('rotate-180');
}

// Genel site fonksiyonları buraya taşınabilir
function scrollToSection(sectionId) {
    const element = document.getElementById(sectionId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}

// Filtreleme fonksiyonları
function filterTable(tableId, searchValue, filterValue, columns) {
    const rows = document.querySelectorAll(`#${tableId} tbody tr`);
    
    rows.forEach(row => {
        let matchesSearch = false;
        let matchesFilter = !filterValue;

        columns.forEach(column => {
            const cell = row.querySelector(column.selector);
            if (!cell) return;

            if (column.type === 'search') {
                matchesSearch = cell.textContent.toLowerCase().includes(searchValue.toLowerCase());
            } else if (column.type === 'filter' && filterValue) {
                matchesFilter = cell.dataset[column.dataKey] === filterValue;
            }
        });

        row.style.display = matchesSearch && matchesFilter ? '' : 'none';
    });
}

// Global olarak tanımlayalım
window.scrollToSection = function(sectionId) {
    const element = document.getElementById(sectionId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
};

window.filterTable = function(tableId, searchValue, filterValue, columns) {
    const rows = document.querySelectorAll(`#${tableId} tbody tr`);
    
    rows.forEach(row => {
        let matchesSearch = false;
        let matchesFilter = !filterValue;

        columns.forEach(column => {
            const cell = row.querySelector(column.selector);
            if (!cell) return;

            if (column.type === 'search') {
                matchesSearch = cell.textContent.toLowerCase().includes(searchValue.toLowerCase());
            } else if (column.type === 'filter' && filterValue) {
                matchesFilter = cell.dataset[column.dataKey] === filterValue;
            }
        });

        row.style.display = matchesSearch && matchesFilter ? '' : 'none';
    });
};
