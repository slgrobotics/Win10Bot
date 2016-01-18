using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Ids
{
    /// <summary>
    /// This identifies the Units supported by the Element.
    /// </summary>
    public enum Units
    {
        /// <summary>
        /// Metric sensor units
        /// </summary>
        Metric = 0,

        /// <summary>
        /// English sensor units.
        /// </summary>
        English = 1,

        /// <summary>
        /// Raw sensor values
        /// </summary>
        Raw = 2
    }
}
