# Documentation
## Intension
The goal of this add-in is to write test for your add-in. Because it is not possible to start revit as a 'service' or start from a test, it is necessary to start Revit first, then start your tests. In this case, the Revit context is available in the tests.

## Writing Tests
First add the NuGet package of [NUnit](https://www.nuget.org/packages/NUnit/) to the test project.

A test must be marked with the ```Test``` Attribute of the NUnit 3 library. All marked methods will be recognized when the test assembly is loaded. A ```Test``` is executable. 
A method marked with the ```SetUp``` Attribute will be executed before each test.
A method marked with the ```TearDown``` Attribute will be executed after each test.

```C#
[SetUp]
public void MySetUp(){
    // Do some stuff before the test runs.
}

[TearDown]
public void MyTearDown(){
    // Do some stuff after the test is finished
}

[Test]
public void MyTest(){
    // Do some test stuff
}
```


To get Revit API objects like ```Application``` or ```UIApplication```, extend the test method signature with one or both of the called Classes (order is not relevant). The injected objects can be used to do some stuff, for example open a file.

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
All the code of the add-in lives in the Revit.TestRunner assembly. The ```Main``` Class implements the ```IExternalApplication``` Interface. Log4Net is used as logging framework. 

### Sample Test Project
Containing some sample Tests, showing how they could be implemented.

### Build the Solution
The solution contains a 'Debug' and 'Release' build configuration for all the supported Revit versions (2018, 2019, 2020). A post-build event calls a power shell script, which will create an add-in file in the %ProgramData%\Autodesk\Revit\Addins\20xx pointing to the fresh compiled Revit.TestRunner.dll. See section Power Shell.


## Precompiled binaries
The compiled add-in is also available in the [install](../install) section. Download the whole directory and place it somewhere. Run the corresponding .cmd to install the add-in. The .addin file will be copied to the %ProgramData%\Autodesk\Revit\Addins\20xx folder, and points to the current folder and its libraries. See section Power Shell.


## Power Shell
To automatically create an addin file, power shell must be enabled on your machine. To enable run: ```c:\windows\syswow64\WindowsPowerShell\v1.0\powershell.exe -command set-executionpolicy unrestricted```