import os.path
from waflib import Utils
from waflib.TaskGen import feature

host_plat = [ 'linux' ]

archs = [ 'x86', 'x86_64' ]

tools = [
	'gcc',
	'gxx',
	'cs',
	'resx',
	'tools.utils',
	'tools.externals',
	'tools.tsc',
	'tools.version',
	'tools.xcompile',
	'tools.paket',
]

optional_tools = [
	'tools.asan',
	'tools.mkbundle',
	'tools.test',
	'tools.zip',
]


def prepare(conf):
	env = conf.env
	j = os.path.join

	env['MCS']  = 'mcs'
	env['CC']   = 'gcc'
	env['CXX']  = 'g++'

	if env.SUBARCH == 'x86':
		env['ARCH'] = [ '32' ]
		pin_tgt = 'ia32'
		pin_def = 'IA32'
	else:
		env['ARCH'] = [ '64' ]
		pin_tgt = 'intel64'
		pin_def = 'IA32E'

	env['ARCH_ST'] = '-m%s'

	env['PIN_VER'] = 'pin-3.19-98425-gcc-linux'

	pin = j(conf.get_third_party(), 'pin', env['PIN_VER'])

	env['EXTERNALS'] = {
		'pin' : {
			'INCLUDES'  : [
				j(pin, 'source', 'include', 'pin'),
				j(pin, 'source', 'include', 'pin', 'gen'),
				j(pin, 'extras', 'components', 'include'),
				j(pin, 'extras', 'xed-%s' % pin_tgt, 'include', 'xed'),
			],
			'HEADERS'   : [],
			'STLIBPATH' : [
				j(pin, pin_tgt, 'runtime', 'pincrt'),
				j(pin, pin_tgt, 'lib'),
				j(pin, pin_tgt, 'lib-ext'),
				j(pin, 'extras', 'xed-%s' % pin_tgt, 'lib'),
			],
			'LIBPATH'   : [],
			'LIB'       : [],
			'STLIB'     : [],
			'DEFINES'   : [ 'BIGARRAY_MULTIPLIER=1', 'TARGET_LINUX', 'TARGET_%s' % pin_def, 'HOST_%s' % pin_def, 'PIN_CRT=1', '__PIN__=1' ],
			'CFLAGS'    : [],
			'CXXFLAGS'  : [
				'-isystem', j(pin, 'extras', 'stlport', 'include'),
				'-isystem', j(pin, 'extras', 'libstdc++', 'include'),
				'-isystem', j(pin, 'extras', 'crt', 'include'),
				'-isystem', j(pin, 'extras', 'crt', 'include', 'arch-%s' % env.SUBARCH),
				'-isystem', j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi'),
				'-isystem', j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi', 'asm-x86'),
				'-fno-stack-protector',
				'-fomit-frame-pointer',
				'-fno-strict-aliasing',
				'-fno-exceptions',
				'-funwind-tables',
				'-fasynchronous-unwind-tables',
				'-fno-rtti',
				'-fabi-version=2',
			],
			'LINKFLAGS' : [
				'-Wl,--hash-style=sysv',
				j(pin, pin_tgt, 'runtime', 'pincrt', 'crtbeginS.o'),
				'-Wl,-Bsymbolic',
				'-Wl,--version-script=%s' % j(pin, 'source', 'include', 'pin', 'pintool.ver'),
				'-fabi-version=2',
				'-lpin',
				'-lxed',
				j(pin, pin_tgt, 'runtime', 'pincrt', 'crtendS.o'),
				'-lpin3dwarf',
				'-ldl-dynamic',
				'-nostdlib',
				'-lstlport-dynamic',
				'-lm-dynamic',
				'-lc-dynamic',
				'-lunwind-dynamic',
			],
			'ENV'       : { 'STLIB_MARKER' : '-Wl,-Bsymbolic', 'SHLIB_MARKER' : '-Wl,-Bsymbolic', 'cxxshlib_PATTERN' : '%s.so' },
		},
	}

	env['TARGET_FRAMEWORK'] = 'v4.5'
	env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5'

	env['ASAN_CC'] = 'gcc'
	env['ASAN_CXX'] = 'g++'

	env['RUN_NETFX'] = 'mono'
	env['PEACH_PLATFORM_DLL'] = 'Peach.Pro.OS.Linux.dll'

	env.append_value('supported_features', [
		'peach',
		'linux',
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
		'network',
		'unix',
		'flexnetls',
		'install_task',
		'mono', # so we bundle sqlite
	])

def configure(conf):
	env = conf.env

	env.append_value('CSFLAGS', [
		'/sdk:4.5',
		'/warn:4',
		'/define:PEACH,UNIX,MONO',
		'/warnaserror',
		'/nowarn:1591', # Missing XML comment for publicly visible type
	])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG;TRACE;MONO',
	])

	env.append_value('CSFLAGS_release', [
		'/define:TRACE;MONO',
		'/optimize+',
	])

	env['CSPLATFORM'] = 'AnyCPU'
	env['CSDOC'] = True

	env.append_value('DEFINES_debug', [
		'DEBUG',
	])

	cppflags = [
		'-pipe',
		'-Werror',
		'-Wno-unused',
	]

	cppflags_debug = [
		'-ggdb',
	]

	cppflags_release = [
		'-O3',
	]

	asan = [
		'-fsanitize=address'
	]

	env.append_value('CFLAGS_asan', asan)
	env.append_value('CXXFLAGS_asan', asan)
	env.append_value('LINKFLAGS_asan', asan)

	env.append_value('CPPFLAGS', cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)
	
	env.append_value('LIB', [ 'dl' ])
	env.append_value('LIB_network', [ 'pthread' ])

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
