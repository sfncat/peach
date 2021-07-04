
/* Copyright (c) Peach Fuzzer, LLC
 *
 * Example of a native Peach Agent using the Poco opensource C++ library.
 *
 * This example uses the REST Agent Channel.
 *
 * Authors:
 *   Michael Eddington <mike@dejavusecurity.com>
 *
 */

#include "Poco/Net/HTTPServer.h"
#include "Poco/Net/HTTPRequestHandler.h"
#include "Poco/Net/HTTPRequestHandlerFactory.h"
#include "Poco/Net/HTTPServerParams.h"
#include "Poco/Net/HTTPServerRequest.h"
#include "Poco/Net/HTTPServerResponse.h"
#include "Poco/Net/HTTPServerParams.h"
#include "Poco/Net/HTMLForm.h"
#include "Poco/Net/ServerSocket.h"
#include "Poco/Timestamp.h"
#include "Poco/DateTimeFormatter.h"
#include "Poco/DateTimeFormat.h"
#include "Poco/Exception.h"
#include "Poco/ThreadPool.h"
#include "Poco/Util/ServerApplication.h"
#include "Poco/Util/Option.h"
#include "Poco/Util/OptionSet.h"
#include "Poco/Util/HelpFormatter.h"
#include "Poco/JSON/JSON.h"
#include "Poco/JSON/Parser.h"
#include "Poco/Dynamic/Var.h"
#include "Poco/Base64Decoder.h"
#include <iostream>

using Poco::Net::HTMLForm;
using Poco::Net::NameValueCollection;
using Poco::Net::ServerSocket;
using Poco::Net::HTTPRequestHandler;
using Poco::Net::HTTPRequestHandlerFactory;
using Poco::Net::HTTPServer;
using Poco::Net::HTTPServerRequest;
using Poco::Net::HTTPServerResponse;
using Poco::Net::HTTPServerParams;
using Poco::Timestamp;
using Poco::DateTimeFormatter;
using Poco::DateTimeFormat;
using Poco::ThreadPool;
using Poco::Util::ServerApplication;
using Poco::Util::Application;
using Poco::Util::Option;
using Poco::Util::OptionSet;
using Poco::Util::OptionCallback;
using Poco::Util::HelpFormatter;
using namespace Poco::JSON;
using namespace std;

class PublisherRequestHandler: public HTTPRequestHandler
{
public:
    PublisherRequestHandler()
    {
		_urlPrefix = "/Agent/Publisher/";
		_cmdCreatePublisher =	_urlPrefix + "CreatePublisher";
		_cmdSetIteration = _urlPrefix + "Set_Iteration";
		_cmdSetIsControlIteration = _urlPrefix + "Set_IsControlIteration";
		_cmdGetResult = _urlPrefix + "Get_Result";
		_cmdSetResult = _urlPrefix + "Set_Result";
		_cmdStart = _urlPrefix + "start";
		_cmdOpen = _urlPrefix + "open";
		_cmdClose = _urlPrefix + "close";
		_cmdAccept = _urlPrefix + "accept";
		_cmdCall = _urlPrefix + "call";
		_cmdSetProperty = _urlPrefix + "setProperty";
		_cmdGetProperty = _urlPrefix + "getProperty";
		_cmdOutput = _urlPrefix + "output";
		_cmdInput = _urlPrefix + "input";
		_cmdWantBytes = _urlPrefix + "WantBytes";
		_cmdReadBytes = _urlPrefix + "ReadBytes";
		_cmdReadAllBytes = _urlPrefix + "ReadAllBytes";
    }

