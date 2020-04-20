using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLE
{
    /// <summary>
    /// Wraps and makes use of the <see cref="BLEAdvertisementWatcher" />
    /// for easier consumption
    /// </summary>
    public class BLEAdvertisementWatcher
    {
        #region Private Members
        /// <summary>
        /// The underlying ble watcher class
        /// </summary>
        private readonly BluetoothLEAdvertisementWatcher mWatcher;

        /// <summary>
        /// List of discovered devices
        /// </summary>
        private readonly Dictionary<ulong, BLEDevice> mDiscoveredDevices = new Dictionary<ulong, BLEDevice>();

        /// <summary>
        /// The details about GATT services
        /// </summary>
        private readonly GattServiceIds mGattServiceIds;

        /// <summary>
        /// A thread lock object for this class
        /// </summary>
        private readonly object mThreadLock = new object();
        #endregion

        #region Public Properties
        /// <summary>
        /// Indicates if this watcher is listening for advertisements
        /// </summary>
        public bool Listening => mWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        /// <summary>
        /// List of discovered devices
        /// </summary>
        public IReadOnlyCollection<BLEDevice> DiscoveredDevices
        {
            get
            {
                // Clean up any device timeouts
                CleanupTimeouts();

                // Practice thread-safety
                lock (mThreadLock)
                {
                    // Convert to read only list
                    return mDiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Timeout in seconds before a device is removed from the <see cref="DiscoveredDevices"/>
        /// list if it is not re-advertised within this time
        /// </summary>
        public int HeartbeatTimeout { get; set; } = 30;
        #endregion 

        #region Public Events
        /// <summary>
        /// Fired when bluetooth watcher stops listening
        /// </summary>
        public event Action StoppedListening = () => { };

        /// <summary>
        /// Fired when bluetooth watcher starts listening
        /// </summary>
        public event Action StartedListening = () => { };

        /// <summary>
        /// Fired when new device is discovered
        /// </summary>
        public event Action<BLEDevice> NewDeviceDiscovered = (device) => { };

        /// <summary>
        /// Fired when a device is discovered
        /// </summary>
        public event Action<BLEDevice> DeviceDiscovered = (device) => { };

        /// <summary>
        /// Fired when a device name changes
        /// </summary>
        public event Action<BLEDevice> DeviceNameChanged = (device) => { };

        /// <summary>
        /// Fired when a device's data changed
        /// </summary>
        public event Action<BLEDevice> DeviceDataChanged = (device) => { };

        /// <summary>
        /// Fired when a device is removed for timing out
        /// </summary>
        public event Action<BLEDevice> DeviceTimeout = (device) => { };
        #endregion

        #region Constructor 
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="gattIds"></param>
        public BLEAdvertisementWatcher(GattServiceIds gattIds)
        {
            // Null guard
            mGattServiceIds = gattIds ?? throw new ArgumentNullException(nameof(gattIds));

            // Create bluetooth listener
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            // Listens for new advertisements
            mWatcher.Received += WatcherAdvertisementReceived;

            // Listens for when watcher stops listening
            mWatcher.Stopped += (watcher, e) =>
            {
                StoppedListening();
            };
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Listens for new advertisements
        /// </summary>
        /// <param name="sender">The watcher</param>
        /// <param name="args">The arguments</param>
        private  void WatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Clean up any device timeouts
            CleanupTimeouts();

            // Get ble device info
            //var device = await GetBluetoothLEDeviceAsync(args.BluetoothAddress, args.Timestamp, args.RawSignalStrengthInDBm);
            BLEDevice device = null;

            // Null guard
            //if (device == null)
            //    return;               

            // Is new discovery?
            var newDiscovery = false;

            lock (mThreadLock)
            {
                //newDiscovery = !mDiscoveredDevices.ContainsKey(device.DeviceId); 
                newDiscovery = !mDiscoveredDevices.ContainsKey(args.BluetoothAddress); 
            }

            // Name changed?
            var nameChanged =
                // If it already exists
                !newDiscovery &&
                // And it's not a blank name
                !string.IsNullOrEmpty(args.Advertisement.LocalName) &&
                // And the name is different
                mDiscoveredDevices[args.BluetoothAddress].Name != args.Advertisement.LocalName;
         
            lock (mThreadLock)
            {
                // Get name of device
                var name = args.Advertisement.LocalName;

                // If new name is blank, and we already have a device...
                if (string.IsNullOrEmpty(name) && !newDiscovery)
                    // Don't override what could be an actual name already
                    name = mDiscoveredDevices[args.BluetoothAddress].Name;

                // Get manufacturer data
                var manufacturerSections = args.Advertisement.ManufacturerData;
                ushort companyId = 0;
                byte[] advertisementData = null;

                if (manufacturerSections.Count > 0)
                {
                    // Only print the first one of the list
                    var manufacturerData = manufacturerSections[0];

                    // Get the advertisement data
                    advertisementData = new byte[manufacturerData.Data.Length];
                    using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                    {
                        reader.ReadBytes(advertisementData);
                    }

                    // Get the company id
                    companyId = manufacturerData.CompanyId;                   
                }

                // If not new device...
                if (!newDiscovery)
                {
                    // If company id is invalid...
                    if (companyId == 0)
                        // Use last valid id
                        companyId = mDiscoveredDevices[args.BluetoothAddress].CompanyId;

                    // And data is invalid..
                    if (advertisementData == null)
                        // Use last valid data
                        advertisementData = mDiscoveredDevices[args.BluetoothAddress].Data;

                    // If data of know devices changes..
                    if (!advertisementData.Equals(mDiscoveredDevices[args.BluetoothAddress].Data))
                        // then notify listeners
                        DeviceDataChanged(device);
                }

                // Create new device
                device = new BLEDevice
                (
                    broadcastTime: args.Timestamp,
                    address: args.BluetoothAddress,
                    name: name,
                    rssi: args.RawSignalStrengthInDBm,
                    companyId: companyId,
                    data: advertisementData
                );

                // Add/update device in dictionary
                mDiscoveredDevices[args.BluetoothAddress] = device;
            }

            // Inform listeners
            DeviceDiscovered(device);

            // If name changed...
            if (nameChanged)
                // Inform listeners
                DeviceNameChanged(device);

            // If new discovery...
            if (newDiscovery)
                // Inform listeners
                NewDeviceDiscovered(device);
        }

        /// <summary>
        /// Connects to the ble device and extract more info
        /// <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice?view=winrt-18362"/>
        /// </summary>
        /// <param name="address">The ble address of device to connect to</param>
        /// <param name="broadcastTime">The time that advertisement was received</param>
        /// <param name="rssi">The signal strength in dB</param>
        /// <returns></returns>
        private async Task<BLEDevice> GetBluetoothLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, short rssi)
        {           
            // Get bluetooth device info
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            // Null guard
            if (device == null)
                return null;

            // Device name
            var name = device.Name;

            // Get GATT services that are available
            var gattServices = await device.GetGattServicesAsync().AsTask();

            // If we have any services
            if (gattServices.Status == GattCommunicationStatus.Success)
            {
                // Loop each GATT service
                foreach(var service in gattServices.Services)
                {
                    // This GUUID contains the GATT Profile Assigned Number we want!
                    // TODO: Get more info and connect
                    var gattProfileId = service.Uuid;
                }

                return new BLEDevice(broadcastTime: broadcastTime, 
                    address: device.BluetoothAddress, 
                    name: device.Name, 
                    rssi: rssi, 
                    companyId: 0, // ??? 
                    data: new byte[] { 0 }, // ???
                    conncted: device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                    canPair: device.DeviceInformation.Pairing.CanPair, 
                    paired: device.DeviceInformation.Pairing.IsPaired, 
                    deviceId: device.DeviceId);
            }

            return null;
        }

        /// <summary>
        /// Prune any timed out devices
        /// </summary>
        private void CleanupTimeouts()
        {
            lock (mThreadLock)
            {
                // The date in time that if less than it means a device has timed out
                var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(HeartbeatTimeout);

                // Any devices that have timed out
                mDiscoveredDevices.Where(f => f.Value.BroadcastTime < threshold).ToList().ForEach(device =>
                {
                    // Remove device
                    mDiscoveredDevices.Remove(device.Key);

                    // Inform listeners
                    DeviceTimeout(device.Value);
                });
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts listening for advertisements
        /// </summary>
        public void StartListening()
        {
            lock (mThreadLock)
            {
                if (Listening)
                    // Do nothing more
                    return;

                // Start watcher
                mWatcher.Start();
            }

            // Inform listener that were starting
            StartedListening();
        }

        /// <summary>
        /// Stops listening for advertisements
        /// </summary>
        public void StopListening()
        {
            lock (mThreadLock)
            {
                // If were not currently listening
                if (!Listening)                // Do nothing
                    return;

                // Stop listening
                mWatcher.Stop();

                // Clear any devices
                mDiscoveredDevices.Clear();
            }
        }
        #endregion
    }
}
