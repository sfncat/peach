/*
 * Copyright 2002-2020 Intel Corporation.
 * 
 * This software is provided to you as Sample Source Code as defined in the accompanying
 * End User License Agreement for the Intel(R) Software Development Products ("Agreement")
 * section 1.L.
 * 
 * This software and the related documents are provided as is, with no express or implied
 * warranties, other than those that are expressly stated in the License.
 */

#include <stdlib.h>
#include <stdio.h>

extern void FldzFunc();
extern void FptagInitFunc();
extern void FldInfFunc();
extern void DoFnstenv(unsigned char* mem28B);
extern void FstpFunc();

/*
 *  This application is being used to test that Pin delivers the correct ftag value to the debugger (16 bits instead of 8 bits).
 *  The applications changes the value of ftag register several times.
 */
int main(int argc, char** argv)
{
    int i;
    unsigned char FnstenvArea[28];
    //  Initialize FPU after checking for pending unmasked floating-point exceptions.
    FptagInitFunc();
    //Saves the current FPU operating environment at the memory location specified with the destination operand.
    DoFnstenv(FnstenvArea);
    //Push +0.0 onto the FPU register stack. change the ftage value
    FldzFunc();
    DoFnstenv(FnstenvArea);
    // Copies the value in the ST(0) register to the register that is currently at the top of the stack. change the ftage value
    FstpFunc();
    // divide pi with zero. change the ftage value
    FldInfFunc();
    DoFnstenv(FnstenvArea);
    return 0;
}
