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
using System.Threading.Tasks;

using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Drive;

namespace slg.RobotAbstraction
{
    public enum CommunicationPortType
    {
        Serial, BT4
    }

    public interface IAbstractRobotHardware
    {
        ISharpGP2D12 produceSharpGP2D12(AnalogPinId pin, int updateFrequency, double distanceChangedThreshold);

        ISonarSRF04 produceSonarSRF04(GpioPinId triggerPin, GpioPinId outputPin, int updateFrequency, double distanceChangedThreshold);

        IAnalogSensor produceAnalogSensor(AnalogPinId pin, int updateFrequency, double valueChangedThreshold);

        ICompassCMPS03 produceCompassCMPS03(int i2CAddress, int updateFrequency, double headingChangedThreshold);

        IWheelEncoder produceWheelEncoder(WheelEncoderId wheelEncoderId, int updateFrequency, int resolution, int countChangedThreshold);

        void PumpEvents();

        #region Serial Device related

        /*
        On Windows IoT you have to use Windows.Devices.SerialCommunication namespace to access serial ports.
        You have to have Windows 10 IoT Extension SDK (installer should be bundled with the Windows 10 image file for you board,
        you have to register there for downloads to be availble) installed and added as reference to be able to access that namespace.
        Keep in mind though that if you use Raspberry Pi onboard UART will be inaccessible anyway, as it is used for kernel debugger.
        */
        string PortName { get; set; }

        int BaudRate { get; set; }

        CommunicationPortType PortType { get; set; }

        event HardwareComponentEventHandler CommunicationStarted;

        event CommunicationChannelEventHandler CommunicationsStarting;

        event CommunicationChannelEventHandler CommunicationsStopping;

        Task StartCommunication();

        Task StopCommunication();

        #endregion // Serial Device related
    }
}
