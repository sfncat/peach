# 通过Docker运行Peach

## 说明

从其它电脑导出运行镜像导入后运行peach。

这里以在ubuntu 20.04运行为例。

## 运行目录树

```
-pfce
  --run_r.sh
  --import_img.sh
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

## 部署Docker环境install_docker.sh

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

## 运行本地目录树初始化init_r_env.sh

```bash
mkdir -p pfce/peach
echo "export INT_PATH=`pwd`/pfce" >> ~/.bashrc
echo "export PATH=$INT_PATH:$PATH" >>~/.bashrc
echo "alias runpit='docker exec -it peach_ir /peach/peach'" >> ~/.bashrc
source ~/.bashrc
```

## 当前用户增加docker权限

增加权限后，用户需要退出重登录

```
sudo gpasswd -a ${USER} docker
```

## 导入镜像import_img.sh

```
docker import - peach:ir <peach_ir.tar
```

## 运行镜像run_r.sh

```bash
docker run -itd --name peach_ir -v $INT_PATH/peach:/peach  peach:ir /bin/bash
```

## 运行测试套

```
runpit pits/http/http.xml -1
```


