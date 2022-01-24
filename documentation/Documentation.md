# Documentation
## Intension
The goal of this add-in is to write test for your add-in. Because it is not possible to start Revit as a 'service' or start from a test, it is necessary to start Revit first, then start your tests. In this case, the Revit context is available in the tests.

## Clients
Tests must be executed either the desktop or the console application. They must run in context of Revit. On test start, Revit starts if it is not running already.

### Desktop application
**Explore** a test assembly by select a dll or exe file. All Test methods marked by the ```Test``` Attribute are shown hierarchically.

Select one or more tests and press the 'Run' button to **execute** them.  
Alternatively save a **request** file by pressing the 'Create Request' button.

### Console application
The console runner consumes a request json file containing the tests to execute. See Desktop application, how to create a request.
The console runner can also consume a specific test assembly. All tests in this assembly will be executed.

``` Revit.TestRunner.Console.exe request c:\temp\myRequest.json -r 2021 ```

``` Revit.TestRunner.Console.exe assembly c:\temp\myTestAssembly.dll -r 2021 ```

## Writing Tests
First add the NuGet package of [NUnit](https://www.nuget.org/packages/NUnit/) to the test project. But be aware that this is only a NUnit like framework. Not all features of NUnit are supported. 

A test must be marked with the ```Test``` Attribute of the NUnit 3 library. All marked methods will be recognized when the test assembly is loaded. A ```Test``` is executable. 
A method marked with the ```OneTimeSetUp``` Attribute will be executed before the first test runs.
A method marked with the ```SetUp``` Attribute will be executed before each test.
A method marked with the ```TearDown``` Attribute will be executed after each test.
A method marked with the ```OneTimeTearDown``` Attribute will be executed after the last test runs.

```C#
[OneTimeSetUp]
public void MyOneTimeSetUp(){
    // Do some stuff before all test runs. Run once.
}

[SetUp]
public void MySetUp(){
    // Do some stuff before the test runs.
}

[TearDown]
public void MyTearDown(){
    // Do some stuff after the test is finished
}

[OneTimeTearDown]
public void MyOneTimeTearDown(){
    // Do some stuff after all test are finished. Run once.
}

[Test]
public void MyTest(){
    // Do some test stuff
}
```

NUnit ```Explicit``` and ```Ignore``` Attributes are also supported.

The ```TestCase``` Attribute, to pass some parameters, is NOT supported!

To get Revit API objects like ```Application``` or ```UIApplication```, extend the test method signature with one or both Classes (order is not relevant). The injected objects can be used to do some stuff, for example open a file.

```C#
[Test]
public void MyTest( UIApplication uiApplication, Application application ){
    // Do some test stuff. ex.:
    uiApplication.Application.OpenDocumentFile( "C:\myTestFile.rvt" );
}
```

A sample test assembly is included in the visual studio solution.


## VisualStudio Solution
### Add-In Project
All the code of the add-in service part lives in the Revit.TestRunner assembly. The ```Main``` Class implements the ```IExternalApplication``` Interface. Log4Net is used as logging framework.

### App Project
WPF Desktop application.

### Console Project
Console application.

### Sample Test Project
Containing some sample Tests, showing how they could be implemented.

### Build the Solution
The solution contains 'Debug' and 'Release' build configurations for all the supported Revit versions (2019, 2020, 2021, 2022). A post-build event calls a power shell script, which will create an add-in file in the %ProgramData%\Autodesk\Revit\Addins\20xx pointing to the fresh compiled Revit.TestRunner.dll. See section Power Shell.


## Precompiled binaries
The compiled add-in is also available in the [install](../install) section. Download the whole directory and place it somewhere. Run the corresponding .cmd to install the add-in. The .addin file will be copied to the %ProgramData%\Autodesk\Revit\Addins\20xx folder, and points to the current folder and its libraries. See section Power Shell.


## Power Shell
To automatically create an addin file, power shell must be enabled on your machine. To enable run: ```c:\windows\syswow64\WindowsPowerShell\v1.0\powershell.exe -command set-executionpolicy unrestricted```