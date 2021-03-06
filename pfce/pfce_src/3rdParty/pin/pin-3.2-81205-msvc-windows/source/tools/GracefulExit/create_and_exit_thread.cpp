/*BEGIN_LEGAL 
Intel Open Source License 

Copyright (c) 2002-2016 Intel Corporation. All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.  Redistributions
in binary form must reproduce the above copyright notice, this list of
conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.  Neither the name of
the Intel Corporation nor the names of its contributors may be used to
endorse or promote products derived from this software without
specific prior written permission.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
END_LEGAL */
#include <iostream>
#include <fstream>
#include <ostream>
#include <string>
#include <vector>
#include "pin.H"
#include "create_and_exit_thread_utils.h"

using std::cerr;
using std::endl;
using std::ostream;
using std::ofstream;
using std::string;
using std::vector;


/**************************************************
 * Global variables                               *
 **************************************************/

const unsigned int fiveMinutesInMilliseconds = 5 * 60 * 1000;
const unsigned int maxNumTests = 500;

volatile unsigned int numThreads = 0;
volatile unsigned int testCounter = 0;
volatile bool finiCalled = false;
volatile THREADID currTid = INVALID_THREADID;
volatile THREADID nextTid = INVALID_THREADID;

PIN_SEMAPHORE noActiveThreads;

ostream* outFile = NULL;

KNOB<string> KnobOutputFile(KNOB_MODE_WRITEONCE,  "pintool",
    "o", "", "Specify file name for the tool's output. If no filename is specified, the output will be directed to stderr.");


/**************************************************
 * Analysis routines                              *
 **************************************************/

void DoTest(const THREADID tid, CONTEXT* ctxt)
{
    *outFile << "TOOL: Thread " << tid << " executing DoTest." << endl;

    if (!PIN_SemaphoreTimedWait(&noActiveThreads, fiveMinutesInMilliseconds))
    {
        PIN_ExitProcess(RETVAL_FAILURE_TEST_TIMEOUT);
    }
    PIN_SemaphoreClear(&noActiveThreads);

    BOOL ret = PIN_SpawnApplicationThread(ctxt);
    if (!ret)
    {
        *outFile << "TOOL ERROR: Failed to spawn new application thread." << endl;
        PIN_ExitProcess(RETVAL_FAILURE_TOOL_FAILED_TO_SPAWN);
    }
    PIN_ExitThread(RETVAL_SUCCESS);

    *outFile << "TOOL ERROR: Should not reach this point in DoTest." << endl;
    PIN_ExitProcess(RETVAL_FAILURE_TOOL_FAILED_TO_EXIT);
}


/**************************************************
 * Instrumentation routines                       *
 **************************************************/

VOID Instruction(INS ins, VOID* v)
{
    INS_InsertCall(ins, IPOINT_BEFORE, AFUNPTR(DoTest), IARG_THREAD_ID, IARG_CONTEXT, IARG_END);
}


/**************************************************
 * Callback functions                             *
 **************************************************/

VOID ThreadStart(const THREADID tid, CONTEXT* ctxt, INT32 flags, VOID* v)
{
    ASSERTX(INVALID_THREADID == nextTid);

    if (finiCalled)
    {
        cerr << "TOOL ERROR: ThreadStart called after application Fini." << endl;
        PIN_ExitProcess(RETVAL_FAILURE_START_AFTER_FINI);
    }

    if (maxNumTests == testCounter)
    {
        PIN_ExitProcess(RETVAL_FAILURE_MAX_TRIALS);
    }
    ++testCounter;

    if (0 == numThreads)
    {
        ASSERTX(INVALID_THREADID == currTid);
        ASSERTX(PIN_SemaphoreIsSet(&noActiveThreads));
        ++numThreads;
        currTid = tid;
    }
    else if (1 == numThreads)
    {
        ASSERTX(INVALID_THREADID != currTid);
        ASSERTX(!PIN_SemaphoreIsSet(&noActiveThreads));
        ++numThreads;
        nextTid = tid;
    }
    else
    {
        PIN_ExitProcess(RETVAL_FAILURE_TOO_MANY_THREADS);
    }
}


VOID ThreadFini(const THREADID tid, const CONTEXT* ctxt, INT32 code, VOID* v)
{
    ASSERTX(tid == currTid);
    ASSERTX(!PIN_SemaphoreIsSet(&noActiveThreads));
    *outFile << "TOOL: ThreadFini for thread " << tid << " with code " << code << "." << endl;
    --numThreads;
    currTid = nextTid;
    nextTid = INVALID_THREADID;
    PIN_SemaphoreSet(&noActiveThreads);
}


VOID Fini(INT32 code, VOID* v)
{
    ASSERTX(0 == numThreads);
    *outFile << "TOOL: Fini called after " << testCounter << " trials." << endl;
    finiCalled = true;
    if (&cerr != outFile)
    {
        delete outFile;
    }
    PIN_SemaphoreFini(&noActiveThreads);
}


/**************************************************
 * Main function                                  *
 **************************************************/

int main(int argc, char* argv[])
{
    if(PIN_Init(argc, argv)) return RETVAL_FAILURE_PIN_INIT_FAILED;

    if (!PIN_SemaphoreInit(&noActiveThreads)) return RETVAL_FAILURE_SEMAPHORE_INIT_FAILED;
    PIN_SemaphoreSet(&noActiveThreads);

    outFile = KnobOutputFile.Value().empty() ? &cerr : new ofstream(KnobOutputFile.Value().c_str());

    INS_AddInstrumentFunction(Instruction, 0);

    PIN_AddThreadStartFunction(ThreadStart, 0);
    PIN_AddThreadFiniFunction(ThreadFini, 0);
    PIN_AddFiniFunction(Fini, 0);

    PIN_StartProgram();
    return RETVAL_FAILURE_TOOL_MAIN_RETURN;
}
