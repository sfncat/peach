; Copyright 2002-2020 Intel Corporation.
; 
; This software is provided to you as Sample Source Code as defined in the accompanying
; End User License Agreement for the Intel(R) Software Development Products ("Agreement")
; section 1.L.
; 
; This software and the related documents are provided as is, with no express or implied
; warranties, other than those that are expressly stated in the License.

include asm_macros.inc

PROLOGUE

; This function is for testing Pin's execution prediction using
; IARG_EXECUTING. If input % 8 is zero, it performs the cmov and
; return 0, otherwise it returns a non zero integer.
; param[in] - int number - a number to be checked if devides by 8
; return - 0 if number % 8 == 0, non zero int otherwise.
PUBLIC SimpleCmovTest

.code

SimpleCmovTest PROC
        BEGIN_STACK_FRAME
        mov GAX_REG,PARAM1
        push GBX_REG
        mov GBX_REG,0
        and AX,0007h
        cmove GAX_REG, GBX_REG
        pop GBX_REG
        END_STACK_FRAME
        ret
SimpleCmovTest ENDP

end