    void handleRequest(HTTPServerRequest& request,
                       HTTPServerResponse& response)
    {
		Object::Ptr jsonObject;

		Application& app = Application::instance();
			app.logger().information("Request from "
				+ request.clientAddress().toString() + " " + request.getURI());

        response.setChunkedTransferEncoding(true);
        response.setContentType("application/json");

		std::string uri = request.getURI();
        std::ostream& ostr = response.send();

		if(request.getMethod() == "POST")
		{
			std::string json( (std::istreambuf_iterator<char>( request.stream() )),
               (std::istreambuf_iterator<char>()) );

			app.logger().information(json);

			// Parse the JSON and get the Results
			//
			Poco::Dynamic::Var parsedJson;
			Poco::Dynamic::Var parsedJsonResult;
			Parser parser;
			parsedJson = parser.parse(json);
			parsedJsonResult = parser.result();

			// Get the JSON Object
			//
			jsonObject = parsedJsonResult.extract<Object::Ptr>();
		}

		if(uri.compare(0, _cmdCreatePublisher.length(), _cmdCreatePublisher) == 0)
		{
			// A Peach instance is connecting to us. We should disconnect any
			// existing connections.
			
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdSetIteration.length(), _cmdSetIteration) == 0)
		{
			string iteration = GetJsonValue(jsonObject, "iteration");

			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdSetIsControlIteration.length(), _cmdSetIsControlIteration) == 0)
		{
			string isControlIteration = GetJsonValue(jsonObject, "isControlIteration");

			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdGetResult.length(), _cmdGetResult) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdSetResult.length(), _cmdSetResult) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdStart.length(), _cmdStart) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdOpen.length(), _cmdOpen) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdClose.length(), _cmdClose) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdAccept.length(), _cmdAccept) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdCall.length(), _cmdCall) == 0)
		{
			// name of method from call action
			string method = GetJsonValue(jsonObject, "method");
			vector<stringstream> args;

			Array::Ptr jsonArgs = jsonObject->getArray("args");

			//for(Array::ValueVec::const_iterator it = jsonArgs->begin(); it != jsonArgs->end(); ++it)
			//{
			//	Object::Ptr arg = it->extract<Object::Ptr>();
			for(int cnt = 0; cnt < jsonArgs->size(); ++cnt)
			{
				Object::Ptr arg = (jsonArgs->get(cnt)).extract<Object::Ptr>();

				// Name of argument/parameter
				string name = GetJsonValue(arg, "name");
				// Type of parameter
				string type = GetJsonValue(arg, "type");
				// Binary data from Peach
				std::stringstream data;

				istringstream ifs(GetJsonValue(arg, "data"));
				Poco::Base64Decoder b64in(ifs);

				copy(istreambuf_iterator<char>(b64in),
					istreambuf_iterator<char>(),
					ostreambuf_iterator<char>(data));

				// TODO - do something with argument
			}

			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdSetProperty.length(), _cmdSetProperty) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdGetProperty.length(), _cmdGetProperty) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdOutput.length(), _cmdOutput) == 0)
		{
			istringstream ifs(GetJsonValue(jsonObject, "data"));
			Poco::Base64Decoder b64in(ifs);
			std::stringstream data;

			copy(istreambuf_iterator<char>(b64in),
				istreambuf_iterator<char>(),
				ostreambuf_iterator<char>(data));

			// data now contains the raw binary data

			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdInput.length(), _cmdInput) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdWantBytes.length(), _cmdWantBytes) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdReadBytes.length(), _cmdReadBytes) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else if(uri.compare(0, _cmdReadAllBytes.length(), _cmdReadAllBytes) == 0)
		{
			ostr << "{ \"error\":\"false\", \"errorString\":null }";
		}
		else
		{
			ostr << "{\"Error\":\"Error, unknown command.\"}";
		}
    }

	// Helper method for getting std::string value back
	string GetJsonValue(Object::Ptr aoJsonObject, const char *aszKey)
	{
		Poco::Dynamic::Var loVariable;
		string lsReturn;
		string lsKey(aszKey);

		// Get the member Variable
		//
		loVariable = aoJsonObject->get(lsKey);

		// Get the Value from the Variable
		//
		lsReturn = loVariable.convert<std::string>();

		return lsReturn;
	}


