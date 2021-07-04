
# Dockerfile for Peach Fuzzer Professional
# Copyright (c) Peach Fuzzer, LLC
#

FROM ubuntu:14.04
MAINTAINER Peach Support <support@peachfuzzer.com>

# Peach Linux x64 Install Zip
ARG peachzip=peach-pro-3.8.65-linux_x86_64_release.zip

#
# Install Mono
#

RUN apt-get update \
	&& apt-get install -y curl zip openssh-client  \
	&& rm -rf /var/lib/apt/lists/*

RUN apt-key adv --keyserver pgp.mit.edu --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb http://download.mono-project.com/repo/debian wheezy/snapshots/4.2.1.102 main" > /etc/apt/sources.list.d/mono-xamarin.list \
	&& apt-get update \
	&& apt-get install -y mono-devel ca-certificates-mono fsharp mono-vbnc nuget \
	&& rm -rf /var/lib/apt/lists/*

#
# Install Peach
#

EXPOSE 8888

RUN mkdir -p /opt/peach
WORKDIR /opt/peach

ADD $peachzip /opt/peach
RUN unzip /opt/peach/$peachzip

ADD runpeach.sh /opt/peach
RUN chmod a+x /opt/peach/runpeach.sh

VOLUME /opt/peach

USER root

RUN chmod a+x /opt/peach/peach
CMD /opt/peach/runpeach.sh

# end
