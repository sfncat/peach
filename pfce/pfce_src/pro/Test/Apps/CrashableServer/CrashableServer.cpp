#ifdef WIN32

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0501
#endif

#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <stdio.h>
#include <winsock2.h>
#include <tchar.h>
#include <windows.h>

typedef int socklen_t;

// FD_SET uses while(0) so disable conitional expression is constant warning
#pragma warning(disable:4127)

#else

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>

typedef int SOCKET;

struct WSAData {};

#define SOCKET_ERROR -1
#define INVALID_SOCKET -1
#define _tmain main

#define MAKEDWORD(a,b) (0)
#define WSAStartup(a,b) (0)
#define __try if(1)
#define __except(a) if(0)

inline int closesocket(int s) { return close(s); }
inline void WSACleanup() {}
inline int WSAGetLastError() { return errno; }
inline void WSASetLastError(int err) { errno = err; }

#define WSAETIMEDOUT ETIMEDOUT

#endif

const int kDefaultServerPort = 4242;
const int kBufferSize = 1024;

SOCKET SetUpListener(const char* host, int port);
SOCKET AcceptConnection(SOCKET listener, sockaddr_in* remote, int timeout);
bool EchoIncomingPackets(SOCKET socket);
int Run(const char* host, int port, int timeout);

int main(int argc, char* argv[])
{
	WSAData wsaData;
	int ret;
	const char* host;
	int port;
	int timeout;
	int retval = 0;

	// disable buffering of stdout
	setbuf(stdout, NULL);

	printf("CrashableServer starting...\n");

	// Do we have enough command line arguments?
	if (argc < 2) {
		fprintf(stderr, "usage: %s <server-address> [server-port] [timeout]\n", argv[0]);
		fprintf(stderr, "\tIf you don't pass server-port, it defaults to %d.\n", kDefaultServerPort);
		return 1;
	}

	host = argv[1];
	port = (argc >= 3) ? atoi(argv[2]) : kDefaultServerPort;
	timeout = (argc >= 4) ? atoi(argv[3]) : -1;

	if (argc > 4) {
		fprintf(stderr, "%d extra argument%s ignored.  FYI.\n", argc - 3, argc == 4 ? "" : "s");
	}

	if ((ret = WSAStartup(MAKEWORD(1, 1), &wsaData)) != 0) {
		fprintf(stderr, "WSAStartup() returned error code %d.\n", ret);
		return 255;
	}

	__try
	{
		retval = Run(host, port, timeout);
	}
	__except(GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION)
	{
		fprintf(stderr, "Caught AV exception.\n");
	}

	WSACleanup();
	return retval;
}

int Run(const char* host, int port, int timeout)
{
	SOCKET listener;

	printf("Establishing the listener...\n");

	listener = SetUpListener(host, htons((u_short)port));
	if (listener == INVALID_SOCKET) {
		fprintf(stderr, "\nestablish listener error: %d\n", WSAGetLastError());
		return 3;
	}

	for (;;) {
		SOCKET socket;
		sockaddr_in remote;

		printf("Waiting for a connection...\n");

		socket = AcceptConnection(listener, &remote, timeout);
		if (socket == INVALID_SOCKET) {
			if (WSAETIMEDOUT != WSAGetLastError()) {
				fprintf(stderr, "\naccept connection error: %d\n", WSAGetLastError());
			}
			else {
				fprintf(stderr, "\nTimed out waiting for connection\n");
			}
			return 3;
		}

		printf("Accepted connection from %s:%d.\n",
			inet_ntoa(remote.sin_addr),
			ntohs(remote.sin_port));

		if (!EchoIncomingPackets(socket)) {
			fprintf(stderr, "\necho incoming packets error: %d\n", WSAGetLastError());
			return 3;
		}

		printf("Shutting connection down...\n");

		if (closesocket(socket) != 0) {
			fprintf(stderr, "\nshutdown connection error: %d\n", WSAGetLastError());
			return 3;
		}

		printf("Connection is down.\n");
	}
}

SOCKET SetUpListener(const char* host, int port)
{
	SOCKET listener;
	u_long addr;
	sockaddr_in sa;
	int optval;

	addr = inet_addr(host);
	if (addr == INADDR_NONE)
		return INVALID_SOCKET;

	listener = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (listener == INVALID_SOCKET)
		return INVALID_SOCKET;

	optval = 1;
	if (setsockopt(listener, SOL_SOCKET, SO_REUSEADDR, (const char*)&optval, sizeof(optval)) == SOCKET_ERROR)
		return INVALID_SOCKET;

	memset(&sa, 0, sizeof(sa));
	sa.sin_family = AF_INET;
	sa.sin_addr.s_addr = addr;
	sa.sin_port = (u_short)port;

	if (bind(listener, (sockaddr*)&sa, sizeof(sa)) == SOCKET_ERROR)
		return INVALID_SOCKET;

	if (listen(listener, 1) == SOCKET_ERROR)
		return INVALID_SOCKET;

	return listener;
}

SOCKET AcceptConnection(SOCKET listener, sockaddr_in* remote, int timeout)
{
	socklen_t size = sizeof(*remote);

	if (timeout >= 0) {
		struct timeval tv;
		fd_set set;
		int ret;

		FD_ZERO(&set);
		FD_SET(listener, &set);

		tv.tv_sec = timeout;
		tv.tv_usec = 0;

		ret = select((int)listener + 1, &set, NULL, NULL, &tv);
		if (ret == 1) {
			return INVALID_SOCKET;
		}
		else if (ret == 0) {
			WSASetLastError(WSAETIMEDOUT);
			return INVALID_SOCKET;
		}
	}

	return accept(listener, (sockaddr*)remote, &size);
}

void CrashMe(char* in)
{
	printf("\nIn CrashMe()\n");
	char buff[10];
	// Should A/V us :)
	strcpy(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
	strcat(buff, in);
}

bool EchoIncomingPackets(SOCKET socket)
{
	char* buf = (char*)malloc(kBufferSize);
	int len;

	do {
		len = recv(socket, buf, kBufferSize, 0);
		if (len > 0) {
			printf("Received %d bytes from client.\n", len);

			// Add a silly stack overflow
			if (len >= 1024) {
				CrashMe(buf);
			}
		}
		else if (len == SOCKET_ERROR) {
			return false;
		}
	} while (len != 0);

	printf("Connection closed by peer.\n");

	return true;
}
