cd pfce/install
wget https://download.mono-project.com/sources/mono/mono-4.8.1.0.tar.bz2
wget https://download.mono-project.com/sources/mono/mono-6.12.0.122.tar.xz
cd $INT_PATH
git clone https://gitlab.com/gitlab-org/security-products/protocol-fuzzer-ce.git
cd protocol-fuzzer-ce/3rdParty/pin
wget http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz
tar -xf pin-3.2-81205-gcc-linux.tar.gz
cd $INT_PATH/protocol-fuzzer-ce/paket/.paket
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.bootstrapper.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.targets
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/Paket.Restore.targets