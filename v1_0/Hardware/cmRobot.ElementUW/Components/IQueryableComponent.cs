using System;


namespace cmRobot.Element.Components
{

	/// <summary>
    /// An interface that represents a <c>ElementComponent</c> 
	/// that has a readable value or values.
	/// </summary>
	public interface IQueryableComponent
	{

		/// <summary>
		/// The frequency, in milliseconds, at which the 
        /// <c>ElementComponet</c>'s values are queried.
		/// </summary>
		int UpdateFrequency { get; set; }

		/// <summary>
		/// Turn querying this component on/off.
		/// </summary>
		bool Enabled { get; set; }

	}

}
