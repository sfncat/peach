# 在Linux下编译peach

## 主要参考

https://github.com/vanhauser-thc/peachpro

主要参考这位大佬的文章。不过他写的过于简单，还存在错误。

## 说明

主要难点在mono的安装上。当前的主要问题是编译需要mono 6，安装和执行需要mono4。mono4又特别难装。

操作系统建议使用debian9或ubuntu16.04。推荐debian9。在ubuntu20.04上mono4.8.1没能编译成功。

## 1.安装基础依赖

```
sudo apt-get update
```

### ubuntu16.04

```
sudo apt-get install -y ruby doxygen gcc g++ wget nodejs node-typescript python-is-python2 libglib2.0-dev libcairo2-dev
```

### debian9

```
sudo apt-get install -y ruby doxygen gcc g++ wget nodejs node-typescript
```

## 2.mono

### mono下载源配置

不配置mono官方源的话，debain9默认安装的是mono4.6。

https://www.mono-project.com/download/stable/#download-lin-ubuntu

参考mono官方进行配置

#### ubuntu 16.04

https://www.mono-project.com/download/stable/#download-lin-ubuntu

```
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
sudo apt install apt-transport-https ca-certificates
echo "deb https://download.mono-project.com/repo/ubuntu stable-xenial main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update
```

#### debian9

https://www.mono-project.com/download/stable/#download-lin-debian

```
sudo apt install apt-transport-https dirmngr gnupg ca-certificates
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/debian stable-stretch main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update
```

### 安装mono最新版本

当前是6.12.0.122,装这个是因为在做waf configure的时候，packet需要mono高版本支持。

```
sudo apt install mono-devel -y
```

检查

```
$mono -V
Mono JIT compiler version 6.12.0.122 (tarball Mon Feb 22 17:28:32 UTC 2021)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
        TLS:           __thread
        SIGSEGV:       altstack
        Notifications: epoll
        Architecture:  amd64
        Disabled:      none
        Misc:          softdebug
        Interpreter:   yes
        LLVM:          yes(610)
        Suspend:       hybrid
        GC:            sgen (concurrent by default)
```

### 编译 mono 4.8.1

注意只编译，不安装。编译只要最后没有ERROR就行。

```
wget https://download.mono-project.com/sources/mono/mono-4.8.1.0.tar.bz2
tar -jxvf mono-4.8.1.0.tar.bz2
cd mono-4.8.1
./configure --prefix=/usr
make
```

## 2.peach代码及依赖

### 全从网络下载

```
mkdir peach
cd peach
git clone https://gitlab.com/gitlab-org/security-products/protocol-fuzzer-ce.git
cd protocol-fuzzer-ce/3rdParty/pin
wget http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz
tar -xf pin-3.2-81205-gcc-linux.tar.gz
cd protocol-fuzzer-ce/paket/.paket
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.bootstrapper.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.targets
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/Paket.Restore.targets
```

### 使用本地代码

建议在windows下直接下好，复制到linux中

## 3.编译peach

### configue

成功如下linux_x86_64所示

```shell
cd protocol-fuzzer-ce/
python2 waf configure --buildtag=0.0.2
Configuring variant linux_x86            : Not Available - Cross compilation failed
Configuring variant linux_x86_64         : Available
Configuring variant win_x86              : Not Available - Unsupported build host
Configuring variant win_x64              : Not Available - Unsupported build host
Configuring variant osx                  : Not Available - Unsupported build host
Configuring variant doc                  : Available - Missing Features: asciidoctor-pdf,webhelp
'configure' finished successfully (36.764s)
```

#### build

```
python2 waf build
最后会提示成功
'build' finished successfully (1m28.530s)
```



## 4.mono处理

### 卸载mono6

```
sudo apt-get autoremove mono-devel -y
```

#### 安装mono4.8.1

```
cd mono-4.8.1
sudo make install
```

#### 检查

```
mono -V
Mono JIT compiler version 4.8.1 (Stable 4.8.1.0/22a39d7 2021年 07月 14日 星期三 14:30:01 CST)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
        TLS:           __thread
        SIGSEGV:       altstack
        Notifications: epoll
        Architecture:  amd64
        Disabled:      none
        Misc:          softdebug
        LLVM:          supported, not enabled.
        GC:            sgen

```

## 5.安装Peach

```
cd protocol-fuzzer-ce
python2 waf install
最后会提示成功
'install' finished successfully (9.800s)
```

## 6.运行peach

```
mkdir ~/peach_linux
cd ~/peach_linux
cp ~/peach/protocol-fuzzer-ce/output/linux_x86_64_debug/bin/* -R .

~/peach_linux$ ./peach

[[ Peach Pro v0.0.2.1
[[ Copyright (c) 2021 Peach Fuzzer, LLC

[*] Web site running at: http://10.0.2.15:8888/
[*] Press Ctrl-C to exit.
```

## 7.FAQ

### 7.1下载地址

#### debian 9镜像

```
9.13
https://cdimage.debian.org/cdimage/archive/9.13.0/amd64/iso-dvd/debian-9.13.0-amd64-DVD-1.iso
9.9
https://repo.huaweicloud.com/debian-cd/9.9.0/amd64/iso-dvd/debian-9.9.0-amd64-DVD-1.iso
```

#### 7.2文件格式转换

如果文件是从windows里拷过来，最好做一下文件格式转换，在peach代码根目录执行

```
for x in $(find . -type f);do dos2unix $x $x;done
```







