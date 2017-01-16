using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using slg.LibMapping;
using slg.RobotBase.Interfaces;
using slg.RobotAbstraction.Sensors;
using slg.LibRobotMath;

namespace slg.LibRobotSLAM
{
    public class RobotSLAM : IRobotSLAM
    {
        public bool useOdometryHeading = false;

        /// <summary>
        /// takes current state/pose, new sensors data, odometry and everything else - and evaluates new state and pose
        /// </summary>
        /// <param name="behaviorData"></param>
        public void EvaluatePoseAndState(IBehaviorData behaviorData, IDrive driveController)
        {
            bool wasHeadingSetByCompass = false;
            IDisplacement displacement = null;

            // consider compass or AHRS reading for determining direction:
            //if (!useOdometryHeading && behaviorData.sensorsData.CompassHeadingDegrees != null)
            //{
            //    behaviorData.robotPose.direction.heading = behaviorData.sensorsData.CompassHeadingDegrees;
            //    wasHeadingSetByCompass = true;
            //}

            // use GPS data to update position:
            if (behaviorData.sensorsData.GpsFixType != GpsFixTypes.None)
            {
                // we need to know displacement in meters:
                displacement = new GeoPosition(behaviorData.sensorsData.GpsLongitude, behaviorData.sensorsData.GpsLatitude) - behaviorData.robotPose.geoPosition;

                behaviorData.robotPose.geoPosition.Lat = behaviorData.sensorsData.GpsLatitude;
                behaviorData.robotPose.geoPosition.Lng = behaviorData.sensorsData.GpsLongitude;
            }
            else
            {
                // utilize odometry data
                // we might have wheels encoders data - it will be ignored if using hardwareBrickOdometry:
                long[] encoderTicks = new long[] { behaviorData.sensorsData.WheelEncoderLeftTicks, behaviorData.sensorsData.WheelEncoderRightTicks };

                // compute dXMeters  dYMeters  dThetaRadians:
                displacement = driveController.OdometryCompute(behaviorData.robotPose, encoderTicks);
            }

            if (displacement != null)
            {
                // update robotPose: XMeters  YMeters, Lat, Lng,  ThetaRadians, heading:
                behaviorData.robotPose.translate(displacement.dXMeters, displacement.dYMeters);
                behaviorData.robotPose.rotate(displacement.dThetaRadians);
            }

            if (!wasHeadingSetByCompass)
            {
                // rotate according to odometry:
                SetCurrentHeadingByTheta(behaviorData);
            }
        }

        private void SetCurrentHeadingByTheta(IBehaviorData behaviorData)
        {
            // XMeters and Lng axes are "horizontal right pointed". YMeters and Lat are "vertical up". North (0 degrees) is up.
            // Theta 0 is along the X axis, so we need a 90 degrees offset here.
            // Positive Theta is counterclockwise, positive heading - clockwise. That's why the minus sign.
            double currentHeading = DirectionMath.toDegrees(-behaviorData.robotPose.ThetaRadians + Math.PI / 2);

            if (behaviorData.robotPose.direction != null)
            {
                behaviorData.robotPose.direction.heading = currentHeading;
            }
            else
            {
                behaviorData.robotPose.direction = new Direction() {
                    heading = currentHeading,
                    bearing = behaviorData.robotPose.direction == null ? null : behaviorData.robotPose.direction.bearing
                };
            }
        }
    }
}
