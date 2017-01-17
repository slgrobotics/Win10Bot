# Win10Bot
Windows 10 IoT Robot and Framework (Arduino, Raspberry Pi)

Win10Bot is a research project that I created to provide solid foundation for my experiments in Robotics.
It is intended to be open for extension and be rich with basic support, so that experimentation is easy.
Being a C# / Universal Windows project, it compiles to ARM architecture, and can be run on Raspberry Pi 2 or 3 under Windows 10 IoT.

There are two implementations, and more can be added. The first implementation has "hardware brick" built around Element (a.k.a. Serializer 3.0) board.
Therefore a port of original CMRobot .NET library to Universal Windows is included in the source. 
This implementation is under "RobotShorty" projects in the code. "Shorty" is a Roomba-sized robot I use indoors for most work.
It has a Netbook running Windows 10 or can be driven from my desktop PC.

Alternatively  Arduino based hardware layer is used, as Element boards became rarity (and are limited in functionality).
This implementation is under "RobotPlucky" projects in the code. "Plucky" is a bigger robot designed to operate outdoors.
It has four ARM boards (3 RPi-3 and one Odroid C) one of which runs Windows 10 IoT, others run Linux.

Plucky uses components based on:
- Arduino:

 (a) ParkingSensorI2C: using "Witson® LED Display Car Vehicle Parking Reverse Backup Radar System with 4 Parking Sensors" available on Amazon - provides sonar-based obstacle detection. It is queried by PluckyWheels over I2C. Works on Pro Mini 5V 328.
 
 (b) GPSKitchenSink: - a modified TinyGPS example that takes NMEA lines from U-blox NEO 6M GPS and sends only relevant info to PluckyWheels Serial1. Works on Arduino Leonardo.

 (c) PluckyWheels: - an Arduino Mega based controller, communicates to other components and takes commands from the C# "Win10Bot" code running on Raspberry Pi under Windows 10 IoT

- Linux (Raspberry Pi 2, Raspbian OS):

 (a) OpenCV Python based code to detect "targets" like color blobs and pedestrians. It reports to a web server in C# "Win10Bot" code.

As any live project, this is work in progress. I publish it here in the hope that it will be useful to
my friends at http://RSSC.org and other Robotics enthusiasts.

The code is published under Apache License, Version 2.0 - this is a no-warranty no-liability permissive license
- you do not have to publish your changes, although doing so, donating and contributing code and feedback is always appreciated (https://www.paypal.me/vitalbytes).

Have fun and feel free to provide feedback,
- Sergei Grichine   ( slg at quakemap.com )

