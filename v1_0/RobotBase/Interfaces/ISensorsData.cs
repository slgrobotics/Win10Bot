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

namespace slg.RobotBase.Interfaces
{
    public interface ISensorsData
    {
        // Rangers' timestamps come from the first handler and are preserved.

        // typical IR sensors location - middle on each side:
        double IrLeftMeters  { get; set; }
        long IrLeftMetersTimestamp { get; set; }

        double IrRightMeters { get; set; }
        long IrRightMetersTimestamp { get; set; }

        double IrFrontMeters { get; set; }
        long IrFrontMetersTimestamp { get; set; }

        double IrRearMeters { get; set; }
        long IrRearMetersTimestamp { get; set; }

        // typical sonars or parking sonar locations - corners: 
        double RangerFrontLeftMeters  { get; set; }
        long RangerFrontLeftMetersTimestamp { get; set; }

        double RangerFrontRightMeters { get; set; }
        long RangerFrontRightMetersTimestamp { get; set; }

        double RangerRearLeftMeters { get; set; }
        long RangerRearLeftMetersTimestamp { get; set; }

        double RangerRearRightMeters { get; set; }
        long RangerRearRightMetersTimestamp { get; set; }

        // all Ranger Sensors are in this Dictionary (by name) for easy access to Pose and min/max ranges from SensorsData:
        IDictionary<string, IRangerSensor> RangerSensors { get; set; }

        long WheelEncoderLeftTicks  { get; }
        long WheelEncoderRightTicks { get; }

        // Compass reading - for example, CMPS03 Compass connected via I2C
        double? CompassHeadingDegrees { get; set; }

        // Targeting camera (i.e. Pixy Camera) bearing to a detected object:
        double? TargetingCameraBearingDegrees { get; set; }
        double? TargetingCameraInclinationDegrees { get; set; }
        long TargetingCameraTimestamp { get; set; }
        bool IsTargetingCameraDataValid();

        // battery timestamp is set by private setter.
        double BatteryVoltage { get; }
        long BatteryVoltageTimestamp { get; }

        // overall timestamp is updated on any copy as well.
        DateTime timestamp    { get; set; }
    }
}
