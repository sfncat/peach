/*BEGIN_LEGAL 
Copyright 2002-2020 Intel Corporation.

This software and the related documents are Intel copyrighted materials, and your
use of them is governed by the express license under which they were provided to
you ("License"). Unless the License provides otherwise, you may not use, modify,
copy, publish, distribute, disclose or transmit this software or the related
documents without Intel's prior written permission.

This software and the related documents are provided as is, with no express or
implied warranties, other than those that are expressly stated in the License.
END_LEGAL */
/// @file xed-reg-class-enum.h

// This file was automatically generated.
// Do not edit this file.

#if !defined(XED_REG_CLASS_ENUM_H)
# define XED_REG_CLASS_ENUM_H
#include "xed-common-hdrs.h"
#define XED_REG_CLASS_INVALID_DEFINED 1
#define XED_REG_CLASS_BNDCFG_DEFINED 1
#define XED_REG_CLASS_BNDSTAT_DEFINED 1
#define XED_REG_CLASS_BOUND_DEFINED 1
#define XED_REG_CLASS_CR_DEFINED 1
#define XED_REG_CLASS_DR_DEFINED 1
#define XED_REG_CLASS_FLAGS_DEFINED 1
#define XED_REG_CLASS_GPR_DEFINED 1
#define XED_REG_CLASS_GPR16_DEFINED 1
#define XED_REG_CLASS_GPR32_DEFINED 1
#define XED_REG_CLASS_GPR64_DEFINED 1
#define XED_REG_CLASS_GPR8_DEFINED 1
#define XED_REG_CLASS_IP_DEFINED 1
#define XED_REG_CLASS_MASK_DEFINED 1
#define XED_REG_CLASS_MMX_DEFINED 1
#define XED_REG_CLASS_MSR_DEFINED 1
#define XED_REG_CLASS_MXCSR_DEFINED 1
#define XED_REG_CLASS_PSEUDO_DEFINED 1
#define XED_REG_CLASS_PSEUDOX87_DEFINED 1
#define XED_REG_CLASS_SR_DEFINED 1
#define XED_REG_CLASS_TMP_DEFINED 1
#define XED_REG_CLASS_TREG_DEFINED 1
#define XED_REG_CLASS_UIF_DEFINED 1
#define XED_REG_CLASS_X87_DEFINED 1
#define XED_REG_CLASS_XCR_DEFINED 1
#define XED_REG_CLASS_XMM_DEFINED 1
#define XED_REG_CLASS_YMM_DEFINED 1
#define XED_REG_CLASS_ZMM_DEFINED 1
#define XED_REG_CLASS_LAST_DEFINED 1
typedef enum {
  XED_REG_CLASS_INVALID,
  XED_REG_CLASS_BNDCFG,
  XED_REG_CLASS_BNDSTAT,
  XED_REG_CLASS_BOUND,
  XED_REG_CLASS_CR,
  XED_REG_CLASS_DR,
  XED_REG_CLASS_FLAGS,
  XED_REG_CLASS_GPR,
  XED_REG_CLASS_GPR16,
  XED_REG_CLASS_GPR32,
  XED_REG_CLASS_GPR64,
  XED_REG_CLASS_GPR8,
  XED_REG_CLASS_IP,
  XED_REG_CLASS_MASK,
  XED_REG_CLASS_MMX,
  XED_REG_CLASS_MSR,
  XED_REG_CLASS_MXCSR,
  XED_REG_CLASS_PSEUDO,
  XED_REG_CLASS_PSEUDOX87,
  XED_REG_CLASS_SR,
  XED_REG_CLASS_TMP,
  XED_REG_CLASS_TREG,
  XED_REG_CLASS_UIF,
  XED_REG_CLASS_X87,
  XED_REG_CLASS_XCR,
  XED_REG_CLASS_XMM,
  XED_REG_CLASS_YMM,
  XED_REG_CLASS_ZMM,
  XED_REG_CLASS_LAST
} xed_reg_class_enum_t;

/// This converts strings to #xed_reg_class_enum_t types.
/// @param s A C-string.
/// @return #xed_reg_class_enum_t
/// @ingroup ENUM
XED_DLL_EXPORT xed_reg_class_enum_t str2xed_reg_class_enum_t(const char* s);
/// This converts strings to #xed_reg_class_enum_t types.
/// @param p An enumeration element of type xed_reg_class_enum_t.
/// @return string
/// @ingroup ENUM
XED_DLL_EXPORT const char* xed_reg_class_enum_t2str(const xed_reg_class_enum_t p);

/// Returns the last element of the enumeration
/// @return xed_reg_class_enum_t The last element of the enumeration.
/// @ingroup ENUM
XED_DLL_EXPORT xed_reg_class_enum_t xed_reg_class_enum_t_last(void);
#endif
