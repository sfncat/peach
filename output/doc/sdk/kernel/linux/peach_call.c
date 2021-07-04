#include <stdlib.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/ioctl.h>
//#include <linux/ioctl.h>
#include "peach_ioctl.h"


int main(int argc, char** argv)
{
  char* cmd = argv[1];

  int fd = open("/dev/peach", 0, O_RDWR);

  printf("Command \"%s\"\n", cmd);

  if(!strcmp(cmd, "start"))    
  {
    ioctl(fd, PEACH_IOCTL_METHOD_START, 0);
  }
  else if(!strcmp(cmd, "stop")) 
  {
    ioctl(fd, PEACH_IOCTL_METHOD_STOP, 0);
  }
  else if(!strcmp(cmd, "next")) 
  {
    ioctl(fd, PEACH_IOCTL_METHOD_NEXT, 0);
  }
  else if(!strcmp(cmd, "call")) 
  {
    ioctl(fd, PEACH_IOCTL_METHOD_CALL, 0);
  }
  else if(!strcmp(cmd, "data")) 
  {
    FILE* fdData = fopen(argv[2], "r"); 
    int dataSize = 0;
    char* data = 0;
    
    fseek(fdData, 0, SEEK_END);
    dataSize = ftell(fdData);
    fseek(fdData, 0, SEEK_SET);
    
    data = (char*) malloc(dataSize);
    fread(data, dataSize, 1, fdData);
    fclose(fdData);
    
    printf("Sending %d bytes\n", dataSize);
  
    ioctl(fd, PEACH_IOCTL_METHOD_DATA_SIZE, dataSize);
    ioctl(fd, PEACH_IOCTL_METHOD_DATA, data);
  }
  
  close(fd);
}

// END
