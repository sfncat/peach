FROM mono:latest

RUN apt-get update \
	&& apt-get upgrade -y \
	&& apt-get install -y curl software-properties-common \
	&& rm -rf /var/lib/apt/lists/*

RUN curl -sL https://deb.nodesource.com/setup_6.x | bash -

RUN echo "deb http://http.debian.net/debian wheezy-backports main" > /etc/apt/sources.list.d/backports.list

RUN apt-get update \
	&& apt-get install -y \
		apt-transport-https \
		python-pip \
		build-essential \
		ca-certificates \
		gnupg2 \
		nodejs \
		zip \
	&& rm -rf /tmp/*

		# clang \
		# doxygen \
		# g++-multilib \
		# libxml2-utils \
		# openjdk-7-jdk \
		# python \
		# xsltproc \

RUN pip install awscli

RUN npm install -g \
	typescript@1.6

# RUN gem install bundler

RUN apt-key adv \
	--keyserver hkp://ha.pool.sks-keyservers.net:80 \
	--recv-keys 58118E89F3A912897C070ADBF76221572C52609D

RUN echo "deb https://apt.dockerproject.org/repo debian-wheezy main" > /etc/apt/sources.list.d/docker.list

RUN apt-get update \
	&& apt-get install -y \
		docker-engine

RUN curl https://www.npmjs.com/install.sh | sh

RUN cd /tmp \
	&& curl -O https://releases.hashicorp.com/packer/0.12.1/packer_0.12.1_linux_amd64.zip \
	&& unzip packer_0.12.1_linux_amd64.zip \
	&& mv packer /usr/bin \
	&& rm -rf /tmp/*

RUN https_proxy="" \
	&& cd /tmp \
	&& curl -k -O https://10.0.1.200/userContent/VMware-ovftool-4.2.0-4586971-lin.x86_64.bundle \
	&& bash VMware-ovftool-4.2.0-4586971-lin.x86_64.bundle --eulas-agreed --required \
	&& rm -rf /tmp/*

VOLUME /build
WORKDIR /build
