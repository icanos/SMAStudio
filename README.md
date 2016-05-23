# Automation Studio
SMA Studio has changed name to Automation Studio, since it supports more than just SMA these days.

Download the latest release at the release page. Latest version is 2.0.1 and **Powershell 5.0 is required**!

## Overview
SMA Studio is a free IDE for developing and managing a Microsoft System Center Service Management Automation environment as well as Microsoft Azure Automation (both Classic and Resource Manager). It comes with a wide range of great features to ease your work.

The work on Automation Studio started in June 2014 by Marcus Westin.

## Features
 - Service Management Automation, Azure Resource Manager and Azure Classic support.
 - Code Analysis is implemented in drafted runbooks, meaning that you will get help to improve your runbooks based on Microsoft's best practices (ScriptAnalyzerEngine).
 - Snappy Auto Complete function that works even with runbook parameters across all runbooks in the same connection.
 - Debugging of runbooks directly inside Automation Studio, no need to copy your runbook to ISE anymore.
 - Object Inspection when debugging (like the inspection found in Visual Studio).
 - Bug fixes, bug fixes and bug fixes.

I have certainly missed something.

## Known Issues
 - **FIXED** SMA connections were not correctly loaded prior to 2.0.1 r2.
 - When saving a connection, the application may crash, the connection is saved so please restart Automation Studio and it will work.
 - **FIXED** Environment Explorer takes some time to load all connections (if more than one).
 - Auto Documentation is currently not working (does not crash Automation Studio, but hangs it in background if you try to close it).
 - **FIXED** Activity Notifications are not implemented across all resources (only runbooks at the moment).
 - **FIXED** Debugging is currently not working as expected. Working on improving this currently!

Please notify me if you find more.

## How to Compile
Download the source code, open in Visual Studio 2013+ and restore the packages from NuGet to get started.
 
## System Requirements

 - Windows 7+
 - [.NET 4.6](http://www.microsoft.com/en-au/download/details.aspx?id=30653)
 - SMA Environment, Azure Classic or Azure Resource Manager up and running
 - Powershell 5.0
 
## Credits
 - Gemini (https://github.com/icanos/gemini) UI framework, awesome work!
 - GitSharp, bits and parts used from this project for tracking diffs in Draft/Published runbooks
 - Icon for the application and other windows within SMA Studio from http://graphicloads.com/
 
Forgot anyone? Please notify me.

######Copyright 2014-2016 Marcus Westin. Automation Studio is distributed under the Apache 2.0 License
