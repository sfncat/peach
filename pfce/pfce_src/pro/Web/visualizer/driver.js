/* Global variables */

// HTML console output
var output = null;
// HTML iframe for tests
var testFrame = null;;
// Timer variable
var timer = null;
// Only required for browsers like Opera
var interval = null;
// Debug variable
var debug = false;
// buffer to keep incomplete lines received from server
var buffer = "";
// queue for our tests
var testQueue = new Array();
// Ready variable
var ready = false;
// If log() should also dump()
var logDump = true;

var logBuffer = new Array();

var isOpera = typeof window.opera !== 'undefined';
var isChrome = navigator.userAgent.toLowerCase().indexOf('chrome') > -1;

/* End of global variables */

if (typeof(dump) == "undefined") {
	dump = function(msg) { log(msg); }
	// Avoid recursion
	logDump = false;
}

function init() {
  // Grab console reference
  output = document.getElementById("output");
  testFrame = document.getElementById("testFrame");

  // Add onload event for our iframe
  document.getElementById("testFrame").onload = loadedChild;
  document.getElementById("testFrame").onerror = loadedChild;

  var params = mapParametersSequence(location.search);
  ws = new WebSocket("ws://127.0.0.1:8889/");
  log("Socket: Attempting connection...");
  ws.onopen = function(evt) { onOpen(evt) };
  ws.onclose = function(evt) { onClose(evt) };
  ws.onmessage = function(evt) { onMessage(evt) };
  ws.onerror = function(evt) { onError(evt) };
}

function mapParametersSequence(seq)
{
    var dict = {};

    seq = seq.substring(1).split("&");

    for (var i=0; i<seq.length; i++)
    {
        var pair = seq[i].split('=');
        var key = decodeURIComponent(pair[0]);
        var value = decodeURIComponent(pair[1]);

        dict[key] = value;
    }

    return dict;
}

function onOpen(evt) {
  log("Socket open");

  isChrome = false;
  if (isChrome) {
	log("isChrome");
    var body = document.getElementsByTagName('body')[0];
    var img = document.createElement('img');
    img.width=300;
    img.height=300;
    img.alt="Here be images";
    img.id="img";
    body.appendChild(img);
    //body.removeChild(testFrame);
    //testFrame = this;
    testFrameDoc = document;
    appendChildScript(createPrologCodeOnly());
  } else {
   // Initialize to some empty document
    testFrame.src = 'data:text/html;charset=utf-8,'  
  	  + escape('<html><head>') 
  	  + createPrologData()
  	  + escape('</head><body></body></html>');
    testFrameDoc = testFrame.contentDocument;
 }
}

function onClose(evt) {
  log("Socket closed");
}

function onError(evt) {
  log("Socket error: " + evt.data);
}

// This function assembles arbitrary data received from the
// server into lines. If the line is a JSON-encoded message,
// that message is passed to processJSONLine() for further
// handling.
function onMessage(evt) {
  var data = evt.data;
  debugLog("Socked received: " + data);
  // TODO: FIXME: Temporary hack to circumvent problems
  // with toString and UTF-8 conversion. This will break
  // binary data transfer. Ultimately, we must encode all
  // content data in base64 before transfer.
  //data = data.toString()
  /*var dataStr = "";
  for (var i = 0; i < data.length; ++i) {
	dataStr += String.fromCharCode(data.get(i));
  }
  data = dataStr;*/

  var keepLastChunk = true;
  // Data ends exactly with newline, no remainder to keep in buffer
  if (data.substr(-1) == "\n") {
    keepLastChunk = false;
  }

  var chunks = data.split("\n");
  var lastChunk;

  // If the last chunk is incomplete, don't process it
  if (keepLastChunk) {
    lastChunk = chunks.pop();
  }

  for (i in chunks) {
    chunk = chunks[i];

    // First chunk, prepend buffer and reset it
    if (i == 0) {
      chunk = buffer + chunk;
      buffer = "";
    }

    // Process only JSON encoded messages, ignore everything else
    if (chunk.substr(0,1) == '{') {
      processJSONLine(chunk);
    }
  }

  // If we have an incomplete chunk, keep it in our buffer
  if (keepLastChunk) {
    buffer += lastChunk;
  }
}

