docker run -itd --name peach_ir -v $INT_PATH/protocol-fuzzer-ce/:/protocol-fuzzer-ce -v $INT_PATH/peach:/peach peach:ir -v $INT_PATH/logs:/logs /bin/bash
docker exec -it peach_ir /bin/bash
cd /protocol-fuzzer-ce
python waf install
cp -r output/linux_x86_64_release/bin/* /peach