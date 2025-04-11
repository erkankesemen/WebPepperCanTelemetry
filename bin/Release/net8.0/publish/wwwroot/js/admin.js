// Global confirmDelete fonksiyonu
async function confirmDelete(event, entityName) {
    event.preventDefault(); // Varsayılan davranışı engelle
    const confirmed = await window.services.crud.confirmDelete(entityName); // Onay penceresini göster
    return confirmed; // Onay durumunu döndür
}

// Temel operasyon sınıfı
class BaseOperations {
    constructor(entityName, requiredFields = []) {
        this.entityName = entityName;
        this.requiredFields = requiredFields;
        this.isSubmitting = false;
        this.initializeEventListeners();
    }

    initializeEventListeners() {
        document.addEventListener('click', (e) => {
            const toggleButton = e.target.closest('.toggle-button');
            if (!toggleButton) return;

            const modal = toggleButton.closest('[id$="Modal"]');
            if (!modal) return;

            // Toggle butonunun durumunu güncelle
            const isActive = toggleButton.classList.contains('bg-blue-600');
            this.updateToggleState(toggleButton, !isActive);

            // Hidden input'u güncelle
            const hiddenInput = toggleButton.parentElement.querySelector('input[name="AktifMi"]');
            if (hiddenInput) {
                hiddenInput.value = (!isActive).toString().toLowerCase();
            }
        });
    }

