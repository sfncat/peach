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

#include "asm_macros.h"

.text

# This function is for testing Pin's execution prediction using
# IARG_EXECUTING. If input % 8 is zero, it performs the cmov and
# return 0, otherwise it returns a non zero integer.
# param[in] - int number - a number to be checked if devides by 8
# return - 0 if number % 8 == 0, non zero int otherwise.
DECLARE_FUNCTION(SimpleCmovTest)
NAME(SimpleCmovTest):
        BEGIN_STACK_FRAME
        mov PARAM1, GAX_REG
        push GBX_REG
        mov $0,GBX_REG
        and $7,%AX
        cmove GBX_REG,GAX_REG
        pop GBX_REG
        END_STACK_FRAME
        ret
