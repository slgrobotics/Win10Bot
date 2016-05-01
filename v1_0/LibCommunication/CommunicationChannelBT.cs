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
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using System.Diagnostics;

using slg.RobotAbstraction;

namespace slg.LibCommunication
{
    // Bluetooth 4 comm channel
    // TODO: opening device seems to work, but the rest has not been tried.

    public class CommunicationChannelBT : ICommunicationChannel
    {
        public string Name { get; }
        public string Parameters { get; set; }
        public string NewLineIn { get; set; }
        public string NewLineOut { get; set; }

        private DeviceInformationCollection deviceCollection;
        private DeviceInformation selectedDevice;
        private RfcommDeviceService deviceService;

        public string deviceName = "Roomba2"; // Specify the device name to be selected; You can find the device name from the webb under bluetooth 

        StreamSocket streamSocket = new StreamSocket();

        public string errorStatusText;

        public async Task Open()
        {
            Debug.WriteLine("CommunicationChannelBT: Open():  BT deviceName: {0}   port: {1}", deviceName, Name);

            await InitializeRfcommServer();

            if (await ConnectToDevice())
            {
                Debug.WriteLine("OK: ConnectToDevice() successful");
            }
        }

        public void DiscardInBuffer()
        {
        }

        public async Task<string> ReadLine()
        {
            return string.Empty;
        }

        public async Task WriteLine(string str)
        {
            Send(str);
        }

        public void Close()
        {
            ;
        }

        #region Bluetooth operations

        private async Task InitializeRfcommServer()
        {
            try
            {
                string device1 = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
                deviceCollection = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(device1);

            }
            catch (Exception exception)
            {
                //errorStatus.Visibility = Visibility.Visible;
                errorStatusText = exception.Message;
                Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
            }
        }

        private async Task<bool> ConnectToDevice()
        {
            foreach (var item in deviceCollection)
            {
                if (item.Name == deviceName)
                {
                    selectedDevice = item;
                    break;
                }
            }

            if (selectedDevice == null)
            {
                //errorStatus.Visibility = Visibility.Visible;
                errorStatusText = "Cannot find the device specified; Please check the device name";
                Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
                return false;
            }

            deviceService = await RfcommDeviceService.FromIdAsync(selectedDevice.Id);

            if (deviceService != null)
            {
                //connect the socket   
                try
                {
                    await streamSocket.ConnectAsync(deviceService.ConnectionHostName, deviceService.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                    //errorStatus.Visibility = Visibility.Visible;
                    errorStatusText = "Cannot connect bluetooth device:" + ex.Message;
                    Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
                    return false;
                }
            }
            else
            {
                //errorStatus.Visibility = Visibility.Visible;
                errorStatusText = "Didn't find the specified bluetooth device named " + deviceName;
                Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
                return false;
            }

            Debug.WriteLine("OK: connected to bluetooth device named " + deviceName);

            return true;
        }

        public async void Send(string s)
        {
            if (deviceService != null)
            {
                //send data
                string sendData = s;
                if (string.IsNullOrEmpty(sendData))
                {
                    //errorStatus.Visibility = Visibility.Visible;
                    errorStatusText = "Please specify the string you are going to send";
                    Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
                }
                else
                {
                    DataWriter dwriter = new DataWriter(streamSocket.OutputStream);
                    UInt32 len = dwriter.MeasureString(sendData);
                    dwriter.WriteUInt32(len);
                    dwriter.WriteString(sendData);
                    await dwriter.StoreAsync();
                    await dwriter.FlushAsync();
                }

            }
            else
            {
                //errorStatus.Visibility = Visibility.Visible;
                errorStatusText = "Bluetooth is not connected correctly!";
                Debug.WriteLine("CommunicationChannelBT: " + errorStatusText);
            }
        }

        #endregion // Bluetooth operations
    }
}
