# Communicating with BLE and Serial services

## Commands to BLE service

**"ble-start"** -- Starts scanning for ble devices.

**"ble-stop"** -- Stops scanning.

**"ble-devices"** -- Lists the devices already found.

**"ble-add-###xx"** -- Adds "xx" as a filter. Once filters are added only the filtered devices will be monitored.

**"ble-clear"** -- Clears the filters.

**"ble-filters"** -- Request active filters

## Messages from BLE service

**"ble-scan-started"** -- Message that scanning has started.

**"ble-scan-stopped"** -- Message that scanning has stopped.

**"ble-message-###xx"** -- Message -- in HEX -- detailing information about a scanned device.

**"ble-devices-###xx"** -- Message listing the devices already found.

**"ble-filters-###xx"** -- Message listing active filters.

## Commands to Serial service

**"serial-open-###comPort: xx, baudRate: xx, parity: None, dataBits: 8, stopBits: One"**
              -- Opens the specified serial port, eg. "comPort: 5, baudRate: 9600, parity: None, dataBits: 8, stopBits: One".
              -- parity options = Even, Mark, None, Odd, Space
              -- stopBit options = None, One, OnePointFive, Two

**"serial-close"** -- Close a opened serial port.

**"serial-stx-###xx"** -- Sets the STX value in the serial service. Format is -- in HEX -- xx,xx,...

**"serial-etx-###xx"** -- Sets the ETX value in the serial service. Format is -- in HEX -- xx,xx,...

**"serial-message-###xx"** -- Sends message "xx" to service to be transmitted. Format is -- in HEX -- xx,xx,...

## Messages from Serial service

**"serial-error-###xx"** -- Error that occurred on serial port.

**"serial-opened"** -- Message that serial port is open.

**"serial-closed"** -- Message that serial port is closed.

**"serial-data-sent-###xx"** -- Message that packet -- in HEX -- xx was sent.

**"serial-data-received-###xx"** -- Message that packet -- in HEX -- xx was received.
