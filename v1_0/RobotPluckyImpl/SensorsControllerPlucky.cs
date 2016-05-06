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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using slg.RobotBase;
using slg.RobotBase.Interfaces;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Drive;
using slg.Sensors;
using slg.RobotMath;

namespace slg.RobotPluckyImpl
{
    /// <summary>
    /// Sensor Controller for Arduino based robot (Plucky)
    /// </summary>
    public class SensorsControllerPlucky : ISensorsController
    {
        //
        // some parameters to tune up traffic via 19200 baud serial line to Hardware Brick (i.e. Element board).
        // *SamplingIntervalMs      - how often the board is commanded to read (sample) sensors by a low priority serial command stream. A PC timer triggers "read" command at that rate.
        // *SensitivityThresholdCm  - nothing happens unless the value read during sampling is different from the previous sample by this amount. If it exceeds, a "DistanceChanged" handler is called.
        // decreasing the Sampling Interval may contribute to serial line conjestion.
        // At 19200 baud (1.5KB per second) and 20 bytes per command - it is only ~70 commands per second, shared with PWM commands to Drive.
        //
        private const int rangersSamplingIntervalMs = 100;
        private const double rangersSensitivityThresholdCm = 1.0d;    // in chosen units. We set "element.Units = Units.Metric" in ElementRobot, so it is centimeters here.

        //private const int encodersSamplingIntervalMs = 100;  - we use mainLoopCycleMs instead
        private const int encodersSensitivityThresholdTicks = 1;    // report every tick for precision. We don't increase traffic by doing that. 

        //private const int CompassSamplingIntervalMs = 100;
        //private const short CompassSensitivityThreshold = 1;    // in units 1..255

        private const int batterySamplingIntervalMs = 2000; // "frequency" in milliseconds. 
        private const double batterySensitivityThresholdVolts = 0.0d; // report every reading.

        // properties:
        public ISensorsData currentSensorsData { get; set; }

        public object currentSensorsDataLock = new Object();

        // injected parameters:
        private IAbstractRobotHardware hardwareBrick { get; set; }

        private double mainLoopCycleMs;     // must be set when controller is created, to match main loop cycle

        // ranger sensors - Parking Sonar:
        private IRangerSensor ParkingSonar;

        //private ICompassCMPS03 Compass;

        //private IAnalogSensor Ahrs;

        private RPiCamera RPiCameraSensor;

        /// <summary>
        /// all Ranger Sensors are in this list for easy access to Pose and min/max ranges from SensorsData:
        /// </summary>
        public Dictionary<string, IRangerSensor> RangerSensors = new Dictionary<string, IRangerSensor>();

        public SensorsControllerPlucky(IAbstractRobotHardware brick, double _mainLoopCycleMs)
        {
            hardwareBrick = brick;
            mainLoopCycleMs = _mainLoopCycleMs;

            RPiCameraSensor = new RPiCamera("RPiCamera", 9097);
        }

