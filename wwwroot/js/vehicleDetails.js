let map;
let currentView = 'map';
let connection = null;
let markers = {};
let routes = {};
let isFirstData = true;
let selectedVehicleId = null;
let reconnectAttempts = 0;
const maxReconnectAttempts = 5;
let activeRoute = null; // Aktif rotayı takip etmek için
let selectedDeviceId = null; // Seçili aracın device ID'si
let currentSpeed = 0; // Mevcut hız
let currentAccelPct = 0; // Mevcut ivme yüzdesi
let currentSoc = 0; // Mevcut batarya yüzdesi
let lastSpeed = null;
let lastAccelPct = null;
let lastBrakePct = null;
let lastMotorRpm = null;
let lastSoc = null;
 
 
// Global olarak ikon tanımlaması
const busIcon = L.icon({
    iconUrl: '/images/bus-marker.png',
    iconSize: [32, 32],
    iconAnchor: [16, 32],
    popupAnchor: [0, -32]
});

// View değiştirme fonksiyonu
function setView(view) {
    currentView = view;
    const mapView = document.getElementById('mapView');
    const signalsView = document.getElementById('signalsView');
    const mapButton = document.getElementById('mapButton');
    const signalsButton = document.getElementById('signalsButton');

    if (view === 'map') {
        mapView.classList.remove('hidden');
        signalsView.classList.add('hidden');
        mapButton.classList.add('bg-blue-600', 'text-white');
        mapButton.classList.remove('bg-gray-100', 'text-gray-600');
        signalsButton.classList.remove('bg-blue-600', 'text-white');
        signalsButton.classList.add('bg-gray-100', 'text-gray-600');
        if (map) map.invalidateSize();
    } else {
        mapView.classList.add('hidden');
        signalsView.classList.remove('hidden');
        mapButton.classList.remove('bg-blue-600', 'text-white');
        mapButton.classList.add('bg-gray-100', 'text-gray-600');
        signalsButton.classList.add('bg-blue-600', 'text-white');
        signalsButton.classList.remove('bg-gray-100', 'text-gray-600');
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        await initializeMap();
        await initializeSignalR();

        // Başlangıçta seçili aracın device ID'sini al
        if (typeof selectedVehicle !== 'undefined' && selectedVehicle) {
            const displayName = selectedVehicle.displayName;
            if (displayName && displayName.includes(' / ')) {
                selectedDeviceId = displayName.split(' / ')[1];
                const vehicleElement = document.querySelector(`[data-vehicle-id="${selectedVehicle.id}"]`);
                if (vehicleElement) {
                    focusVehicle(selectedVehicle.id);
                }
            } else {
                console.warn('Vehicle display name format is incorrect:', displayName);
            }
        } else {
            console.log('No vehicle selected initially');
        }
    } catch (error) {
        console.error("Başlatma hatası:", error);
    }
});

