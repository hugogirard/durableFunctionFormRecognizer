#------------------------------------------------------------------------------
#MIT License

#Copyright(c) 2021 Microsoft Corporation. All rights reserved.

#Permission is hereby granted, free of charge, to any person obtaining a copy
#of this software and associated documentation files (the "Software"), to deal
#in the Software without restriction, including without limitation the rights
#to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
#copies of the Software, and to permit persons to whom the Software is
#furnished to do so, subject to the following conditions:

#The above copyright notice and this permission notice shall be included in all
#copies or substantial portions of the Software.

#THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
#IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
#FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
#AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
#LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
#OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
#SOFTWARE.
#------------------------------------------------------------------------------

Start-Transcript
## Install .NET Core 5.0
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./dotnet-install.ps1" 

./dotnet-install.ps1 -Channel 5.0 -InstallDir c:\dotnet

# Install chocolately to be able to install git
Invoke-WebRequest 'https://chocolatey.org/install.ps1' -OutFile "./choco-install.ps1"
./choco-install.ps1

# Install Git with choco
choco install git -y

# Install nuget
choco install nuget.commandline -y

# clone the sample repo
New-Item -ItemType Directory -Path C:\git -Force
Set-Location C:\git
Write-host "cloning repo"
& 'C:\Program Files\git\cmd\git.exe' clone https://github.com/hugogirard/durableFunctionFormRecognizer.git

write-host "Changing directory to $((Get-Item -Path ".\" -Verbose).FullName)"
Set-Location c:\git\durableFunctionFormRecognizer\src\consoleSeeder\SeederApp

# Restore NuGet packages and build applocation
Write-host "restoring nuget packages"
c:\dotnet\dotnet.exe restore
c:\dotnet\dotnet.exe build

# Set the path for dotnet.
$OldPath=(Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name PATH).Path

$dotnetpath = "c:\dotnet"
IF(Test-Path -Path $dotnetpath)
{
    $NewPath=$OldPath+';'+$dotnetpath
    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name PATH -Value $NewPath
}

Stop-Transcript