        /// <summary>
        /// we can create sensors here, but cannot send commands before bridge_CommunicationStarted is called in PluckyTheRobot
        /// for example, encoder.Clear() will hang.
        /// </summary>
        public async Task InitSensors(CancellationTokenSource cts)
        {
            // see C:\Projects\Serializer_3_0\ExampleApps\AnalogSensorExample\AnalogSensorExample\Form1.cs

            // Note: the Element board communicates at 19200 Baud, which is roughly 1.5 kbytes/sec
            //       Comm link is busy with motor commands and has to be responsive to encoders, for odometry to work.
            //       Sensors must carefully adjust their demands by setting UpdateFrequency and Enabled properties.

            // *********************** Parking Sonar:
            SensorPose spParkingSonar = new SensorPose() { X = 0.0d, Y = 0.0d, Theta = 0.0d };
            ParkingSonar = RangerSensorFactory.produceRangerSensor(RangerSensorFactoryProducts.RangerSensorParkingSonar, "ParkingSonar", spParkingSonar,
                                                                                        hardwareBrick, rangersSamplingIntervalMs);
            ParkingSonar.distanceChangedEvent += new EventHandler<RangerSensorEventArgs>(RangerDistanceChangedEvent);
            RangerSensors.Add(ParkingSonar.Name, ParkingSonar);

            // *********************** wheel encoders - roughly XXX ticks per wheel revolution, XXX ticks per meter.
            encoderLeft = CreateWheelEncoder(hardwareBrick, WheelEncoderId.Encoder2, (int)mainLoopCycleMs, encodersSensitivityThresholdTicks);
            encoderRight = CreateWheelEncoder(hardwareBrick, WheelEncoderId.Encoder1, (int)mainLoopCycleMs, encodersSensitivityThresholdTicks);

            //Compass = hardwareBrick.produceCompassCMPS03(0x60, CompassSamplingIntervalMs, CompassSensitivityThreshold);
            //Compass.HeadingChanged += new HardwareComponentEventHandler(Compass_HeadingChanged);

            // arduino based AHRS - PWM DAC to pin 4:
            //Ahrs = hardwareBrick.produceAnalogSensor(AnalogPinId.A4, CompassSamplingIntervalMs, CompassSensitivityThreshold);
            //Ahrs.AnalogValueChanged += new HardwareComponentEventHandler(Ahrs_ValueChanged);

            await RPiCameraSensor.Open(cts);
            RPiCameraSensor.TargetingCameraTargetsChanged += RPiCameraSensor_TargetsChanged;

            batteryVoltage = CreateBatteryVoltageMeter(hardwareBrick, batterySamplingIntervalMs, batterySensitivityThresholdVolts);

            batteryVoltage.Enabled = true;  // slow update rate, leave it turned on

            SonarsEnabled = true;
            EncodersEnabled = true;

            //CompassEnabled = true;    // compass no good inside concrete buildings, use gyro instead
            //CompassEnabled = false;
            //Ahrs.Enabled = true;
            RPiCameraSensor.Enabled = true;

            currentSensorsData = new SensorsData() { RangerSensors = this.RangerSensors };
        }

        public void Close()
        {
            Debug.WriteLine("SensorsControllerPlucky: Close()");

            RPiCameraSensor.TargetingCameraTargetsChanged -= RPiCameraSensor_TargetsChanged;
            RPiCameraSensor.Close();
        }

        void RPiCameraSensor_TargetsChanged(object sender, TargetingCameraEventArgs args)
        {
            //Debug.WriteLine("RPi Camera Event: " + args);

            // On Raspberry Pi:
            //      pixy.blocks[i].signature    The signature number of the detected object (1-7)
            //      pixy.blocks[i].x       The x location of the center of the detected object (0 to 319)
            //      pixy.blocks[i].y       The y location of the center of the detected object (0 to 199)
            //      pixy.blocks[i].width   The width of the detected object (1 to 320)
            //      pixy.blocks[i].height  The height of the detected object (1 to 200)

            // Field of view:
            //     goal 45 degrees  left  x=10
            //                    middle  x=160
            //     goal 45 degrees right  x=310
            //
            //     goal 30 degrees  up    y=10
            //                    middle  y=90
            //     goal 30 degrees down   y=190
            //

            if (args.width * args.height > 500) // only large objects count
            {
                int bearing = GeneralMath.map(args.x, 0, 320, -45, 45);
                int inclination = GeneralMath.map(args.y, 0, 200, 30, -30);

                //Debug.WriteLine("RPi: bearing=" + bearing + "  inclination: " + inclination);

                lock (currentSensorsDataLock)
                {
                    SensorsData sensorsData = new SensorsData(this.currentSensorsData);

                    sensorsData.TargetingCameraBearingDegrees = bearing;
                    sensorsData.TargetingCameraInclinationDegrees = inclination;
                    sensorsData.TargetingCameraTimestamp = args.timestamp;

                    //Debug.WriteLine(sensorsData.ToString());

                    this.currentSensorsData = sensorsData;
                }
            }
        }

