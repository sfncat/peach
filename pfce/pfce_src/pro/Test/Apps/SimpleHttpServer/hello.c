#include <stdio.h>
#include <string.h>
#include "mongoose.h"

void CrashMe(const char* in)
{
	char buff[10];
	printf("\nIn CrashMe()\n");
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

// This function will be called by mongoose on every new request
static int index_html(struct mg_connection *conn) {
  mg_printf_data(conn, "Hello! Requested URI is [%s], query string is [%s]",
                 conn->uri,
                 conn->query_string == NULL ? "(none)" : conn->query_string);

  if (strlen(conn->uri) > 1024) {
    CrashMe(conn->uri);
  }

  return 0;
}

int stop = 0;

int main(void) {
  struct mg_server *server;

  // Create and configure the server
  server = mg_create_server(NULL);
  mg_set_option(server, "listening_port", "8080");
  mg_add_uri_handler(server, "/", index_html);

  // Serve request. Hit Ctrl-C to terminate the program
  printf("Starting on port %s\n", mg_get_option(server, "listening_port"));
  while (!stop) {
    mg_poll_server(server, 1000);
  }

  // Cleanup, and free server instance
  mg_destroy_server(&server);

  return 0;
}
