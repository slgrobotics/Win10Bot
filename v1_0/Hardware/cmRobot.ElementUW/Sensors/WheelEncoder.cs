using cmRobot.Element;
using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element.Internal;
using System;
using System.Collections.Generic;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Drive;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Ids;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Represents a WheelEncoder instance.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="WheelEncoder"]'/>
	public class WheelEncoder : QueryableComponentBase, IDistanceSensor, IWheelEncoder
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>WheelEncoderId</c> property.
		/// </summary>
		public const WheelEncoderId WheelEncoderIdDefault = WheelEncoderId.Encoder1;

		/// <summary>
		/// The default value for the <c>CountChangedThreshold</c> property.
		/// </summary>
		public const int CountChangedThresholdDefault = 1;

		/// <summary>
		/// The default value for the <c>DistanceChangedThreshold</c> property.
		/// </summary>
		public const double DistanceChangedThresholdDefault = 1;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>WheelEncoder</c> class.
		/// </summary>
		public WheelEncoder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>WheelEncoder</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public WheelEncoder(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties


		/// <summary>
		/// Id of the pin to which the encoder sensor is attached.
		/// </summary>
		public WheelEncoderId WheelEncoderId
		{
			get { return id; }
			set { id = value; }
		}

		/// <summary>
		/// The current wheel encoder count, or number of ticks.
		/// </summary>
		public int Count
		{
			get { return count;  }
		}

		/// <summary>
		/// The number of counts, or ticks, per revolution.  This value, as 
		/// well as  the <c>WheelDiameter</c> property, are used to calculate 
		/// distance.
		/// </summary>
		public int Resolution
		{
			get { return resolution;  }
			set { resolution = value;  }
		}

		/// <summary>
		/// Diameter of the wheel in inches.  This value, as 
		/// well as  the <c>Resolution</c> property, are used to calculate 
		/// distance.
		/// </summary>
		public double WheelDiameter
		{
			get { return wheelDiameter; }
			set { wheelDiameter = value; }
		}

		/// <summary>
		/// The distance traveled since the last call to <c>Clear</c>.
		/// </summary>
		public double Distance
		{
			get { return distance; }
		}

		/// <summary>
		/// Specifies the amount that <c>Count</c> must change before 
		/// the <c>CountChanged</c> event is signalled.
		/// </summary>
		public int CountChangedThreshold
		{
			get { return countThreshold; }

			set 
			{
				Toolbox.AssertInRange(value, 1, Int32.MaxValue);
				countThreshold = value; 
			}
		}

		/// <summary>
		/// Specifies the amount that <c>Distance</c> must change before 
		/// the <c>DistanceChanged</c> event is signalled.
		/// </summary>
		public double DistanceChangedThreshold
		{
			get { return distanceThreshold; }
			set { distanceThreshold = value; }
		}
		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>Distance</c> has changed by an amount greater 
		/// than <c>DistanceChangedThreshold</c>.
		/// </summary>
		public event HardwareComponentEventHandler DistanceChanged;

		/// <summary>
		/// Occurs when <c>Count</c> has changed by an amount greater 
		/// than <c>CountChangedThreshold</c>.
		/// </summary>
		public event HardwareComponentEventHandler CountChanged;

		#endregion


		#region Public Methods

		/// <summary>
		/// Clears the wheel encoders count, or ticks.
		/// </summary>
		public void Clear()
		{
			// we wait for the clear command to complete
			// before updating our state so that we don't
			// give any spurious events in case a query completes
			// in the middle
			Element.CommunicationTask.EnqueueCommJobAndWait(
				Priority.High, String.Format("clrenc {0}", (ushort)id));
			count = 0;
			lastCount = 0;
		}

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a wheel encoder sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("getenc {0}", (ushort)id);
		}

		/// <summary>
		/// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
		/// by <c>GenerateCommand</c>.
		/// </summary>
		/// <param name="response">The response string.</param>
		protected override void ProcessResponse(string response)
		{
			int count = Int32.Parse(response);
			OnCountChanged(count);
		}

		/// <summary>
		/// Occurs when the count for the Encoder input is updated.
		/// Signals <c>CountChanged</c>, if necessary.
		/// </summary>
		/// <param name="count"></param>
		protected void OnCountChanged(int count)
		{
			this.count = count;

            //Debug.WriteLine("OnCountChanged, cnt:{0} lCnt:{1} cThresh:{2}", count, lastCount, countThreshold);

			// if distance change exceeds threshold, then fire event 
			if (Math.Abs(count - lastCount) >= countThreshold)
			{
				lastCount = count;
				Element.SignalEvent(CountChanged, this);
			}

            double distance = (count / resolution) * wheelDiameter;
			OnSetDistance(distance);
		}

		/// <summary>
		/// Occurs when the distance read for the Encoder input changes.
		/// Signals <c>DistanceChanged</c>, if necessary.
		/// </summary>
		/// <param name="distance"></param>
		protected void OnSetDistance(double distance)
		{
			this.distance = distance;

			// if distance change exceeds threshold, then fire event 
			if (Math.Abs(distance - lastDistance) > distanceThreshold)
			{
				lastDistance = distance;
				Element.SignalEvent(DistanceChanged, this);
			}
		}

		#endregion


		#region Privates

		private WheelEncoderId id = WheelEncoderIdDefault;
		private int resolution = 0;
		private double wheelDiameter = 0;

		private int count = 0;
		private int lastCount = 0;
		private int countThreshold = CountChangedThresholdDefault;

		private double distance = 0;
		private double lastDistance = 0;
		private double distanceThreshold = DistanceChangedThresholdDefault;

		#endregion

	}
}