        //void Ahrs_ValueChanged(IHardwareComponent sender)
        //{
        //    IAnalogSensor a = (IAnalogSensor)sender;
        //    int val = a.AnalogValue; // 0v = 0, 5V = 470 approx

        //    /*
        //     Yaw    PWM     val
        //     --------------------

        //     */

        //    double heading;
        //    if (val > 507)
        //    {
        //        heading = GeneralMath.map(val, 508, 1024, 0, 180);
        //    }
        //    else
        //    {
        //        heading = GeneralMath.map(val, 0, 507, 180, 360);
        //    }

        //    //Debug.WriteLine("Ahrs: Value=" + val + "  heading: " + heading);

        //    lock (currentSensorsDataLock)
        //    {
        //        SensorsData sensorsData = new SensorsData(this.currentSensorsData);
        //        sensorsData.CompassHeadingDegrees = heading;
        //        //Debug.WriteLine(sensorsData.ToString());

        //        this.currentSensorsData = sensorsData;
        //    }
        //}

        //void Compass_HeadingChanged(IHardwareComponent sender)
        //{
        //    // The cmps03 command queries a Devantech CMPS03 Electronic compass module attached to the Serializer’s I2C port.
        //    // The current heading is returned in Binary Radians, or BRADS. To convert BRADS to DEGREES, multiply BRADS by 360/255 (~1.41).

        //    ICompassCMPS03 cmps = (ICompassCMPS03)sender;
        //    double heading = Math.Round(cmps.Heading * 360.0d / 255.0d, 1);

        //    Debug.WriteLine("Compass: heading=" + heading);

        //    lock (currentSensorsDataLock)
        //    {
        //        SensorsData sensorsData = new SensorsData(this.currentSensorsData);
        //        sensorsData.CompassHeadingDegrees = heading;
        //        //Debug.WriteLine(sensorsData.ToString());

        //        this.currentSensorsData = sensorsData;
        //    }
        //}

        // called every 50ms from the main loop
        public void Process()
        {
        }

        void RangerDistanceChangedEvent(object sender, RangerSensorEventArgs e)
        {
            //Debug.WriteLine("Ranger: " + e.Name + "         RangeMeters=" + e.RangeMeters);

            lock (currentSensorsDataLock)
            {
                SensorsData sensorsData = new SensorsData(this.currentSensorsData);

                switch (e.Name)
                {
                    case "ParkingSonar":
                        sensorsData.RangerFrontRightMeters = e.RangeMeters[0];
                        sensorsData.RangerFrontLeftMeters = e.RangeMeters[1];
                        sensorsData.RangerRearRightMeters = e.RangeMeters[2];
                        sensorsData.RangerRearLeftMeters = e.RangeMeters[3];

                        sensorsData.RangerFrontRightMetersTimestamp = sensorsData.RangerFrontLeftMetersTimestamp = 
                        sensorsData.RangerRearRightMetersTimestamp = sensorsData.RangerRearLeftMetersTimestamp = e.TimeTicks;
                        break;

                    default:
                        throw new NotImplementedException("Error: RangerDistanceChangedEvent: unknown name " + e.Name);
                }

                //Debug.WriteLine(sensorsData.ToString());

                this.currentSensorsData = sensorsData;
            }
        }

        // if "Enabled" is false, do not sample or update - save the traffic across serial line:

        public bool EncodersEnabled  { set { encoderLeft.Enabled = encoderRight.Enabled = value; } }

        public bool SonarsEnabled { set { ParkingSonar.Enabled = value; } }

        //public bool CompassEnabled    { set { Compass.Enabled = value; } }

        #region Battery Voltage related

        private IAnalogSensor batteryVoltage;

