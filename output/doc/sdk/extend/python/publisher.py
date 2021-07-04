
import clr, clrtype

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")
clr.AddReference("NLog")

import System
import NLog
import json

import Peach.Core
from Peach.Core import Variant
from Peach.Core.IO import BitwiseStream
from Peach.Pro.Core.Publishers import BasePythonPublisher

# Create wrappers for class attributes we will use
PublisherAttr = clrtype.attribute(Peach.Core.PublisherAttribute)
DescriptionAttr = clrtype.attribute(System.ComponentModel.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonPublisher(BasePythonPublisher):
	'''Example of adding a custom Monitor to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"


	_clrclassattribs = [
		System.SerializableAttribute,
		PublisherAttr("PythonPublisher", True),
		DescriptionAttr("Example Publisher in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	logger = None

	@property
	@clrtype.accepts()
	@clrtype.returns(clr.GetClrType(str))
	def Param1(self):
		return self.param1

	@Param1.setter
	@clrtype.accepts(clr.GetClrType(str))
	@clrtype.returns()
	def Param1(self, value):
		self.param1 = value

	@property
	@clrtype.accepts()
	@clrtype.returns(clr.GetClrType(str))
	def Param2(self):
		return self.param2

	@Param1.setter
	@clrtype.accepts(clr.GetClrType(str))
	@clrtype.returns()
	def Param2(self, value):
		self.param2 = value

	@property
	@clrtype.accepts()
	@clrtype.returns(NLog.Logger)
	def Logger(self):
		if self.logger == None:
			self.logger = NLog.LogManager.GetLogger("PythonPublisher")
		return self.logger

	@clrtype.accepts()
	@clrtype.returns()
	def __init__(self):
		print '>>> INIT'
		print '>>>  Param1: %s' % self.param1
		print '>>>  Param2: %s' % self.param2
		pass

	@clrtype.accepts(BitwiseStream)
	@clrtype.returns()
	def OnOutput(self, data):
		'''Output data as a json string'''

		out = ""
		for i in range(data.Length):
			out += chr(data.ReadByte())

		print json.dumps(out)

# end


