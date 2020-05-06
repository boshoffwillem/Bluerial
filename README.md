# Communicating with BLE and Serial services

## Commands to BLE serivce

**"ble-command-start"** -- Starts scanning for ble devices.
**"ble-command-stop"** -- Stops scanning.
**"ble-command-devices"** -- Lists the devices already found.
**"ble-command-add###xx"** -- Adds "xx" as a filter. Once filters are added only the filtered devices will be monitored.
**"ble-command-clear"** -- Clears the filters.

## Messages from BLE service

**"ble-scan-started"** -- Message that scanning has started.
**"ble-scan-stopped"** -- Message that scanning has stopped.
**"ble-message###xx"** -- Message detailing information about a scanned device.
**"ble-message-devices###xx"** -- Message listing the devices already found.

## Commands to Serial serivce

**"serial-open###comPort: xx, baudRate: xx, parity: Parity.None, dataBits: 8, stopBits: StopBits.One"** -- Opens the specified serial port, eg. "comPort: 5, baudRate: 9600,
                                                                                                       parity: Parity.  None, dataBits: 8, stopBits: StopBits.One".
**"serial-stx###xx"** -- Sets the STX value in the serial service. Format is xx,xx...
**"serial-etx###xx"** -- Sets the ETX value in the serial service. Format is xx,xx...
**"serial-message###xx"** -- Sends message "xx" to service to be transmitted.

## Messages from Serial serivce

**"serial-error###xx"** -- Error that occurred on serial port.
**"serial-opened"** -- Message that serial port is open.
**"serial-closed"** -- Message that serial port is closed.
**"serial-data-sent###xx"** -- Message that packet xx was sent.
**"serial-data-received###xx"** -- Message that packet xx was received.