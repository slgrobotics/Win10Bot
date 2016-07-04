using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using slg.LibMapping;
using slg.RobotBase.Interfaces;
using slg.RobotAbstraction.Sensors;

namespace slg.LibRobotSLAM
{
    public class RobotSLAM : IRobotSLAM
    {
        /// <summary>
        /// takes current state/pose, new sensors data, odometry and everything else - and evaluates new state and pose
        /// </summary>
        /// <param name="behaviorData"></param>
        public void EvaluatePoseAndState(IBehaviorData behaviorData, IDrive driveController)
        {
            // consider compass or AHRS reading for determining direction:
            if(behaviorData.sensorsData.CompassHeadingDegrees != null)
            {
                behaviorData.robotPose.direction.heading = behaviorData.sensorsData.CompassHeadingDegrees;
            }

            // use GPS data to update position:
            if (behaviorData.sensorsData.GpsFixType != GpsFixTypes.None)
            {
                behaviorData.robotPose.geoPosition.Lat = behaviorData.sensorsData.GpsLatitude;
                behaviorData.robotPose.geoPosition.Lng = behaviorData.sensorsData.GpsLongitude;
            }

            // utilize odometry data: 
            if (driveController.hardwareBrickOdometry != null)
            {
                // already calculated odometry comes from the hardware brick (i.e. Arduino)

                IOdometry odom = driveController.hardwareBrickOdometry;

                behaviorData.robotPose.XMeters = odom.XMeters;
                behaviorData.robotPose.YMeters = odom.YMeters;
                behaviorData.robotPose.ThetaRadians = odom.ThetaRadians;
            }
            else
            {
                // we have wheels encoders data and must calculate odometry here:
                long[] encoderTicks = new long[] { behaviorData.sensorsData.WheelEncoderLeftTicks, behaviorData.sensorsData.WheelEncoderRightTicks };

                driveController.OdometryCompute(behaviorData.robotPose, encoderTicks);
            }
        }
    }
}
