# 编译PeachPro 4.0(protocol-fuzzer-ce) for windows

## 主要参考
更新：2021.9.20 更新pin 3.19.98425

主要参考这位大佬的编译过程，其实主要是安装依赖的过程实在是过于复杂。
https://medium.com/csg-govtech/lifes-a-peach-fuzzer-how-to-build-and-use-gitlab-s-open-source-protocol-fuzzer-fd78c9caf05e

## 安装依赖
### 1.安装python2.7.18
Peach是基于python2.7的，需要安装python2.7并增加路径
https://repo.huaweicloud.com/python/2.7.18/python-2.7.18.amd64.msi

如果同时安装python3,可以通过python launcher使用py -2.7来调用python2
### 2.安装TypeScript Compiler及chocolatey
https://nodejs.org/dist/v14.17.1/node-v14.17.1-x64.msi
安装过程要选择安装chocolatey
安装结束后安装typescript

```
npm install typescript --global
```


### 3.安装Ruby 2.7
https://github.com/oneclick/rubyinstaller2/releases/download/RubyInstaller-2.7.3-1/rubyinstaller-2.7.3-1-x64.exe
安装msys2 and mingw development toolchain

### 4.使用choco安装Java,xsltprocs,git
前面安装node时已经安装了choco，这里使用choco安装Java,xsltprocs,git
```
choco install jdk8
choco install xsltproc
choco install git
```
### 5.安装doxygen
https://doxygen.nl/files/doxygen-1.9.1-setup.exe
安装doxygen并把C:\Program Files\doxygen\bin加到Path

### 6.安装.NET Framework 4.6.1/.NET Framework 4.5.1及C++ compilers

使用Visual Studio Community 2017 (version 15.9)进行安装，安装时间较长。

https://my.visualstudio.com/Downloads?q=visual%20studio%202017&wt.mc_id=o~msft~vscom~older-downloads 
选择"使用C++的桌面开发"，"使用C++的Linux开发","Framework 3.5/4.5/4.5.1/4.6.1/4.6.1SDK"

![image-20210703111228141](README.assets/image-20210703111228141.png)

![image-20210703111557242](README.assets/image-20210703111557242.png)

![image-20210703111724703](README.assets/image-20210703111724703.png)

### 7.Intel Pin

更新3.19.98425

windows:

https://software.intel.com/sites/landingpage/pintool/downloads/pin-3.19-98425-gd666b2bee-msvc-windows.zip

linux:

https://software.intel.com/sites/landingpage/pintool/downloads/pin-3.19-98425-gd666b2bee-gcc-linux.tar.gz

解压到3rdParty/pin目录下，目录改名为pin-3.19-98425-msvc-windows



****作废2021.9.20****

直接下载，界面上找不到下载链接

windows:

http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-msvc-windows.zip

linux:

http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz

下载后解压放到3rdParty/pin/pin-3.2-81205-msvc-windows下

![image-20210704124543906](README.assets/image-20210704124543906.png)

****作废2021.9.20****

### 8.Visual C++ Redistributable for Visual Studio 2012 Update 4

https://www.microsoft.com/en-us/download/confirmation.aspx?id=30679&6B49FDFB-8E5B-4B07-BC31-15695C5A2143=1

下载后直接安装

### 9.WinDBG

这个不安装不影响编译。

下载ISO,挂载后安装，选Debugging Tools for Windows

https://go.microsoft.com/fwlink/p/?linkid=2120735

![img](README.assets/debugger-download-sdk.png)



## 下载代码

```
git clone https://gitlab.com/gitlab-org/security-products/protocol-fuzzer-ce.git
```

## 修改代码

当前commit（Merge branch 'pin3.19support' into 'main'）下修改代码不完整，无法编译，需要修改代码。

build\config\win.py

STLIB中的'pinvm','m-static','c-static', 'os-apis', 'ntdll-64' 删除

			'STLIB': [
				'pin', 'xed',  'pincrt',
	
			],

core\BasicBlocks\bblocks.cpp增加STATIC_ASSERT定义

```
#define STATIC_ASSERT(expr) typedef char __static_assert[expr ? 1 : -1] __attribute__((__unused__));
```

## 编译

### 注册表

通过管理员PowerShell执行

```powershell
new-itemproperty -path "HKLM:\SOFTWARE\Microsoft\.NETFramework\v4.0.30319" -name "SchUseStrongCrypto" -Value 1 -PropertyType "DWord";
new-itemproperty -path "HKLM:\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319" -name "SchUseStrongCrypto" -Value 1 -PropertyType "DWord"
```



### waf configure

使用以下命令进行配置 -v显示详细信息，如果有依赖不存在会有打印。

```
G:\github\protocol-fuzzer-ce\py -2.7 waf configure -v
```

![image-20210704171922979](README.assets/image-20210704171922979.png)

如果有utf-8的报错，可以不用管。

![image-20210704171820022](README.assets/image-20210704171820022.png)

waf configure其实也在下载依赖组件，通过paket调用nuget下载组件（paket.dependencies）,会先下载到C:\Users\当前用户\\.nuget\packages再将其中一部分复制到paket\packages下。

这个过程对网络要求较高，网络好一会就下好了，可以使用下面的命令单独下载组件

```
G:\github\protocol-fuzzer-ce\paket\.paket> .\paket.exe restore --verbose
```

这里提供了下载好的zip包(https://github.com/sfncat/peach/blob/main/packages.zip)，可以在执行configure之前解压放到

```
C:\Users\当前用户\.nuget\packages
```

![image-20210704172309833](README.assets/image-20210704172309833.png)

最后会提示成功

![image-20210704191059162](README.assets/image-20210704191059162.png)



### waf build

如果configure正常，build一般成功率就比较高了。

```
py -2.7 waf build
```



![image-20210704173206262](README.assets/image-20210704173206262.png)

### waf install

![image-20210704173528674](README.assets/image-20210704173528674.png)

可执行文件在G:\github\protocol-fuzzer-ce\output下

## 样例测试套

这里根据这位大佬给的样例改造了一个正常一点样例测试套，主要用来测试一下peach是否可以正常运行。熟悉的peach界面出现了：）

![image-20210704184739047](README.assets/image-20210704184739047.png)

测试套https://github.com/sfncat/peach/tree/main/pits/http放到pits/http目录下





# docker linux
https://github.com/vanhauser-thc/peachpro
