import os, os.path, platform
from waflib import Utils, Errors
from waflib.TaskGen import feature

host_plat = [ 'win32' ]

archs = [ 'x86', 'x64' ]

tools = [
	'msvc',
	'cs',
	'resx',
	'midl',
	'tools.utils',
	'tools.externals',
	'tools.tsc',
	'tools.version',
	'tools.paket',
]

optional_tools = [
	'tools.msbuild',
	'tools.test',
	'tools.zip',
]

def prepare(conf):
	env = conf.env
	j = os.path.join

	env['MSVC_VERSIONS'] = ['msvc 16.8', 'msvc 15.0', 'msvc 14.0', 'msvc 12.0', 'msvc 11.0', 'msvc 10.0', 'wsdk 7.1' ]
	env['MSVC_TARGETS']  = 'x64' in env.SUBARCH and [ 'x64', 'x86_amd64' ] or [ 'x86' ]

	env['PIN_VER'] = 'pin-3.19-98425-msvc-windows'

	pin = j(conf.get_third_party(), 'pin', env['PIN_VER'])

	env['EXTERNALS_x86'] = {
		'pin' : {
			'CXXFLAGS'  : [
				'/MT', '/EHs-', '/EHa-', '/wd4530', '/Gy', '/Oi-',
				'/GR-', '/GS-', '/Zc:threadSafeInit-',
				'/I%s' % j(pin, 'source', 'include', 'pin'),
				'/I%s' % j(pin, 'source', 'include', 'pin', 'gen'),
				'/I%s' % j(pin, 'extras', 'stlport', 'include'),
				'/I%s' % j(pin, 'extras'),
				'/I%s' % j(pin, 'extras', 'libstdc++', 'include'),
				'/I%s' % j(pin, 'extras', 'crt', 'include'),
				'/I%s' % j(pin, 'extras', 'crt' ),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'arch-x86'),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi'),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi', 'asm-x86'),
				'/I%s' % j(pin, 'extras', 'components', 'include'),
				'/I%s' % j(pin, 'extras', 'xed-ia32', 'include', 'xed'),
				'/FIinclude/msvc_compat.h',
			],
			'STLIBPATH' : [
				j(pin, 'ia32', 'lib'),
				j(pin, 'ia32', 'lib-ext'),
				j(pin, 'ia32', 'runtime', 'pincrt'),
				j(pin, 'extras', 'xed-ia32', 'lib'),
			],
			'STLIB'     : [
				'pin', 'xed', 'pinvm', 'pincrt', 'm-static',
				'c-static', 'os-apis', 'ntdll-32'
			],
			'DEFINES'   : [
				'TARGET_WINDOWS', 'BIGARRAY_MULTIPLIER=1',
				'__PIN__=1', 'PIN_CRT=1', '_HAS_EXCEPTIONS=0',
				'__i386__', 'TARGET_IA32', 'HOST_IA32',
			],
			'LINKFLAGS' : [
				'/EXPORT:main', '/ENTRY:Ptrace_DllMainCRTStartup@12', '/BASE:0x55000000',
				'/FORCE:MULTIPLE', '/INCREMENTAL:NO',
				'/IGNORE:4210', '/IGNORE:4217', '/IGNORE:4049',
				'/NODEFAULTLIB', '/DYNAMICBASE', '/NXCOMPAT',
				'crtbeginS.obj', 'kernel32.lib'
			],
		},
		'com' : {
			'DEFINES' : [ '_WINDLL' ],
			'STLIB' : [ 'Ole32', 'OleAut32', 'Advapi32' ],
		},
		'network' : {
			'HEADERS' : [ 'winsock2.h' ],
			'STLIB'   : [ 'ws2_32' ],
		},
	}

	env['EXTERNALS_x64'] = {
		'pin' : {
			'CXXFLAGS'  : [
				'/MT', '/EHs-', '/EHa-', '/wd4530', '/Gy', '/Oi-',
				'/GR-', '/GS-', '/Zc:threadSafeInit-',
				'/I%s' % j(pin, 'source', 'include', 'pin'),
				'/I%s' % j(pin, 'source', 'include', 'pin', 'gen'),
				'/I%s' % j(pin, 'extras', 'stlport', 'include'),
				'/I%s' % j(pin, 'extras'),
				'/I%s' % j(pin, 'extras', 'libstdc++', 'include'),
				'/I%s' % j(pin, 'extras', 'crt', 'include'),
				'/I%s' % j(pin, 'extras', 'crt' ),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'arch-x86_64'),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi'),
				'/I%s' % j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi', 'asm-x86'),
				'/I%s' % j(pin, 'extras', 'components', 'include'),
				'/I%s' % j(pin, 'extras', 'xed-intel64', 'include', 'xed'),
				'/FIinclude/msvc_compat.h',
			],
			'STLIBPATH' : [
				j(pin, 'intel64', 'lib'),
				j(pin, 'intel64', 'lib-ext'),
				j(pin, 'intel64', 'runtime', 'pincrt'),
				j(pin, 'extras', 'xed-intel64', 'lib'),
			],
			'STLIB'     : [
				'pin', 'xed', 'pinvm', 'pincrt', 'm-static',
				'c-static', 'os-apis', 'ntdll-64'
			],
			'DEFINES'   : [
				'TARGET_WINDOWS', 'BIGARRAY_MULTIPLIER=1',
				'__PIN__=1', 'PIN_CRT=1', '_HAS_EXCEPTIONS=0',
				'__LP64__', 'TARGET_IA32E', 'HOST_IA32E',
			],
			'LINKFLAGS' : [
				'/EXPORT:main', '/ENTRY:Ptrace_DllMainCRTStartup', '/BASE:0xC5000000',
				'/FORCE:MULTIPLE', '/INCREMENTAL:NO',
				'/IGNORE:4210', '/IGNORE:4217', '/IGNORE:4049',
				'/NODEFAULTLIB', '/DYNAMICBASE', '/NXCOMPAT',
				'crtbeginS.obj', 'kernel32.lib'
			],
		},
		'com' : {
			'DEFINES' : [ '_WINDLL' ],
			'STLIB' : [ 'Ole32', 'OleAut32', 'Advapi32' ],
		},
		'network' : {
			'HEADERS' : [ 'winsock2.h' ],
			'STLIB'   : [ 'ws2_32' ],
		},
	}

	env['EXTERNALS'] = env['EXTERNALS_%s' % env.SUBARCH]

	# This is lame, the resgen that vcvars for x64 finds is the .net framework 3.5 version.
	# The .net 4 version is in the x86 search path.
	if env.SUBARCH == 'x64':
		env['MCS'] = getattr(conf.all_envs.get('win_x86'), 'MCS', [ None ])[0]
		env['RESGEN'] = getattr(conf.all_envs.get('win_x86'), 'RESGEN', [ None ])[0]

	pfiles = os.getenv('PROGRAMFILES(X86)', os.getenv('PROGRAMFILES'))
	env['TARGET_FRAMEWORK'] = 'v4.5.1'
	env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5.1'
	env['REFERENCE_ASSEMBLIES'] = j(pfiles, 'Reference Assemblies', 'Microsoft', 'Framework', '.NETFramework', env['TARGET_FRAMEWORK'])

	env['RUN_NETFX'] = ''
	env['PEACH_PLATFORM_DLL'] = 'Peach.Pro.OS.Windows.dll'

