using WebPepperCan.Data;
using System.Collections.Generic;

namespace WebPepperCan.Models.ViewModels
{
    public class VehicleModalViewModel
    {
        public bool IsEdit { get; set; }
        public string ModalId => IsEdit ? "editVehicleModal" : "newVehicleModal";
        public Vehicle Vehicle { get; set; }
        public IEnumerable<Organization> Organizations { get; set; }
    }
} 