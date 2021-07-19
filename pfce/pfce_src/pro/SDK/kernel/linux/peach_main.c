
#include <linux/module.h>
#include <linux/moduleparam.h>
#include <linux/init.h>
#include <linux/kernel.h>	/* printk() */
#include <linux/slab.h>		/* kmalloc() */
#include <linux/fs.h>		/* everything... */
#include <linux/errno.h>	/* error codes */
#include <linux/types.h>	/* size_t */
#include <linux/proc_fs.h>
#include <linux/fcntl.h>	/* O_ACCMODE */
#include <linux/aio.h>
#include <linux/seq_file.h>
#include <asm/uaccess.h>
#include <linux/vmalloc.h>
#include "peach.h"		/* local definitions */

#define FALSE 0
#define TRUE 1

int peach_major =   PEACH_MAJOR;
int peach_devs =    PEACH_DEVS;

struct peach_dev *peach_device;

/* Set by START/STOP IOCTLs */
int peach_started = FALSE;

/* Initialized by START IOCTL, incremented by NEXT IOCTL */
int peach_iteration = 0;

size_t peach_data_size = 0;
char* peach_data = 0;

void peach_cleanup(void);

/*
 * The ioctl() implementation
 */

long peach_ioctl (struct file *filp,
                 unsigned int cmd, unsigned long arg)
{

	int err = 0, ret = 0;

	/* don't even decode wrong cmds: better returning  ENOTTY than EFAULT */
	if (_IOC_TYPE(cmd) != PEACH_IOC_MAGIC) return -ENOTTY;
	if (_IOC_NR(cmd) > PEACH_IOC_MAXNR) return -ENOTTY;

	/*
	 * the type is a bitmask, and VERIFY_WRITE catches R/W
	 * transfers. Note that the type is user-oriented, while
	 * verify_area is kernel-oriented, so the concept of "read" and
	 * "write" is reversed
	 */
	if (_IOC_DIR(cmd) & _IOC_READ)
		err = !access_ok(VERIFY_WRITE, (void __user *)arg, _IOC_SIZE(cmd));
	else if (_IOC_DIR(cmd) & _IOC_WRITE)
		err =  !access_ok(VERIFY_READ, (void __user *)arg, _IOC_SIZE(cmd));
	if (err)
		return -EFAULT;

	switch(cmd) {

	case PEACH_IOCTL_METHOD_START:
	  
	  peach_started = TRUE;
	  peach_iteration = 1;
	  
	  printk(KERN_NOTICE "PEACH_IOCTL_METHOD_START\n");
	  
	  /* TODO: Put any code here that runs on start. */
	  
		break;
		
  case PEACH_IOCTL_METHOD_STOP:
    
    peach_started = FALSE;
    
    printk(KERN_NOTICE "PEACH_IOCTL_METHOD_STOP\n");
    
    /* TODO: Put any code here that runs on stop. */
  
    break;

  case PEACH_IOCTL_METHOD_DATA:
    printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_DATA\n", peach_iteration);
    
    if(peach_data)
    {
      vfree(peach_data);
      peach_data = NULL;
    }
    
    peach_data = (char*) vmalloc(peach_data_size);
    if(peach_data == NULL)
    {
      printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_DATA: kmalloc failed\n", peach_iteration);
      break;
    }
    
    ret = copy_from_user(peach_data, (void __user*) arg, peach_data_size);
    if(ret != 0)
    {
      vfree(peach_data);
      peach_data = NULL;
      peach_data_size = 0;
      
      printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_DATA: copy_from_user failed %d\n", 
        peach_iteration, ret);
    }
  
    break;

  case PEACH_IOCTL_METHOD_DATA_SIZE:
    /* Receive the size of data we should read */
    
    if(peach_data)
    {
      vfree(peach_data);
      peach_data = NULL;
    }
    
    peach_data_size = (size_t) arg;
  
    printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_DATA_SIZE: %zu\n", peach_iteration, peach_data_size);
    
    break;

  case PEACH_IOCTL_METHOD_CALL:
    printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_CALL\n", peach_iteration);
    
    /* Place code here that uses DATA and performs fuzzing */
    
    break;

  case PEACH_IOCTL_METHOD_PROPERTY:
    printk(KERN_NOTICE "%d PEACH_IOCTL_METHOD_PROPERTY\n", peach_iteration);
    
    /* Place code here that uses DATA as property value */
    
    break;

  case PEACH_IOCTL_METHOD_NEXT:
    
    if(peach_started)
      peach_iteration += 1;
      
    if(peach_data)
    {
      vfree(peach_data);
      peach_data = 0;
      peach_data_size = 0;
    }
    
    printk(KERN_NOTICE "PEACH_IOCTL_METHOD_NEXT: %d\n", peach_iteration);
    break;


	default:  /* redundant, as cmd was checked against MAXNR */
    printk(KERN_NOTICE "PEACH: UNKNOWN IOCTL CALLED\n");
		return -ENOTTY;
	}

	return ret;
}

/*
 * The fops
 */

struct file_operations peach_fops = {
	.owner =     THIS_MODULE,
	.unlocked_ioctl =     peach_ioctl,
};

static void peach_setup_cdev(struct peach_dev *dev, int index)
{
	int err, devno = MKDEV(peach_major, index);
    
	cdev_init(&dev->cdev, &peach_fops);
	dev->cdev.owner = THIS_MODULE;
	dev->cdev.ops = &peach_fops;
	err = cdev_add (&dev->cdev, devno, 1);
	
	/* Fail gracefully if need be */
	if (err)
		printk(KERN_NOTICE "Error %d adding peach%d", err, index);
}

/*
 * Finally, the module stuff
 */

int peach_init(void)
{
	int result;
	dev_t dev = MKDEV(peach_major, 0);

  printk(KERN_NOTICE "peach_init()\n");
	
	/*
	 * Register your major, and accept a dynamic number.
	 */
	if (peach_major)
		result = register_chrdev_region(dev, peach_devs, "peach");
	else {
		result = alloc_chrdev_region(&dev, 0, peach_devs, "peach");
		peach_major = MAJOR(dev);
	}
	if (result < 0)
		return result;

	
	/* 
	 * allocate the devices -- we can't have them static, as the number
	 * can be specified at load time
	 */
	peach_device = kmalloc(sizeof(struct peach_dev), GFP_KERNEL);
	if (!peach_device) {
		result = -ENOMEM;
		goto fail_malloc;
	}
	
	memset(peach_device, 0, sizeof(struct peach_dev));
	
	peach_setup_cdev(peach_device, 0);

	return 0; /* succeed */

  fail_malloc:
	unregister_chrdev_region(dev, peach_devs);
	return result;
}



void peach_cleanup(void)
{
  printk(KERN_NOTICE "peach_cleanup()\n");
  
  if(peach_data)
  {
    vfree(peach_data);
    peach_data = NULL;
  }
  
	cdev_del(&peach_device->cdev);
	kfree(peach_device);
	unregister_chrdev_region(MKDEV (peach_major, 0), peach_devs);
}

