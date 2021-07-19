import os.path
from waflib import Utils
from waflib.TaskGen import feature

host_plat = [ 'darwin', 'linux', 'win32' ]

archs = []

tools = []

if Utils.unversioned_sys_platform() == 'win32':
	tools.append('msvc')

tools.extend([
	'cs',
	'misc',
	'tools.asciidoctor-pdf',
	'tools.doxygen',
	'tools.flexnet',
	'tools.paket',
	'tools.utils',
	'tools.version',
	'tools.webhelp',
	'tools.zip',
])

optional_tools = [
]

def prepare(conf):
	env = conf.env
	j = os.path.join

	if Utils.unversioned_sys_platform() == 'win32':
		pfiles = os.getenv('PROGRAMFILES(X86)', os.getenv('PROGRAMFILES'))
		env['TARGET_FRAMEWORK'] = 'v4.5.1'
		env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5.1'
		env['REFERENCE_ASSEMBLIES'] = j(pfiles, 'Reference Assemblies', 'Microsoft', 'Framework', '.NETFramework', env['TARGET_FRAMEWORK'])
		env['RUN_NETFX'] = ''
		env['MSVC_VERSIONS'] = ['msvc 14.0']
		env['MSVC_TARGETS']  = 'x64' in env.SUBARCH and [ 'x64' ] or [ 'x86' ]

	else:
		env['MCS']  = 'mcs'
		env['TARGET_FRAMEWORK'] = 'v4.5'
		env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5'
		env['RUN_NETFX'] = 'mono'

	env.append_value('supported_features', [
		'peach',
		'test',
		'fake_lib',
		'nuget_lib',
		'cs',
		'debug',
		'release',
		'vnum',
		'flexnetls',
		'doc',
		'emit',
		'subst',
		'webhelp',
		'simple_zip',
	])

def configure(conf):
	env = conf.env

	env.append_value('supported_features', [
		'peach-web',
	])

	if Utils.unversioned_sys_platform() == 'win32':
		if not os.path.isdir(env.REFERENCE_ASSEMBLIES):
			raise Errors.WafError("Could locate .NET Framework %s reference assemblies in: %s" % (env.TARGET_FRAMEWORK, env.REFERENCE_ASSEMBLIES))

		# Make sure all ASSEMBLY entries are fully pathed
		env.ASS_ST = '/reference:%s%s%%s' % (env.REFERENCE_ASSEMBLIES, os.sep)
			
		env.append_value('CSFLAGS', [
			'/noconfig',
			'/nologo',
			'/nostdlib+',
			'/warn:4',
			'/errorreport:prompt',
			'/warnaserror',
			'/nowarn:1591', # Missing XML comment for publicly visible type
		])

		env.append_value('ASSEMBLIES', [
			'mscorlib.dll',
		])
	else:
		env.append_value('CSFLAGS', [
			'/define:UNIX,MONO',
			'/sdk:4.5',
			'/warn:4',
			'/warnaserror',
			'/nowarn:1591', # Missing XML comment for publicly visible type
		])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG;TRACE',
	])

	CSFLAGS_release = [
		'/define:TRACE',
		'/optimize+',
	]

	env.append_value('CSFLAGS_vm', CSFLAGS_release + [ 
		'/define:VARIANT_VM'
	])

	env.append_value('CSFLAGS_ami', CSFLAGS_release + [
		'/define:VARIANT_AMI'
	])

	env['CSPLATFORM'] = 'AnyCPU'
	env['CSDOC'] = True

	env['VARIANTS'] = [ 'debug', 'vm', 'ami' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'

def vm(env):
	release(env)

def ami(env):
	release(env)
