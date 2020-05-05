using System;

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

        /// <summary>
        /// Indicate if we are connected to this device
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Indicates if device supports pairing
        /// </summary>
        public bool CanPair { get; }

        /// <summary>
        /// Indicates if we are currently paired to this device
        /// </summary>
        public bool Paired { get; }

        /// <summary>
        /// Permanent unique id of this device
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// The Bluetooth LE company identifier code as defined by the Bluetooth Special Interest Group (SIG).
        /// <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothlemanufacturerdata?view=winrt-18362"/>
        /// </summary>
        public ushort CompanyId { get; }

        /// <summary>
        /// The latest data advertised
        /// <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothlemanufacturerdata?view=winrt-18362"/>
        /// </summary>
        public byte[] Data { get; }
        #endregion

        #region Constructor 
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="broadcastTime">The broadcast time of discovery</param>
        /// <param name="address">The device address</param>
        /// <param name="name">The device name</param>
        /// <param name="rssi">The signal strength</param>
        /// <param name="companyId">The company(SIG) id of the device</param>
        /// <param name="data">The advertised data</param>
        /// <param name="conncted">If we are connected to the device</param>
        /// <param name="canPair">If we can pair with device</param>
        /// <param name="paired">If we are paired with the device</param>
        /// <param name="deviceId">The unique id of the device (stays the same 
        /// even if address changes)</param>
        public BLEDevice(DateTimeOffset broadcastTime, 
            ulong address, 
            string name, 
            short rssi,            
            ushort companyId, 
            byte[] data,
            bool conncted = false,
            bool canPair = false,
            bool paired = false,
            string deviceId = null)
        {
            BroadcastTime = broadcastTime;
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
            CompanyId = companyId;
            Data = data;
            Connected = conncted;
            CanPair = canPair;
            Paired = paired;
            DeviceId = deviceId ?? address.ToString("X");
        }
        #endregion

        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name) ? "[No name]" : Name)} {Address:X} ({SignalStrengthInDB})\n" +
                $"\tConnected: {Connected}, Pairable: {CanPair}, Paired: {Paired}\n" +
                $"\tDevice id: {DeviceId}\n" + 
                $"\tCompany(SIG) id: {CompanyId:X} => Data: {BitConverter.ToString(Data ?? new byte[] { 0 })}\n" +
                $"\t{DateTime.Now}";
        }
    }
}
