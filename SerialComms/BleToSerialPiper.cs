using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace BLE
{
    /// <summary>
    /// This class takes data received from <see cref="BLEDevice"/>
    /// And outputs it to a COM port
    /// </summary>
    public class BleToSerialPiper
    {
        #region Private Members
        private SerialPort mSerialPort;
        #endregion

        #region Public Properties
        /// <summary>
        /// This is a custom STX to prepend to data being sent
        /// </summary>
        public byte[] STX { get; set; }

        /// <summary>
        /// This is a custom ETX to append to data being sent
        /// </summary>
        public byte[] ETX { get; set; }

        /// <summary>
        /// Indicates if port is open
        /// </summary>
        public bool IsOpen { get; set; } = false;
        #endregion

        #region Public Events
        /// <summary>
        /// Fired when a data frame is sent
        /// </summary>
        public event Action DataSent = () => { };

        /// <summary>
        /// Fired when a data frame is received
        /// </summary>
        public event Action DataReceived = () => { };

        /// <summary>
        /// Fired when port is open
        /// </summary>
        public event Action OpenedPort = () => { };


        /// <summary>
        /// Fired when port is closed
        /// </summary>
        public event Action ClosedPort = () => { };

        /// <summary>
        /// Fired when there is an error on the port
        /// </summary>
        public event SerialErrorReceivedEventHandler PortError = (port, args) => { };
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="comPort">Active COM port</param>
        public BleToSerialPiper(byte[] stx, byte[] etx)
        {
            STX = stx;
            ETX = etx;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create and open serial port
        /// </summary>
        /// <param name="comPort">COM port to write to</param>
        /// <param name="baudRate">Baud rate</param>
        /// <param name="parity">Parity</param>
        /// <param name="dataBits">Data bits</param>
        /// <param name="stopBits">Stop bits</param>
        /// <returns></returns>
        public bool OpenPort(byte comPort, int baudRate, Parity parity = Parity.None, 
            int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            // If serial port is already open...
            if (mSerialPort != null)
                // then close it
                ClosePort();

            // Create serial port
            mSerialPort = new SerialPort("COM" + comPort, baudRate, parity, dataBits, stopBits);

            // Create incoming data listener
            mSerialPort.DataReceived += SerialDataReceived;

            // Create listener for port errors
            mSerialPort.ErrorReceived += PortError;

            try
            {
                // Open serial port
                mSerialPort.Open();
                IsOpen = true;
                OpenedPort();
                return true;
            }
            catch (UnauthorizedAccessException)
            { return false; }
            catch (ArgumentOutOfRangeException)
            { return false; }
            catch (ArgumentException)
            { return false; }
            catch (System.IO.IOException)
            { return false; }
            catch (InvalidOperationException)
            { return false; }
        }

        /// <summary>
        /// Listens for incoming serial data
        /// </summary>
        /// <param name="sender">The source or COM port</param>
        /// <param name="e">The accompanying arguments</param>
        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // TODO: implement function
            DataReceived();
        }

        /// <summary>
        /// Write data out on active COM port
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns>Whether or not write was successful</returns>
        public bool WriteSerialData(byte[] data)
        {
            // Indicates whether data was successfully transmitted
            bool result = false;

            // Only write date if port is already open
            if (mSerialPort.IsOpen)
            {
                // Modified data to send
                List<byte> frame = data.ToList();

                // If stx has values...
                if (STX != null)
                    // prepend to data
                    for (int i = 0; i < STX.Length; i++)
                        frame.Insert(i, STX[i]);

                // If etx has values...
                if (ETX != null)
                    // append to data
                    for (int i = 0; i < ETX.Length; i++)
                        frame.Add(ETX[i]);

                // Write data frame
                mSerialPort.Write(frame.ToArray(), 0, frame.Count);

                // If everything executed...
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Close the serial port if open
        /// </summary>
        /// <returns></returns>
        public bool ClosePort()
        {
            if (mSerialPort.IsOpen)
            {
                try
                {
                    mSerialPort.Close();
                    IsOpen = false;
                    ClosedPort();
                    return true;
                }
                catch (System.IO.IOException)
                {
                    return false;
                }
            }
            else
                return true;
        }
        #endregion

    }
}
