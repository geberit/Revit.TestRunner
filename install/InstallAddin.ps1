############################################################################################################
#
# Script to install the Revit.testRunner Addin for Autodesk Revit
# The .addin file will be copied to the addin folder of revit in %ProgramData%
#
# The executable assembly will be set in the target addin file on basis of the current folder.
#
# 
# Created by Tobias Flöscher, Geberit Verwaltungs AG
# Date: 04.10.2019
#
# To enable PowerShell Scripts run in a Command Prompt as Administrator:
# c:\windows\syswow64\WindowsPowerShell\v1.0\powershell.exe -command set-executionpolicy unrestricted
#
# InputParameter:
# - Revit version number => ex. 2019
#
############################################################################################################

param (
    [string]$revitVersion = "2020"
)

$binPath = $PSScriptRoot
$addinFile = "Revit.TestRunner.addin"
$assemblyFile = "Addin\Revit.TestRunner.dll"

$assemblyFileName = Join-Path -Path $binPath -ChildPath $assemblyFile
$addinFileName = Join-Path -Path $binPath -ChildPath $addinFile


Write-Host "##### Install Revit.TestRunner #####"
Write-Host

# Exit Codes
$errorAddinFileDoesNotExist = 111
$errorAddinDirTargetDoesNotExist = 113

# General Parameters
$addinRootPath = $env:ProgramData
$addinPathRelative = "Autodesk\Revit\Addins\"
$addinPath = Join-Path -Path $addinRootPath -ChildPath $addinPathRelative
$addinVersionPath = Join-Path -Path $addinPath -ChildPath $revitVersion
$targetAddinFile = Join-Path -Path $addinVersionPath -ChildPath $addinFile

# Validate
if(!(Test-Path $addinFileName)){
    Write-Host "source addin does not exist '$addinFileName'"
    exit $errorAddinFileDoesNotExist
}

if(!(Test-Path $assemblyFileName)){
    Write-Host "assembly path does not exist '$assemblyFileName'"
    exit $errorIncorrectparAmassemblyFileName
}

if(!(Test-Path $addinVersionPath)){
    Write-Host "target addin path does not exist '$addinVersionPath'"
    exit $errorAddinDirTargetDoesNotExist
}

# Copy addin file to target path

Copy-Item $addinFileName -Destination $addinVersionPath

if(!(Test-Path $targetAddinFile)){
    Write-Host "target addin does not exist exist '$targetAddinFile'"
    exit $errorIncorrectparamAddinDir
}

# Manipulate target addin File
Write-Host "Executing assembly "$assemblyFileName
(Get-Content $targetAddinFile).Replace('###insertExecutingAssembly###', $assemblyFileName) | Set-Content $targetAddinFile

Write-Host "Install addin file "$targetAddinFile
Write-Host "Installation successful"
Write-Host
Write-Host
