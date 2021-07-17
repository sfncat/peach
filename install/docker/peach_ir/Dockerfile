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
WORKDIR /peach