// when we receive a JSON-encoded testcase from the server, write it to a local
// temp file and notify our test-loading worker (via a custom event)
// data is of the form:
// {"type": "html", "content": "<html><body>...</body></html>"} or
// {"type": "js", "content": "var f = function() { return 1 }..."}
function processJSONLine(data) {
  debugLog("[JSON] received: " + data);
  var resp = JSON.parse(data);
  switch (resp.type) {
  case "msg":
    var msg = resp.content;
    switch(msg) {
      case "reset":
	    /* We don't implement reset. Instead we let the command timeout and will be shutdown */
        ready = false;
        break;
      case "evaluate":
        // If we have any tests, we start processing the first test now.
	    // The completion of this test will automatically trigger the processing
	    // of the next test in queue, until the queue is entirely empty.
	    // If we don't have any tests at this point (which does not make sense
	    // but is perfectly valid), we directly respond to the server again.
        if (testQueue.length > 0) {
	      processTest(testQueue.shift());
        } else {
          debugLog("Evaluation complete");
          ws.send('{"msg": "Evaluation complete"}\n');
        }
        break;
      case "ping":
        ws.send('{"msg": "pong"}\n');
        break;
      default:
        warnLog("Received unknown message from server: " + msg);
    }
    break;
  case "html":
  case "xhtml":
  case "js":
  case "svg":
  case "jpg":
  case "template":
    // One of our supported test types.
    // Store it in our queue for processing later when the
    // server sends us an evaluate command.
    testQueue.push(resp);
    break;
  default:
    // Malformed data or incomplete JSON object? Warn!
    warnLog("Malformed or incomplete JSON object received: " + data);
    break;
  }
}

// Process a single test, provided as JSON object
function processTest(testObj) {
    loadTemplate(testObj.content);
}

// Load received data as data URL into iframe
function loadChild(data) {
  testFrame.src = 'data:text/html;charset=utf-8,' + createPrologData() + escape(data);
  timer = setTimeout("abortedChild()", 5000);
}

function loadTemplate(data) {
  testFrame.src = 'data:text/html;charset=utf-8,' + data;
  timer = setTimeout("loadedChild()", 5000);
}

function createPrologData() {
  var prologData = '<script src="data:text/javascript;charset=utf-8,';
  //prologData += "netscape.security.PrivilegeManager.enablePrivilege = function() {};\n";
  prologData += escape(escape(createPrologCodeOnly()));
  prologData += '"></script>';
  return prologData;
}

function createPrologCodeOnly() {
  var prolog = {
    gc: function gc() { },
    alert: function alert(msg) { }
  };
  var prologCode = "";
  for (var f in prolog) {
    prologCode += prolog[f].toString();
  }
  return prologCode;
}

function appendChildScript(data) {
  timer = setTimeout("abortedChild()", 5000);
  var frameBody = testFrameDoc.getElementsByTagName('body')[0];
  var frameScript = testFrameDoc.createElement('script');
  
  frameScript.type = 'text/javascript';
  frameScript.src = 'data:text/javascript;charset=utf-8,' + escape(data);

  if (!isOpera) {
    frameScript.onload = loadedChild;
    frameBody.appendChild(frameScript);
  } else {
    frameBody.appendChild(frameScript);
    // Opera doesn't support onload for script tags added, use readyState
    interval = setInterval(function() {
      if (/loaded|complete/.test(testFrameDoc.readyState)) {
	    clearInterval(interval);
	    loadedChild();
      }
    }, 10);
  }
}

function addChildImage(data) {
  timer = setTimeout("abortedChild()", 5000);
  var frameImgTag = testFrame.contentDocument.getElementById('img');

  frameImgTag.onload = loadedChild;
  frameImgTag.onerror = loadedChild;
  frameImgTag.src = "data:image/svg+xml," + escape(data);
}

function addChildImageJPEG(data) {
  timer = setTimeout("abortedChild()", 5000);
  var frameImgTag = testFrame.contentDocument.getElementById('img');

  frameImgTag.onload = loadedChild;
  frameImgTag.onerror = loadedChild;
  frameImgTag.src = "data:image/jpeg;base64," + data;
}

function completedChild() {
  if (testQueue.length > 0)
  {
    // Process the next test. This call will cause this
    // function to be called again once that test is complete.
    processTest(testQueue.shift());
  }
  else
  {
    if (!ready)
	{
      // request the first testcase
      debugLog("Ready");
      ws.send('{"msg": "Evaluation complete"}\n');
      ws.send('{"msg": "Client ready"}\n');
      ready = true;
    }
	else
	{
      debugLog("LangFuzz-Client: Evaluation complete");
      //ws.send('{"msg": "Evaluation complete"}\n');
    }
  }
}

// Called on load of the child
function loadedChild() {
   clearTimeout(timer);
   debugLog("Child load complete");
   completedChild();
}

// Called on timeout while waiting for child loading
function abortedChild() {
   debugLog("Child loading aborted");
   completedChild();
}

/* Logging functions */
function debugLog(message) {
  if (debug) { log(message); }
}

function warnLog(message) {
  log(message);
}

function log(message) {
  var pre = document.createElement("p");
  pre.style.wordWrap = "break-word";
  pre.innerHTML = message;
  output.appendChild(pre);

  logBuffer.push(pre);
  if (logBuffer.length > 100) {
	  var oldPre = logBuffer.shift();
	  output.removeChild(oldPre);
  }
  
  if (logDump) {
  	dump(message + "\n");
  }
}
/* End of logging functions */

// Add event listener for starting up
window.addEventListener("load", init, false);
