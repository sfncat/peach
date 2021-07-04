
#include <linux/ioctl.h>
#include <linux/cdev.h>

/*
 * Macros to help debugging
 */

#undef PDEBUG             /* undef it, just in case */
#ifdef PEACH_DEBUG
#  ifdef __KERNEL__
     /* This one if debugging is on, and kernel space */
#    define PDEBUG(fmt, args...) printk( KERN_DEBUG "peach: " fmt, ## args)
#  else
     /* This one for user space */
#    define PDEBUG(fmt, args...) fprintf(stderr, fmt, ## args)
#  endif
#else
#  define PDEBUG(fmt, args...) /* not debugging: nothing */
#endif

#undef PDEBUGG
#define PDEBUGG(fmt, args...) /* nothing: it's a placeholder */

#include "peach_ioctl.h"

struct peach_dev {
	struct cdev cdev;
};

extern struct peach_dev *peach_device;

extern struct file_operations peach_fops;

/*
 * The different configurable parameters
 */
extern int peach_major;     /* main.c */
extern int peach_devs;
extern int peach_order;
extern int peach_qset;

/*
 * Prototypes for shared functions
 */
int peach_trim(struct peach_dev *dev);
struct peach_dev *peach_follow(struct peach_dev *dev, int n);