    async edit(id) {
        try {
            
            window.services.ui.showLoading();
            
            const url = `/${this.entityName}s/Edit/${id}`;

            
            const response = await window.services.crud.get(url);
          
            
            if (response.success && response.data) {
                const modalId = `edit${this.entityName}Modal`;
                window.services.modal.open(modalId);
                this.fillEditForm(response.data);
            } else {
                throw new Error(response.message || `${this.entityName} bilgileri alınamadı`);
            }
        } catch (error) {
            
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
    }

    async delete(event, id) {
        event.preventDefault();
        try {
            const confirmed = await window.services.crud.confirmDelete(this.entityName.toLowerCase());
            if (!confirmed) return false;

            window.services.ui.showLoading();
            const response = await window.services.crud.delete(`/${this.entityName}s/Delete/${id}`);
            
            if (response.success) {
                await window.services.table.refresh(`${this.entityName.toLowerCase()}s`);
                window.services.ui.showToast('success', response.message || `${this.entityName} silindi`);
            } else {
                throw new Error(response.message || 'Silme işlemi başarısız oldu');
            }
        } catch (error) {
            console.error('Silme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
        return false;
    }

    async submitForm(event) {
        event.preventDefault();
        
        if (this.isSubmitting) return;
        this.isSubmitting = true;
        
        try {
            const form = event.target;
            const formData = new FormData(form);

            // Form validasyonunu sadece yeni kayıt eklerken yap
            const isEdit = form.id.startsWith('edit');
            if (!isEdit) {
                const missingFields = this.requiredFields.filter(field => !formData.get(field));
                if (missingFields.length > 0) {
                    throw new Error(`Lütfen zorunlu alanları doldurun: ${missingFields.join(', ')}`);
                }
            }

            const url = isEdit ? 
                `/${this.entityName}s/Edit` : 
                `/${this.entityName}s/Create`;

            window.services.ui.showLoading();
            const response = await window.services.crud.post(url, formData);

            if (response.success) {
                const modalId = isEdit ? `edit${this.entityName}Modal` : `new${this.entityName}Modal`;
                this.resetAndCloseModal(modalId);
                
                // İlgili tabloları güncelle
                const updatePromises = [];

                // Ana entity'nin tablosunu güncelle
                updatePromises.push(window.services.table.refresh(`${this.entityName.toLowerCase()}s`));

                // Organizasyon tablosunu güncelleme kontrolü
                if (this.entityName === 'User' || this.entityName === 'Vehicle') {
                    updatePromises.push(window.services.table.refresh('organizations'));
                }

                // Telemetri cihazları tablosunu güncelle
                if (this.entityName === 'Vehicle') {
                    updatePromises.push(window.services.table.refresh('telemetrydevices')); // Telemetri cihazları tablosunu güncelle
                }

                // Combobox güncellemeleri
                if (this.entityName === 'Organization' || this.entityName === 'User') {
                    await new Promise(resolve => setTimeout(resolve, 500));
                    updatePromises.push(this.updateOrganizationCombos());
                } else if (this.entityName === 'Vehicle') {
                    await new Promise(resolve => setTimeout(resolve, 500));
                    updatePromises.push(this.updateVehicleCombos());
                }

                // Tüm güncellemeleri bekle
                await Promise.all(updatePromises);
                
                window.services.ui.showToast('success', response.message || `${this.entityName} ${isEdit ? 'güncellendi' : 'oluşturuldu'}`);
            } else {
                throw new Error(response.message || `${this.entityName} ${isEdit ? 'güncellenemedi' : 'oluşturulamadı'}`);
            }
        } catch (error) {
            console.error('Form gönderme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            this.isSubmitting = false;
            window.services.ui.hideLoading();
        }
    }

    updateToggleState(button, isActive) {
        if (!button) return;

        // Toggle button renklerini güncelle
        button.classList.remove('bg-blue-600', 'bg-gray-200');
        button.classList.add(isActive ? 'bg-blue-600' : 'bg-gray-200');
        
        // Dot'u güncelle
        const dot = button.querySelector('.dot');
        if (dot) {
            dot.classList.remove('translate-x-5', 'translate-x-0');
            dot.classList.add(isActive ? 'translate-x-5' : 'translate-x-0');
            
            // İkonları güncelle
            const checkIcon = dot.querySelector('.fa-check')?.closest('span');
            const timesIcon = dot.querySelector('.fa-times')?.closest('span');
            
            if (checkIcon) {
                checkIcon.style.opacity = isActive ? '1' : '0';
            }
            if (timesIcon) {
                timesIcon.style.opacity = isActive ? '0' : '1';
            }
        }
        
        // Hidden input'u güncelle
        const form = button.closest('form');
        if (form) {
            const hiddenInput = form.querySelector('input[name="AktifMi"]');
            if (hiddenInput) {
                hiddenInput.value = isActive.toString().toLowerCase();
            }
        }
        
        // Status text'i güncelle
        const statusText = button.parentElement.querySelector('.status-text');
        if (statusText) {
            statusText.textContent = isActive ? 'Aktif' : 'Pasif';
        }
    }

    // Organizasyon combobox'larını güncelle
    async updateOrganizationCombos() {
        try {
            const response = await window.services.crud.get('/Organizations/GetAll');
            if (!response.success) return;

            const organizations = response.data;
            
            // Tüm organizasyon combobox'larını güncelle
            document.querySelectorAll('select[name="OrganizationId"]').forEach(select => {
                const currentValue = select.value;
                select.innerHTML = '<option value="">Seçiniz...</option>';
                organizations.forEach(org => {
                    const option = new Option(org.kurumAdi, org.id, false, org.id === currentValue);
                    select.add(option);
                });
            });

            // Combobox güncellemesi tamamlandıktan sonra kısa bir bekleme
            await new Promise(resolve => setTimeout(resolve, 200));

        } catch (error) {
            console.error('Organizasyon listesi güncellenirken hata:', error);
        }
    }

    // Araç combobox'larını güncelle
    async updateVehicleCombos() {
        try {
            const response = await window.services.crud.get('/Vehicles/GetAll');
            if (!response.success) return;

            const vehicles = response.data;
            
            // Tüm araç combobox'larını güncelle
            document.querySelectorAll('select[name="VehicleId"]').forEach(select => {
                const currentValue = select.value;
                select.innerHTML = '<option value="">Seçiniz...</option>';
                vehicles.forEach(vehicle => {
                    const option = new Option(vehicle.plaka, vehicle.id, false, vehicle.id === currentValue);
                    select.add(option);
                });
            });
        } catch (error) {
            console.error('Araç listesi güncellenirken hata:', error);
        }
    }

    // Yeni metod ekleyelim
    resetAndCloseModal(modalId) {
        const modal = document.getElementById(modalId);
        if (!modal) return;

        // Form'u bul ve resetle
        const form = modal.querySelector('form');
        if (form) {
            // Form elemanlarını temizle
            form.reset();

            // Select elementlerini temizle ve güncelle
            if (this.entityName === 'User') {
                this.updateOrganizationCombos();
            }

            // Hidden input'ları temizle
            form.querySelectorAll('input[type="hidden"]').forEach(input => {
                if (input.name !== '__RequestVerificationToken') {
                    input.value = '';
                }
            });

            // Toggle butonlarını varsayılan duruma getir
            const toggleButton = form.querySelector('.toggle-button');
            if (toggleButton) {
                this.updateToggleState(toggleButton, true);
            }
        }

        // Modalı kapat
        modal.classList.add('hidden');
    }

    async updateOrganization(organizationId) {
        const telemetryDeviceSelect = document.querySelector('select[name="TelemetryDeviceId"]');
        if (telemetryDeviceSelect) {
            const selectedDeviceId = telemetryDeviceSelect.value;
            await window.services.crud.post(`/api/telemetrydevices/${selectedDeviceId}`, { OrganizationId: organizationId });
        }
    }
}

// Organization Operations
class OrganizationOperations extends BaseOperations {
    constructor() {
        super('Organization', ['KurumAdi', 'Tel', 'Email', 'IlgiliKisi']);
    }

    fillEditForm(data) {
        const form = document.querySelector('#editOrganizationModal form');
        if (!form) return;

        form.querySelector('input[name="Id"]').value = data.id;
        form.querySelector('input[name="KurumAdi"]').value = data.kurumAdi || '';
        form.querySelector('input[name="Tel"]').value = data.tel || '';
        form.querySelector('input[name="Email"]').value = data.email || '';
        form.querySelector('input[name="IlgiliKisi"]').value = data.ilgiliKisi || '';
        form.querySelector('textarea[name="Adres"]').value = data.adres || '';

        const toggleButton = form.querySelector('.toggle-button');
        if (toggleButton) {
            this.updateToggleState(toggleButton, data.aktifMi === true);
        }
    }

    // Organizasyon kaydedildikten sonra combobox'ları güncelle
    async submitForm(event) {
        await super.submitForm(event);
        await this.updateOrganizationCombos();
    }

    async delete(event, id) {
        event.preventDefault();
        try {
            const confirmed = await window.services.crud.confirmDelete(this.entityName.toLowerCase());
            if (!confirmed) return false;

            window.services.ui.showLoading();
            const response = await window.services.crud.delete(`/Organizations/Delete/${id}`);
            
            if (response.success) {
                // Organizasyonlar tablosunu ve tüm organizasyon combobox'larını güncelle
                await Promise.all([
                    window.services.table.refresh('organizations'),
                    this.updateOrganizationCombos()
                ]);
                window.services.ui.showToast('success', response.message || 'Organizasyon silindi');
            } else {
                throw new Error(response.message || 'Silme işlemi başarısız oldu');
            }
        } catch (error) {
            console.error('Silme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
        return false;
    }
}

// User Operations
class UserOperations extends BaseOperations {
    constructor() {
        // Kullanıcı için gerekli alanları belirle
        super('User', ['Name', 'LastName', 'Email', 'OrganizationId', 'Password']);
    }

    async submitForm(event) {
        event.preventDefault();
        
        if (this.isSubmitting) return;
        this.isSubmitting = true;
        
        try {
            const form = event.target;
            const formData = new FormData(form);
            const isEdit = form.id.startsWith('edit');

            // Yeni kullanıcı eklerken şifre kontrolü yap
            if (!isEdit) {
                const password = formData.get('Password');
                const confirmPassword = formData.get('ConfirmPassword');
                
                if (!password) {
                    throw new Error('Şifre alanı zorunludur');
                }
                if (password !== confirmPassword) {
                    throw new Error('Şifreler eşleşmiyor');
                }
            }

            const url = isEdit ? '/Users/Edit' : '/Users/Create';
            
            window.services.ui.showLoading();
            const response = await window.services.crud.post(url, formData);

            if (response.success) {
                const modalId = isEdit ? 'editUserModal' : 'newUserModal';
                this.resetAndCloseModal(modalId);

                // Tabloları ve combobox'ları güncelle
                await Promise.all([
                    window.services.table.refresh('users'),
                    window.services.table.refresh('organizations'),
                    this.updateOrganizationCombos()
                ]);

                window.services.ui.showToast('success', response.message || `Kullanıcı ${isEdit ? 'güncellendi' : 'oluşturuldu'}`);
            } else {
                throw new Error(response.message || `Kullanıcı ${isEdit ? 'güncellenemedi' : 'oluşturulamadı'}`);
            }
        } catch (error) {
            console.error('Form gönderme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            this.isSubmitting = false;
            window.services.ui.hideLoading();
        }
    }

    async edit(id) {
        try {
            
            window.services.ui.showLoading();
            
            const response = await window.services.crud.get(`/Users/Edit/${id}`);
            
            if (response.success && response.data) {
                // Önce modalı açalım ve açılmasını bekleyelim
                await window.services.modal.open('editUserModal');
                
                // Modal açıldıktan sonra form doldurma işlemini yapalım
                setTimeout(() => {
                    const form = document.querySelector('#editUserModal form');
                    if (!form) {
                        throw new Error('Form bulunamadı');
                    }
                    this.fillEditForm(response.data);
                }, 100);
            } else {
                throw new Error(response.message || 'Kullanıcı bilgileri alınamadı');
            }
        } catch (error) {
            
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
    }

    fillEditForm(data) {
        const form = document.querySelector('#editUserModal form');
        if (!form) {
            console.error('Form bulunamadı');
            return;
        }

        try {
            // Debug için gelen veriyi kontrol edelim
            console.log('Gelen kullanıcı verisi:', data);

            // Form elemanlarını doldur
            const setFormValue = (name, value) => {
                const element = form.querySelector(`[name="${name}"]`);
                if (element) {
                    element.value = value || '';
                } else {
                    console.warn(`${name} form elemanı bulunamadı:`, name);
                    // DOM'u kontrol etmek için tüm form elemanlarını yazdıralım
                    console.log('Mevcut form elemanları:', form.elements);
                }
            };

            // Property isimlerini düzeltelim
            setFormValue('Id', data.Id || data.id);
            setFormValue('Name', data.Name || data.name);
            setFormValue('LastName', data.LastName || data.lastName);
            setFormValue('Email', data.Email || data.email);
            setFormValue('OrganizationId', data.OrganizationId || data.organizationId);

            // Toggle butonunu güncelle
            const toggleButton = form.querySelector('.toggle-button');
            if (toggleButton) {
                this.updateToggleState(toggleButton, data.AktifMi === true || data.aktifMi === true);
            }

            // Şifre alanlarını temizle
            const passwordInputs = form.querySelectorAll('input[type="password"]');
            passwordInputs.forEach(input => input.value = '');

        } catch (error) {
            console.error('Form doldurma hatası:', error);
            console.error('Hata detayı:', error.message);
            window.services.ui.showToast('error', 'Form doldurulurken bir hata oluştu');
        }
    }

    async delete(event, id) {
        event.preventDefault();
        try {
            const confirmed = await window.services.crud.confirmDelete(this.entityName.toLowerCase());
            if (!confirmed) return false;

            window.services.ui.showLoading();
            const response = await window.services.crud.delete(`/Users/Delete/${id}`);
            
            if (response.success) {
                // Hem kullanıcılar tablosunu hem de organizasyonlar tablosunu güncelle
                await Promise.all([
                    window.services.table.refresh('users'),
                    window.services.table.refresh('organizations')
                ]);
                window.services.ui.showToast('success', response.message || 'Kullanıcı silindi');
            } else {
                throw new Error(response.message || 'Silme işlemi başarısız oldu');
            }
        } catch (error) {
            console.error('Silme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
        return false;
    }
}

// Vehicle Operations
class VehicleOperations extends BaseOperations {
    constructor() {
        super('Vehicle', ['Plaka', 'Sase', 'OrganizationId']);
    }

    async edit(id) {
        try {
            window.services.ui.showLoading();
            const response = await window.services.crud.get(`/Vehicles/Edit/${id}`);
            
            if (response.success && response.data) {
                window.services.modal.open('editVehicleModal');
                this.fillEditForm(response.data);
            } else {
                throw new Error(response.message || 'Araç bilgileri alınamadı');
            }
        } catch (error) {
            console.error('Edit error:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
    }

    fillEditForm(data) {
        const form = document.querySelector('#editVehicleModal form');
        if (!form) return;

        form.querySelector('input[name="Id"]').value = data.id;
        form.querySelector('input[name="Plaka"]').value = data.plaka || '';
        form.querySelector('input[name="Sase"]').value = data.sase || '';
        form.querySelector('input[name="Location"]').value = data.location || '';

        const organizationSelect = form.querySelector('select[name="OrganizationId"]');
        if (organizationSelect) organizationSelect.value = data.organizationId || '';

        const toggleButton = form.querySelector('.toggle-button');
        if (toggleButton) this.updateToggleState(toggleButton, data.aktifMi === true);
    }

    // Araç kaydedildikten sonra combobox'ları güncelle
    async submitForm(event) {
        await super.submitForm(event);
        await this.updateVehicleCombos();
    }

    async delete(event, id) {
        event.preventDefault();
        try {
            const confirmed = await window.services.crud.confirmDelete(this.entityName.toLowerCase());
            if (!confirmed) return false;

            window.services.ui.showLoading();
            const response = await window.services.crud.delete(`/Vehicles/Delete/${id}`);
            
            if (response.success) {
                // Hem araçlar tablosunu hem de organizasyonlar tablosunu güncelle
                await Promise.all([
                    window.services.table.refresh('vehicles'),
                    window.services.table.refresh('organizations'),
                    window.services.table.refresh('telemetrydevices')
                ]);
                window.services.ui.showToast('success', response.message || 'Araç silindi');
            } else {
                throw new Error(response.message || 'Silme işlemi başarısız oldu');
            }
        } catch (error) {
            console.error('Silme hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
        return false;
    }
}

// TelemetryDevice Operations
class TelemetryDeviceOperations extends BaseOperations {
    constructor() {
        super('TelemetryDevice', ['SerialNumber', 'SimCardNumber', 'OrganizationId']);
        this.initializeTelemetryToggle();
        this.initializeOrganizationChange();
        this.initializeModalClose();
    }

    initializeTelemetryToggle() {
        // Sadece telemetri modalındaki toggle için event listener
        document.addEventListener('click', (e) => {
            const toggleButton = e.target.closest('.toggle-button');
            if (!toggleButton) return;

            // Sadece telemetri modalı içindeki toggle'ları yakala
            const telemetryModal = toggleButton.closest('#editTelemetryDeviceModal, #newTelemetryDeviceModal');
            if (!telemetryModal) return;

            const isActive = toggleButton.classList.contains('bg-blue-600');
            
            // Toggle button renklerini güncelle
            toggleButton.classList.remove('bg-blue-600', 'bg-gray-200');
            toggleButton.classList.add(isActive ? 'bg-gray-200' : 'bg-blue-600');
            
            // Dot'u güncelle
            const dot = toggleButton.querySelector('.dot');
            if (dot) {
                dot.classList.remove('translate-x-5', 'translate-x-0');
                dot.classList.add(isActive ? 'translate-x-0' : 'translate-x-5');
                
                // İkonları güncelle
                const checkIcon = dot.querySelector('.fa-check').closest('span');
                const timesIcon = dot.querySelector('.fa-times').closest('span');
                
                checkIcon.style.opacity = isActive ? '0' : '1';
                timesIcon.style.opacity = isActive ? '1' : '0';
            }
            
            // Hidden input'u güncelle
            const hiddenInput = toggleButton.parentElement.querySelector('input[name="AktifMi"]');
            if (hiddenInput) {
                hiddenInput.value = (!isActive).toString().toLowerCase();
            }
            
            // Status text'i güncelle
            const statusText = toggleButton.parentElement.querySelector('.status-text');
            if (statusText) {
                statusText.textContent = isActive ? 'Pasif' : 'Aktif';
            }
        });
    }

    // Organizasyon değişikliğini dinle
    initializeOrganizationChange() {
        document.addEventListener('change', async (e) => {
            const organizationSelect = e.target;
            if (!organizationSelect.matches('select[name="OrganizationId"]')) return;

            // Sadece telemetri modallarındaki değişiklikleri yakala
            const telemetryModal = organizationSelect.closest('#editTelemetryDeviceModal, #newTelemetryDeviceModal');
            if (!telemetryModal) return;

            const organizationId = organizationSelect.value;
            const vehicleSelect = telemetryModal.querySelector('select[name="VehicleId"]');
            
            if (!vehicleSelect) return;

            // Önce araç listesini temizle
            vehicleSelect.innerHTML = '<option value="">Seçiniz...</option>';
            vehicleSelect.value = '';

            // Organizasyon seçili değilse işlemi sonlandır
            if (!organizationId) return;

            await this.loadVehiclesForOrganization(organizationId, vehicleSelect);
        });
    }

    // Araçları yükleme işlemini ayrı bir metoda taşıyalım
    async loadVehiclesForOrganization(organizationId, vehicleSelect, selectedVehicleId = null) {
        try {
            const form = vehicleSelect.closest('form');
            const deviceId = form ? form.querySelector('input[name="Id"]')?.value : null;

            const response = await window.services.crud.get(
                `/Vehicles/GetByOrganization/${organizationId}?currentDeviceId=${deviceId || ''}`
            );

            if (response && Array.isArray(response)) {
                vehicleSelect.innerHTML = '<option value="">Seçiniz...</option>';
                response.forEach(vehicle => {
                    const option = new Option(
                        vehicle.plaka,
                        vehicle.id,
                        vehicle.id === selectedVehicleId,
                        vehicle.id === selectedVehicleId
                    );
                    vehicleSelect.add(option);
                });

                if (selectedVehicleId) {
                    vehicleSelect.value = selectedVehicleId;
                }
            }
        } catch (error) {
            console.error('Araçlar yüklenirken hata:', error);
            window.services.ui.showToast('error', 'Araçlar yüklenirken bir hata oluştu');
        }
    }

    // Yeni metod: Modal kapanışını izle
    initializeModalClose() {
        document.addEventListener('click', (e) => {
            const closeButton = e.target.closest('[data-modal-close]');
            if (!closeButton) return;

            const modalId = closeButton.getAttribute('data-modal-close');
            if (modalId && (modalId === 'editTelemetryDeviceModal' || modalId === 'newTelemetryDeviceModal')) {
                const modal = document.getElementById(modalId);
                if (!modal) return;

                const form = modal.querySelector('form');
                if (form) {
                    // Araç select'ini temizle
                    const vehicleSelect = form.querySelector('select[name="VehicleId"]');
                    if (vehicleSelect) {
                        vehicleSelect.innerHTML = '<option value="">Seçiniz...</option>';
                        vehicleSelect.value = '';
                    }
                }
            }
        });
    }

    async edit(id) {
        try {
            window.services.ui.showLoading();
            
            const url = `/TelemetryDevices/Edit/${id}`;
            
            const response = await window.services.crud.get(url);
            
            if (response.success && response.data) {
                const modalId = 'editTelemetryDeviceModal';
                await window.services.modal.open(modalId);
                await new Promise(resolve => setTimeout(resolve, 200));

                const form = document.querySelector(`#${modalId} form`);
                if (!form) {
                    throw new Error('Form bulunamadı');
                }

                this.fillEditForm(response.data);
            } else {
                throw new Error(response.message || 'Telemetri cihazı bilgileri alınamadı');
            }
        } catch (error) {
            console.error('Edit hatası:', error);
            window.services.ui.showToast('error', error.message);
        } finally {
            window.services.ui.hideLoading();
        }
    }

    async fillEditForm(data) {
        const form = document.querySelector('#editTelemetryDeviceModal form');
        if (!form) return;

        try {
            // Form elemanlarını güvenli bir şekilde set edelim
            const setFormValue = (name, value) => {
                const element = form.querySelector(`[name="${name}"]`);
                if (element) {
                    element.value = value || '';
                }
            };

            // Önce temel form alanlarını doldur
            setFormValue('Id', data.id);
            setFormValue('SerialNumber', data.serialNumber);
            setFormValue('SimCardNumber', data.simCardNumber);
            setFormValue('Notes', data.notes);
            setFormValue('FirmwareVersion', data.firmwareVersion);
            setFormValue('InstallationDate', data.installationDate?.split('T')[0]);

            // Organizasyon seçimini yap
            const organizationSelect = form.querySelector('select[name="OrganizationId"]');
            if (organizationSelect) {
                organizationSelect.value = data.organizationId || '';
            }

            // Eğer organizasyon seçili ise araçları yükle
            if (data.organizationId) {
                const vehicleSelect = form.querySelector('select[name="VehicleId"]');
                if (vehicleSelect) {
                    await this.loadVehiclesForOrganization(data.organizationId, vehicleSelect, data.vehicleId);
                }
            }

            // Toggle butonunu güncelle
            const toggleButton = form.querySelector('.toggle-button');
            if (toggleButton) {
                this.updateToggleState(toggleButton, data.aktifMi === true);
            }
        } catch (error) {
            console.error('Form doldurma hatası:', error);
            window.services.ui.showToast('error', 'Form doldurulurken bir hata oluştu');
        }
    }
}

// Global tanımlamalar
window.organizationOperations = new OrganizationOperations();
window.userOperations = new UserOperations();
window.vehicleOperations = new VehicleOperations();
window.telemetryDeviceOperations = new TelemetryDeviceOperations();

// En son event listener'ları ekleyelim
document.addEventListener('DOMContentLoaded', () => {
    if (!window.services) {
        console.error('Services not loaded!');
        return;
    }

    // Modal açma butonları için event listener
    document.addEventListener('click', (e) => {
        const target = e.target;

        // Yeni ekle butonları
        const newButton = target.closest('[data-new-entity]');
        if (newButton) {
            e.preventDefault();
            const entityType = newButton.dataset.newEntity;
            const modalId = `new${entityType}Modal`;
            console.log('Opening modal:', modalId); // Debug için
            window.services.modal.open(modalId);
            return;
        }

        // Modal kapatma butonları
        const closeButton = target.closest('[data-modal-close]');
        if (closeButton) {
            e.preventDefault();
            const modalId = closeButton.dataset.modalClose;
            console.log('Closing modal:', modalId); // Debug için
            window.services.modal.close(modalId);
            return;
        }
    });

    // Toggle butonları için genel event listener
    document.addEventListener('click', (e) => {
        const toggleButton = e.target.closest('.toggle-button');
        if (!toggleButton) return;

        const modal = toggleButton.closest('[id$="Modal"]');
        if (!modal) return;

        const isActive = toggleButton.classList.contains('bg-blue-600');
        const entityType = modal.id.replace(/(edit|new|Modal)/g, '').toLowerCase();
        const operations = window[`${entityType}Operations`];
        
        if (operations) {
            operations.updateToggleState(toggleButton, !isActive);
        }
    });

    // Tüm operasyonları başlat
    window.organizationOperations.initializeEventListeners();
    window.userOperations.initializeEventListeners();
    window.vehicleOperations.initializeEventListeners();
    window.telemetryDeviceOperations.initializeEventListeners();
});

// Modal servisi güncellemesi
window.services.modal = {
    open: async function(modalId) {
        // Önce tüm modalları kapatalım
        document.querySelectorAll('[id$="Modal"]').forEach(modal => {
            modal.classList.add('hidden');
        });
        
        // Sonra istenen modalı açalım
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('hidden');
        }
        
        // Modal açılmasını bekleyelim
        return new Promise(resolve => setTimeout(resolve, 100));
    },
    
    close: function(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            
            // Form varsa resetleyelim
            const form = modal.querySelector('form');
            if (form) form.reset();
        }
    }
};
