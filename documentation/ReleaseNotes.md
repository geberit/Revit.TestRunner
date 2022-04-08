# Release Notes
* v1.3.7 [April 2022]
    * Support of Revit 2023
    * Console runner Revit language support
    * Console runner exit code -1 on some failed tests
    * Fix bug where longer running tests are not accounted
    * Update notification on start of test group


* v1.3.6 [March 2022]
    * Support of NUnit attribute 'Explicit' on class level
    * Fix 'service may not be running' issue
    * Bin clean up

* v1.3.5 [November 2021]
    * SetRaiseWithoutDelay on OnIdle event

* v1.3.4 [November 2021]
    * NUnit attribute 'OneTimeSetUp' and 'OneTimeTearDown' are supported.
    * NUnit attribute 'TestCase' is not supported anymore!
    * Console output in message
    * Test duration
    * Small UI improvements

* v1.3.3 [September 2021]
    * NUnit result files
    * Clients using .net core 3.1
    * Shared using .net standard 2.0

* v1.3 [21Q02]
    * 'Run all tests from an assembly' command in console application
    * Change of parameters in console application
    * Rough implementation of nUnit Explicit- and Ignore- Attributes
    * Change of communication between client and service (Please recreate request files!)
    * Recent files in desktop app

* v1.2 [20Q03]
    * Service based execution
    * Standalone desktop application
    * Console application
    * Multi test execution
    * Async execution

* v0.10 [20Q02]
    * Revit 2021 Support
    * .NET 4.8
    * Support of NUnit Attributes 'TestCase'

* v0.9 [19Q04]
    * Using NUnit3
    * No reference to Revit.TestRunner needed in test assembly
    * Support of NUnit attributes SetUp and TearDown
    * Injection of API objects in test method   
    * Works as an addin with GUI
    * Installation script for libraries
    * Post build event in VS project to create addin files

