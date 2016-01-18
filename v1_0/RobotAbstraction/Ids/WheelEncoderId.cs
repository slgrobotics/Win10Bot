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
using System.Text;

namespace slg.RobotAbstraction.Ids
{
	/// <summary>
    /// This identifies the wheel encoders supported by the Hardware Brick.
	/// </summary>
	public enum WheelEncoderId 
	{ 
		/// <summary>
        /// Wheel encoder 0 on Element. Right side.
		/// </summary>
		Encoder1 = 1,

		/// <summary>
        /// Wheel encoder 1 on Element. Left side.
		/// </summary>
		Encoder2 = 2,
	}
}
