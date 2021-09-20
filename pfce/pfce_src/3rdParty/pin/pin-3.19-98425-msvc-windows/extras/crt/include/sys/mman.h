#ifndef _SYS_MMAN_H_
#define _SYS_MMAN_H_

#ifdef TARGET_MAC
#include <sys/mac/mman.h>
#else
#include <sys/nonmac/mman.h>
#endif


#endif /* _SYS_MMAN_H_ */
