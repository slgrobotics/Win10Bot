using System;
using System.Collections.Generic;
using System.Text;
using cmRobot.Element.Internal;
using cmRobot.Element.Components;

namespace cmRobot.Element.Controllers
{
    /// <summary>
    /// Represents a Bipolar Stepper Motor Controller:
    /// </summary>
    public class BipolarStepperMotorController : ElementComponent
    {
        #region CTORS
        /// <summary>
        /// Initializes a new instance of the <c>BipolarStepperMotorController</c> class.
        /// </summary>
        public BipolarStepperMotorController()
        {
            stepCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("step {0}:{1}:{2}", _direction, _speed, _steps);
            });

            sweepCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("sweep {0}:{1}", _speed, _steps);
            });

            stopCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("stop");
            });
        }

        /// <summary>
        /// Initializes a new instance of the <c>BipolarStepperMotorController</c> class, attaching
        /// it to the specified <c>Element</c> instance.
        /// </summary>
        public BipolarStepperMotorController(Element element)
			: this()
		{
			this.Element = element;
        }
        #endregion

        #region PROPERTIES
        /// <summary>
        /// Specifies the number of steps that the Bipolar Stepper Motor will step.
        /// </summary>
        public int Steps
        {
            get { return _steps; }
            set { _steps = value; }
        }

        /// <summary>
        /// Specifies the stepping speed (0-100) of the Bipolar Stepper Motor.
        /// </summary>
        public int Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        /// <summary>
        /// Specifies the rotational direction (CW or CCW) that the Bipolar Stepper Motor will step.
        /// </summary>
        public Direction RotationalDirection
        {
            get { return _direction; }
            set { _direction = value; }
        }
        #endregion

        #region PUBLIC INTERACE

        /// <summary>
        /// Rotational direction of the motor (looking at output motor shaft end):
        /// </summary>
        public enum Direction 
		{ 
			/// <summary>
			/// Clockwise Direction
			/// </summary>
			cw = 0, 

			/// <summary>
			/// Counter-Clockwise Direction
			/// </summary>
			ccw = 1 
		};

        /// <summary>
        /// Commands the bipolar stepper motor to start stepping based on the currently
        /// configured Steps, Speed, and RotationalDirection properties.
        /// </summary>
        public void Step()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, stepCommJob);
        }

        /// <summary>
        /// Commands the bipolar stepper motor to start sweeping, based on the currently
        /// configured Steps and Speed property.  The motor will start sweeping in a 
        /// CW direction for half of the steps, and then sweep in an alternating CCW/CW
        /// direction for the full amount of steps.   So, if you set the Steps property 
        /// to 100, then the sweep command will sweep the motor 50 steps in a CW direction,
        /// then 100 steps in a CCW direction, then 100 steps in CW direction, over and over
        /// until it is stopped.
        /// </summary>
        public void Sweep()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, sweepCommJob);
        }

        /// <summary>
        /// Stops the bipolar stepper motor from stepping or sweeping.
        /// </summary>
        public void Stop()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, stopCommJob);
        }
        #endregion

        #region PRIVATES
        private int _steps;
        private int _speed;
        private Direction _direction;
        private ICommunicationJob stepCommJob;
        private ICommunicationJob sweepCommJob;
        private ICommunicationJob stopCommJob;
        #endregion
    }
}
