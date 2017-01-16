using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using slg.LibSensors;
using slg.RobotBase.Interfaces;

namespace slg.RobotShortyImpl
{
    public class SensorsDataShorty : SensorsData
    {
        public SensorsDataShorty()
            : base()
        {
        }

        public SensorsDataShorty(ISensorsData src)
            : base(src)
        {
        }

        public override string ToString()
        {
            // "Timestamp" properties are updated when data changes, not when a valid reading comes in.
            bool hasBatteryLevel = (DateTime.Now.Ticks - this.BatteryVoltageTimestamp) / TimeSpan.TicksPerSecond < 60L; // bad if battery sensor didn't report in 60 seconds

            string batteryLevel = hasBatteryLevel ? string.Format("{0:0.00}V ({1:0.00}V per cell)", BatteryVoltage, BatteryVoltage / 3.0d) : "Unknown";

            bool isCameraValid = IsTargetingCameraDataValid();

            return ""

            + string.Format("IR:   left: {0:0.00}   right: {1:0.00}   front: {2:0.00}   rear: {3:0.00}     ",
                                    IrLeftMeters, IrRightMeters, IrFrontMeters, IrRearMeters
                                )
            + string.Format("\r\nSONARS FRONT:   left: {0:0.00}   right: {1:0.00}   ENCODERS:   left: {2}   right: {3}    ",
                                    RangerFrontLeftMeters, RangerFrontRightMeters,
                                    WheelEncoderLeftTicks, WheelEncoderRightTicks
                                )
            + string.Format("\r\nBATTERY: {0}   COMPASS: {1:0}    Pixy Camera: {2:0} {3:0}    ",
                                    batteryLevel, CompassHeadingDegrees,
                                    (isCameraValid ? TargetingCameraBearingDegrees : null), (isCameraValid ? TargetingCameraInclinationDegrees : null)
                                );
        }
    }
}
