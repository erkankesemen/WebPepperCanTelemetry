using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebPepperCan.Data;
using WebPepperCan.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Threading;
using System.Text.Json;

namespace WebPepperCan.Hubs
{
    public class MessageHub : Hub
    {
        private readonly ILogger<MessageHub> _logger;
        private readonly IUserSessionService _sessionService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private const int VEHICLE_TIMEOUT_SECONDS = 30; // Araç 30 saniye veri göndermezse offline sayılır
        private static ConcurrentDictionary<string, DateTime> _lastMessageTimes = new ConcurrentDictionary<string, DateTime>();
        private static Timer _checkVehicleStatusTimer; // static olarak tanımla
        private static readonly object _timerLock = new object();

        public MessageHub(
            ILogger<MessageHub> logger,
            IUserSessionService sessionService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _sessionService = sessionService;
            _serviceScopeFactory = serviceScopeFactory;

            // Timer'ı sadece bir kez başlat
            lock (_timerLock)
            {
                if (_checkVehicleStatusTimer == null)
                {
                    _checkVehicleStatusTimer = new Timer(CheckVehicleStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    await _sessionService.CreateSessionAsync(
                        userId,
                        Context.ConnectionId,
                        httpContext?.Connection?.RemoteIpAddress?.ToString(),
                        httpContext?.Request?.Headers["User-Agent"].ToString()
                    );
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bağlantı kurulurken hata oluştu");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                await _sessionService.EndSessionAsync(Context.ConnectionId);
                if (exception != null)
                {
                    _logger.LogError(exception, "Bağlantı hatası");
                }
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bağlantı kapatılırken hata oluştu");
            }
        }

        public async Task SendVehicleData(VehicleData data)
        {
            try 
            {
                // Telemetri verilerini gönder
                await Clients.All.SendAsync("receiveTelemetry", new
                {
                    deviceId = data.DeviceId,
                    speed = data.Speed,
                    brake= data.BrakePct,
                    accelPct = data.AccelPct,
                    timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                    latitude = data.Latitude,
                    longitude = data.Longitude
                });

                // Aracın aktif olduğunu bildir
                await Clients.All.SendAsync("vehicleStatusChanged", new
                {
                    deviceId = data.DeviceId,
                    isActive = true
                });

                // Son aktivite zamanını güncelle
                _lastMessageTimes.AddOrUpdate(data.DeviceId, DateTime.UtcNow, (key, old) => DateTime.UtcNow);

                // Dashboard'ı güncelle
                var stats = await GetVehicleStatsAsync(data.DeviceId);
                await Clients.All.SendAsync("updateDashboardStats", stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR veri işleme hatası");
            }
        }

        private async void CheckVehicleStatus(object state)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var inactiveDevices = new List<string>();

                foreach (var kvp in _lastMessageTimes.ToList()) // ToList() ile güvenli kopya al
                {
                    try
                    {
                        var deviceId = kvp.Key;
                        var lastMessageTime = kvp.Value;

                        if ((now - lastMessageTime).TotalSeconds > VEHICLE_TIMEOUT_SECONDS)
                        {
                            var vehicle = await context.Vehicles
                                .Include(v => v.TelemetryDevice)
                                .FirstOrDefaultAsync(v => 
                                    v.TelemetryDevice != null && 
                                    v.TelemetryDevice.SerialNumber == deviceId);

                            if (vehicle != null && vehicle.AktifMi)
                            {
                                vehicle.AktifMi = false;
                                vehicle.UpdatedAt = now;
                                inactiveDevices.Add(deviceId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Araç durumu kontrol edilirken hata oluştu");
                    }
                }

                if (inactiveDevices.Any())
                {
                    await context.SaveChangesAsync();
                    
                    foreach (var deviceId in inactiveDevices)
                    {
                        try
                        {
                            await Clients.All.SendAsync("vehicleStatusChanged", new
                            {
                                deviceId = deviceId,
                                isActive = false
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Araç durumu bildirimi gönderilemedi: {deviceId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç durumu kontrol döngüsünde hata oluştu");
            }
        }

        private async Task<object> GetVehicleStatsAsync(string deviceId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var vehicle = await context.Vehicles
                .Include(v => v.TelemetryDevice)
                .FirstOrDefaultAsync(v => v.TelemetryDevice.SerialNumber == deviceId);

            if (vehicle != null)
            {
                var vehicles = await context.Vehicles
                    .Where(v => v.OrganizationId == vehicle.OrganizationId)
                    .ToListAsync();

                int totalVehicles = vehicles.Count;
                int activeVehicles = vehicles.Count(v => v.AktifMi);
                int inactiveVehicles = totalVehicles - activeVehicles;

                return new
                {
                    totalVehicles = totalVehicles,
                    activeVehicles = $"{(totalVehicles > 0 ? (activeVehicles * 100 / totalVehicles) : 0)} %",
                    inactiveVehicles = $"{(totalVehicles > 0 ? (inactiveVehicles * 100 / totalVehicles) : 0)} %"
                };
            }

            return null;
        }
    }

    public class VehicleData
    {
        public string DeviceId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public double Speed { get; set; }
        public double AccelPct { get; set; }
        public bool HasCanData { get; set; }
        public bool BrakePct { get; set; }
    }
}