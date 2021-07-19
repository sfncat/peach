#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#include "mongoose.h"
#include "frozen.h"

static int on_status_true(struct mg_connection *conn)
{
	printf("  Status: True\n");
	mg_send_header(conn, "Content-Type", "application/json");
	mg_printf_data(conn, "{\"status\":true}");
	return MG_TRUE;
}

static int on_status_false(struct mg_connection *conn)
{
	printf("  Status: False\n");
	mg_send_header(conn, "Content-Type", "application/json");
	mg_printf_data(conn, "{\"status\":false}");
	return MG_TRUE;
}

static int on_agent_connect(struct mg_connection *conn) {
	printf("Agent Connect\n");
	return on_status_true(conn);
}

static int on_agent_disconnect(struct mg_connection *conn) {
	printf("Agent Disconnect\n");
	return on_status_true(conn);
}

static int on_create_publisher(struct mg_connection *conn) {
	printf("Create Publisher\n");
	return on_status_false(conn);
}

static int on_start_monitor(struct mg_connection *conn) {
	struct json_token tokens[32];
	const struct json_token *tok;
	char name[64];
	int name_len;
	char cls[64];
	int cls_len;
	int i;

	if (strcmp("POST", conn->request_method))
	{
		mg_send_status(conn, 405);
		mg_printf_data(conn, "This resource does not support the HTTP method %s.", conn->request_method);
		return MG_TRUE;
	}

	name_len = mg_get_var(conn, "name", name, sizeof(name));
	cls_len = mg_get_var(conn, "cls", cls, sizeof(cls));

	if (-1 == name_len || -1 == cls_len)
	{
		printf("Start Monitor\n");
		return on_status_false(conn);
	}

	i = parse_json(conn->content, conn->content_len, tokens, sizeof(tokens) / sizeof(tokens[0]));
	if (i < 0)
	{
		printf("Start Monitor (JSON parse failed)\n");
		return on_status_false(conn);
	}

	tok = find_json_token(tokens, "args");
	if (NULL == tok || tok->type != JSON_TYPE_OBJECT)
	{
		printf("Start Monitor (Missing 'args' object)\n");
		return on_status_false(conn);
	}

	printf("Start Monitor [name=%s,cls=%s]\n", name, cls);

	for (i = 1; i <= tok->num_desc; i += 2)
	{
		const struct json_token *item = tok + i;
		char *p_name, *p_value;

		p_name = (char*)malloc(item->len + 1);
		memcpy(p_name, item->ptr, item->len);
		p_name[item->len] = '\0';

		++item;

		p_value = (char*)malloc(item->len + 1);
		memcpy(p_value, item->ptr, item->len);
		p_value[item->len] = '\0';

		printf("    %s = %s\n", p_name, p_value);

		free(p_name);
		free(p_value);

		i += item->num_desc;
	}

	return on_status_true(conn);
}

static int on_stop_monitor(struct mg_connection *conn) {
	printf("Stop Monitor\n");
	return on_status_true(conn);
}

static int on_stop_all_monitors(struct mg_connection *conn) {
	printf("Stop All Monitors\n");
	return on_status_true(conn);
}

static int on_session_starting(struct mg_connection *conn) {
	printf("Session Starting\n");
	return on_status_true(conn);
}

static int on_session_finished(struct mg_connection *conn) {
	printf("Session Finished\n");
	return on_status_true(conn);
}

static int on_iteration_starting(struct mg_connection *conn) {
	char iter[32];
	int iter_len = mg_get_var(conn, "iterationCount", iter, sizeof(iter));
	char repro[32];
	int repro_len = mg_get_var(conn, "isReproduction", repro, sizeof(repro));

	if (-1 == iter_len || -1 == repro_len)
	{
		printf("Iteration Starting\n");
		return on_status_false(conn);
	}

	printf("Iteration Starting [iterationCount=%s,isReproduction=%s]\n", iter, repro);
	return on_status_true(conn);
}

static int on_iteration_finished(struct mg_connection *conn) {
	printf("Iteration Finished\n");
	return on_status_true(conn);
}

static int on_detected_fault(struct mg_connection *conn) {
	printf("Detected Fault\n");
	return on_status_false(conn);
}

static int on_get_monitor_data(struct mg_connection *conn) {
	//*
	const char* results =
	"{"
		"\"iteration\":0,"
		"\"controlIteration\":false,"
		"\"controlRecordingIteration\":false,"
		"\"type\":0," //  (0 unknown, 1 Fault, 2 Data)"
		"\"detectionSource\":null,"
		"\"title\":null,"
		"\"description\":null,"
		"\"majorHash\":null,"
		"\"minorHash\":null,"
		"\"exploitability\":null,"
		"\"folderName\":null,"
		"\"collectedData\":["
			"{\"Key\":\"data1\",\"Value\":\"AA==\"}"
		"]"
	"}";
	/*/
	const char* results = "";
	//*/

	printf("Get Monitor Data\n");

	mg_send_header(conn, "Content-Type", "application/json");
	mg_printf_data(conn, "{\"Results\":[%s]}", results);

	return MG_TRUE;
}

static int on_must_stop(struct mg_connection *conn) {
	printf("Must Stop\n");
	return on_status_false(conn);
}

static int agent_request(struct mg_connection *conn) {
	if (!strcmp("/Agent/AgentConnect", conn->uri))
		return on_agent_connect(conn);
	if (!strcmp("/Agent/AgentDisconnect", conn->uri))
		return on_agent_disconnect(conn);
	if (!strcmp("/Agent/CreatePublisher", conn->uri))
		return on_create_publisher(conn);
	if (!strcmp("/Agent/StartMonitor", conn->uri))
		return on_start_monitor(conn);
	if (!strcmp("/Agent/StopMonitor", conn->uri))
		return on_stop_monitor(conn);
	if (!strcmp("/Agent/StopAllMonitors", conn->uri))
		return on_stop_all_monitors(conn);
	if (!strcmp("/Agent/SessionStarting", conn->uri))
		return on_session_starting(conn);
	if (!strcmp("/Agent/SessionFinished", conn->uri))
		return on_session_finished(conn);
	if (!strcmp("/Agent/IterationStarting", conn->uri))
		return on_iteration_starting(conn);
	if (!strcmp("/Agent/IterationFinished", conn->uri))
		return on_iteration_finished(conn);
	if (!strcmp("/Agent/DetectedFault", conn->uri))
		return on_detected_fault(conn);
	if (!strcmp("/Agent/GetMonitorData", conn->uri))
		return on_get_monitor_data(conn);
	if (!strcmp("/Agent/MustStop", conn->uri))
		return on_must_stop(conn);

	return MG_FALSE;
}

static int agent_handler(struct mg_connection *conn, enum mg_event ev) {
	int result = MG_FALSE;

	if (ev == MG_REQUEST) {
		result = agent_request(conn);
	} else if (ev == MG_AUTH) {
		result = MG_TRUE;
	}

	return result;
}

int main(int argc, const char** argv)
{
	struct mg_server *server;

	// Create and configure the server
	server = mg_create_server(NULL, agent_handler);
	mg_set_option(server, "listening_port", "8080");

	// Serve request. Hit Ctrl-C to terminate the program
	printf("Starting on port %s\n", mg_get_option(server, "listening_port"));
	for (;;) {
		mg_poll_server(server, 1000);
	}

	// Cleanup, and free server instance
	mg_destroy_server(&server);
	return 0;
}
