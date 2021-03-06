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
/*! @file
 */

#include "pin.H"
#include "tool_macros.h"
#include <iostream>
#include <fstream>
#include <cstdio>
#include <signal.h>
#include <sched.h>

BOOL changeSigmask = FALSE;

using namespace std;

INT32 Usage()
{
    cerr <<
        "This pin tool examines the correctness of the retrieve and alteration of the thread sigmask"
            "when the tool registers a THREAD_ATTACH_PROBED_CALLBACK callback.\n";
    cerr << KNOB_BASE::StringKnobSummary();
    cerr << endl;
    return -1;
}

// Notify the application when the sigmask has changed.
BOOL Replacement_waitChangeSigmask()
{
    while(changeSigmask == 0) sched_yield();
    return TRUE;
}


VOID AttachedThreadStart(void *sigmask, VOID * v)
{
    sigset_t * sigset = (sigset_t *)sigmask;

    /* 
     *    change the sigmask of the thread whose sigmask contains SIGUSR2 and doen't contain SIGUSR1
     */  
    if ( 0 != sigismember( sigset, SIGUSR2 )  && ( 0 == sigismember( sigset, SIGUSR1 ) ))
    {
        sigdelset(sigset, SIGUSR2);
        sigaddset(sigset, SIGUSR1);
    }

    changeSigmask = TRUE;
}


// Image load callback for the first Pin session
VOID ImageLoad(IMG img,  VOID *v)
{
    if ( IMG_IsMainExecutable(img))
    {
        RTN rtn = RTN_FindByName(img, C_MANGLE("WaitChangeSigmask"));

        // Relevant only in the attach scenario.
        if (RTN_Valid(rtn))
        {
            if(RTN_IsSafeForProbedReplacement(rtn))
               RTN_ReplaceProbed(rtn, AFUNPTR(Replacement_waitChangeSigmask));
        }
    }
}


/* ===================================================================== */

int main(int argc, CHAR *argv[])
{
    if( PIN_Init(argc,argv) )
    {
        return Usage();
    }
    PIN_InitSymbols();
    IMG_AddInstrumentFunction(ImageLoad, 0);
    PIN_AddThreadAttachProbedFunction(AttachedThreadStart, 0);
    PIN_StartProgramProbed();
    
    return 0;
}

/* ===================================================================== */
/* eof */
/* ===================================================================== */
