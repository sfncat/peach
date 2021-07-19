
####
#### This file contains a number of receipies specific to 
#### scripting Peach using IronPython.
####

##############################################################
## Example of casting Variant to byte[] to python byte string

import clr
clr.AddReference("Peach.Core")

from Peach.Core import Variant
from System import Array, Byte


# Cast a Variant object to Array[Byte]
byteArray = clr.Convert(variant, Array[Byte])

# Convert Array[Byte] to python byte string
bstr = bytes(byteArray)

# Convert python byte string back into an Array[Byte]
byteArray = Array[Byte](bstr)

# Convert Array[Byte] into Variant
variant = Variant(byteArray)


##############################################################
## Example of casting Variant to BitwiseStream and reading all data

import clr
clr.AddReference("Peach.Core")

from Peach.Core import Variant
from Peach.Core.IO import BitwiseStream
from System import Array, Byte

# Convert Variant object to BitwiseStream object
stream = clr.Convert(variant, BitwiseStream)

# Create a byte[] type to hold our data
byteArray = Array[Byte](range(stream.Length))

# Read data into our byte[] buffer
stream.Read(byteArray, 0, len(byteArray))

# Convert our byte[] to a python byte string
byteStr = bytes(byteArray)
