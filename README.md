# Revit.TestRunner
Status: under active development, breaking changes may occur.

Revit.TestRunner is a simple Addin for Autodesk Revit. It runs unit tests from a specified test assembly, which have references to the Revit API. The test framework used is [NUnit v3](https://github.com/nunit).

See [Documentation](/documentation/Documentation.md)

## How it works
The Revit.TestRunner is designed to work as an Addin of Autodesk Revit. Test runs can be started by using the standalone desktop application or the console application.

Choose your favorite test assembly and run the desired tests. There is no need for the test assembly to have any reference to the Revit.TestRunner. All you must do, is get the nuget package of NUnit and write some fancy tests.

## Getting started
Get the Code from github and compile it. The Revit.TestRunner.addin file will be automatically placed in the ProgramData addin folder of the selected Revit version 

Or download the pre compiled binaries from [install dir](/install) . Start the InstallAddin v20xx.cmd of your favorite Revit version and run Revit. 

The Addin hooks in the ‘Add-Ins’ Ribbon of Revit. 

![alt text](/images/testrunner_start.png)

By pressing the Button, the standalone application start. By choosing your testing assembly, the view presents all test in the assembly.

![alt text](/images/testrunner_ui.png)

Select the nodes you want to test and press the ‘Run’ Button. All selected nodes will be executed.

![alt text](/images/testrunner_ui_executed.png)

### Console application
Instead of running test from the standalone application, tests can also be executed by the console application.


### Write Tests
Create a test project in your solution and get the NUnit nuget package.

Let us have a look to the SampleTest class. As you see, test is marked by the NUnit Attribute ‘Test’. Also ‘SetUp’, ‘TearDown’, ‘OneTimeSetUp’ and ‘OneTimeTearDown’ Attributes are supported.

The ‘TestCase’ attribute is not supportet, please use specific test methodes.

```c#
public class SampleTest
{
    [SetUp]
    public void RunBeforeTest()
    {
        Console.WriteLine( $"Run 'SetUp' in {GetType().Name}" );
    }
 
    [TearDown]
    public void RunAfterTest()
    {
        Console.WriteLine( $"Run 'TearDown' in {GetType().Name}" );
    }
 
    [Test]
    public void PassTest()
    {
        Assert.True( true );
    }
 
    [Test]
    public void FailTest()
    {
        Assert.True( false, "This Test should fail!" );
    }
}
```

And now we are happy, almost. It would be nice if we can open a file in Revit and make some test with it. This is not easy because we need the `Application` API object of Revit, but we do not have it available at this point. 
To get the API object, change the signature of your Test Method. The Revit.TestFramework will inject the desired object in the Test, SetUp or TearDown Method. Supported Revit Objects: `UIApplication`, `Application`)

```c#
[Test]
public void MultiParameterTest1( UIApplication uiApplication, Application application )
{
    Assert.IsNotNull( uiApplication.Application );
    Assert.IsNotNull( application );
}

```

In your test, you have access to it, and your able to make stuff with it (ex. `UIApplication.Application.OpenDocumentFile(…)`)

## Release History
See [ReleaseNotes](/documentation/ReleaseNotes.md)

## Open Issues
* ...

## License
[MIT](http://opensource.org/licenses/MIT)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
-	Author Copyright (C) 2017-2022 Tobias Flöscher
-	Company Copyright (C) 2017-2022 Geberit Verwaltungs AG 
