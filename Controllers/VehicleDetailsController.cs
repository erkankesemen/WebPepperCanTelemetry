using Microsoft.AspNetCore.Mvc;
using WebPepperCan.Models;
using Microsoft.AspNetCore.Authorization;
using WebPepperCan.Data;

namespace WebPepperCan.Controllers
{
    [Authorize]
    public class VehicleDetailsController : Controller
    {
        public IActionResult Index()
        {   
           
            return View();
        }
    }
} 