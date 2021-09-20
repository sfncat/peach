from waflib import Utils, Errors
from waflib.TaskGen import feature
import os.path

host_plat = [ 'darwin' ]

archs = [ ]

tools = [
	'clang',
	'clang++',
	'cs',
	'resx',
	'tools.utils',
	'tools.externals',
	'tools.tsc',
	'tools.version',
	'tools.paket',
]

optional_tools = [
	'tools.asan',
	'tools.mkbundle',
	'tools.test',
	'tools.zip',
]

def find_directory(dirs, paths):
	for dirname in dirs:
		for path in paths:
			candidate = os.path.join(path, dirname)
			if os.path.exists(candidate):
				return candidate
	raise Errors.WafError('Could not find directory \'%s\'' % dirs)

def prepare(conf):
	env = conf.env
	j = os.path.join

	env['PATH'] = [
		'/Library/Frameworks/Mono.framework/Commands',
		'/usr/bin',
		'/Developer/usr/bin',
		'/usr/local/bin',
	]

	env['MCS']  = 'mcs'

	env['SYSROOT'] = find_directory([ 
		'MacOSX10.12.sdk', 
		'MacOSX10.11.sdk', 
		'MacOSX10.10.sdk', 
		'MacOSX10.9.sdk', 
		'MacOSX10.8.sdk', 
		'MacOSX10.7.sdk', 
		'MacOSX10.6.sdk' 
	], [
		'/Developer/SDKs',
		'/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs',
	])

	# TODO
	# Reported issues compiling pin using XCode 4.0 on 10.6
	# Figure out a better check for clang version on 10.6
	# For now, just skip all pin tools
	if '10.6' in env['SYSROOT']:
		return

	env['PIN_VER'] = 'pin-3.19-98425-clang-mac'

	pin = j(conf.get_third_party(), 'pin', env['PIN_VER'])

	env['EXTERNALS'] = {
		'pin' : {
			'INCLUDES'  : [],
			'HEADERS'   : [],
			'DEFINES'   : [ 'BIGARRAY_MULTIPLIER=1', 'TARGET_MAC', '__PIN__=1', 'PIN_CRT=1', '__DARWIN_ONLY_UNIX_CONFORMANCE=1', '__DARWIN_UNIX03=0' ],
			'CPPFLAGS'  : [
				'-fno-exceptions',
				'-funwind-tables',
				'-fno-rtti',

				'-I%s' % j(pin, 'source', 'include', 'pin'),
				'-I%s' % j(pin, 'source', 'include', 'pin', 'gen'),
				'-I%s' % j(pin, 'extras', 'components', 'include'),

				'-isystem', j(pin, 'extras', 'stlport', 'include'),
				'-isystem', j(pin, 'extras', 'libstdc++', 'include'),
				'-isystem', j(pin, 'extras', 'crt', 'include'),

				'-Xarch_i386',   '-isystem %s' % j(pin, 'extras', 'crt', 'include', 'arch-x86'),
				'-Xarch_x86_64', '-isystem %s' % j(pin, 'extras', 'crt', 'include', 'arch-x86_64'),

				'-isystem', j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi'),
				'-isystem', j(pin, 'extras', 'crt', 'include', 'kernel', 'uapi', 'asm-x86'),

				'-Xarch_i386',   '-DTARGET_IA32',
				'-Xarch_i386',   '-DHOST_IA32',
				'-Xarch_i386',   '-I%s' % j(pin, 'extras', 'xed-ia32', 'include', 'xed'),

				'-Xarch_x86_64', '-DTARGET_IA32E',
				'-Xarch_x86_64', '-DHOST_IA32E',
				'-Xarch_x86_64', '-I%s' % j(pin, 'extras', 'xed-intel64', 'include', 'xed'),
			],
			'LINKFLAGS' : [
				'-Xarch_i386',   j(pin, 'ia32', 'runtime', 'pincrt', 'crtbeginS.o'),
				'-Xarch_x86_64', j(pin, 'intel64', 'runtime', 'pincrt', 'crtbeginS.o'),

				'-w',
				'-Wl,-exported_symbols_list,%s/source/include/pin/pintool.exp' % pin,

				'-Xarch_i386',   '-L%s' % j(pin, 'ia32', 'runtime', 'pincrt'),
				'-Xarch_i386',   '-L%s' % j(pin, 'ia32', 'lib'),
				'-Xarch_i386',   '-L%s' % j(pin, 'ia32', 'lib-ext'),
				'-Xarch_i386',   '-L%s' % j(pin, 'extras', 'xed-ia32', 'lib'),

				'-Xarch_x86_64', '-L%s' % j(pin, 'intel64', 'runtime', 'pincrt'),
				'-Xarch_x86_64', '-L%s' % j(pin, 'intel64', 'lib'),
				'-Xarch_x86_64', '-L%s' % j(pin, 'intel64', 'lib-ext'),
				'-Xarch_x86_64', '-L%s' % j(pin, 'extras', 'xed-intel64', 'lib'),

				'-lpin',       '-lxed',             '-lpin3dwarf',
				'-nostdlib',   '-lstlport-dynamic', '-lm-dynamic',
				'-lc-dynamic', '-lunwind-dynamic',
			],
			'ENV'       : { 'cxxshlib_PATTERN' : '%s.dylib' },
		},
	}

	env['TARGET_FRAMEWORK'] = 'v4.5'
	env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5'

	env['ASAN_CC'] = 'clang-3.6'
	env['ASAN_CXX'] = 'clang++-3.6'

	env['MKBUNDLE_AS'] = 'as -arch i386'
	env['MKBUNDLE_CC'] = 'cc -arch i386 -framework CoreFoundation -lobjc -liconv'
	env['MKBUNDLE_PKG_CONFIG_PATH'] = '/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig'

	env['RUN_NETFX'] = 'mono'
	env['PEACH_PLATFORM_DLL'] = 'Peach.Pro.OS.OSX.dll'

def configure(conf):
	env = conf.env

	env.append_value('supported_features', [
		'peach',
		'osx',
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
		'install_task',
		'mono', # so we bundle sqlite
	])

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

	arch_flags = [
		'-mmacosx-version-min=10.6',
		'-isysroot',
		env.SYSROOT,
		'-arch',
		'i386',
		'-arch',
		'x86_64',
	]

	cppflags = [
		'-pipe',
		'-Werror',
		'-Wno-unused',
	]

	cppflags_debug = [
		'-g',
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

	env.append_value('CPPFLAGS', arch_flags + cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)

	env.append_value('LINKFLAGS', arch_flags)

	env.append_value('DEFINES_debug', ['DEBUG'])

	# Override g++ darwin defaults in tools/gxx.py
	env['CXXFLAGS_cxxshlib'] = []

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
