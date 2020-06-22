# LemulatorU
This is a part of the source code from a new project named "LemulatorU".
The intention of this is to virtualize .NET applications so that they can be inspected and debugged for malware.

## Usage
The **Execute(< string** **>)** method requires a path to the executeable.
You'll have to import "dnlib" either by DLL, or through a NuGet (``Install-Package dnlib -Version 3.3.2``).
