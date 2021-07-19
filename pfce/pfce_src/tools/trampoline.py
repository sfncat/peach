import os
import sys

os.setpgid(0, 0)
os.execvp(sys.argv[1], sys.argv[1:])
