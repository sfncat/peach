import clr

clr.AddReference("Titanium.Web.Proxy")

from System.Collections.Generic import List
from Titanium.Web.Proxy.Models import HttpHeader
from System import Array, Byte

__current_route = None
__request_funcs = {}

EVENT_ACTION = 1

def register_event(kind, func):
	if kind != 1:
		return

	global __current_route;
	global __request_funcs;

	funcs = __request_funcs.get(__current_route, [])
	funcs.append(func)
	__request_funcs[__current_route] = funcs

class Uri(object):
	def __init__(self, uri):
		self._peach_uri = uri

	@property
	def scheme(self):
		return self._peach_uri.Scheme

	@property
	def host(self):
		return self._peach_uri.Host

	@property
	def port(self):
		return self._peach_uri.Port

	@property
	def path(self):
		return self._peach_uri.AbsolutePath

	@property
	def query(self):
		return self._peach_uri.Query

class Header(object):
	def __init__(self, name, value, item = None):
		self._item = item or HttpHeader(name, value)

	def __repr__(self):
		return "{'name' : '%s', 'value' : '%s'}" % (self._item.Name, self._item.Value)

	def __str__(self):
		return "{'name' : '%s', 'value' : '%s'}" % (self._item.Name, self._item.Value)

	@property
	def name(self):
		return self._item.Name

	@name.setter
	def name(self, value):
		self._item.Name = value

	@property
	def value(self):
		return self._item.Value

	@value.setter
	def value(self, value):
		self._item.Value = value

class HeaderDict(dict):
	def __init__(self, peach_req, *args, **kwargs):
		super(HeaderDict, self).__init__(self, *args, **kwargs)
		self._peach_req = peach_req

	def __repr__(self):
		return '{%s}' % ', '.join(['\'%s\' : [%s]' % (k, ', '.join(v)) for k,v in self.iteritems()])

	def __str__(self):
		return '{%s}' % ', '.join(['\'%s\' : [%s]' % (k, ', '.join(v)) for k,v in self.iteritems()])

	def __len__(self):
		return self._peach_req.RequestHeaders.Count + self._peach_req.NonUniqueRequestHeaders.Count

	def __getitem__(self, key):
		if self._peach_req.RequestHeaders.ContainsKey(key):
			val = self._peach_req.RequestHeaders[key];
			return [ Header(None, None, val) ]
		if self._peach_req.NonUniqueRequestHeaders.ContainsKey(key):
			vals = self._peach_req.NonUniqueRequestHeaders[key];
			return [ Header(None, None, val) for val in vals ]

		raise KeyError()

	def __setitem__(self, key, value):
		if isinstance(value, str):
			value = [ Header(key, value) ]

		if len(value) == 1:
			self._peach_req.NonUniqueRequestHeaders.Remove(key)
			self._peach_req.RequestHeaders.Remove(key)
			self._peach_req.RequestHeaders.Add(key, value[0]._item)
		else:
			values = List[HttpHeader]()
			for x in value:
				values.Add(x._item)
			self._peach_req.NonUniqueRequestHeaders.Remove(key)
			self._peach_req.RequestHeaders.Remove(key)
			self._peach_req.NonUniqueRequestHeaders.Add(key, values)

	def __delitem__(self, key):
		if self._peach_req.RequestHeaders.Remove(key):
			return
		if self._peach_req.NonUniqueRequestHeaders.Remove(key):
			return

		raise KeyError()

	def __iter__(self):
		for x in self._peach_req.RequestHeaders.Keys:
			yield x
		for x in self._peach_req.NonUniqueRequestHeaders.Keys:
			yield x

	def __contains__(self, key):
		return self._peach_req.RequestHeaders.ContainsKey(key) or self._peach_req.NonUniqueRequestHeaders.ContainsKey(key)

	def keys(self):
		return [ x for x in self.iterkeys() ]

	def values(self):
		return [ x for x in self.itervalues() ]

	def items(self):
		return [ x for x in self.iteritems() ]

	def has_key(self, key):
		if self._peach_req.RequestHeaders.ContainsKey(key):
			return True
		if self._peach_req.NonUniqueRequestHeaders.ContainsKey(key):
			return True

		return False

	def get(self, key, failobj=None):
		if self._peach_req.RequestHeaders.ContainsKey(key):
			val = self._peach_req.RequestHeaders[key];
			return [ Header(None, None, val) ]
		if self._peach_req.NonUniqueRequestHeaders.ContainsKey(key):
			vals = self._peach_req.NonUniqueRequestHeaders[key];
			return [ Header(None, None, val) for val in vals ]

		return failobj

	def clear(self):
		raise NotImplementedError

	def setdefault(self, key, failobj=None):
		raise NotImplementedError

	def iterkeys(self):
		for kv in self._peach_req.RequestHeaders:
			yield kv.Key
		for kv in self._peach_req.NonUniqueRequestHeaders:
			yield kv.Key

	def itervalues(self):
		for kv in self._peach_req.RequestHeaders:
			yield [ Header(None, None, kv.Value) ]
		for kv in self._peach_req.NonUniqueRequestHeaders:
			yield [ Header(None, None, v) for v in kv.Value ]

	def iteritems(self):
		for kv in self._peach_req.RequestHeaders:
			yield (kv.Key, [ Header(None, None, kv.Value) ])
		for kv in self._peach_req.NonUniqueRequestHeaders:
			yield (kv.Key, [ Header(None, None, v) for v in kv.Value ])

	def pop(self, key, failobj=None):
		raise NotImplementedError

	def popitem(self):
		raise NotImplementedError

	def copy(self):
		raise NotImplementedError

	def update(self):
		raise NotImplementedError

class Request(object):
	def __init__(self, peach_req):
		self._peach_req = peach_req

	@property
	def uri(self):
		return Uri(self._peach_req.RequestUri)

	@property
	def method(self):
		return self._peach_req.Method

	@method.setter
	def method(self, value):
		self._peach_req.Method = value

	@property
	def host(self):
		return self._peach_req.Host

	@host.setter
	def host(self, value):
		self._peach_req.Host = value

	@property
	def contentLength(self):
		return self._peach_req.ContentLength

	@contentLength.setter
	def contentLength(self, value):
		self._peach_req.ContentLength = value

	@property
	def contentEncoding(self):
		return self._peach_req.ContentEncoding

	@contentEncoding.setter
	def contentEncoding(self, value):
		self._peach_req.ContentEncoding = value

	@property
	def contentType(self):
		return self._peach_req.ContentType

	@contentType.setter
	def contentType(self, value):
		self._peach_req.ContentType = value

	@property
	def headers(self):
		return HeaderDict(self._peach_req)

def __set_current_route(route):
	global __current_route;
	__current_route = route

def __get_request_funcs():
	global __request_funcs;
	return '%r' % __request_funcs

def __on_request(route, context, req, body):
	global __request_funcs;
	funcs = __request_funcs.get(route, [])

	r = Request(req)

	if body:
		body = bytes(body)

	for func in funcs:
		func(context, r, body)


