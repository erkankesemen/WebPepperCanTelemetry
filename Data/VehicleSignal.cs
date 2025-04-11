using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WebPepperCan.Data
{
    public class VehicleSignal
    {
        public int Id { get; set; }

        [ForeignKey("TelemetryDeviceId")]
        public int TelemetryDeviceId { get; set; }
        public DateTime TimeStamp {get; set;}
        public int Channel { get; set; }
        public string FrameId { get; set; }
        public string Direction { get; set; }
        public string Type { get; set; }
        public string Lenght { get; set; }
        public string HexData { get; set; }
        public virtual TelemetryDevice TelemetryDevice { get; set; }
        
    }
}