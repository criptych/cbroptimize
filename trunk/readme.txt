usage: cbroptimize FILENAME

	convert all cbr's in folder:
		for %i in (*.cbr) do cbrOptimize "%i"

try using included batch files:

	convertAllCbrRecursive.bat
	convertCbrCbzInFolder.bat
	convertCbrInFolder.bat

file cbrOptimize.exe.config  may be located in the same directory as cbroptimize.exe, then it reads quality and width from config file.
otherwise default values width=800, jpeg quality=50 are used.

Happy comic book converting!
Borza Industries