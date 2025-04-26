let dashboardMap;
let dashboardMarkers = new Map();
let connection = null;

// Aktif araçları takip etmek için global bir Set kullanalım
const activeVehicles = new Set();

document.addEventListener('DOMContentLoaded', async function() {
    initializeDashboardMap();
    setView('map');
    await initializeSignalR();
});

async function initializeSignalR() {
    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/messageHub")
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Error)
            .build();

        connection.on("receiveTelemetry", (data) => {
            if (!data || !data.deviceId) return;
            updateVehicleStatus(data.deviceId, true);
            updateDashboardStats();
        });

        connection.on("vehicleStatusChanged", (data) => {
            if (!data || !data.deviceId) return;
            updateVehicleStatus(data.deviceId, data.isActive);
            updateDashboardStats();
        });

        await connection.start();
    } catch (error) {
        console.error("SignalR başlatma hatası:", error);
    }
}

function initializeDashboardMap() {
    const defaultLocation = [39.793629, 32.432211];
    
    dashboardMap = L.map('map', {
        center: defaultLocation,
        zoom: 13,
        zoomControl: false
    });

    L.control.zoom({ position: 'topleft' }).addTo(dashboardMap);

    const layers = {
        'Road Map': L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'),
        'Silver Map': L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth/{z}/{x}/{y}{r}.png'),
        'Hybrid Map': L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}')
    };

    layers['Road Map'].addTo(dashboardMap);
    L.control.layers(layers).addTo(dashboardMap);

    // Araçları haritaya ekle
    if (typeof dashboardVehicles !== 'undefined') {
        dashboardVehicles.forEach(vehicle => {
            if (vehicle.deviceId) {
                const marker = L.marker([vehicle.latitude, vehicle.longitude]).addTo(dashboardMap);
                dashboardMarkers.set(vehicle.deviceId, marker);
            }
        });
    }
}

function setView(viewType) {
    const mapView = document.getElementById('mapView');
    const vehiclesView = document.getElementById('vehiclesView');
    const buttons = document.querySelectorAll('.view-button');

    buttons.forEach(button => {
        if (button.dataset.view === viewType) {
            button.classList.add('bg-blue-600', 'text-white');
            button.classList.remove('bg-white', 'text-gray-600', 'hover:bg-gray-100');
        } else {
            button.classList.remove('bg-blue-600', 'text-white');
            button.classList.add('bg-white', 'text-gray-600', 'hover:bg-gray-100');
        }
    });

    if (viewType === 'map') {
        mapView.classList.remove('hidden');
        vehiclesView.classList.add('hidden');
        if (dashboardMap) dashboardMap.invalidateSize();
    } else {
        mapView.classList.add('hidden');
        vehiclesView.classList.remove('hidden');
    }
}

function updateVehicleStatus(deviceId, isActive) {
    document.querySelectorAll('[data-vehicle-id]').forEach(vehicleElement => {
        const deviceIdSpan = vehicleElement.querySelector('.font-medium')?.textContent;
        if (deviceIdSpan && deviceIdSpan.includes(' / ')) {
            const currentDeviceId = deviceIdSpan.split(' / ')[1].trim();
            if (currentDeviceId === deviceId) {
                const iconImg = vehicleElement.querySelector('img[alt="Bus Icon"]');
                if (iconImg) {
                    iconImg.src = `/images/bus-${isActive ? 'active' : 'inactive'}.png`;
                }
            }
        }
    });
}

function updateDashboardStats() {
    const totalVehicles = document.querySelectorAll('[data-vehicle-id]').length;
    const activeCount = SignalRManager.activeVehicles.size;
    const inactiveCount = totalVehicles - activeCount;
    
    // Metrik kartlarını güncelle
    document.querySelectorAll('.bg-white.p-2').forEach(card => {
        const title = card.querySelector('h3').textContent;
        const valueElement = card.querySelector('p');
        
        if (title === 'Total Vehicles') {
            valueElement.textContent = totalVehicles;
        }
        else if (title === 'Active Vehicles') {
            const percentage = totalVehicles > 0 ? Math.round((activeCount * 100) / totalVehicles) : 0;
            valueElement.textContent = `${percentage} %`;
            valueElement.classList.toggle('text-green-600', percentage > 0);
        }
        else if (title === 'Inactive Vehicles') {
            const percentage = totalVehicles > 0 ? Math.round((inactiveCount * 100) / totalVehicles) : 0;
            valueElement.textContent = `${percentage} %`;
            valueElement.classList.toggle('text-red-600', percentage > 0);
        }
    });
}

function createPopupContent(vehicle) {
    return `
        <div class="vehicle-popup">
            <div class="popup-header">
                <strong>${vehicle.plaka} / ${vehicle.serialNumber}</strong>
            </div>
            ...
        </div>
    `;
} 