private:
	string _urlPrefix;
	string _cmdCreatePublisher;
	string _cmdSetIteration;
	string _cmdSetIsControlIteration;
	string _cmdGetResult;
	string _cmdSetResult;
	string _cmdStart;
	string _cmdOpen;
	string _cmdClose;
	string _cmdAccept;
	string _cmdCall;
	string _cmdSetProperty;
	string _cmdGetProperty;
	string _cmdOutput;
	string _cmdInput;
	string _cmdWantBytes;
	string _cmdReadBytes;
	string _cmdReadAllBytes;
};

class PeachRequestHandler: public HTTPRequestHandler
{
public:
    PeachRequestHandler()
    {
		_urlPrefix = "/Agent/";
		_cmdAgentConnect =	_urlPrefix + "AgentConnect";
		_cmdAgentDisconnect = _urlPrefix + "AgentDisconnect";
		_cmdCreatePublisher = _urlPrefix + "CreatePublisher";
		_cmdStartMonitor = _urlPrefix + "StartMonitor";
		_cmdStopMonitor = _urlPrefix + "StopMonitor";
		_cmdStopAllMonitors = _urlPrefix + "StopAllMonitors";
		_cmdSessionStarting = _urlPrefix + "SessionStarting";
		_cmdSessionFinished = _urlPrefix + "SessionFinished";
		_cmdIterationStarting = _urlPrefix + "IterationStarting";
		_cmdIterationFinished = _urlPrefix + "IterationFinished";
		_cmdDetectedFault = _urlPrefix + "DetectedFault";
		_cmdGetMonitorData = _urlPrefix + "GetMonitorData";
		_cmdMustStop = _urlPrefix + "MustStop";
    }

