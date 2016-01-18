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


namespace slg.RobotAbstraction.Ids
{
    /// <summary>
    /// GPIO pins for Arduino (0-13) and Element. 
    /// Note that Element has its own GpioPinId.cs which is directly mapping to this one. Pins 10,11,12 there have special assignments.
    /// </summary>
    public enum GpioPinId
    {
        Pin0 = 0, Pin1 = 1, Pin2 = 2, Pin3 = 3, Pin4 = 4, Pin5 = 5, Pin6 = 6, Pin7 = 7, Pin8 = 8, Pin9 = 9,
        Pin10 = 10, Pin11 = 11, Pin12 = 12, Pin13 = 13,
    }
}