async function initializeSignalR() {
    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/messageHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
            .configureLogging(signalR.LogLevel.Error)
            .build();

        // Bağlantı durumu değişikliklerini yönet
        connection.onreconnecting(error => {
            updateConnectionStatus("Yeniden Bağlanıyor...", "bg-yellow-500");
            reconnectAttempts++;
            
            if (reconnectAttempts > maxReconnectAttempts) {
                connection.stop();
                updateConnectionStatus("Bağlantı Hatası", "bg-red-500");
            }
        });

        connection.onreconnected(connectionId => {
            updateConnectionStatus("Bağlandı", "bg-green-500");
            reconnectAttempts = 0;
        });

        connection.onclose(async error => {
            updateConnectionStatus("Bağlantı Kesildi", "bg-red-500");
            
            // Yeniden bağlanmayı dene
            if (reconnectAttempts <= maxReconnectAttempts) {
                await startConnection();
            }
        });

        // Aktif araçları takip etmek için bir Set kullanalım
        const activeVehicles = new Set();

        // Rotayı saklamak için yeni bir nesne
        let routesData = {};
        let firstPositionSet = {}; // İlk konumun ayarlandığını takip etmek için

        connection.on("receiveTelemetry", (data) => {
            if (!data || !data.deviceId) return;

         
            // Araç aktif olarak işaretle ve son aktivite zamanını kaydet
            activeVehicles.add(data.deviceId);
            updateVehicleIcon(data.deviceId, true);

            // Konum güncellemesi
            if (data.deviceId && data.latitude && data.longitude) {
                const lat = parseFloat(data.latitude.replace(",", "."));
                const lng = parseFloat(data.longitude.replace(",", "."));

                // Rotayı sakla
                if (!routesData[data.deviceId]) {
                    routesData[data.deviceId] = []; // Yeni bir rota oluştur
                    firstPositionSet[data.deviceId] = false; // İlk konum ayarlanmamış
                }

                // İlk konum ayarlandı mı kontrol et
                if (!firstPositionSet[data.deviceId]) {
                    routesData[data.deviceId].push([lat, lng]); // İlk konumu ekle
                    firstPositionSet[data.deviceId] = true; // İlk konum ayarlandı
                } else {
                    routesData[data.deviceId].push([lat, lng]); // Yeni konumu ekle
                }

                // Sadece seçili araç için rotayı güncelle
                if (data.deviceId === selectedDeviceId) {
                    // Seçili aracın rotasını çizme
                    if (routes[data.deviceId]) {
                        routes[data.deviceId].setLatLngs(routesData[data.deviceId]); // Rotayı güncelle
                    } else {
                        // Eğer rota yoksa, yeni bir rota oluştur
                        routes[data.deviceId] = L.polyline(routesData[data.deviceId], {
                            color: '#2563eb', // Koyu mavi (Tailwind'in blue-600 rengi)
                            weight: 3,
                            opacity: 0.8
                        }).addTo(map);
                    }
                    map.panTo([lat, lng]);
                    updateMetrics({
                        speed: data.Speed,
                        accelPct: data.AccelerationPercentage,
                        brakePct: data.BrakePercentage,
                        MotorRpm: data.MotorRpm,
                        SOC: data.SOC
                    });
                    

                    // Tablodaki metrik verilerini güncelle
                    updateMetricsTable(data);
                }

                // İkonu güncelle
                const marker = markers[data.deviceId];
                if (marker) {
                    marker.setLatLng([lat, lng]); // İkonun konumunu güncelle
                }
            }

            // Metrikleri güncelle
            updateMetrics({
                speed: data.Speed,
                accelPct: data.AccelerationPercentage,
                brakePct: data.Brake,
                MotorRpm: data.MotorRpm,
                SOC: data.SOC
            });
            

            // Tablodaki metrik verilerini güncelle
            updateMetricsTable(data);

            
        });

        connection.on("vehicleStatusChanged", (data) => {
            if (!data || !data.deviceId) return;


            // Tüm araçları kontrol et ve device ID'ye göre eşleşeni bul
            const vehicles = document.querySelectorAll('[data-vehicle-id]');
            vehicles.forEach(vehicleElement => {
                const displayName = vehicleElement.querySelector('.font-medium')?.textContent;
                if (displayName && displayName.includes(' / ')) {
                    const deviceId = displayName.split(' / ')[1].trim();
                    
                    // Device ID eşleşiyorsa ikonu güncelle
                    if (deviceId === data.deviceId) {
                        const iconImg = vehicleElement.querySelector('img[alt="Bus Icon"]');
                        if (iconImg) {
                            const newSrc = `/images/bus-${data.isActive ? 'active' : 'inactive'}.png`;
                            iconImg.src = newSrc;
                        } else {
                            console.warn('İkon elementi bulunamadı');
                        }
                    }
                }
            });

            // Marker'ı güncelle
            const marker = markers[data.deviceId];
            if (marker) {
                marker.setOpacity(data.isActive ? 1 : 0.5);
            }
        });

        // Her 30 saniyede bir aktif araçları kontrol et
        setInterval(() => {
            // Tüm araçları kontrol et
            document.querySelectorAll('[data-vehicle-id]').forEach(vehicleElement => {
                const displayName = vehicleElement.querySelector('.font-medium')?.textContent;
                if (displayName && displayName.includes(' / ')) {
                    const deviceId = displayName.split(' / ')[1].trim();
                    
                    // Eğer araç aktif araçlar listesinde değilse, ikonu inactive yap
                    const isActive = activeVehicles.has(deviceId);
                    updateVehicleIcon(deviceId, isActive);
                    
                   
                }
            });

            // Aktif araçlar listesini temizle
            activeVehicles.clear();
        }, 30000);

        await startConnection();
    } catch (error) {
        console.error("SignalR başlatma hatası:", error);
        updateConnectionStatus("Bağlantı Hatası", "bg-red-500");
    }
}

