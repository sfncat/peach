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

#include <stdio.h>

extern int SimpleCmovTest(int i);

int main()
{
    int counter, condition;
    // run 1000 cmov commands
    for (counter = 2000; counter > 1000; counter--)
    {
        condition = SimpleCmovTest(counter);
    }
    // doing this to avoid compiler error - initialized but unused variable
    condition = 0;
    return condition;
}
