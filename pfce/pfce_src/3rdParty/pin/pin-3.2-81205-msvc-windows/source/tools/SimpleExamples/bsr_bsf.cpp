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
#include "pin.H"
extern "C" {
#include "xed-interface.h"
}

#if defined(TARGET_MAC)
    const char* BSR_RTN_NAME="_bsr_func";
    const char* BSF_RTN_NAME="_bsf_func";
#else
    const char* BSR_RTN_NAME="bsr_func";
    const char* BSF_RTN_NAME="bsf_func";
#endif

const char* BSR_MNEMONICS="BSR";
const char* BSF_MNEMONICS="BSF";

const ADDRINT EFLAGS_ZF=0x40;

/* ===================================================================== */
/* Global variables                                                      */
/* ===================================================================== */

std::ostream* outFile = 0;
string instrumented_routines[] = {BSR_RTN_NAME, BSF_RTN_NAME};

/* ===================================================================== */
/* Command line switches                                                 */
/* ===================================================================== */

KNOB<string> KnobOutputFile(KNOB_MODE_WRITEONCE,  "pintool",
    "o", "", "Specify file name for the tool's output. If no filename is specified, the output will be directed to stdout.");


/* ===================================================================== */
/* Print Help Message                                                    */
/* ===================================================================== */

INT32 Usage()
{
    cerr << "This tool prints information on the destination's validity of the BSR/BSF instructions." << endl;
    cerr << KNOB_BASE::StringKnobSummary() << endl;
    return 1;
}

/* ===================================================================== */
/* Analysis routines                                                     */
/* ===================================================================== */
 
/*
 * Analysis-time routine inspecting the value of the zero flag after the execution
 * of the BSR/BSF instructions to determine the validity of the destination operand.
 */
VOID examine_z_flag(char *mnemonics, ADDRINT eflags)
{
    *outFile << mnemonics << " destination operand is " << ((eflags & EFLAGS_ZF) ? "undefined (source equals zero)."
                                                           : "valid (source is not zero).") << endl << flush;
}


/* ===================================================================== */
/* Instrumentation routines                                              */
/* ===================================================================== */

/*
 * Instrumentation-time routine inspecting a single instruction, looking for
 * the BSR/BSF instructions.
 */
VOID Instruction(INS ins, VOID *v)
{
    xed_decoded_inst_t* xedd = INS_XedDec(ins);
    xed_iclass_enum_t inst_iclass = xed_decoded_inst_get_iclass(xedd);
    if ((inst_iclass == XED_ICLASS_BSR) || (inst_iclass == XED_ICLASS_BSF))
    {
        INS_InsertCall(ins, IPOINT_AFTER, (AFUNPTR)examine_z_flag, 
            IARG_PTR, ((inst_iclass == XED_ICLASS_BSR) ? BSR_MNEMONICS : BSF_MNEMONICS),
            IARG_REG_VALUE, REG_GFLAGS, IARG_END);
    }
}

/*
 * Instrumentation-time routine looking for the routine we'd like to instrument.
 */
VOID ImageLoad(IMG img, VOID * v)
{
    if (IMG_IsMainExecutable(img))
    {
        // Search for the assembly routines in the application
        for (unsigned int i = 0; i < sizeof(instrumented_routines)/sizeof(instrumented_routines[0]); i++)
        {
            RTN AsmRtn = RTN_FindByName(img, instrumented_routines[i].c_str());
            if (RTN_Valid(AsmRtn))
            {
                RTN_Open(AsmRtn);
                // Go over each of the routine's instructions
                for (INS ins = RTN_InsHead(AsmRtn); INS_Valid(ins); ins = INS_Next(ins))
                {
                    Instruction(ins, 0);
                }
                RTN_Close(AsmRtn);
            }
        }
    }
}

/* ===================================================================== */
/* Main                                                                  */
/* ===================================================================== */

int main(int argc, char *argv[])
{
    if( PIN_Init(argc,argv) )
        return Usage();
    // Next call is needed, otherwise we can't find routines by name
#if defined(TARGET_WINDOWS)
    PIN_InitSymbolsAlt(EXPORT_SYMBOLS);
#else
    PIN_InitSymbols();
#endif
    outFile = KnobOutputFile.Value().empty() ? &cout : new std::ofstream(KnobOutputFile.Value().c_str());
    IMG_AddInstrumentFunction(ImageLoad, 0);
    PIN_StartProgram();    // Never returns
    return 0;
}
