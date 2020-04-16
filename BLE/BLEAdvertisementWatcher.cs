using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;

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
                    // Convert to readonly list
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
        /// Fired when when bluetooth watcher stops listening
        /// </summary>
        public event Action StoppedListening = () => { };

        /// <summary>
        /// Fired when when bluetooth watcher starts listening
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
        /// Fired when a device is removed for timing out
        /// </summary>
        public event Action<BLEDevice> DeviceTimeout = (device) => { };
        #endregion

        #region Constructor 
        public BLEAdvertisementWatcher()
        {
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
        private void WatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Clean up any device timeouts
            CleanupTimeouts();
            
            BLEDevice device = null;

            // Is new dicovery?
            var newDiscovery = !mDiscoveredDevices.ContainsKey(args.BluetoothAddress);

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

                // Create new device
                device = new BLEDevice
                (
                    broadcastTime: args.Timestamp,
                    address: args.BluetoothAddress,
                    name: name,
                    rssi: args.RawSignalStrengthInDBm
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
