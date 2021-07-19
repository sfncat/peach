
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Variant, Fixup
from Peach.Core.Dom import Block, String, DataElement
from Peach.Pro.Core.Fixups import BasePythonFixup

# Create wrappers for class attributes we will use
FixupAttr = clrtype.attribute(Peach.Core.FixupAttribute)
DescriptionAttr = clrtype.attribute(System.ComponentModel.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonFixup(BasePythonFixup):
	'''
	Example of adding a custom Fixup to Peach using only Python.

	BasePythonFixup is a special base class needed to create
	pure python Fixups.
	'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   

	# This array sets the class attributes to use. This
	# is like saying [Fixup(...)] in c#
	_clrclassattribs = [
		System.SerializableAttribute,
		FixupAttr("PythonFixup", True),
		DescriptionAttr("Example Analyzer in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	@clrtype.accepts(DataElement, System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	@clrtype.returns()
	def __init__(self, parent, args):
		print '>>> FIXUP INIT Param1=%s' % (str(args['Param1']))
		pass

	@clrtype.accepts()
	@clrtype.returns(Variant)
	def fixupImpl(self):
		return Variant("hello from python fixup\n")


# end