def configure(conf):
	env = conf.env

	# Ensure reference assembly folder exists
	if not os.path.isdir(env.REFERENCE_ASSEMBLIES):
		raise Errors.WafError("Could locate .NET Framework %s reference assemblies in: %s" % (env.TARGET_FRAMEWORK, env.REFERENCE_ASSEMBLIES))

	# Make sure all ASSEMBLY entries are fully pathed
	env.ASS_ST = '/reference:%s%s%%s' % (env.REFERENCE_ASSEMBLIES, os.sep)

	env.append_value('supported_features', [
		'peach',
		'win',
		'c',
		'cstlib',
		'cshlib',
		'cprogram',
		'cxx',
		'cxxstlib',
		'cxxshlib',
		'cxxprogram',
		'fake_lib',
		'nuget_lib',
		'cs',
		'debug',
		'release',
		'emit',
		'vnum',
		'subst',
		'msbuild',
		'flexnetls',
		'install_task',
	])

	cppflags = [
		'/Z7',
		'/W4',
		'/WX',
	]

	cppflags_debug = [
		'/MTd',
		'/Od',
	]

	cppflags_release = [
		'/MT',
		'/Ox',
	]

	for x in env['INCLUDES']:
		win_h = os.path.join(x, 'Windows.h')
		if os.path.exists(win_h):
			# Pin needs this define, but its not known during prepare()
			env.append_value('DEFINES', ['_WINDOWS_H_PATH_=%s' % x] )
			break

	env.append_value('CPPFLAGS', cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)

	env.append_value('CXXFLAGS_com', [ '/EHsc' ])

	env.append_value('DEFINES', [
		'WIN32',
		'_CRT_SECURE_NO_WARNINGS',
	])

	env.append_value('DEFINES_debug', [
		'DEBUG',
		'_DEBUG',
	])

	env.append_value('CSFLAGS', [
		'/noconfig',
		'/nologo',
		'/nostdlib+',
		'/warn:4',
		'/define:PEACH',
		'/errorreport:prompt',
		'/warnaserror',
		'/nowarn:1591', # Missing XML comment for publicly visible type
	])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG;TRACE',
	])

	env.append_value('CSFLAGS_release', [
		'/define:TRACE',
		'/optimize+',
	])

	env.append_value('ASSEMBLIES', [
		'mscorlib.dll',
	])

	env.append_value('LINKFLAGS', [
		'/NOLOGO',
		'/DEBUG',
		'/INCREMENTAL:NO',
		'/MACHINE:%s' % env.SUBARCH,
	])

	env['CSPLATFORM'] = env.SUBARCH
	env['CSDOC'] = True

	env.append_value('MIDLFLAGS', [
		'/%s' % ('x86' in env.SUBARCH and 'win32' or 'amd64'),
	])

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
