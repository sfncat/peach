# 编译PeachPro 4.0(protocol-fuzzer-ce) for windows

## 主要参考
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

![image-20210703111228141](G:\github\peach\readme\image-20210703111228141.png)

![image-20210703111557242](G:\github\peach\readme\image-20210703111557242.png)

![image-20210703111724703](G:\github\peach\readme\image-20210703111724703.png)

### 7.Intel Pin

直接下载，界面上找不到下载链接

windows:

http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-msvc-windows.zip

linux:

http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz

下载后解压放到3rdParty/pin/pin-3.2-81205-msvc-windows下

![image-20210704124543906](G:\github\peach\readme\image-20210704124543906.png)



### 8.Visual C++ Redistributable for Visual Studio 2012 Update 4

https://www.microsoft.com/en-us/download/confirmation.aspx?id=30679&6B49FDFB-8E5B-4B07-BC31-15695C5A2143=1

下载后直接安装

### 9.WinDBG

这个不安装不影响编译。

下载ISO,挂载后安装，选Debugging Tools for Windows

https://go.microsoft.com/fwlink/p/?linkid=2120735

![img](G:\github\peach\readme\debugger-download-sdk.png)



## 下载代码

```
git clone https://gitlab.com/gitlab-org/security-products/protocol-fuzzer-ce.git
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

![image-20210704171922979](G:\github\peach\readme\image-20210704171922979.png)

如果有utf-8的报错，可以不用管。

![image-20210704171820022](G:\github\peach\readme\image-20210704171820022.png)

waf configure其实也在下载依赖组件，通过paket调用nuget下载组件（paket.dependencies）,会先下载到C:\Users\当前用户\\.nuget\packages再将其中一部分复制到paket\packages下。

这个过程对网络要求较高，网络好一会就下好了，可以使用下面的命令单独下载组件

```
G:\github\protocol-fuzzer-ce\paket\.paket> .\paket.exe restore --verbose
```

这里提供了下载好的zip包，可以在执行configure之前解压放到

```
C:\Users\当前用户\.nuget\packages
```

![image-20210704172309833](G:\github\peach\readme\image-20210704172309833.png)

最后会提示成功

![image-20210704172359684](G:\github\peach\readme\image-20210704172359684.png)



### waf build

如果configure正常，build一般成功率就比较高了。

```
py -2.7 waf build
```



![image-20210704173206262](G:\github\peach\readme\image-20210704173206262.png)

### waf install

![image-20210704173528674](G:\github\peach\readme\image-20210704173528674.png)

可执行文件在G:\github\protocol-fuzzer-ce\output下

## 样例测试套





## choco



https://chocolatey.org/install



```
管理员powershell
Set-ExecutionPolicy AllSigned 选A
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
```



```
choco install jdk8
choco install xsltproc
choco install git
```
## doxygen
https://www.doxygen.nl/download.html
 edit the PATH environment variable to include C:\Program Files\doxygen\bin.





## TypeScript Compiler



https://nodejs.org/dist/v14.17.1/node-v14.17.1-x64.msi
```
npm install typescript --global
```
## Intel Pin
http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-msvc-windows.zip
http://software.intel.com/sites/landingpage/pintool/downloads/pin-3.2-81205-gcc-linux.tar.gz



## .NET Framework 4.5.1
## WinDBG
https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools
https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/

## WireShark
## Visual C++ Redistributable for Visual Studio 2012 Update 4
https://www.microsoft.com/en-us/download/details.aspx?id=30679
https://www.microsoft.com/en-us/download/confirmation.aspx?id=30679&6B49FDFB-8E5B-4B07-BC31-15695C5A2143=1

# docker linux
https://github.com/vanhauser-thc/peachpro
