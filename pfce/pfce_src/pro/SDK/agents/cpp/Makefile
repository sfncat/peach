
# Copyright (c) Peach Fuzzer, LLC

# Native agent make file. Assumes Poco has already been build in sub folder poco.

# NOTE: You may need to update the -Lpoco/lib/Linux/i686 path when building poco
#       on different architectures.  Please check your poco build to identify the
#       correct path to use.

all: NativeAgent

clean:
	rm -f NativeAgent
	
NativeAgent: NativeAgent.cpp
	g++ -Ipoco/Foundation/include -Ipoco/JSON/include -Ipoco/Net/include -Ipoco/Util/include \
		-Lpoco/lib/Linux/i686 -o NativeAgent NativeAgent.cpp -lPocoFoundation -lPocoJSON \
		-lPocoNet -lPocoUtil -lPocoXML

android: NativeAgent.cpp
	arm-linux-androideabi-g++ -s -static -Ipoco/Foundation/include -Ipoco/JSON/include -Ipoco/Net/include -Ipoco/Util/include \
		-DPOCO_ANDROID -DPOCO_NO_FPENVIRONMENT -DPOCO_NO_WSTRING -DPOCO_NO_SHAREDMEMORY \
		-Lpoco/lib/Android/armeabi -o NativeAgent NativeAgent.cpp -lPocoUtil -lPocoJSON -lPocoXML \
		-lPocoNet -lPocoFoundation
	
# end
