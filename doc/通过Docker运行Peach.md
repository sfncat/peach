# 通过Docker运行Peach

## 说明

这里以在ubuntu 20.04运行为例。

## 运行目录树

```
-pfce
  --run_ir.sh
  --peach:安装目录
    ---pits:测试套目录
      ----XX:XX测试套
        -----bin:消息文件目录
        -----python:python拓展目录
        -----logs:logs目录
        -----XX_Data.xml:数据模型文件
        -----XX1.xml:XX测试套1入口文件
        -----XX1.xml.conf:XX测试套1配置文件
        -----XX1_State1.xml:XX1状态模型文件
        -----XX2.xml:XX测试套2入口文件
        -----XX2.xml.conf:XX测试套2配置文件
        -----XX2_State1.xml:XX2状态模型文件
```

## 部署Docker环境

```bash
# Install dependencies to install Docker
sudo apt -qqy -o Dpkg::Options::='--force-confdef' -o Dpkg::Options::='--force-confold' install apt-transport-https ca-certificates curl gnupg-agent software-properties-common openssl

# Register Docker package registry
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

# Refresh package udpates and install Docker
sudo apt -qqy update
sudo apt -qqy -o Dpkg::Options::='--force-confdef' -o Dpkg::Options::='--force-confold' install docker-ce docker-ce-cli containerd.io
```

## 运行本地目录树初始化

```
mkdir -p pfce/peach
echo "export INT_PATH=`pwd`/pfce" >>/etc/profile
echo "export PEACH_PATH=$INT_PATH/peach">>/etc/profile
echo "export PATH=$INT_PATH:$PATH" >>/etc/profile
export PEACH_PATH=`pwd`/pfce/peach
$export PATH=$PEACH_PATH:$PATH
$cd pfce/install
$wget https://dl.google.com/go/go1.14.2.linux-amd64.tar.gz

```

## 创建并运行镜像

```bash
cd $INT_PATH
sudo su
docker build -t peach:ir -f ./docker/ir/Dockerfile .
chmod +x ./run_cb.sh
./run_cb.sh

```

## 



```

docker run --name peach -v $INT_PATH/protocol-fuzzer-ce/:/protocol-fuzzer-ce -v $INT_PATH/peach:/peach peach:ir
```

