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
using System.Threading.Tasks;

using slg.LibRobotMath;

namespace slg.LibMapping
{
    /// <summary>
    /// Displacement serves as a "3D Distance" describing difference between two positions.
    /// Used as universal container.
    /// See similar (and more specific) RelPosition
    /// </summary>
    public class Displacement : IDisplacement, ICloneable
    {
        public double dXMeters { get; private set; }
        public double dYMeters { get; private set; }
        public double dZMeters { get; private set; }
        public double dThetaRadians { get; private set; }

        public Displacement(double dx, double dy, double dz=0.0d, double dth=0.0d)
        {
            dXMeters = dx;
            dYMeters = dy;
            dZMeters = dz;
            dThetaRadians = dth;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2}):{3})", dXMeters, dYMeters, dZMeters, dThetaRadians);
        }

        #region ICloneable Members

        public object Clone()
        {
            Displacement ret = (Displacement)this.MemberwiseClone();  // shallow copy, only value types are cloned

            return ret;
        }

        #endregion // ICloneable Members

    }
}
