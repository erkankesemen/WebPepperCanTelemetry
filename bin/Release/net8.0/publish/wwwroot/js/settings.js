// UI İşlemleri
function updateSettingsUI(settings) {
    // UI güncelleme işlemleri...
}

// Form işlemleri
document.addEventListener('DOMContentLoaded', function() {
    const settingsForm = document.getElementById('settingsForm');
    if (settingsForm) {
        settingsForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const submitBtn = settingsForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;

            try {
                const formData = new FormData(settingsForm);
                const data = Object.fromEntries(formData.entries());

                const response = await fetch('/api/settings', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(data)
                });

                if (!response.ok) throw new Error('Ayarlar kaydedilemedi');

                alert('Ayarlar başarıyla kaydedildi');
                window.location.reload();
            } catch (error) {
                alert(error.message);
            } finally {
                submitBtn.disabled = false;
            }
        });
    }
});

function resetForm() {
    document.getElementById('passwordForm').reset();
}

function validateAndSubmit() {
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (newPassword !== confirmPassword) {
        alert('Yeni şifreler eşleşmiyor!');
        return;
    }

    // Şifre karmaşıklık kontrolü
    const passwordRegex = new RegExp('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{6,}$');
    if (!passwordRegex.test(newPassword)) {
        alert('Şifre en az 6 karakter uzunluğunda olmalı ve en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir!');
        return;
    }

    document.getElementById('passwordForm').submit();
} 