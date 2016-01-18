using System;
using System.Collections.Generic;
using System.Text;
using cmRobot.Element.Components;
using cmRobot.Element.Internal;
using System.Threading;

namespace cmRobot.Element.Sensors
{
	/// <summary>
	/// Represents a Generic I2 Device.
	/// </summary>
    public class I2CDevice : ElementComponent
    {
        #region CTORS

        /// <summary>
        /// Initializes a new instance of the <c>I2CDevice</c> class.
        /// </summary>
        public I2CDevice() 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>I2CDevice</c> class, attaching
        /// it to the specified <c>Element</c> instance.
        /// </summary>
        /// <param name="element"></param>
        public I2CDevice(Element element)
        {
            this.Element = element;
        }

        #endregion

        #region PROPERTIES

        /// <summary>
        /// I2C address of the device which the instance of this class will
        /// communicate with, and which is connected to the Element.
        /// </summary>
        public byte I2CAddress
        {
            get { return _address; }
            set { _address = value; }
        }

        /// <summary>
        /// String property used to store a read command to send to the I2C device
        /// </summary>
        public string ReadCommand
        {
            get { return _readCommand; }
            set { _readCommand = value; }
        }

        /// <summary>
        /// String property used to store a write command to send to the I2C device
        /// </summary>
        public string WriteCommand
        {
            get { return _writeCommand; }
            set { _writeCommand = value; }
        }

        #endregion

        #region PUBLIC READ INTERFACE

        /// <summary>
        /// Sends value of ReadCommand property to the I2C device located at address I2CAddress:
        /// </summary>
        public string Read()
        {
            string cmd = String.Format("i2c r {0} {1}", _address, _readCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Sends readCommand string parameter to the I2C device located at address I2CAddress:
        /// </summary>
        public string Read(string readCommand)
        {
            string cmd = String.Format("i2c r {0} {1}", _address, readCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Sends value of ReadCommand property to the I2C device address located within the address parameter:
        /// </summary>
        public string Read(byte address)
        {
            string cmd = String.Format("i2c r {0} {1}", address, _readCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Sends readCommand string to the I2C device address located within the address parameter:
        /// </summary>
        public string Read(byte address, string readCommand)
        {
            string cmd = String.Format("i2c r {0} {1}", address, readCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }
        #endregion

        #region PUBLIC WRITE INTERFACE
        /// <summary>
        /// Writes value of WriteCommand property to the I2C device located at address I2CAddress:
        /// </summary>
        public string Write()
        {
            string cmd = String.Format("i2c w {0} {1}", _address, _writeCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Writes writeCommand string parameter to the I2C device located at address I2CAddress:
        /// </summary>
        public string Write(string writeCommand)
        {
            string cmd = String.Format("i2c w {0} {1}", _address, writeCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Writes value of WriteCommand property to the I2C device address located within the address parameter:
        /// </summary>
        public string Write(byte address)
        {
            string cmd = String.Format("i2c w {0} {1}", address, _writeCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        /// <summary>
        /// Writes writeCommand string parameter to the I2C device address located within the address parameter:
        /// </summary>
        public string Write(byte address, string writeCommand)
        {
            string cmd = String.Format("i2c w {0} {1}", address, writeCommand);
            return Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, cmd);
        }

        #endregion

        #region PRIVATES	
        private byte _address;
        private string _writeCommand;
        private string _readCommand;

        #endregion
    }
}
