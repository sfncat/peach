
#
# Copyright (c) Peach Fuzzer, LLC
#
#
# Example Python Agent. Implement stubs as needed.
#
# Dependencies:
#  web.py - http://webpy.org/
#    sudo easy_install web.py
#
# Author:
#  Michael Eddington (mike@dejavusecurity.com)
#


import web
import json

urls = (
	'/Agent/AgentConnect',		'Agent_AgentConnect',
	'/Agent/StartMonitor', 'Agent_StartMonitor',
	'/Agent/SessionStarting',	'Agent_SessionStarting',
	'/Agent/IterationStarting', 'Agent_IterationStarting',
	'/Agent/IterationFinished',	'Agent_IterationFinished',
	'/Agent/DetectedFault',		'Agent_DetectedFault',
	'/Agent/GetMonitorData',	'Agent_GetMonitorData',
	'/Agent/SessionFinished',	'Agent_SessionFinished',
	'/Agent/StopAllMonitors',	'Agent_StopAllMonitors',
	'/Agent/MustStop',	'Agent_MustStop',
	'/Agent/AgentDisconnect',	'Agent_AgentDisconnect',
	
	'/Agent/Publisher/CreatePublisher',	'Agent_Publisher_CreatePublisher',
	'/Agent/Publisher/Set_Iteration',			'Agent_Publisher_Set_Iteration',
	'/Agent/Publisher/Set_IsControlIteration',	'Agent_Publisher_Set_IsControlIteration',
	'/Agent/Publisher/start',		'Agent_Publisher_start',
	'/Agent/Publisher/open',		'Agent_Publisher_open',
	'/Agent/Publisher/output',		'Agent_Publisher_output',
	'/Agent/Publisher/input',		'Agent_Publisher_input',
	'/Agent/Publisher/WantBytes',	'Agent_Publisher_WantBytes',
	'/Agent/Publisher/Read',		'Agent_Publisher_Read',
	'/Agent/Publisher/ReadByte',	'Agent_Publisher_ReadByte',
	'/Agent/Publisher/ReadBytes',	'Agent_Publisher_ReadBytes',
	'/Agent/Publisher/ReadAllBytes','Agent_Publisher_ReadAllBytes',
	'/Agent/Publisher/close',		'Agent_Publisher_close',
	'/Agent/Publisher/call',		'Agent_Publisher_call',
	'/Agent/Publisher/stop',		'Agent_Publisher_stop'
)
app = web.application(urls, globals())

class Agent_AgentConnect:
	def GET(self):
		
		# TODO - Place agent connect logic here
		
		return json.dumps({ "Status":"true" })

class Agent_StartMonitor:
	def POST(self):
		
		name = web.input().name
		cls = web.input().name
		
		monitor = json.loads(web.data())
		
		# Example of getting arguments assuming use of example
		#commandLine = monitor.args.CommandLine
		#winDbgPath = monitor.args.WinDbgPath
		#startOnCall = monitor.args.StartOnCall
		
		# TODO - Place start monitor logic here
		
		return json.dumps({ "Status":"true" })

class Agent_SessionStarting:
	def GET(self):
		
		# TODO - Place session starting logic here
		
		return json.dumps({ "Status":"true" })

class Agent_IterationStarting:
	def GET(self):
		
		iterationCount = web.input().iterationCount
		isReproduction = web.input().isReproduction
		
		# TODO - Place iteration starting logic here
		
		return json.dumps({ "Status":"true" })

class Agent_IterationFinished:
	def GET(self):
		
		# TODO - Place iteration finished logic here
		
		return json.dumps({ "Status":"true" })

class Agent_DetectedFault:
	def GET(self):
		
		# TODO - Place detected fault logic here
		#        or return false always. The example
		#        here will return true.
		
		return json.dumps({ "Status":"true" })

class Agent_MustStop:
	def GET(self):
		
		# TODO - Place detected fault logic here
		#        or return false always. The example
		#        here will return true.
		
		return json.dumps({ "Status":"false" })

