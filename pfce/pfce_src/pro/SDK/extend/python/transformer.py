
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Variant
from Peach.Core.IO import BitwiseStream, BitStream
from Peach.Core.Dom import DataElement
from Peach.Pro.Core.Transformers import BasePythonTransformer

# Create wrappers for class attributes we will use
TransformerAttr = clrtype.attribute(Peach.Core.TransformerAttribute)
DescriptionAttr = clrtype.attribute(System.ComponentModel.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonTransformer(BasePythonTransformer):
	'''
	Example of adding a custom Transformer to Peach using only Python.

	BasePythonTransformer is a special base class needed to create
	pure python Transformers.
	'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   

	# This array sets the class attributes to use. This
	# is like saying [Fixup(...)] in c#
	_clrclassattribs = [
		System.SerializableAttribute,
		TransformerAttr("PythonTransformer", True),
		DescriptionAttr("Example Transformer in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	@clrtype.accepts(DataElement, System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	@clrtype.returns()
	def __init__(self, parent, args):
		print '>>> TRANSFORMER INIT Param1=%s' % (str(args['Param1']))
		pass

	@clrtype.accepts(BitwiseStream)
	@clrtype.returns(BitwiseStream)
	def internalEncode(self, data):
		print '>>> TRANSFORMER ENCODE'

		# Truncate output to 5 bytes
		if data.LengthBits < 40:
			return data;

		return data.SliceBits(5 * 8)

	@clrtype.accepts(BitStream)
	@clrtype.returns(BitStream)
	def internalDecode(self, data):
		print '>>> TRANSFORMER DECODE'

		# Duplicate data prior to input
		ret = BitStream()
		data.CopyTo(ret)
		data.Position = 0
		data.CopyTo(ret)
		ret.Position = 0
		return ret
# end


