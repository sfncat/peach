#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef WIN32

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#define sleep Sleep
#define SLEEP_FACTOR 1000

#else // WIN32

#include <unistd.h>
#include <signal.h>
#include <sys/wait.h>
#define SLEEP_FACTOR 1

void cmd_fork();
void cmd_nosigterm();

#endif // WIN32

int main(int argc, char** argv) {
	if (argc < 2) {
		printf("Missing args\n");
		return 1;
	}
	if (!strcmp(argv[1], "exit")) {
		return atoi(argv[2]);
	}
	else if (!strcmp(argv[1], "timeout")) {
		sleep(atoi(argv[2]) * SLEEP_FACTOR);
	}
	else if (!strcmp(argv[1], "regex")) {
		fprintf(stdout, "%s\n", argv[2]);
		fprintf(stderr, "%s\n", argv[3]);
	}
	else if (!strcmp(argv[1], "when")) {
		FILE* fout = fopen(argv[2], "w");
		fprintf(fout, "%s", argv[3]);
		fclose(fout);
	}
	else if (!strcmp(argv[1], "when")) {
		FILE* fout = fopen(argv[2], "w");
		fprintf(fout, "%s", argv[3]);
		fclose(fout);
	}
	else if (!strcmp(argv[1], "fork")) {
#ifdef WIN32
		printf("Not supported\n");
#else
		cmd_fork();
#endif
	}
	else if (!strcmp(argv[1], "nosigterm")) {
#ifdef WIN32
		printf("Not supported\n");
#else
		cmd_nosigterm();
#endif
	}
	return 0;
}

#ifndef WIN32

const char* signal_str(int sig) {
	switch (sig) {
		case SIGHUP   : return "SIGHUP";
		case SIGINT   : return "SIGINT";
		case SIGQUIT  : return "SIGQUIT";
		case SIGILL   : return "SIGILL";
		case SIGTRAP  : return "SIGTRAP";
		case SIGABRT  : return "SIGABRT";
		// case SIGEMT   : return "SIGEMT";
		case SIGFPE   : return "SIGFPE";
		case SIGKILL  : return "SIGKILL";
		case SIGBUS   : return "SIGBUS";
		case SIGSEGV  : return "SIGSEGV";
		case SIGSYS   : return "SIGSYS";
		case SIGPIPE  : return "SIGPIPE";
		case SIGALRM  : return "SIGALRM";
		case SIGTERM  : return "SIGTERM";
		case SIGURG   : return "SIGURG";
		case SIGSTOP  : return "SIGSTOP";
		case SIGTSTP  : return "SIGTSTP";
		case SIGCONT  : return "SIGCONT";
		case SIGCHLD  : return "SIGCHLD";
		case SIGTTIN  : return "SIGTTIN";
		case SIGTTOU  : return "SIGTTOU";
		case SIGIO    : return "SIGIO";
		case SIGXCPU  : return "SIGXCPU";
		case SIGXFSZ  : return "SIGXFSZ";
		case SIGVTALRM: return "SIGVTALRM";
		case SIGPROF  : return "SIGPROF";
		case SIGWINCH : return "SIGWINCH";
		// case SIGINFO  : return "SIGINFO";
		case SIGUSR1  : return "SIGUSR1";
		case SIGUSR2  : return "SIGUSR2";
		default:        return "<unknown>";
	}
}

int handle_sigterm = 1;

void signal_handler(int sig) {
	int* foo = (int*)0xDEADBEEF;
	printf("[%d] signal: (%d) %s\n", getpid(), sig, signal_str(sig));

	if (!handle_sigterm) {
		fflush(stdout);
		close(0);
		close(1);
		close(2);
		return;
	}

	switch (sig) {
		case SIGTERM:
			for (;;) {
				memset(foo, 1, 4*1024*1024);
				foo += 4*1024*1024;
			}
			_exit(EXIT_SUCCESS);
			break;
	}
}

void cmd_fork() {
	signal(SIGINT, signal_handler);
	signal(SIGKILL, signal_handler);
	signal(SIGTERM, signal_handler);
	signal(SIGCHLD, signal_handler);

	printf("fork()\n");
	pid_t pid = fork();
	if (pid == -1) {
		perror("fork() failed");
		exit(EXIT_FAILURE);
	}
	if (pid == 0) {
		printf("child: %d\n", getpid());
		pause();
		_exit(EXIT_SUCCESS);
	} else {
		printf("parent: %d\n", getpid());
		int status;

		do {
			printf("waitpid(%d)\n", pid);
			int ret = waitpid(pid, &status, 0);
			if (ret == -1) {
				perror("waitpid() failed");
				exit(EXIT_FAILURE);
			}

			if (WIFEXITED(status)) {
				printf("exited, status=%d\n", WEXITSTATUS(status));
			} else if (WIFSIGNALED(status)) {
				printf("killed by signal %d\n", WTERMSIG(status));
			} else if (WIFSTOPPED(status)) {
				printf("stopped by signal %d\n", WSTOPSIG(status));
			} else if (WIFCONTINUED(status)) {
				printf("continued\n");
			}
		} while (!WIFEXITED(status) && !WIFSIGNALED(status));

		exit(EXIT_SUCCESS);
	}
}

void cmd_nosigterm() {
	handle_sigterm = 0;
	printf("Ignoring SIGTERM, closing stdout/stderr and pausing...\n");
	signal(SIGINT, signal_handler);
	signal(SIGKILL, signal_handler);
	signal(SIGTERM, signal_handler);
	signal(SIGCHLD, signal_handler);
	pause();
	pause();
}

#endif
