# 通过Docker编译安装Peach

## 说明

因为当前编译和运行需要的mono版本不同，需要切换，如果经常要编译，就比较麻烦。

这里给出的是通过两个不同Docker进行编译和运行，同时数据文件放在容器外的方案。

这里以在ubuntu 20.04运行为例。

## 流程

1.在Linux上部署Docker环境

2.构建本地目录

3.创建配置编译镜像peach_c_b

4.创建安装运行镜像peach_i_r

5.运行peach_c_b

6.编译并编译peach

7.运行peach_i_r

8.安装peach

9运行peach

## 制作镜像目录树

```
-pfce
  --install:安装需要本地文件目录
    ---docker:
        ----Dockerfile.cb:配置编译Dockerfile文件
        ----Dockerfile.ir:安装运行Dockerfile文件
        ----conf.sh 配置
        ----build.sh 编译
        ----install.sh 安装
    ---mono-4.8.1.0.tar.bz2
    ---mono-6.12.0.122.tar.xz
    ---pin-3.2-81205-gcc-linux.tar.gz
    ---packages.tar.bz2
    ---sources.list:apt阿里镜像源
    ---mono-official.list:mono 4.8.1源
  --protocol-fuzzer-ce：peach源文件目录，需要已经将packet和pin下载并完成初始化
  --peach:安装运行目录
  
  --build_cb.sh:编译peach:cr镜像
  --run_cb.sh:启动peach_cb
  
  --build_ir.sh:编译peach:ir镜像
  --run_ir.sh:启动安装运行peach_ir
  --run_r.sh:启动只运行peach_ir
  
  --export_img.sh:导出镜像 
```



## 部署Docker环境

```bash
#install_docker.sh
# Install dependencies to install Docker
sudo apt -qqy -o Dpkg::Options::='--force-confdef' -o Dpkg::Options::='--force-confold' install apt-transport-https ca-certificates curl gnupg-agent software-properties-common openssl

# Register Docker package registry
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

# Refresh package udpates and install Docker
sudo apt -qqy update
sudo apt -qqy -o Dpkg::Options::='--force-confdef' -o Dpkg::Options::='--force-confold' install docker-ce docker-ce-cli containerd.io
```

## 本地目录树初始化

```bash
#init_cbi_env.sh
mkdir -p pfce/peach
mkdir -p pfce/install
mkdir -p pfce/protocol-fuzzer-ce
echo "export INT_PATH=`pwd`/pfce" >> ~/.bashrc
echo "export PATH=$INT_PATH:$PATH" >>~/.bashrc
echo "alias runpit='docker exec -it peach_ir /peach/peach'" >> ~/.bashrc
source ~/.bashrc
cd pfce/install

#download_for_cb.sh
#通过源代码安装mono,在线安装不用下
wget https://download.mono-project.com/sources/mono/mono-4.8.1.0.tar.bz2
wget https://download.mono-project.com/sources/mono/mono-6.12.0.122.tar.xz
cd $INT_PATH
#如果不使用本地代码，从gitlab下载
git clone https://gitlab.com/gitlab-org/security-products/protocol-fuzzer-ce.git
cd protocol-fuzzer-ce/3rdParty/pin
wget http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz
tar -xf pin-3.2-81205-gcc-linux.tar.gz
cd $INT_PATH/protocol-fuzzer-ce/paket/.paket
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.bootstrapper.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.targets
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/paket.exe
wget https://github.com/fsprojects/Paket/releases/download/5.258.1/Paket.Restore.targets
```

## 当前用户增加docker权限

增加权限后，用户需要退出重登录

```bash
sudo gpasswd -a ${USER} docker
```



## 创建配置编译镜像

创建镜像时如果出来hash错误导致安装失败，把image删除，重试，或者可以换个代理

```bash
#build_cb.sh
cd $INT_PATH
docker build -t peach:cb -f ./install/docker/cb/Dockerfile .
```

