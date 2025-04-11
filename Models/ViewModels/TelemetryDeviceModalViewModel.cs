using System;
using System.Collections.Generic;
using WebPepperCan.Data;

namespace WebPepperCan.Models.ViewModels
{
    public class TelemetryDeviceModalViewModel
    {
        public bool AktifMi { get; set; }
        public string ModalId => AktifMi ? "editTelemetryDeviceModal" : "newTelemetryDeviceModal";
        public TelemetryDevice Device { get; set; }
        public IEnumerable<Organization> Organizations { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
    }
}
