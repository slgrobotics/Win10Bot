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

namespace slg.RobotBase.Data
{
    /// <summary>
    /// data container helping to coordinate operation among behaviors in a behaviors combo ("chain")
    /// </summary>
    public class BehaviorsCoordinatorData
    {
        private string enablingRequest;
        private string grabbingBehaviorName;

        /// <summary>
        /// a request for specific operation to be performed by the behavior chain (ex. "Escape").
        /// a behavior that recognizes the request would fire on, possibly grabbing the chain.
        /// </summary>
        public string EnablingRequest
        {
            get { return enablingRequest; }
            set { enablingRequest = value == null ? string.Empty : value; EnablingRequestTimestamp = DateTime.Now; }
        }

        public DateTime EnablingRequestTimestamp { get; private set; }

        public void ClearEnablingRequest()
        {
            EnablingRequest = string.Empty;
        }

        /// <summary>
        /// a way of "grabbing" the behavior chain outside of normal priority order.
        /// when a grabbing behavior's name is filled here the behavior will be called until it fires OFF.
        /// Other behaviors will be blocked. The GrabbingBehaviorName will be cleared by dispatcher based on FireOn flag being reset.
        /// Normal firing on does not need to fill GrabbingBehaviorName, grabbing is a special mechanism.
        /// </summary>
        public string GrabbingBehaviorName
        {
            get { return grabbingBehaviorName; }
            set { grabbingBehaviorName = value == null ? string.Empty : value; GrabbingBehaviorTimestamp = DateTime.Now; }
        }

        public DateTime GrabbingBehaviorTimestamp { get; private set; }    // timestamp when the behaviors chain was grabbed

        public void ClearGrabbingBehavior()
        {
            GrabbingBehaviorName = string.Empty;
        }

        public BehaviorsCoordinatorData()
        {
            ClearEnablingRequest();
            ClearGrabbingBehavior();
        }
    }
}
