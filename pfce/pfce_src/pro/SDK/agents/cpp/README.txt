Peach Native Agent Stub
=======================

This is an example of a native agent implementation in C++ using the POCO
open source library. Poco handles HTTP and JSON parsing for us.

This code has been tested on Windows, Linux, and Android.

== Build Linux

 1. Build Poco from NativeAgent/poco folder

----
./configure --no-tests --no-samples --omit=Crypto,NetSSL_OpenSSL,Data/SQLite,Data/ODBC,Data/MySQL,MongoDB
./make -s -j4
----

 2. Build NativeAgent from NativeAgent folder

----
make
----


== Build Android

The included Poco source code is the development branch (1.5) from git.
Version 1.5.2 does not correctly build for Android.

 1. Download Android NDK
 2. Run the build/tools/make-standalone-toolchain.sh. Additional documentation can be found in /docs/STANDALONE-TOOLCHAIN.HTML.

----
./make-standalone-toolchain.sh --platform=android-9
--install-dir=/home/mike/android-cross --ndk-dir=/home/mike/android
----

 3. Add install dir to path

----
export PATH=$PATH:/home/mike/android-cross/bin
----

 4. Build Poco from NativeAgent/poco

NOTE: At time of writing, had to fix one bug in Error.cpp
 
----
./configure --config=Android --no-tests --no-samples --omit=Crypto,NetSSL_OpenSSL,Data/SQLite,Data/ODBC,Data/MySQL,MongoDB
./make -s -j4
----

 5. Build NativeAgent from NativeAgent folder

---- 
make android
----

