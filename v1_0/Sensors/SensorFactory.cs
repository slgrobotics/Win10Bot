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
using System.Linq;
using System.Text;

using slg.RobotBase;
using slg.RobotBase.Interfaces;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Ids;

namespace slg.Sensors
{
    public enum RangerSensorFactoryProducts
    {
        RangerSensorIR10_80,
        RangerSensorIR20_150,
        RangerSensorSonar,
        RangerSensorParkingSonar
    }

    /// <summary>
    /// produces one of RangerSensor instances - IR or Sonar type, passing parameters to its constructor
    /// </summary>
    public class RangerSensorFactory
    {
        public static IRangerSensor produceRangerSensor(RangerSensorFactoryProducts productType, string name, SensorPose pose, IAbstractRobotHardware brick, params Object[] args)
        {
            switch (productType)
            {
                case RangerSensorFactoryProducts.RangerSensorIR10_80:
                    return new RangerSensorIR10_80(name, pose, brick, (AnalogPinId)args[0], (int)args[1], (double)args[2]);

                case RangerSensorFactoryProducts.RangerSensorIR20_150:
                    return new RangerSensorIR20_150(name, pose, brick, (AnalogPinId)args[0], (int)args[1], (double)args[2]);

                case RangerSensorFactoryProducts.RangerSensorSonar:
                    return new RangerSensorSonar(name, pose, brick, (GpioPinId)args[0], (GpioPinId)args[1], (int)args[2], (double)args[3]);

                case RangerSensorFactoryProducts.RangerSensorParkingSonar:
                    return new RangerSensorParkingSonar(name, pose, brick, (int)args[0]);

                default:
                    throw new NotImplementedException("Error: RangerSensorFactory cannot produce ranger type " + productType);
            }
        }
    }
}