async function startConnection() {
    try {
        updateConnectionStatus("Bağlanıyor...", "bg-yellow-500");
        await connection.start();
        console.log("SignalR bağlantısı başarılı");
        updateConnectionStatus("Bağlandı", "bg-green-500");
        reconnectAttempts = 0;
    } catch (error) {
        console.error("SignalR bağlantı hatası:", error);
        updateConnectionStatus("Bağlantı Hatası", "bg-red-500");
        
        // Yeniden bağlanmayı dene
        if (reconnectAttempts <= maxReconnectAttempts) {
            setTimeout(startConnection, 5000);
        }
    }
}

function updateConnectionStatus(text, className) {
    const statusDiv = document.getElementById("connectionState");
    if (!statusDiv) return;

    const status = {
        text: text,
        class: className,
        animate: text.includes('Bağlanıyor')
    };
    
    statusDiv.innerHTML = `
        <span class="w-1.5 h-1.5 rounded-full bg-white ${status.animate ? 'animate-pulse' : ''}"></span>
        <span class="flex-1 text-center">${status.text}</span>
    `;
    
    statusDiv.className = `px-3 py-1 rounded-full text-white text-xs ${status.class} w-24 flex items-center justify-center gap-2`;
}
function updateMetrics(data) {
    if (typeof data.speed !== 'undefined') lastSpeed = data.speed;
    if (typeof data.accelPct !== 'undefined') lastAccelPct = data.accelPct;
    if (typeof data.brakePct !== 'undefined') lastBrakePct = data.brakePct;
    if (typeof data.MotorRpm !== 'undefined') lastMotorRpm = data.MotorRpm;
    if (typeof data.SOC !== 'undefined') lastSoc = data.SOC;

    updateGauge('speed', lastSpeed, 180);
    updateGauge('acceleration', lastAccelPct, 100);
    updateGauge('brake', lastBrakePct, 100);
    updateGauge('rpm', lastMotorRpm, 3000);
    updateGauge('battery', lastSoc, 100);
}



function updateGauge(metricKey, value, maxValue) {
    const container = document.querySelector(`[data-metric="${metricKey}"]`);
    if (!container) {
        console.warn(`Container for '${metricKey}' not found`);
        return;
    }


    const valueElement = container.querySelector('.text-lg');
    const progressCircle = container.querySelector('circle.progress');

    const displayValue = value !== null ? Math.round(value) : 'Veri Yok';
    const percent = value !== null ? (value / maxValue) * 100 : 0;
    const circumference = 2 * Math.PI * 36;
    const offset = circumference * (1 - percent / 100);

    if (valueElement) valueElement.textContent = displayValue;
    if (progressCircle) {
        progressCircle.style.strokeDasharray = circumference;
        progressCircle.style.strokeDashoffset = offset;
    }
}






function initializeMap() {
    const defaultLocation = [39.793629, 32.432211];
    
    map = L.map('map', {
        center: defaultLocation,
        zoom: 13,
        zoomControl: false
    });

    L.control.zoom({ position: 'topleft' }).addTo(map);

    const layers = {
        'Standart Harita': L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }),
        'Gece Modu': L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '© OpenStreetMap contributors, © CARTO',
            maxZoom: 19
        }),
        'Uydu Görüntüsü': L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
            attribution: '© Esri, Maxar, Earthstar Geographics',
            maxZoom: 19
        })
    };

    layers['Standart Harita'].addTo(map);
    L.control.layers(layers).addTo(map);

    vehicles.forEach(vehicle => {
        const deviceId = vehicle.displayName.split(' / ')[1];
        
        // Sadece marker'ları oluştur, rota henüz oluşturma
        markers[deviceId] = L.marker([vehicle.latitude, vehicle.longitude], {
            icon: busIcon
        }).addTo(map);

        // Rotaları sabit mavi renk ile oluştur
        routes[deviceId] = L.polyline([], {
            color: '#2563eb', // Koyu mavi (Tailwind'in blue-600 rengi)
            weight: 3,
            opacity: 0.8
        });
    });
}