class Agent_GetMonitorData:
	def GET(self):
		
		# TODO - Return actual result data (or null)
		
		return json.dumps({
		"Results":[
			{
			"iteration":0,
			"controlIteration":False,
			"controlRecordingIteration":False,
			"type":1,
			"detectionSource":None,
			"title":None,
			"description":None,
			"majorHash":None,
			"minorHash":None,
			"exploitability":None,
			"folderName":None,
			"collectedData":[
					{"Key":"data1","Value":"AA=="}
			]
			}
			]
			})

class Agent_SessionFinished:
	def GET(self):
		
		# TODO - Put sesison finished logic here
		
		return json.dumps({ "Status":"true" })

class Agent_StopAllMonitors:
	def GET(self):
		
		# TODO - Put stop all monitors logic here
		
		return json.dumps({ "Status":"true" })

class Agent_AgentDisconnect:
	def GET(self):
		
		# TODO - Put agent disconnect logic here
		
		return json.dumps({ "Status":"true" })

class Agent_Publisher_Set_Iteration:
	def POST(self):
		iteration = json.loads(web.data())['iteration']
		
		# TODO - Utalize iteration if needed
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_Set_IsControlIteration:
	def POST(self):
		isControlIteration = json.loads(web.data())['isControlIteration']
		
		# TODO - Utalize isControlIteration if needed
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_CreatePublisher:
	def POST(self):

		# TODO - Put create logic here

		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_start:
	def GET(self):

		# TODO - Put start logic here
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_open:
	def GET(self):
		
		# TODO - Put open logic here
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_output:
	def POST(self):
		print web.data()
		# data to output
		data = json.loads(web.data())['data']
		
		# TODO - Output data
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_input:
	def GET(self):
		
		# TODO - Logic for input
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_WantBytes:
	def POST(self):
		
		# count of requested bytes
		count = json.loads(web.data())['count']
		
		# TODO - Logic for want bytes
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_Read:
	def POST(self):
		
		# count of requested bytes
		offset = json.loads(web.data())['offset']
		count = json.loads(web.data())['count']
		
		# TODO - Return asked for count of bytes
		
		return json.dumps({ "count":11, "data":"T3V0cHV0IGRhdGE=", "error":"false", "errorString":None })

class Agent_Publisher_ReadByte:
	
	cnt = 0
	
	def GET(self):

		# TODO - Return a single byte

		# Example code will return 4 bytes per iteration
		
		if Agent_Publisher_ReadByte.cnt > 3:
			# Error to indicate EOF
			Agent_Publisher_ReadByte.cnt = 0
			return json.dumps({ "data":-1, "error":"false", "errorString":None })
		
		Agent_Publisher_ReadByte.cnt = Agent_Publisher_ReadByte.cnt + 1
		return json.dumps({ "data":42, "error":"false", "errorString":None })

class Agent_Publisher_ReadBytes:
	def POST(self):
		
		# count of requested bytes
		count = json.loads(web.data())['count']
		
		# TODO - Return asked for count of bytes
		
		return json.dumps({ "data":"T3V0cHV0IGRhdGE=", "error":"false", "errorString":None })

class Agent_Publisher_ReadAllBytes:
	def GET(self):
		
		# TODO - Return all available bytes
		
		return json.dumps({ "data":"T3V0cHV0IGRhdGE=", "error":"false", "errorString":None })

class Agent_Publisher_close:
	def GET(self):
		
		# TODO - Put close logic here
		
		return json.dumps({ "error":"false", "errorString":None })

class Agent_Publisher_call:
	def POST(self):
		call = json.loads(web.data())
		
		# method name
		method = call['method']
		
		# loop through arguments
		for arg in call['args']:
			# argument name
			name = arg['name']
			
			# argument data
			data = arg['data']
			
			# TODO - Do something with arguments!

		# TODO - Do something with method call

		# If method needs to return data, put 'data' member in returned object
		return json.dumps({ "value":"T3V0cHV0IGRhdGE=", "error":"false" })

		# If no data is needed set value to None
		#return json.dumps({ "value": None, "error":"false", "errorString":None })
		
		# Or you can return an error
		#return json.dumps({ "error":"true", "errorString":"Connection lost..." })


class Agent_Publisher_stop:
	def GET(self):

		return json.dumps({ "error":"false", "errorString":None })


if __name__ == "__main__":
	app.run()

# end
