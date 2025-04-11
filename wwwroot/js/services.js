// Servisleri global olarak tanımla
window.services = {
    crud: {
        async get(url) {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return await response.json();
        },
        async post(url, data) {
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value,
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: data
                });

                let responseData;
                const contentType = response.headers.get("content-type");
                if (contentType && contentType.indexOf("application/json") !== -1) {
                    responseData = await response.json();
                } else {
                    const textResponse = await response.text();
                    throw new Error(`Sunucu yanıtı JSON formatında değil: ${textResponse}`);
                }

                if (!response.ok) {
                    throw new Error(responseData.message || 'İşlem başarısız oldu');
                }

                return responseData;
            } catch (error) {
                console.error('Post error:', error);
                throw error;
            }
        },
        async delete(url) {
            try {
                console.log('Delete request to:', url); // Debug için
                
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                console.log('CSRF Token:', token); // Debug için
                
                const response = await fetch(url, {
                    method: 'DELETE',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                console.log('Delete response status:', response.status); // Debug için

                const responseData = await response.json();
                console.log('Delete response data:', responseData); // Debug için

                if (!response.ok) {
                    throw new Error(responseData.message || 'Silme işlemi başarısız oldu');
                }

                return responseData;
            } catch (error) {
                console.error('Delete request error:', error);
                throw error;
            }
        },
        async confirmDelete(entityName) {
            const result = await Swal.fire({
                title: 'Emin misiniz?',
                text: `Bu ${entityName} silinecek. Devam etmek istiyor musunuz?`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Evet, sil!',
                cancelButtonText: 'İptal'
            });
            return result.isConfirmed;
        }
    },
    ui: {
        showLoading() {
            const loader = document.getElementById('loader');
            if (loader) loader.classList.remove('hidden');
        },
        hideLoading() {
            const loader = document.getElementById('loader');
            if (loader) loader.classList.add('hidden');
        },
        showToast(type, message) {
            Swal.fire({
                icon: type,
                text: message,
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 3000
            });
        },
        async confirmDelete(title, text) {
            return await Swal.fire({
                title,
                text,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Evet, sil',
                cancelButtonText: 'İptal',
                reverseButtons: true
            });
        }
    },
    modal: {
        open: async function(modalId) {
            try {
                console.log('Modal açılmaya çalışılıyor:', modalId);
                
                // Tüm modalları kapat
                const allModals = document.querySelectorAll('[id$="Modal"]');
                allModals.forEach(modal => {
                    modal.style.display = 'none';
                    modal.classList.add('hidden');
                });

                // İstenen modalı bul
                const modal = document.getElementById(modalId);
                if (!modal) {
                    throw new Error(`Modal bulunamadı: ${modalId}`);
                }

                // Modalı görünür yap
                modal.style.cssText = `
                    display: block !important;
                    z-index: 9999;
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background-color: rgba(0, 0, 0, 0.5);
                    overflow-y: auto;
                `;
                modal.classList.remove('hidden');

                // Body scroll'u engelle
                document.body.style.overflow = 'hidden';

                // Modal içeriğini kontrol et
                const modalContent = modal.querySelector('.relative');
                if (modalContent) {
                    modalContent.style.cssText = `
                        background: white;
                        margin: 5rem auto;
                        padding: 2rem;
                        border-radius: 0.75rem;
                        box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
                        position: relative;
                        width: 500px;
                        max-width: 90%;
                    `;
                }

                // Modal açıldığında event'i tetikle
                const event = new CustomEvent('modalOpened', { 
                    detail: { modalId } 
                });
                document.dispatchEvent(event);

            } catch (error) {
                console.error('Modal açma hatası:', error);
            }
        },
        
        close: function(modalId) {
            try {
                const modal = document.getElementById(modalId);
                if (modal) {
                    modal.style.display = 'none';
                    modal.classList.add('hidden');
                    document.body.style.overflow = 'auto';

                    // Formu resetle
                    const form = modal.querySelector('form');
                    if (form) {
                        form.reset();
                        // Validasyon ikonlarını sıfırla
                        const requirements = modal.querySelectorAll('[data-requirement] i');
                        requirements.forEach(icon => {
                            icon.style.color = '#EF4444';
                            icon.className = 'fas fa-times-circle';
                        });
                    }
                }
            } catch (error) {
                console.error('Modal kapatma hatası:', error);
            }
        },
        onOpen: function(modalId, callback) {
            document.addEventListener('modalOpened', function(e) {
                if (e.detail.modalId === modalId) {
                    callback();
                }
            });
        }
    },
    table: {
        async refresh(tableName) {
            try {
                console.log(`Refreshing table: ${tableName}`);
                const url = `/${tableName}/GetTable`;
                
                const response = await fetch(url, {
                    headers: {
                        'Accept': 'text/html',
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                
                const html = await response.text();
                const containerId = `${tableName.toLowerCase()}TableContainer`;
                const container = document.querySelector(`#${containerId}`);
                
                if (container) {
                    container.innerHTML = html;
                    setTimeout(() => {
                        this.initializeTableEventListeners(tableName);
                    }, 0);
                    console.log('Table refreshed successfully');
                } else {
                    throw new Error(`Table container not found: #${containerId}`);
                }
            } catch (error) {
                console.error(`Table refresh error (${tableName}):`, error);
                window.services.ui.showToast('error', 'Tablo yenilenirken bir hata oluştu');
                throw error;
            }
        },
        initializeTableEventListeners(tableName) {
            const container = document.querySelector(`#${tableName.toLowerCase()}TableContainer`);
            if (!container) return;

            // Edit ve Delete butonları için event listener'ları ekle
            container.querySelectorAll('[data-edit-id]').forEach(button => {
                button.addEventListener('click', async (e) => {
                    e.preventDefault();
                    e.stopPropagation(); // Event'in yukarı çıkmasını engelle
                    const id = button.dataset.editId;
                    const entityType = button.dataset.entityType;
                    console.log('Edit clicked:', { id, entityType, operations: window[`${entityType}Operations`] });
                    
                    if (!window[`${entityType}Operations`]) {
                        console.error(`Operations not found for entity type: ${entityType}`);
                        return;
                    }
                    
                    await window[`${entityType}Operations`].edit(id);
                });
            });

            container.querySelectorAll('[data-delete-id]').forEach(button => {
                button.addEventListener('click', async (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    const id = button.dataset.deleteId;
                    const entityType = button.dataset.entityType;
                    await window[`${entityType.toLowerCase()}Operations`].delete(id);
                });
            });

            // Form submit olayını engelle
            container.querySelectorAll('form').forEach(form => {
                form.addEventListener('submit', (e) => {
                    e.preventDefault();
                    return false;
                });
            });
        }
    },
    form: {
        fill(formSelector, data) {
            const form = document.querySelector(formSelector);
            if (!form) {
                console.error('Form not found:', formSelector);
                return;
            }

            if (!data || typeof data !== 'object') {
                console.error('Invalid data:', data);
                return;
            }

            // Tüm input, select ve textarea elementlerini doldur
            Object.entries(data).forEach(([key, value]) => {
                try {
                    // Önce normal form elemanlarını dene
                    const element = form.querySelector(`[name="${key}"]`);
                    if (element) {
                        if (element.type === 'checkbox' || element.type === 'radio') {
                            element.checked = Boolean(value);
                        } else {
                            element.value = value ?? '';
                        }
                    }

                    // Toggle button kontrolü
                    const toggleButton = form.querySelector(`#${key}Toggle`);
                    if (toggleButton) {
                        const isActive = Boolean(value);
                        
                        // Toggle button rengini güncelle
                        toggleButton.classList.toggle('bg-blue-600', isActive);
                        toggleButton.classList.toggle('bg-gray-200', !isActive);
                        
                        // Toggle noktasını güncelle
                        const toggleDot = toggleButton.querySelector('[id$="toggleDot"]');
                        if (toggleDot) {
                            toggleDot.classList.toggle('translate-x-5', isActive);
                            toggleDot.classList.toggle('translate-x-0', !isActive);
                            
                            // İkonları güncelle
                            const checkIcon = toggleDot.querySelector('.fa-check')?.parentElement;
                            const timesIcon = toggleDot.querySelector('.fa-times')?.parentElement;
                            
                            if (checkIcon) checkIcon.style.display = isActive ? 'block' : 'none';
                            if (timesIcon) timesIcon.style.display = isActive ? 'none' : 'block';
                        }

                        // Durum metnini güncelle
                        const statusText = toggleButton.parentElement.querySelector('[id$="statusText"]');
                        if (statusText) {
                            statusText.textContent = isActive ? 'Aktif' : 'Pasif';
                        }
                    }
                } catch (error) {
                    console.error(`Error setting field ${key}:`, error);
                }
            });

            // Şifre alanlarını temizle
            const passwordInputs = form.querySelectorAll('input[type="password"]');
            passwordInputs.forEach(input => input.value = '');
        },

        clear(formSelector) {
            const form = document.querySelector(formSelector);
            if (!form) return;

            // Formu resetle
            form.reset();

            // Hidden input'ları temizle
            form.querySelectorAll('input[type="hidden"]').forEach(input => {
                if (input.name === 'AktifMi') input.value = 'true';
                else if (input.name === 'Id') input.value = '';
            });

            // Toggle butonları varsayılan duruma getir
            form.querySelectorAll('[id$="Toggle"]').forEach(toggle => {
                toggle.classList.add('bg-blue-600');
                toggle.classList.remove('bg-gray-200');
                
                const dot = toggle.querySelector('[id$="toggleDot"]');
                if (dot) {
                    dot.classList.add('translate-x-5');
                    dot.classList.remove('translate-x-0');
                }
            });
        }
    },
    forms: {
        user: {
            selector: '#editUserModal form, #newUserModal form',
            fill(data) {
                console.log('Gelen veri:', data); // Debug için

                const form = document.querySelector('#editUserModal form');
                if (!form) {
                    console.error('Form bulunamadı');
                    return;
                }

                try {
                    // Hidden input'ları doldur
                    const idInput = form.querySelector('input[name="Id"]');
                    const aktifMiInput = form.querySelector('input[name="AktifMi"]');
                    
                    if (idInput) idInput.value = data.id || '';
                    if (aktifMiInput) aktifMiInput.value = (data.aktifMi || false).toString();

                    // Text input'ları doldur
                    const nameInput = form.querySelector('input[name="Name"]');
                    const lastNameInput = form.querySelector('input[name="LastName"]');
                    const emailInput = form.querySelector('input[name="Email"]');
                    
                    if (nameInput) nameInput.value = data.name || '';
                    if (lastNameInput) lastNameInput.value = data.lastName || '';
                    if (emailInput) emailInput.value = data.email || '';

                    // Organizasyon seçimini güncelle
                    const organizationSelect = form.querySelector('select[name="OrganizationId"]');
                    if (organizationSelect) {
                        organizationSelect.value = data.organizationId || '';
                    }

                    // Toggle butonunu güncelle
                    const toggleButton = form.querySelector('#userStatusToggle');
                    if (toggleButton) {
                        const isActive = data.aktifMi === true;
                        toggleButton.classList.toggle('bg-blue-600', isActive);
                        toggleButton.classList.toggle('bg-gray-200', !isActive);

                        const toggleDot = toggleButton.querySelector('#toggleDot');
                        if (toggleDot) {
                            toggleDot.classList.toggle('translate-x-5', isActive);
                            toggleDot.classList.toggle('translate-x-0', !isActive);

                            const checkIcon = toggleDot.querySelector('.fa-check').parentElement;
                            const timesIcon = toggleDot.querySelector('.fa-times').parentElement;
                            checkIcon.style.display = isActive ? 'block' : 'none';
                            timesIcon.style.display = isActive ? 'none' : 'block';
                        }

                        const statusText = form.querySelector('#statusText');
                        if (statusText) {
                            statusText.textContent = isActive ? 'Aktif' : 'Pasif';
                        }
                    }

                    // Şifre alanlarını temizle
                    const passwordInputs = form.querySelectorAll('input[type="password"]');
                    passwordInputs.forEach(input => input.value = '');

                } catch (error) {
                    console.error('Form doldurma hatası:', error);
                }
            }
        },
        organization: {
            selector: '#editOrgModal form, #newOrgModal form',
            toggleFields: ['aktifMi'],
            fill(data) {
                window.services.form.fill(this.selector, data);
            },
            clear() {
                window.services.form.clear(this.selector);
            }
        },
        vehicle: {
            selector: '#editVehicleModal form, #newVehicleModal form',
            toggleFields: ['aktifMi'],
            fill(data) {
                window.services.form.fill(this.selector, data);
            },
            clear() {
                window.services.form.clear(this.selector);
            }
        },
        telemetryDevice: {
            selector: '#editTelemetryDeviceModal form, #newTelemetryDeviceModal form',
            toggleFields: ['aktifMi'],
            fill(data) {
                window.services.form.fill(this.selector, data);
            },
            clear() {
                window.services.form.clear(this.selector);
            }
        }
    },
    tables: {
        user: {
            containerId: '#usersTableContainer',
            refreshUrl: '/Users/GetUsersTable',
            async refresh() {
                try {
                    const response = await fetch(this.refreshUrl, {
                        headers: {
                            'Accept': 'text/html'  // HTML yanıt beklediğimizi belirt
                        }
                    });
                    
                    if (!response.ok) throw new Error('Tablo verisi alınamadı');
                    const html = await response.text(); // response.json() yerine text() kullan
                    
                    const container = document.querySelector(this.containerId);
                    if (!container) {
                        throw new Error(`Tablo container bulunamadı (${this.containerId})`);
                    }
                    
                    container.innerHTML = html;
                    return true;
                } catch (error) {
                    console.error('Tablo güncelleme hatası:', error);
                    window.services.ui.showToast('error', 'Tablo güncellenirken bir hata oluştu');
                    return false;
                }
            }
        },
        organization: {
            containerId: '#organizationsTableContainer',
            refreshUrl: '/Organizations/GetOrganizationsTable',
            async refresh() {
                return await window.services.table.refresh(this.containerId, this.refreshUrl);
            }
        },
        vehicle: {
            containerId: '#vehiclesTableContainer',
            refreshUrl: '/Vehicles/GetVehiclesTable',
            async refresh() {
                return await window.services.table.refresh(this.containerId, this.refreshUrl);
            }
        },
        telemetryDevice: {
            containerId: '#telemetrydevicesTableContainer',
            refreshUrl: '/TelemetryDevices/GetTelemetryDevicesTable',
            async refresh() {
                return await window.services.table.refresh(this.containerId, this.refreshUrl);
            }
        }
    },
    toggle: {
        handleClick(button) {
            const isActive = button.classList.contains('bg-blue-600');
            this.updateToggleState(button, !isActive);
        },

        updateToggleState(button, isActive) {
            // Toggle button görünümünü güncelle
            if (isActive) {
                button.classList.add('bg-blue-600');
                button.classList.remove('bg-gray-200');
            } else {
                button.classList.remove('bg-blue-600');
                button.classList.add('bg-gray-200');
            }

            // Dot pozisyonunu güncelle
            const dot = button.querySelector('#toggleDot');
            if (dot) {
                if (isActive) {
                    dot.classList.add('translate-x-5');
                    dot.classList.remove('translate-x-0');
                } else {
                    dot.classList.remove('translate-x-5');
                    dot.classList.add('translate-x-0');
                }
            }

            // İkonları güncelle
            const checkIcon = button.querySelector('.fa-check');
            const timesIcon = button.querySelector('.fa-times');
            if (checkIcon) checkIcon.style.display = isActive ? 'block' : 'none';
            if (timesIcon) timesIcon.style.display = isActive ? 'none' : 'block';

            // Hidden input değerini güncelle
            const form = button.closest('form');
            if (form) {
                const hiddenInput = form.querySelector('input[name="AktifMi"]');
                if (hiddenInput) {
                    hiddenInput.value = isActive.toString().toLowerCase();
                }
            }

            // Status text'i güncelle
            const statusText = button.parentElement.querySelector('#statusText');
            if (statusText) {
                statusText.textContent = isActive ? 'Aktif' : 'Pasif';
            }
        }
    }
};

// Modal kapatma işlemlerini doğrudan DOMContentLoaded içinde tanımlayalım
document.addEventListener('DOMContentLoaded', () => {
    // Modal kapatma butonları için event listener
    document.addEventListener('click', (e) => {
        const closeButton = e.target.closest('[data-modal-close]');
        if (closeButton && window.services.modal) {
            const modalId = closeButton.getAttribute('data-modal-close');
            window.services.modal.close(modalId);
        }
    });
});

// Global değişkenleri tanımlayalım
window.crudService = window.services.crud;
window.uiService = window.services.ui;
window.modalService = window.services.modal;
window.tableService = window.services.table;

// Bu kısmı kaldıralım - artık init fonksiyonunu kullanmıyoruz
// document.addEventListener('DOMContentLoaded', () => {
//     if (window.services && window.services.modal) {
//         window.services.modal.init();
//     }
// }); 
