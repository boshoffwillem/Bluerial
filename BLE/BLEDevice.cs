using System;
using System.Collections.Generic;
using System.Text;

namespace BLE
{
    /// <summary>
    /// Information of BLE device
    /// </summary>
    public class BLEDevice
    {
        #region Public Properties
        /// <summary>
        /// Time of broadcast advertisement message of device 
        /// </summary>
        public DateTimeOffset BroadcastTime { get; }

        /// <summary>
        /// Address of device
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// Name of device
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Signal strength is in dB
        /// </summary>
        public short SignalStrengthInDB { get; }
        #endregion

        #region Constructor 
        /// <summary>
        /// Default constructor
        /// </summary>
        public BLEDevice(DateTimeOffset broadcastTime, ulong address, string name, short rssi)
        {
            BroadcastTime = broadcastTime;
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
        }
        #endregion

        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name) ? "[No name]" : Name)} {Address:X} ({SignalStrengthInDB})";
        }
    }
}
