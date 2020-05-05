namespace BLE
{
    /// <summary>
    /// Details to specific GATT service <see cref="https://www.bluetooth.com/specifications/gatt/services/"/>
    /// </summary>
    public class GattService
    {
        #region Public Properties
        /// <summary>
        /// Human readable name for the service
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Uniform identifier that is unique to the service
        /// </summary>
        public string UniformTypeIdentifier { get; set; }

        /// <summary>
        /// The 16-bit number associated with this service.
        /// The Bluetooth GATT Service UUID contains this.
        /// </summary>
        public ushort AssignedNumber { get; set; }

        /// <summary>
        /// The type of specification that this service is <see cref="https://www.bluetooth.com/specifications/gatt/"/>
        /// </summary>
        public string ProfileSpecification { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="uniformTypeIdentifier">UniformTypeIdentifier</param>
        /// <param name="assingedNumber">AssignedNumber</param>
        /// <param name="profileSpecification">ProfileSpecification</param>
        public GattService(string name, string uniformTypeIdentifier, ushort assingedNumber, string profileSpecification)
        {
            Name = name;
            UniformTypeIdentifier = uniformTypeIdentifier;
            AssignedNumber = assingedNumber;
            ProfileSpecification = profileSpecification;
        }
        #endregion
    }
}
