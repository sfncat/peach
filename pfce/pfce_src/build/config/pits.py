from waflib import Utils, Errors
from waflib.TaskGen import feature
import os.path

host_plat = [ 'win32', 'linux', 'darwin' ]

archs = [ ]

tools = [
	'misc',
	'tools.utils',
	'tools.pit',
	'tools.zip',
]

optional_tools = [
	'tools.asciidoctor-pdf',
]

def prepare(conf):
	pass

def configure(conf):
	env = conf.env

	env.append_value('supported_features', [
		'emit',
		'subst',
		'pit',
	])
