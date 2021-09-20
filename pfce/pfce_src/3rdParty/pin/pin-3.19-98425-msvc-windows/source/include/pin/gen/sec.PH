
#ifdef PIN_G_SEC_PH
#error duplicate inclusion of sec
#else
#define PIN_G_SEC_PH
/*! @file
  This file contains @ref SEC and Mapping primitives
 */
/*! @ingroup SEC
  SEC types
*/
typedef enum
{
    SEC_TYPE_INVALID,
    SEC_TYPE_UNUSED,
    SEC_TYPE_REGREL,  ///< relocations
    SEC_TYPE_DYNREL,  ///< dynamic relocations
    SEC_TYPE_EXEC,    ///< contains code
    SEC_TYPE_DATA,    ///< contains initialized data
    SEC_TYPE_DYNAMIC, ///< contains dynamic code
    SEC_TYPE_OPD,
    SEC_TYPE_GOT,
    SEC_TYPE_STACK,
    SEC_TYPE_PLTOFF,
    SEC_TYPE_HASH,
    SEC_TYPE_LSDA, ///< old exception_info (obsolete)
    SEC_TYPE_UNWIND,
    SEC_TYPE_UNWINDINFO,
    SEC_TYPE_REGSYM,
    SEC_TYPE_DYNSYM,
    SEC_TYPE_DEBUG,
    SEC_TYPE_BSS, ///< contains uninitialized data
    SEC_TYPE_SYMSTR,
    SEC_TYPE_DYNSTR,
    SEC_TYPE_SECSTR,
    SEC_TYPE_COMMENT,
    SEC_TYPE_LOOS, ///< OS specific
    SEC_TYPE_USER, ///< Application specific
    SEC_TYPE_PROC, ///< Processor specific
    SEC_TYPE_LAST
} SEC_TYPE;

#endif // PIN_G_SEC_PH
