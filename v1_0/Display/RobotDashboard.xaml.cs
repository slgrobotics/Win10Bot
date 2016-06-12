using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using slg.RobotBase.Interfaces;
//using slg.ControlDevices;
//using slg.LibMapping;
using slg.LibRobotMath;
using System.Diagnostics;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace slg.Display
{
    public sealed partial class RobotDashboard : UserControl, IRobotDashboard
    {
        public RobotDashboard()
        {
            this.InitializeComponent();
        }

        public void DisplayRobotPose(IRobotPose robotPose)
        {
            //Debug.WriteLine("RobotDashboard: DisplayRobotPose()   pose: " + robotPose.ToString());

            //xLast = robotPose.X;
            //yLast = robotPose.Y;
            //thetaLast = robotPose.Theta;

            //mainWindow1.AddMapable(new TrackPointData(xLast, yLast, Colors.Red));
        }

        public void DisplayRobotState(IRobotState robotState, IRobotPose robotPose)
        {
            //Debug.WriteLine("RobotDashboard: DisplayRobotState()   state: " + robotState.ToString());

            robotStateDashboard1.StateDataBlock.Post(robotState);
            robotStateDashboard1.PoseDataBlock.Post(robotPose);
        }

        public void DisplayRobotJoystick(IJoystickSubState joystickData)
        {
            //Debug.WriteLine("RobotDashboard: DisplayRobotJoystick()   joystick: " + joystickData.ToString());

            rumblePad2Dashboard1.JoystickDataBlock.Post(joystickData);
        }

        public void DisplayRobotSensors(ISensorsData sensorsData)
        {
            //Debug.WriteLine("RobotDashboard: DisplayRobotSensors()   sensors: " + sensorsData.ToString());

            sensorsDashboard1.SensorsDataBlock.Post(sensorsData);

            /*
            RobotPoseBase robotPose = new RobotPoseBase() { X = xLast, Y = yLast, Theta = thetaLast };
            PoseBase obstaclePoseRel = new PoseBase() { X = 0.0d, Y = 0.0d, Theta = 0.0d };    // relative to sensor

            double distanceMeters = sensorsData.IrLeftMeters;    // range 10...80 cm
            IRangerSensor sensor = sensorsData.RangerSensors["IrLeft"];

            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                //PoseBase sensorPose = robotPose * irLeftSensor.Pose;        // sensor Pose absolute
                //PoseBase obstaclePose = sensorPose * obstaclePoseRel;       // obstacle Pose absolute

                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Magenta));
            }

            distanceMeters = sensorsData.IrRightMeters;   // range 10...80 cm
            sensor = sensorsData.RangerSensors["IrRight"];
            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Green));
            }

            distanceMeters = sensorsData.IrFrontMeters;   // range 10...80 cm
            sensor = sensorsData.RangerSensors["IrFront"];
            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Blue));
            }

            distanceMeters = sensorsData.IrRearMeters;   // range 10...80 cm
            sensor = sensorsData.RangerSensors["IrRear"];
            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Blue));
            }

            distanceMeters = sensorsData.SonarLeftMeters;   // range 3...250 cm
            sensor = sensorsData.RangerSensors["SonarLeft"];
            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Purple));
            }

            distanceMeters = sensorsData.SonarRightMeters;   // range 3...250 cm
            sensor = sensorsData.RangerSensors["SonarRight"];
            if (sensor.InRange(distanceMeters))
            {
                obstaclePoseRel.X = distanceMeters;
                PoseBase obstaclePose = robotPose * sensor.Pose * obstaclePoseRel;       // obstacle Pose absolute

                mainWindow1.AddMapable(new ObstacleData(obstaclePose.X, obstaclePose.Y, Colors.Purple));
            }
            */
        }
    }
}
