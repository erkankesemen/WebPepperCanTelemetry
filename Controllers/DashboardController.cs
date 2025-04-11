using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebPepperCan.Data;           // ApplicationDbContext için
using WebPepperCan.Hubs;          // DashboardHub için
using WebPepperCan.Models;        // Model sınıfları için

namespace WebPepperCan.Controllers  // Namespace düzeltmesi
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public DashboardController(ApplicationDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _context.Vehicles
                    .Include(v => v.Organization)
                    .Select(v => new { v.Id, v.Plaka, v.Organization.KurumAdi })
                    .ToListAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Veriler alınırken hata oluştu." });
            }
        }
    }
} 