// Marker pozisyonunu güncelle
function updateVehiclePosition(deviceId, lat, lng) {
    const latitude = parseFloat(lat.toString().replace(",", "."));
    const longitude = parseFloat(lng.toString().replace(",", "."));
    
    const marker = markers[deviceId];
    if (marker) {
        marker.setLatLng([latitude, longitude]);
        
        if (selectedVehicleId === deviceId) {
            map.setView([latitude, longitude], map.getZoom(), {
                animate: true,
                duration: 0.5  // Animasyon süresini kısalttık
            });
        }

        // Rotayı güncelle
        const route = routes[deviceId];
        if (route) {
            const currentPath = route.getLatLngs();
            currentPath.push([latitude, longitude]);
            route.setLatLngs(currentPath);
        }
    }
}

function selectVehicle(vehicleId) {
    // Sayfayı yeniden yükle
    window.location.href = `/VehicleDetails?id=${parseInt(vehicleId)}`;
}

function updateVehicleMarker(vehicleId, latitude, longitude) {
    // Haritadaki ilgili aracın marker'ını güncelle
    // Bu fonksiyonu kendi harita implementasyonunuza göre uyarlamanız gerekir
    const marker = markers[vehicleId];
    if (marker) {
        marker.setLatLng([latitude, longitude]);
    }
}

function resetMetrics() {
    const keys = ['speed', 'acceleration', 'brake', 'rpm', 'battery'];
    keys.forEach(k => updateGauge(k, 0, 100));
}


function focusVehicle(vehicleId) {
    selectedVehicleId = vehicleId;
    
    // UI güncelleme
    document.querySelectorAll('[data-vehicle-id]').forEach(el => {
        el.classList.remove('bg-blue-50');
        el.classList.add('hover:bg-gray-50');
    });
    
    const selectedElement = document.querySelector(`[data-vehicle-id="${vehicleId}"]`);
    if (selectedElement) {
        selectedElement.classList.add('bg-blue-50');
        selectedElement.classList.remove('hover:bg-gray-50');

        // Seçili aracın device ID'sini al ve sakla
        const displayNameElement = selectedElement.querySelector('.font-medium');
        if (displayNameElement && displayNameElement.textContent) {
            const displayName = displayNameElement.textContent;
            if (displayName.includes(' / ')) {
                selectedDeviceId = displayName.split(' / ')[1].trim();
            } else {
                console.warn('Araç display name formatı hatalı:', displayName);
            }
        } else {
            console.warn('Araç display name elementi bulunamadı');
        }

        // Metrikleri sıfırla
        resetMetrics();

        // Önceki aktif rotayı kaldır
        if (activeRoute) {
            activeRoute.remove();
        }

        // Yeni rotayı aktifleştir
        if (routes[selectedDeviceId]) {
            routes[selectedDeviceId].addTo(map);
            activeRoute = routes[selectedDeviceId];

            // Mevcut konumları rotaya ekle
            const marker = markers[selectedDeviceId];
            if (marker) {
                const currentPos = marker.getLatLng();
                routes[selectedDeviceId].addLatLng([currentPos.lat, currentPos.lng]);
            }
        }

        // Haritada araca odaklan
        const marker = markers[selectedDeviceId];
        if (marker && map) {
            map.setView(marker.getLatLng(), 16, {
                animate: true,
                duration: 1
            });
        }

        // Seçili aracın verilerini tabloya ekle
        const selectedVehicleData = vehicles.find(v => v.id === vehicleId);
        if (selectedVehicleData) {
            updateMetricsTable(selectedVehicleData); // Burada uygun verileri ekleyin
        }
    }
}