    void handleRequest(HTTPServerRequest& request,
                       HTTPServerResponse& response)
    {
		Object::Ptr jsonObject;

		Application& app = Application::instance();

		app.logger().information(string("Request from ")
			+ request.clientAddress().toString() + " " + request.getURI());

        response.setChunkedTransferEncoding(true);
        response.setContentType("application/json");

		std::string uri = request.getURI();
        std::ostream& ostr = response.send();

		if(request.getMethod() == "POST")
		{
			std::string json( (std::istreambuf_iterator<char>( request.stream() )),
               (std::istreambuf_iterator<char>()) );

			app.logger().information(json);

			// Parse the JSON and get the Results
			//
			Poco::Dynamic::Var parsedJson;
			Poco::Dynamic::Var parsedJsonResult;
			Parser parser;
			parsedJson = parser.parse(json);
			parsedJsonResult = parser.result();

			// Get the JSON Object
			//
			jsonObject = parsedJsonResult.extract<Object::Ptr>();
		}

		if(uri.compare(0, _cmdAgentConnect.length(), _cmdAgentConnect) == 0)
		{
			// A Peach instance is connecting to us. We should disconnect any
			// existing connections.
			
			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdAgentDisconnect.length(), _cmdAgentDisconnect) == 0)
		{
			// Peach instance is disconnecting from us

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdCreatePublisher.length(), _cmdCreatePublisher) == 0)
		{
			// Indicates an I/O instance should be created to send/receive data via
			// our agent instance.

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdStartMonitor.length(), _cmdStartMonitor) == 0)
		{
			// Start a monitor

			HTMLForm form(request);

			string name = form["name"];
			string cls = form["cls"];

			app.logger().information(name + ":" + cls);

			/* Example of arguments json:

			{
				"args":{
					"CommandLine":"mspaint.exe fuzzed.png",
					"WinDbgPath":"C:\\Program Files (x86)\\Debugging Tools for Windows (x86)",
					"StartOnCall":"ScoobySnacks"
					}
			}

			*/

			// Get arguments from json
			std::vector<std::string> argumentNames;
			Object::Ptr jsonArgsObject = jsonObject->getObject("args");
			jsonArgsObject->getNames(argumentNames);

			for(std::vector<std::string>::iterator it = argumentNames.begin(); it != argumentNames.end(); ++it)
			{
				std::string argName = *it;
				std::string argValue = GetJsonValue(jsonArgsObject, argName.c_str());

				// TODO - Use argument!
				app.logger().information(argName + ": " + argValue);
			}

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdStopMonitor.length(), _cmdStopMonitor) == 0)
		{
			// stop a started monitor

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdStopAllMonitors.length(), _cmdStopAllMonitors) == 0)
		{
			// stop all started monitors

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdSessionStarting.length(), _cmdSessionStarting) == 0)
		{
			// starting our fuzzing run

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdSessionFinished.length(), _cmdSessionFinished) == 0)
		{
			// finished our fuzzing run

			ostr << "{ \"Status\":\"true\" }";
		}
		else if(uri.compare(0, _cmdIterationStarting.length(), _cmdIterationStarting) == 0)
		{
			// starting a fuzzing iteration
			HTMLForm form(request);

			unsigned int iterationCount;
			istringstream ( form["iterationCount"] ) >> iterationCount;

			string isReproduction = form["isReproduction"]; // "true" or "false"

			// TODO - Implement me!
		}
		else if(uri.compare(0, _cmdIterationFinished.length(), _cmdIterationFinished) == 0)
		{
			// finished our fuzzing iteration

			ostr << "{ \"Status\":\"false\" }";
		}
		else if(uri.compare(0, _cmdDetectedFault.length(), _cmdDetectedFault) == 0)
		{
			// did one of our monitors detect a fault?

			// did not detect fault
			//ostr << " { \"Status\":\"false\" }";

			// did detect fault
			ostr << "{ \"Status\":\"true\" }";

		}
		else if(uri.compare(0, _cmdGetMonitorData.length(), _cmdGetMonitorData) == 0)
		{
			// TODO - Implement me!

			/*
			{
				"Results":[
					{
						"iteration":0,
						"controlIteration":false,
						"controlRecordingIteration":false,
						"type":0,  (0 unknown, 1 Fault, 2 Data)
						"detectionSource":null,
						"title":null,
						"description":null,
						"majorHash":null,
						"minorHash":null,
						"exploitability":null,
						"folderName":null,
						"collectedData":[
							{"Key":"data1","Value":"AA=="}
						]
					}
				]
			}
			*/

			ostr << "{\"Results\":[{\"iteration\":0,\"controlIteration\":false,\"controlRecordingIteration\":false,\"type\":1,\"detectionSource\":\"Native Agent\",\"title\":\"Test Fault\",\"description\":\"Test fault from native agent.\",\"majorHash\":null,\"minorHash\":null,\"exploitability\":\"CRITICAL\",\"folderName\":\"CRITICAL-1-1\",\"collectedData\":[{\"Key\":\"collected-data.bin\",\"Value\":\"AA==\"}]}]}";
		}
		else if(uri.compare(0, _cmdMustStop.length(), _cmdMustStop) == 0)
		{
			// should we stop all fuzzing?

			ostr << "{ \"Status\":\"false\" }";
		}
		else
		{
			ostr << "{\"Error\":\"Error, unknown command.\"}";
		}
    }

	// Helper method for getting std::string value back
	string GetJsonValue(Object::Ptr aoJsonObject, const char *aszKey)
	{
		Poco::Dynamic::Var loVariable;
		string lsReturn;
		string lsKey(aszKey);

		// Get the member Variable
		//
		loVariable = aoJsonObject->get(lsKey);

		// Get the Value from the Variable
		//
		lsReturn = loVariable.convert<std::string>();

		return lsReturn;
	}


