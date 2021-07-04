
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Analyzer, Variant
from Peach.Core.Dom import Block, String, DataElement
from Peach.Pro.Core.Analyzers import BasePythonAnalyzer

# Create wrappers for class attributes we will use
AnalyzerAttr = clrtype.attribute(Peach.Core.AnalyzerAttribute)
DescriptionAttr = clrtype.attribute(System.ComponentModel.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonAnalyzer(BasePythonAnalyzer, System.Runtime.Serialization.ISerializable):
	'''Example of adding a custom Analyzer to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"

	_clrclassattribs = [
		System.SerializableAttribute,
		AnalyzerAttr("PythonAnalyzer", True),
		DescriptionAttr("Example Analyzer in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	@clrtype.accepts()
	@clrtype.returns()
	def __init__(self):
		pass

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	@clrtype.returns()
	def __init__(self, args):
		print '>>> ANALYZER INIT Param1=%s' % (str(args['Param1']))
		pass

	@clrtype.accepts(DataElement, System.Collections.Generic.Dictionary[DataElement, Peach.Core.Cracker.Position])
	@clrtype.returns()
	def asDataElement(self, parent, args):
		s = String()
		s.DefaultValue = Variant("Hello From Analyzer\n")

		block = Block(parent.name)
		block.Add(s)

		parent.parent[parent.name] = block

# end