## 配置编译Peach

启动容器，并进去进行配置和编译

```bash
#run_cb.sh
cd $INT_PATH
docker run -it --name peach_cb -v $INT_PATH/protocol-fuzzer-ce/:/protocol-fuzzer-ce peach:cb /bin/bash
```

```bash
cd /protocol-fuzzer-ce
#conf.sh
python waf configure --buildtag=0.0.2
#build.sh
python waf build
```

## 创建安装运行镜像

```bash
#build_ir.sh
cd $INT_PATH
docker build -t peach:ir -f ./install/docker/ir/Dockerfile .
```

## 安装Peach

```bash
#run_ir.sh
docker run -itd --name peach_ir -v $INT_PATH/protocol-fuzzer-ce:/protocol-fuzzer-ce -v $INT_PATH/peach:/peach  peach:ir /bin/bash
docker exec -it peach_ir /bin/bash
```



```bash
#install.sh
cd /protocol-fuzzer-ce
python waf install
cp -r output/linux_x86_64_release/bin/* /peach
```



## 运行测试套

```bash
runpit pits/http/http.xml -1
```

## 导出镜像

```bash
#export_img.sh
docker export $(docker ps -aqf"name=peach_ir") > peach_ir.tar
docker export $(docker ps -aqf"name=peach_cb") > peach_cb.tar
```



## 文件

### Dockerfile.cb

这里直接用了aliyun的镜像，网络好可以直接用官方的，不用改。

最后的两行是用来编译doc的，如果这里加上了，运行docker也要加。因此建议不加，不影响正常使用。

在线安装mono和nuget的库可能需要网络条件。可以用nuget的离线安装库。mono可以离线编译并安装。

```dockerfile
FROM debian:stretch AS peachcb
MAINTAINER stackofg@gmail.com
#install mono for configure and build
#online install mono
#use mirrors of aliyun
WORKDIR /etc/apt/
COPY ./install/sources.list .
#install software
RUN apt-get update && apt-get install -y apt-utils apt-transport-https ca-certificates \
    ruby doxygen wget nodejs node-typescript dirmngr gnupg python \
    gcc g++ bash-completion
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb https://download.mono-project.com/repo/debian stable-stretch main" | tee /etc/apt/sources.list.d/mono-official-stable.list
RUN apt-get update && apt-get install -y mono-devel
#if nuget can download the component directly,can not use the local package.
WORKDIR /root/.nuget
COPY ./install/packages.tar.bz2 .
RUN tar -xf packages.tar.bz2 && rm -rf packages.tar.bz2
#for compile doc,suggest not install
#RUN gem install bundler
#RUN apt-get install -y libxml2-utils openjdk-8-jre xsltproc
WORKDIR /protocol-fuzzer-ce
COPY ./install/docker/*.sh .
RUN chmod +x *.sh
```

### Dockerfile.ir

安装运行需要mono 4。这里通过指定源的方式在线安装，也可以离线编译安装。

```
FROM debian:stretch AS peachir
MAINTAINER stackofg@gmail.com

#use mirrors of aliyun
WORKDIR /etc/apt/
COPY ./install/sources.list .
#install software
RUN apt-get update && apt-get install -y apt-utils vim bash-completion wget python \
    gcc g++ apt-transport-https dirmngr gnupg ca-certificates libpcap-dev

#install mono for install and run
#online install mono
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/ubuntu wheezy/snapshots/4.8.1.0 main" | tee /etc/apt/sources.list.d/mono-official.list
RUN apt-get update && apt-get install -y mono-complete=4.8.1.0-0xamarin1
WORKDIR /protocol-fuzzer-ce
COPY ./install/docker/*.sh .
RUN chmod +x *.sh
WORKDIR /peach
```



## 参考

### 删除所有docker

```
docker stop $(docker ps -aq)
docker rm $(docker ps -aq)
docker rmi $(docker images -q)
```