private:
	string _urlPrefix;
	string _cmdAgentConnect;
	string _cmdAgentDisconnect;
	string _cmdCreatePublisher;
	string _cmdStartMonitor;
	string _cmdStopMonitor;
	string _cmdStopAllMonitors;
	string _cmdSessionStarting;
	string _cmdSessionFinished;
	string _cmdIterationStarting;
	string _cmdIterationFinished;
	string _cmdDetectedFault;
	string _cmdGetMonitorData;
	string _cmdMustStop;
};

class ErrorRequestHandler: public HTTPRequestHandler
{
public:
    ErrorRequestHandler()
    {
    }

    void handleRequest(HTTPServerRequest& request,
                       HTTPServerResponse& response)
    {
        Application& app = Application::instance();
        app.logger().information("Request from "
            + request.clientAddress().toString());

        response.setChunkedTransferEncoding(true);
        response.setContentType("text/html");

        std::ostream& ostr = response.send();
        ostr << "<html><head><title>Peach Native Agent</title></head>";
        ostr << "<body><p style=\"text-align: center; "
                "font-size: 48px;\">";
        ostr << "Error, only Peach Fuzzer via Rest Agent Channel is expected.";
        ostr << "</p></body></html>";
    }
};

class PeachRequestHandlerFactory: public HTTPRequestHandlerFactory
{
public:
    PeachRequestHandlerFactory()
    {
		_urlPrefixAgent = "/Agent/";
		_urlPrefixPublisher = "/Agent/Publisher/";
    }

    HTTPRequestHandler* createRequestHandler(
        const HTTPServerRequest& request)
    {
		if(request.getURI().compare(0, _urlPrefixPublisher.length(),  _urlPrefixPublisher) == 0)
			return new PublisherRequestHandler();

		if (request.getURI().compare(0, _urlPrefixAgent.length(), _urlPrefixAgent) == 0)
            return new PeachRequestHandler();
        
		if (request.getURI() == "/")
		{
            return new ErrorRequestHandler();
		}
        else
            return 0;
    }

private:
	string _urlPrefixAgent;
	string _urlPrefixPublisher;
};

class HTTPPeachServer: public Poco::Util::ServerApplication
{
public:
    HTTPPeachServer(): _helpRequested(false)
    {
    }

    ~HTTPPeachServer()
    {
    }

protected:
    void initialize(Application& self)
    {
        loadConfiguration();
        ServerApplication::initialize(self);
    }

    void uninitialize()
    {
        ServerApplication::uninitialize();
    }

    void defineOptions(OptionSet& options)
    {
        ServerApplication::defineOptions(options);

        options.addOption(
        Option("help", "h", "display argument help information")
            .required(false)
            .repeatable(false)
            .callback(OptionCallback<HTTPPeachServer>(
                this, &HTTPPeachServer::handleHelp)));
    }

    void handleHelp(const std::string& name, 
                    const std::string& value)
    {
        HelpFormatter helpFormatter(options());
        helpFormatter.setCommand(commandName());
        helpFormatter.setUsage("OPTIONS");
        helpFormatter.setHeader(
            "A web server that serves the current date and time.");
        helpFormatter.format(std::cout);
        stopOptionsProcessing();
        _helpRequested = true;
    }

    int main(const std::vector<std::string>& args)
    {
        if (!_helpRequested)
        {
            unsigned short port = (unsigned short)
                config().getInt("HTTPPeachServer.port", 9980);
            
			cout << "Listening on port " << port << ".\n";

            ServerSocket svs(port);
            HTTPServer srv(new PeachRequestHandlerFactory(), 
                svs, new HTTPServerParams);
            
			srv.start();
            waitForTerminationRequest();
            srv.stop();
        }

        return Application::EXIT_OK;
    }

private:
    bool _helpRequested;
};

int main(int argc, char** argv)
{
	cout << "\n";
	cout << ">> Peach Native Agent Sample\n";
	cout << ">> Copyright (c) Peach Fuzzer, LLC\n";
	cout << "\n";

    HTTPPeachServer app;
    return app.run(argc, argv);
}

// end
