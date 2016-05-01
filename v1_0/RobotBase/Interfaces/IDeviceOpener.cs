using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace slg.RobotBase.Interfaces
{
    public interface IDeviceOpener
    {
        bool IsDeviceOpen { get; }

        DateTime LastConnectionTime { get; }

        Task<bool> OpenDevice(string deviceId);

        Task<bool> CloseDevice();
    }
}
