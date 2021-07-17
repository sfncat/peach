docker run -it --name peach_cb -v $INT_PATH/protocol-fuzzer-ce/:/protocol-fuzzer-ce peach:cb /bin/bash
python waf configure --buildtag=0.0.2
python waf build