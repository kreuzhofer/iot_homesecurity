# iot_homesecurity
Open source home automation and edge gateway framework for Windows 10 and Windows 10 IoT Core

## Introduction
One of the main issues with home automation devices today is that they are mostly isolated solutions which are not interconnected and use incompatible proprietary protocols for communication.
Since I personally use Homematic for home automation in combination with an ETA heating system and other self-made sensors based on ESP8266 chipsets I needed a solution to interconnect these different devices and make them talk to each other.
The approach of the iot_homesecurity framework is to have a local broker device which has several device adapters to connect to the individual proprietary device interfaces, a local message bus and dynamic LUA functions to handle the different device messages, creating rules and make decisions as well as trigger device methods to react upon those messages.
