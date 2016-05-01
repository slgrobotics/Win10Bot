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

using slg.RobotBase.Interfaces;
using slg.RobotBase.Data;
using slg.LibRuntime;

namespace slg.RobotBase.Bases
{
    /*
     * see http://www.lejos.org/nxt/nxj/tutorial/Behaviors/BehaviorProgramming.htm
     * 
        Only one behavior can be active and in control of the robot at any time.
        Each behavior has a fixed priority.
        Each behavior can determine if it should take control.
        The active behavior has higher priority than any other behavior that should take control.
     */

    public delegate bool BehaviorActivateConditionDelegate(IBehaviorData bd);

    public delegate bool BehaviorDeactivateConditionDelegate(IBehaviorData bd);

    public delegate bool BehaviorTerminateConditionDelegate(IBehaviorData bd);

    public abstract class BehaviorBase : RobotTask
    {
        public IBehaviorData behaviorData { get; set; }

        public ISpeaker speaker;

        public BehaviorActivateConditionDelegate BehaviorActivateCondition = null;          // if not set - never activate

        public BehaviorDeactivateConditionDelegate BehaviorDeactivateCondition = null;      // if not set - immediately deactivate

        public BehaviorTerminateConditionDelegate BehaviorTerminateCondition = null;        // if not set - never terminate

        public bool MustActivate
        {
            get { return BehaviorActivateCondition != null && BehaviorActivateCondition(behaviorData); }
        }

        public bool MustDeactivate
        {
            get { return BehaviorDeactivateCondition == null || BehaviorDeactivateCondition != null && BehaviorDeactivateCondition(behaviorData); }
        }

        public bool MustTerminate
        {
            get { return BehaviorTerminateCondition != null && BehaviorTerminateCondition(behaviorData); }
        }

        public bool MustExit = false;
        private bool firedOn = false;   // activated

        /// <summary>
        /// by setting FiredOn a behavior declares that it wants control.
        /// whether it is granted control will be decided based on priorities and grabbing (via FiredBehaviorType)
        /// </summary>
        public bool FiredOn
        {
            get { return firedOn; }
            set { firedOn = value; if (value) { FiredOnTimestamp = DateTime.Now; } }
        }

        public DateTime FiredOnTimestamp { get; private set; }

        private static BehaviorsCoordinatorData coordinatorData = new BehaviorsCoordinatorData();

        public static BehaviorsCoordinatorData getCoordinatorData() { return coordinatorData; }

        public BehaviorBase()
        {
            FiredOn = false;
        }

        public BehaviorBase(IDriveGeometry ddg)
            : this()
        {
            driveGeometry = ddg;
        }

        public override void Close()
        {
            MustExit = true;    // when MustExit is reacted upon in the Execute(), FiredOn must be reset
            base.Close();
        }

        #region Grab management - inhibit and suppress all other behaviors

        /// <summary>
        /// returns true if the chain is grabbed by another behavior
        /// </summary>
        /// <returns></returns>
        protected bool GrabByOther()
        {
            string gbn = getCoordinatorData().GrabbingBehaviorName;
            return !string.IsNullOrWhiteSpace(gbn) && gbn != this.name;
        }

        /// <summary>
        /// returns true if the chain is grabbed by this behavior
        /// </summary>
        /// <returns></returns>
        protected bool IsGrabbedByMe()
        {
            return getCoordinatorData().GrabbingBehaviorName == this.name;
        }

        // https://en.wikipedia.org/wiki/Subsumption_architecture
        // Inhibition signals block signals from reaching actuators or AFSMs, and suppression signals blocks or replaces the inputs to layers or their AFSMs

        /// <summary>
        /// grab the chain by this behavior - inhibit and suppress all other behaviors.
        /// </summary>
        public void SetGrabByMe()
        {
            getCoordinatorData().GrabbingBehaviorName = this.name;
        }

        /// <summary>
        /// grab the chain by this behavior, if there is no other grab in effect.
        /// if successful, inhibits and suppresses all other behaviors.
        /// </summary>
        /// <returns>true if grab was successful</returns>
        public bool TrySetGrabByMe()
        {
            if (IsGrabbedByMe())
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(getCoordinatorData().GrabbingBehaviorName))
            {
                getCoordinatorData().GrabbingBehaviorName = this.name;
                return true;
            }

            return false;
        }

        /// <summary>
        /// clears grab of the chain by a behavior, uninhibit and un-suppress all other behaviors
        /// </summary>
        public void ClearGrab()
        {
            getCoordinatorData().ClearGrabbingBehavior();
        }

        /// <summary>
        /// clears grab of the chain only it was established by this behavior
        /// if successful, uninhibit and un-suppress all other behaviors
        /// </summary>
        public void ClearGrabIfMine()
        {
            if (getCoordinatorData().GrabbingBehaviorName == this.name)
            {
                getCoordinatorData().ClearGrabbingBehavior();
            }
        }

        #endregion // Grab management - inhibit and suppress all other behaviors

        #region Properties and helper methods frequently needed in derived classes

        protected IDriveGeometry driveGeometry; // can be null if not needed in derived class

        /// <summary>
        /// linear velocity, meters per second
        /// </summary>
        protected double requestedVelocity;

        /// <summary>
        /// angular velocity, radians per second, positive - left
        /// </summary>
        protected double requestedOmega;

        /// <summary>
        /// computes linear velocity, meters per second, based on robot drive geometry
        /// see IDriveInputs
        /// </summary>
        /// <param name="speed">-100...100</param>
        /// <returns>linear velocity, meters per second</returns>
        protected double ToVelocity(double speed)
        {
            return speed * driveGeometry.speedToVelocityFactor;
        }

        /// <summary>
        /// computes angular velocity, radians per second, based on robot drive geometry
        /// see IDriveInputs
        /// </summary>
        /// <param name="turn">-100...100, positive - right</param>
        /// <returns>angular velocity, radians per second, positive - left</returns>
        protected double ToOmega(double turn)
        {
            return -turn * driveGeometry.turnToOmegaFactor;
        }

        /// <summary>
        /// sets Velocity and Omega into behaviorData.driveInputs, based on speed and turn
        /// stores calculated values in requestedVelocity, requestedOmega
        /// </summary>
        /// <param name="speed">-100...100</param>
        /// <param name="turn">-100...100, positive - right</param>
        protected void setSpeedAndTurn(double speed = 0.0d, double turn = 0.0d)
        {
            if (driveGeometry == null)
            {
                // not a good thing to allow it, but useful for Stop 
                requestedVelocity = 0.0d;
                requestedOmega = 0.0d;
            }
            else
            {
                requestedVelocity = ToVelocity(speed);
                requestedOmega = ToOmega(turn);
            }

            setVelocityAndOmega(requestedVelocity, requestedOmega);
        }

        /// <summary>
        /// sets Velocity and Omega into behaviorData.driveInputs 
        /// </summary>
        /// <param name="velocity">meters per second</param>
        /// <param name="omega">radians per second, positive - left</param>
        protected void setVelocityAndOmega(double velocity = 0.0d, double omega = 0.0d)
        {
            DriveInputsBase correctedDriveSpeed = new DriveInputsBase();      // a stop command unless modified 

            correctedDriveSpeed.velocity = velocity;
            correctedDriveSpeed.omega = omega;         // positive - left

            behaviorData.driveInputs = correctedDriveSpeed;
        }

        #endregion // Properties and helper methods frequently needed in derived classes
    }
}
