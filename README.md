# Win10Bot
Windows 10 IoT Robot and Framework (Arduino, Raspberry Pi)

Win10Bot is a research project that I created to provide a solid foundation for my experiments in Robotics.
It is intended to be open for extension and rich with basic support, so that experimentation is easy.
Being a C# / Universal Windows project, it compiles to ARM architecture, and can be run on Raspberry Pi 2 under Windows 10 IoT.

Plucky The Robot had two implementations. The first implementation had "hardware brick" built around Element (a.k.a. Serializer 3.0) board.
Therefore a port of original CMRobot .NET library to Universal Windows is included in the source. 
This implementation is under "RobotShorty" projects in the code. It is not being tested lately, but should still work.

Currently  Arduino based hardware layer is used, as Element boards became rarity.
This implementation is under "RobotPlucky" projects in the code. It is under development and is improved often.

There are components based on:
- Arduino:
 ParkingSensorI2C: using "Witson® LED Display Car Vehicle Parking Reverse Backup Radar System with 4 Parking Sensors" available on Amazon - provides sonar-based obstacle detection
 PluckyWheels: - an Arduino Mega based controller communicates to other components and takes commands from the C# code running on Raspberry Pi under Windows 10 IoT

- Linux:
 OpenCV Python based code to detect "targets" like color blobs and pedestrians 

As any live project, this is work in progress. I publish it here in the hope that it will be useful to
my friends at http://RSSC.org and other Robotics enthusiasts.

The code is published under Apache License, Version 2.0 - this is a no-warranty no-liability permissive license
- you do not have to publish your changes, although doing so, donating and contributing is always appreciated ( https://www.paypal.me/vitalbytes ).

Have fun and feel free to provide feedback,
-- Sergei Grichine

