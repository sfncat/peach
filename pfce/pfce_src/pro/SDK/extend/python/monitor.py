
import clr, clrtype

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import System
import Peach.Core
from Peach.Core import Variant
from Peach.Core.Agent import IterationStartingArgs, MonitorData
from Peach.Core.Agent.MonitorData import Info
from Peach.Pro.Core.Agent.Monitors import BasePythonMonitor

# Create wrappers for class attributes we will use
MonitorAttr = clrtype.attribute(Peach.Core.Agent.MonitorAttribute)
DescriptionAttr = clrtype.attribute(System.ComponentModel.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonMonitor(BasePythonMonitor):
	'''Example of adding a custom Monitor to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   

	_clrclassattribs = [
		MonitorAttr("PythonMonitor"),
		DescriptionAttr("Example Monitor in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	@clrtype.accepts(clr.GetClrType(str))
	@clrtype.returns()
	def __init__(self, name):
		print ">>>> MONITOR INIT %s" % name
		pass

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), clr.GetClrType(str)])
	@clrtype.returns()
	def StartMonitor(self, args):
		print ">>>> START MONITOR '%s/%s' FROM PYTHON" % (self.Name, self.Class)
		for kv in args:
			print ">>>>   PARAM '%s' = '%s'" % (kv.Key, kv.Value)
		self.count = 0
		pass

	@clrtype.accepts()
	@clrtype.returns()
	def StopMonitor(self):
		print ">>>> STOP MONITOR FROM PYTHON"
		pass

	@clrtype.accepts()
	@clrtype.returns()
	def SessionStarting (self):
		print ">>>> SESSION STARTING FROM PYTHON"
		pass

	@clrtype.accepts()
	@clrtype.returns()
	def SessionFinished(self):
		print ">>>> SESSION FINISHED FROM PYTHON"
		pass

	@clrtype.accepts(IterationStartingArgs)
	@clrtype.returns()
	def IterationStarting(self, args):
		print ">>>> ITERATION STARTING FROM PYTHON"
		self.isReproduction = args.IsReproduction
		self.lastWasFault = args.LastWasFault
		self.count += 1
		pass

	@clrtype.accepts()
	@clrtype.returns()
	def IterationFinished(self):
		print ">>>> ITERATION FINISHED FROM PYTHON"
		pass

	@clrtype.accepts()
	@clrtype.returns(clr.GetClrType(bool))
	def DetectedFault(self):
		fault = (self.count % 2) == 0 or self.isReproduction
		print ">>>> DETECTED FAULT: %s" % fault
		return fault

	@clrtype.accepts()
	@clrtype.returns(MonitorData)
	def GetMonitorData(self):
		print ">>> GET MONITOR DATA"
		data = MonitorData()
		data.Title = "Fault generated from Python"
		data.Fault = MonitorData.Info()
		data.Fault.Description = "Description from Python"
		data.Fault.MajorHash = self.Hash("Major Hash Info Goes Here")
		data.Fault.MinorHash = self.Hash("Minor Hash Info Goes Here")
		data.Fault.Risk = "UNKNOWN"
		data.Fault.MustStop = False
		return data

	@clrtype.accepts(clr.GetClrType(str))
	@clrtype.returns()
	def Message(self, name):
		print ">>>> MESSAGE '%s' FROM PYTHON" % name
		pass

# end


