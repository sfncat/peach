cd $INT_PATH
docker run -it --name peach_cb -v $INT_PATH/protocol-fuzzer-ce/:/protocol-fuzzer-ce peach:cb /bin/bash
