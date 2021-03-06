# iot_homesecurity
Open source home automation and edge gateway framework for Windows 10 IoT Core using UWP and .NET Core 2.0

## Introduction
One of the main issues with home automation devices today is that they are mostly isolated solutions which are not interconnected and use incompatible proprietary protocols for communication.

Since I personally use [Homematic](http://amzn.to/2ArNyiV) for home automation in combination with an [ETA heating system](https://www.eta.co.at/), [cheap Sonoff switches](http://amzn.to/2CB8ARf) and other self-made sensors based on ESP8266 chipsets I needed a solution to interconnect these different devices and make them talk to each other.
In the meanwhile I had another PoC with ABUS devices (Secvest alert panel and some security cameras), which are now also available as devices in the solution.

Please find the official ABUS PoC documentation here [https://microsoft.github.io/techcasestudies/iot/azure%20app%20service/2017/09/01/ABUS.html](https://microsoft.github.io/techcasestudies/iot/azure%20app%20service/2017/09/01/ABUS.html)

## Technical approach
The approach of the iot_homesecurity framework is to have a local broker device which has several device plugins to connect to the individual proprietary device interfaces, a local message bus and dynamic LUA functions to handle the different device messages, creating rules and make decisions as well as trigger device methods to react upon those messages.

## Supported hardware devices
- ETA heating systems - Read only access to the heating system's variables available. Still a bit rough.

- Homematic plugin - In development
    - Working
        - Running Homematic CCU programs
        - Reading Datapoints
    - Planned
        - Writing Datapoints

- ABUS SecVest plugin - In development
    - Reading alert loop state is now supported
    - Toggling external switches/relais is now supported

- ABUS Security camera plugin - Planned

- Twilio SMS sending plugin - Working, Twilio account needed

## Communication channels on the gateway
- MQTT Client/Server for receiving/sending from/to mqtt devices
- Azure IoT Hub for receiving/sending messages and commands from and to the cloud
- HTTP REST API for receiving messages from other sources

## Solutions
- IoTApp - The device gateway. Runs in the local network of the user's home and talks to his local devices like the Homematic central, ETA heating system or ABUS alert panel or ABUS security camera.

- DevicePortal - This solution contains the device portal running in the Azure cloud, which enables access to your local devices. It uses Azure IoT Hub as the communication hub for the device gateway.

## Device Installation and Updates
The IoT_Homesecurity UWP app is deployed directly on the device. If you like to use the Windows Store to update your device automatically, you may use the provided appx packages or create your own packages but you will need to submit them to the Windows Store and have an OEM exception. Otherwise you can always deploy the app directly to your device with Visual Studio or by building packages and deploy them to the device using the device portal.

### Packages
- You will find the latest version of the IoT_Homesecurity appx store packages here: [https://github.com/kreuzhofer/iot_homesecurity/releases](https://github.com/kreuzhofer/iot_homesecurity/releases)
