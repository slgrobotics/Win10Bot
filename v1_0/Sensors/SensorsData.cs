/*
 * Copyright (c) 2016..., Sergei Grichine   http://trackroamer.com
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *    
 * this is a no-warranty no-liability permissive license - you do not have to publish your changes,
 * although doing so, donating and contributing is always appreciated
 */

using System;
using System.Collections.Generic;

using slg.RobotBase.Interfaces;

namespace slg.Sensors
{
    /// <summary>
    /// container for all data collected from sensors.
    /// Warning: "Timestamp" properties are updated when data changes, not when a valid reading comes in.
    /// </summary>
    public class SensorsData : ISensorsData
    {
        public double IrLeftMeters { get; set; }
        public long IrLeftMetersTimestamp { get; set; }

        public double IrRightMeters { get; set; }
        public long IrRightMetersTimestamp { get; set; }

        public double IrFrontMeters { get; set; }
        public long IrFrontMetersTimestamp { get; set; }

        public double IrRearMeters { get; set; }
        public long IrRearMetersTimestamp { get; set; }

        public double SonarLeftMeters { get; set; }
        public long SonarLeftMetersTimestamp { get; set; }

        public double SonarRightMeters { get; set; }
        public long SonarRightMetersTimestamp { get; set; }

        // all Ranger Sensors are in this Dictionary (by name) for easy access to Pose and min/max ranges from SensorsData:
        public IDictionary<string, IRangerSensor> RangerSensors { get; set; }

        // no need for timestamps for encoders. They are processed on each tick.
        public long WheelEncoderLeftTicks       { get; set; }
        public long WheelEncoderRightTicks      { get; set; }

        // Compass reading - for example, CMPS03 Compass connected via I2C
        public double CompassHeadingDegrees { get; set; }

        // Pixy camera bearing to detected object:
        public double? PixyCameraBearingDegrees { get; set; }
        public double? PixyCameraInclinationDegrees { get; set; }
        public long PixyCameraTimestamp { get; set; }

        private double _BatteryVoltage;
        public double BatteryVoltage { get { return _BatteryVoltage; } set { _BatteryVoltage = value; BatteryVoltageTimestamp = DateTime.Now.Ticks; } }
        public long BatteryVoltageTimestamp     { get; private set; }

        public DateTime timestamp { get; set; }   // instance timestamp - will be updated on any copy as well

        /// <summary>
        /// default constructor leaves sensor values at 0.0d, but fills the timestamp.
        /// </summary>
        public SensorsData()
        {
            timestamp = DateTime.Now;

            // in case we are debugging with sensors off, set the values to safe distance,
            // to avoid triggering obstacle avoidance:

            IrLeftMeters = IrRightMeters = IrFrontMeters = IrRearMeters = 10.0d;

            SonarLeftMeters = SonarRightMeters = 10.0d;

            CompassHeadingDegrees = 0.0d;

            BatteryVoltage = 12.6d;
            BatteryVoltageTimestamp = DateTime.MinValue.Ticks;
        }

        /// <summary>
        /// makes a copy of the source, and fills the instance timestamp with current time.
        /// preserves individual timestamps.
        /// </summary>
        /// <param name="src"></param>
        public SensorsData(ISensorsData src)
            : this()
        {
            this.RangerSensors = src.RangerSensors;

            this.IrLeftMeters = src.IrLeftMeters;
            this.IrLeftMetersTimestamp = src.IrLeftMetersTimestamp;

            this.IrRightMeters = src.IrRightMeters;
            this.IrRightMetersTimestamp = src.IrRightMetersTimestamp;

            this.IrFrontMeters = src.IrFrontMeters;
            this.IrFrontMetersTimestamp = src.IrFrontMetersTimestamp;

            this.IrRearMeters = src.IrRearMeters;
            this.IrRearMetersTimestamp = src.IrRearMetersTimestamp;

            this.SonarLeftMeters = src.SonarLeftMeters;
            this.SonarLeftMetersTimestamp = src.SonarLeftMetersTimestamp;

            this.SonarRightMeters = src.SonarRightMeters;
            this.SonarRightMetersTimestamp = src.SonarRightMetersTimestamp;

            this.WheelEncoderLeftTicks = src.WheelEncoderLeftTicks;
            this.WheelEncoderRightTicks = src.WheelEncoderRightTicks;

            this.CompassHeadingDegrees = src.CompassHeadingDegrees;

            this.PixyCameraBearingDegrees = src.PixyCameraBearingDegrees;
            this.PixyCameraInclinationDegrees = src.PixyCameraInclinationDegrees;
            this.PixyCameraTimestamp = src.PixyCameraTimestamp;

            this._BatteryVoltage = src.BatteryVoltage;
            this.BatteryVoltageTimestamp = src.BatteryVoltageTimestamp;

            //this.timestamp = src.timestamp;
        }

        public bool IsPixyDataValid()
        {
            return (double)(DateTime.Now.Ticks - PixyCameraTimestamp) / (double)TimeSpan.TicksPerSecond < 1.0d;
        }

        public override string ToString()
        {
            // "Timestamp" properties are updated when data changes, not when a valid reading comes in.
            bool hasBatteryLevel = (DateTime.Now - this.timestamp).TotalSeconds < 60.0d;    // bad if no sensors changed in 60 seconds
 
            string batteryLevel = hasBatteryLevel ? string.Format("{0:0.00}V ({1:0.00}V per cell)", BatteryVoltage, BatteryVoltage / 3.0d) : "Unknown";

            bool isPixyValid = IsPixyDataValid();

            return string.Format("IR:   left: {0:0.00}   right: {1:0.00}   front: {2:0.00}   rear: {3:0.00}     SONAR:   left: {4:0.00}   right: {5:0.00}     ENCODERS:   left: {6}   right: {7}   \r\nBATTERY: {8}   COMPASS: {9:0}   Pixy: {10:0} {11:0}",
                                    IrLeftMeters, IrRightMeters, IrFrontMeters, IrRearMeters, SonarLeftMeters, SonarRightMeters, WheelEncoderLeftTicks, WheelEncoderRightTicks, batteryLevel, CompassHeadingDegrees,
                                    (isPixyValid ? PixyCameraBearingDegrees : null), (isPixyValid ? PixyCameraInclinationDegrees : null));
        }
    }
}
