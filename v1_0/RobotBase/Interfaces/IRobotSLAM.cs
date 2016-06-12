using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace slg.RobotBase.Interfaces
{
    public interface IRobotSLAM
    {
        void EvaluatePoseAndState(IBehaviorData behaviorData, IDrive driveController);
    }
}
