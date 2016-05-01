using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace slg.RobotBase.Data
{
    /// <summary>
    /// convenience class to pass port names and IDs together. Easy to bing to UI dropdowns.
    /// </summary>
    public class SerialPortTuple
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
