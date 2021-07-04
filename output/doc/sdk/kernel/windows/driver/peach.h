/*++

Copyright (c) Michael Eddington

Module Name:

    PEACH.H

Abstract:

    Defines the IOCTL codes that will be used by this driver.  The IOCTL code
    contains a command identifier, plus other information about the device,
    the type of access with which the file must have been opened,
    and the type of buffering.

Environment:

    Kernel mode only.

--*/

//
// Device type           -- in the "User Defined" range."
//
#define PEACH_TYPE 40000

//
// The IOCTL function codes from 0x800 to 0xFFF are for customer use.
//


/* Sent by Peach to say "Starting run" */
#define IOCTL_PEACH_METHOD_START \
    CTL_CODE( PEACH_TYPE, 0x900, METHOD_BUFFERED, FILE_ANY_ACCESS  )

/* Sent by Peach to say "Stopping run" */
#define IOCTL_PEACH_METHOD_STOP \
    CTL_CODE( PEACH_TYPE, 0x901, METHOD_BUFFERED , FILE_ANY_ACCESS  )

/* Sent by Peach to deliver DATA to Kernel */
#define IOCTL_PEACH_METHOD_DATA \
    CTL_CODE( PEACH_TYPE, 0x902, METHOD_BUFFERED, FILE_ANY_ACCESS  )

/* Sent by Peach to indicate a "call" data is "method name".
 * This will be followed by one or more DATA ioctls to deliver
 * each parameter's data.
 */
#define IOCTL_PEACH_METHOD_CALL \
    CTL_CODE( PEACH_TYPE, 0x903, METHOD_BUFFERED , FILE_ANY_ACCESS  )

/* Sent by Peach to indicate a "property" set, data is "property name".
 * This will be followed by a single DATA ioctl. */
#define IOCTL_PEACH_METHOD_PROPERTY \
    CTL_CODE( PEACH_TYPE, 0x904, METHOD_BUFFERED , FILE_ANY_ACCESS  )

/* Sent by Peach to query if driver is ready for next test.  Driver
 * should send back TRUE (1) or FALSE (0) */
#define IOCTL_PEACH_METHOD_NEXT \
    CTL_CODE( PEACH_TYPE, 0x905, METHOD_BUFFERED , FILE_ANY_ACCESS  )

#define DRIVER_FUNC_INSTALL     0x01
#define DRIVER_FUNC_REMOVE      0x02

#define DRIVER_NAME       "Peach"

// end