        private IAnalogSensor CreateBatteryVoltageMeter(IAbstractRobotHardware brick, int frequency, double thresholdVolts)
        {
            // analog pin 5 in Element board is internally tied to 1/3 of the supply voltage level.
            // The 5.0d is 5V, microcontroller's ADC reference and 1024 is range.
            // on Plucky the battery voltage goes through 1/3 divider and comes to Arduino Pin 3, which is reported as Pin 5 to here.
            // see PluckyWheels.ino sketch
            int threshold = (int)Math.Round((thresholdVolts * 1024.0d) / (3.0d * 5.0d));

            Debug.WriteLine("CreateBatteryVoltageMeter()   threshold=" + threshold);

            // analog pin 5 is internally tied to 1/3 of the supply voltage level
            IAnalogSensor bv = brick.produceAnalogSensor(AnalogPinId.A5, frequency, threshold);

            bv.AnalogValueChanged += new HardwareComponentEventHandler(batteryVoltage_ValueChanged);

            return bv;
        }

        void batteryVoltage_ValueChanged(IHardwareComponent sender)
        {
            lock (currentSensorsDataLock)
            {
                SensorsData sensorsData = new SensorsData(this.currentSensorsData);

                IAnalogSensor bv = sender as IAnalogSensor;

                Debug.Assert(bv != null, "SensorsControllerPlucky: batteryVoltage_ValueChanged(): AnalogSensor must be non-null");

                // analog pin 5 in Element board is internally tied to 1/3 of the supply voltage level.
                // The 5.0d is 5V, microcontroller's ADC reference and 1024 is range.
                // on Plucky the battery voltage goes through 1/3 divider and comes to Arduino Pin 3, which is reported as Pin 5 to here.
                // see PluckyWheels.ino sketch

                double voltage = 3.0d * (bv.AnalogValue * 5.0d) / 1024.0d;

                sensorsData.BatteryVoltage = voltage;   // also sets sensorsData.BatteryVoltageTimestamp

                this.currentSensorsData = sensorsData;
            }

            Debug.WriteLine(this.currentSensorsData.ToString());
        }

        #endregion // Battery Voltage related

        #region Wheel Encoders related

        private IWheelEncoder encoderLeft;
        private IWheelEncoder encoderRight;

        /*
			// Set these properties based on your robot drivetrain/encoder config:
			encoder.Resolution = 44;       // 44 ticks per revolution
			encoder.WheelDiameter = 2.0;   // 2 inch wheel diameter
			encoder.WheelEncoderId = WheelEncoderId.Encoder1;
      
			// Clear encoder value:
			encoder.Clear();
      
			// If you're interested in absolute distance, then use this:
			encoder.DistanceChangedThreshold = 0.25;  // 0.25 inches
			encoder.DistanceChanged += new 
			HardwareComponentEventHandler(encoder_DistanceChanged);
      
			// If you're interested in ticks, then use this:
			encoder.CountChangedThreshold = 10;  // 10 ticks;
			encoder.CountChanged += new 
			HardwareComponentEventHandler(encoder_CountChanged);
         */

        private IWheelEncoder CreateWheelEncoder(IAbstractRobotHardware brick, WheelEncoderId id, int frequency, int threshold)
        {
            IWheelEncoder encoder = brick.produceWheelEncoder(id, frequency, 1, threshold);
            // Frequency  milliseconds
            // Resolution must be set to avoid divide by zero in WheelEncoder.cs
            // CountChangedThreshold ticks

            encoder.CountChanged += new HardwareComponentEventHandler(encoder_CountChanged);

            return encoder;
        }

        void encoder_CountChanged(IHardwareComponent sender)
        {
            lock (currentSensorsDataLock)
            {
                SensorsData sensorsData = new SensorsData(this.currentSensorsData);

                IWheelEncoder encoder = sender as IWheelEncoder;

                Debug.Assert(encoder != null, "SensorsControllerPlucky: encoder_CountChanged(): Encoder must be non-null");

                if (encoder.WheelEncoderId == WheelEncoderId.Encoder2)
                {
                    sensorsData.WheelEncoderLeftTicks = encoder.Count;
                }
                else if (encoder.WheelEncoderId == WheelEncoderId.Encoder1)
                {
                    sensorsData.WheelEncoderRightTicks = encoder.Count;
                }

                //Debug.WriteLine(sensorsData.ToString());

                this.currentSensorsData = sensorsData;
            }
        }

        #endregion // Wheel Encoders related
    }
}
