using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using slg.LibSensors;
using slg.RobotBase.Interfaces;

namespace slg.RobotPluckyImpl
{
    public class SensorsDataPlucky : SensorsData
    {
        public SensorsDataPlucky()
            : base()
        {
        }

        public SensorsDataPlucky(ISensorsData src)
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

            + string.Format("\r\nSONARS FRONT:   left: {0:0.00}   right: {1:0.00}    SONARS REAR:   left: {2:0.00}   right: {3:0.00}    ENCODERS:   left: {4}   right: {5}    ",
                                    RangerFrontLeftMeters, RangerFrontRightMeters, RangerRearLeftMeters, RangerRearRightMeters,
                                    WheelEncoderLeftTicks, WheelEncoderRightTicks
                                )
            + string.Format("\r\nBATTERY: {0}   COMPASS: {1:0}    Camera: {2:0} {3:0}    ",
                                    batteryLevel, CompassHeadingDegrees,
                                    (isCameraValid ? TargetingCameraBearingDegrees : null), (isCameraValid ? TargetingCameraInclinationDegrees : null)
                                )
            + string.Format("\r\nGPS Fix: {0}   NSat: {1}   HDOP: {2}   AgeMs: {3}    Lat: {4:N9}   Lon: {5:N9}",
                                    GpsFixType, GpsNsat, GpsHdop, FixAgeMs, GpsLatitude, GpsLongitude
                                );
        }
    }
}