function updateVehicleIcon(deviceId, isActive) {
    let updated = false;
    
    document.querySelectorAll('[data-vehicle-id]').forEach(vehicleElement => {
        const displayName = vehicleElement.querySelector('.font-medium')?.textContent;
        if (displayName && displayName.includes(' / ')) {
            const currentDeviceId = displayName.split(' / ')[1].trim();
            
            if (currentDeviceId === deviceId) {
                const iconImg = vehicleElement.querySelector('img[alt="Bus Icon"]');
                if (iconImg) {
                    const newSrc = `/images/bus-${isActive ? 'active' : 'inactive'}.png`;
                    const currentSrc = iconImg.src.split('/').pop(); // Mevcut dosya adını al
                    
                    // Sadece değişiklik gerekiyorsa güncelle
                    if (!iconImg.src.endsWith(newSrc)) {
                        iconImg.src = `/images/bus-${isActive ? 'active' : 'inactive'}.png`;
                        updated = true;
                    }
                }

                // Marker'ı güncelle
                const marker = markers[deviceId];
                if (marker) {
                    marker.setOpacity(isActive ? 1 : 0.5);
                }
            }
        }
    });

    // Sadece değişiklik olduğunda log yaz
    if (updated) {
    }
}

function updateMetricsTable(data) {
    const metricsTableBody = document.getElementById('metricsTableBody');

    // Hız verisi satırı
    let speedRow = metricsTableBody.querySelector('tr[data-metric="speed"]');
    if (!speedRow) {
        speedRow = document.createElement('tr');
        speedRow.setAttribute('data-metric', 'speed');
        metricsTableBody.appendChild(speedRow);
    }
    speedRow.innerHTML = `
        <td class="px-4 py-2">Speed</td>
        <td class="px-4 py-2">${data.Speed !== undefined ? data.Speed : (lastSpeed !== null ? lastSpeed : 'Veri Yok')}</td>
        <td class="px-4 py-2">${new Date().toLocaleString()}</td>
    `;
    speedRow.querySelector('td:nth-child(2)').classList.add('flash'); // Yanıp sönme efekti

    // İvme verisi satırı
    let accelRow = metricsTableBody.querySelector('tr[data-metric="acceleration"]');
    if (!accelRow) {
        accelRow = document.createElement('tr');
        accelRow.setAttribute('data-metric', 'acceleration');
        metricsTableBody.appendChild(accelRow);
    }
    accelRow.innerHTML = `
        <td class="px-4 py-2">Acceleration</td>
        <td class="px-4 py-2">${data.AccelerationPercentage !== undefined ? data.AccelerationPercentage : (lastAccelPct !== null ? lastAccelPct : 'Veri Yok')}</td>
        <td class="px-4 py-2">${'Son Gelen Sinyal' + new Date().toLocaleString()}</td>
    `;
    accelRow.querySelector('td:nth-child(2)').classList.add('flash'); // Yanıp sönme efekti

    // Motor RPM verisi satırı
    let motorRpmRow = metricsTableBody.querySelector('tr[data-metric="motorRpm"]');
    if (!motorRpmRow) {
        motorRpmRow = document.createElement('tr');
        motorRpmRow.setAttribute('data-metric', 'motorRpm');
        metricsTableBody.appendChild(motorRpmRow);
    }
    motorRpmRow.innerHTML = `
        <td class="px-4 py-2">Motor RPM</td>
        <td class="px-4 py-2">${data.MotorRpm !== undefined ? data.MotorRpm : (lastMotorRpm !== null ? lastMotorRpm : 'Veri Yok')}</td>
        <td class="px-4 py-2">${'Son Gelen Sinyal' + new Date().toLocaleString()}</td>
    `;
    motorRpmRow.querySelector('td:nth-child(2)').classList.add('flash'); // Yanıp sönme efekti

    // Batarya yüzdesi verisi satırı
    let socRow = metricsTableBody.querySelector('tr[data-metric="soc"]');
    if (!socRow) {
        socRow = document.createElement('tr');
        socRow.setAttribute('data-metric', 'soc');
        metricsTableBody.appendChild(socRow);
    }
    socRow.innerHTML = `
        <td class="px-4 py-2">Battery SOC</td>
        <td class="px-4 py-2">${data.SOC !== undefined ? data.SOC : (lastSoc !== null ? lastSoc : 'Veri Yok')}</td>
        <td class="px-4 py-2">${'Son Gelen Sinyal' + new Date().toLocaleString()}</td>
    `;
    socRow.querySelector('td:nth-child(2)').classList.add('flash'); // Yanıp sönme efekti

    
      

}

