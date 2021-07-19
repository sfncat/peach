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
/*
 * This test only runs with the application "action_pending_cachefull_app.c".  It helps
 * test the PIN_IsActionPending() API.
 */
 
#include <stdio.h> 
#include <stdlib.h>

#include "pin.H"
#include <os-apis.h>
#include "utils.H"


static void InstrumentRtn(RTN, VOID *);
static void OnExit(INT32, VOID *);
static void DoWait(CONTEXT *, THREADID);
static void FlushAck();

static BOOL FoundToolWait = FALSE;
static BOOL hadFlushAck = FALSE;
static BOOL hadActionPending = FALSE;

KNOB<BOOL>  KnobHelp(KNOB_MODE_WRITEONCE, "pintool",
     "hh", "0", "Print help message (command-line switches)");

KNOB<int>  KnobScenario(KNOB_MODE_WRITEONCE, "pintool",
     "scenario", "1", "Specify scenario of thread behavior to be checked (1-2)");
// 1 - Thread periodically checks PIN_IsActionPending() in analysis routine
//     and abandons it when Pin VM has pending action requiring all threads to enter VM.
// 2 - Thread is terminated in analysis routine.


/* ===================================================================== */
/* Main                                                                  */
/* ===================================================================== */

int main(int argc, char * argv[])
{
    if (PIN_Init(argc, argv) || KnobHelp) return Usage();
    PIN_InitSymbols();

    RTN_AddInstrumentFunction(InstrumentRtn, 0);
    PIN_AddFiniFunction(OnExit, 0);

    CODECACHE_AddCacheFlushedFunction(FlushAck, 0);

    PIN_StartProgram();
    return 0;
}


static void InstrumentRtn(RTN rtn, VOID *)
{
    if ((RTN_Name(rtn) == "ToolWait") || (RTN_Name(rtn) == "_ToolWait"))
    {
        RTN_Open(rtn);
        RTN_InsertCall(rtn, IPOINT_BEFORE, AFUNPTR(DoWait), IARG_CONTEXT, IARG_THREAD_ID, IARG_END);
        FoundToolWait = TRUE;
        RTN_Close(rtn);
        printf ("FoundToolWait\n");
        fflush (stdout);
    }
}

static void OnExit(INT32, VOID *)
{
    if (!FoundToolWait)
    {
        printf( "Couldn't add instrumentation routine\n");
        fflush (stdout);
        PIN_ExitProcess(3);
    }
    if (!hadFlushAck)
    {
        printf( "Did not get flush ack\n");
        fflush (stdout);
        PIN_ExitProcess(4);
    }
    if (!hadActionPending)
    {
        printf( "Did not get TRUE from PIN_IsActionPending\n");
        fflush (stdout);
        PIN_ExitProcess(5);
    }
}

static void DoWait(CONTEXT *ctxt, THREADID tid)
{
    if (KnobScenario.Value() == 2)
    {
        hadActionPending = TRUE;
        // Terminate current thread in analysis routine thus bypassing Pin VM notification
        // (note this operation is not permitted in regular Pin tool).
        // The test application calls the instrumented function prior to cache full event,
        // so it allows to validate Pin behavior on this event when at least one
        // application thread was dead unnotified.
        OS_ExitThread(NATIVE_TID_CURRENT);    // Should not return
        PIN_ExitProcess(6);
    }
    if (hadActionPending)
    {
        printf ("Tool done waiting\n");
        return;
    }
    printf ("Tool is waiting\n");
    fflush (stdout);
    for (int i = 6000; i > 0; --i)
    {
        if (PIN_IsActionPending(tid))
        {
            hadActionPending = TRUE;
            printf ("hadActionPending\n");
            fflush (stdout);

            PIN_ExecuteAt(ctxt);    // Doesn't return

            printf ("PIN_ExecuteAt finished\n");
            fflush (stdout);
            PIN_ExitProcess(2);
        }
        // 10 ms sleep.
        OS_Sleep(10);
        // Exit the loop in ~ 1 minute if Pin's pending action is not reported.
    }
    PIN_ExitProcess(1);
}

static void FlushAck()
{
    // Test wants at least one flush - but needs no more
    hadFlushAck = TRUE;
    printf ("FlushAck\n");
    fflush (stdout);
}
