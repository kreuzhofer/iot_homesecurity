# iot_homesecurity
Open source home automation and edge gateway framework for Windows 10 and Windows 10 IoT Core

## Introduction
One of the main issues with home automation devices today is that they are mostly isolated solutions which are not interconnected and use incompatible proprietary protocols for communication.

Since I personally use Homematic for home automation in combination with an ETA heating system and other self-made sensors based on ESP8266 chipsets I needed a solution to interconnect these different devices and make them talk to each other.
In the meanwhile I had another PoC with ABUS devices (Secvest alert panel and some security cameras), which are now also available as devices in the solution.

## Technical approach
The approach of the iot_homesecurity framework is to have a local broker device which has several device adapters to connect to the individual proprietary device interfaces, a local message bus and dynamic LUA functions to handle the different device messages, creating rules and make decisions as well as trigger device methods to react upon those messages.

## Supported devices
- ETA heating systems - Read only access to the heating system's variables available. Still a bit rough.

- Homematic plugin - In development

- ABUS SecVest plugin - In development

- ABUS Security camera plugin - Planned

## Solutions
- IoTApp - The device gateway. Runs in the local network of the user's home and talks to his local devices like the Homematic central, ETA heating system or ABUS alert panel or ABUS security camera.

- DevicePortal - This solution contains the device portal running in the Azure cloud, which enables access to your local devices. It uses Azure IoT Hub as the communication hub for the device gateway.
