
#include <linux/ioctl.h>

#define PEACH_MAJOR 			0
#define PEACH_DEVS 				1
#define PEACH_IOC_MAGIC  	'P'
#define PEACH_IOCRESET    _IO(PEACH_IOC_MAGIC, 0)

/* Sent by Peach to say "Starting run" */
#define PEACH_IOCTL_METHOD_START			_IO(PEACH_IOC_MAGIC,	1)

/* Sent by Peach to say "Stopping run" */
#define PEACH_IOCTL_METHOD_STOP				_IO(PEACH_IOC_MAGIC,	2)

/* Sent by Peach to deliver DATA to Kernel */
#define PEACH_IOCTL_METHOD_DATA				_IOW(PEACH_IOC_MAGIC,	3,	char*)

/* Sent by Peach prior to DATA to provide size to Kernel */
#define PEACH_IOCTL_METHOD_DATA_SIZE	_IO(PEACH_IOC_MAGIC,	4)

/* Sent by Peach to indicate a "call" data is "method name".
 * This will be followed by one or more DATA ioctls to deliver
 * each parameter's data.
 */
#define PEACH_IOCTL_METHOD_CALL				_IOW(PEACH_IOC_MAGIC, 5,	char*)

/* Sent by Peach to indicate a "property" set, data is "property name".
 * This will be followed by a single DATA ioctl.
 */
#define PEACH_IOCTL_METHOD_PROPERTY		_IOW(PEACH_IOC_MAGIC, 6,	char*) 

/* Sent by Peach to query if driver is ready for next test.  Driver
 * should send back TRUE (1) or FALSE (0)
 */
#define PEACH_IOCTL_METHOD_NEXT				_IO(PEACH_IOC_MAGIC,	7)
 
#define PEACH_IOC_MAXNR 7

// end
