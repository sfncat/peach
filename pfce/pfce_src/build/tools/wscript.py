#!/usr/bin/env python

import os, re, glob, shutil
from optparse import OptionValueError
from waflib.TaskGen import feature, after_method, before_method
from waflib.Build import InstallContext
from waflib.Configure import conf
from waflib import Utils, Logs, Configure, Context, Options, Errors
from tools import pkg, hooks, nuget, test, gump, paket

"""
Variables:

BASENAME = 'win_x64'
TARGET = 'win'
SUBARCH = 'x64'
VARIANT = 'release'
PREFIX = 'output\\win_x64_release'
BINDIR = 'output\\win_x64_release\\bin'
LIBDIR = 'output\\win_x64_release\\bin'
DOCDIR = 'output\\win_x64_release\\doc'
PKGDIR = 'output\\win_x64_release\\pkg'
"""

cfg_files = glob.glob(os.path.join(os.path.dirname(__file__), '..', 'config', '*.py'))
cfg_names = [ os.path.basename(f)[:-3] for f in cfg_files if not f.endswith('__init__.py')]

@conf
def get_third_party(self):
	subdir = getattr(Context.g_module, 'third_party', '3rdParty')
	return self.path.find_dir(subdir).abspath()

def store_version(option, opt, value, parser):
	if not re.match('^\d+\.\d+\.\d+$', value):
		raise OptionValueError('%s option is not valid - must be <int>.<int>.<int>' % opt)
	branch = getattr(Context.g_module, 'branch', '0')
	setattr(parser.values, option.dest, value)
	setattr(parser.values, 'ver_branch', str(branch))

def options(opt):
	opt.load('tools.paket')
	opt.load('tools.idegen')
	opt.load('tools.test')

	opt.add_option('--strict',
	               action = 'store_true',
	               default = False,
	               help = 'Error if any features are missing')
	opt.add_option('--variant',
	               action = 'store',
	               default = None,
	               help = 'Specifies the variant to build against')
	opt.add_option('--buildtag',
	               action = 'callback',
	               callback = store_version,
	               type = 'string',
	               default = '0.0.0',
	               help = 'Specifies the buildtag to embed in the binaries')


def init(ctx):
	if Logs.verbose == 0:
		def null_msg(self, *k, **kw):
			pass
		setattr(Configure.ConfigurationContext, 'msg', null_msg)


def configure(ctx):
	if Logs.verbose == 0:
		def null_fatal(self, msg, ex=None):
			raise self.errors.ConfigurationError(msg, ex)
		setattr(Configure.ConfigurationContext, 'fatal', null_fatal)

	out = getattr(Context.g_module, 'out')
	inst = getattr(Context.g_module, 'inst')
	appname = getattr(Context.g_module, 'appname')
	supported_variant = getattr(Context.g_module, 'supported_variant')

	base_env = ctx.env
	base_env.APPNAME = appname
	base_env.OUTPUT = base_env.PREFIX = base_env.BINDIR = base_env.LIBDIR = base_env.DOCDIR = base_env.PKGDIR = inst
	base_env.BUILDTAG = Options.options.buildtag
	base_env.VER_BRANCH = getattr(Context.g_module, 'branch')

	tool_dir =  [
		os.path.join(Context.waf_dir, 'waflib', 'Tools'),
		os.path.join(Context.waf_dir, 'waflib', 'extras'),
	]

	platform = Utils.unversioned_sys_platform()

	for tgt in cfg_names:
		try:
			config = Context.load_tool('config.%s' % tgt)
			ctx.msg("Loading '%s' config" % tgt, config.__file__)
		except:
			ctx.msg("Loading '%s' config" % tgt, 'not found', color='YELLOW')
			continue

		if not supported_variant(tgt):
			if Logs.verbose > 0:
				Logs.warn("Skipping '%s' for this build configuration." % tgt)
			continue

		platforms = getattr(config, 'host_plat', [])
		archs = getattr(config, 'archs', None)
		options = [ ('%s_%s' % (tgt, arch), arch) for arch in archs ] or [ (tgt, None) ]

		for (name, arch) in options:
			if Logs.verbose == 0:
				Logs.pprint('NORMAL', 'Configuring variant %s :' % name.ljust(20), sep='')

			try:
				if platform not in platforms:
					raise Exception('Unsupported build host')

				ctx.setenv(name, env=base_env)
				arch_env = ctx.get_env()
				arch_env.BASENAME = name;
				arch_env.TARGET = tgt;
				arch_env.SUBARCH = arch;
				arch_env.PREFIX = os.path.join(base_env.PREFIX, name)
				arch_env.BINDIR = os.path.join(base_env.BINDIR, name)
				arch_env.LIBDIR = os.path.join(base_env.LIBDIR, name)
				arch_env.PKGDIR = os.path.join(base_env.PKGDIR, name)
				arch_env.DOCDIR = os.path.join(base_env.DOCDIR, name)
				config.prepare(ctx)

				for tool in getattr(config, 'tools', []):
					ctx.load(tool, tool_dir)

				for tool in getattr(config, 'optional_tools', []):
					feature = tool[tool.rfind('.')+1:]
					try:
						ctx.load(tool, tool_dir)
						arch_env.append_value('supported_features', feature)
					except Exception, e:
						arch_env.append_value('missing_features', feature)
						if Logs.verbose > 0:
							Logs.warn("Feature '%s' is not available: %s" % (feature, e))

				config.configure(ctx)

				missing = ctx.env['missing_features'] or ''
				if missing and Options.options.strict:
					raise Exception('Missing Features: %s' % ','.join(missing))

				arch_env.append_value('supported_features', name)

				cfgs = ctx.env.VARIANTS

				if not cfgs:
					base_env.append_value('variants', name)

				for cfg in cfgs:
					variant = '%s_%s' % (name, cfg)
					ctx.setenv(variant, env=arch_env)
					cfg_env = ctx.get_env()
					cfg_env.PREFIX = os.path.join(base_env.BINDIR, variant)
					cfg_env.BINDIR = os.path.join(base_env.BINDIR, variant, 'bin')
					cfg_env.LIBDIR = os.path.join(base_env.LIBDIR, variant, 'bin')
					cfg_env.PKGDIR = os.path.join(base_env.BINDIR, variant, 'pkg')
					cfg_env.DOCDIR = os.path.join(base_env.DOCDIR, variant, 'doc')
					cfg_env.VARIANT = cfg
					cfg_func = getattr(config, cfg)
					cfg_func(cfg_env)
					base_env.append_value('variants', variant)

				if Logs.verbose == 0:
					if missing:
						missing = ' - Missing Features: %s' % ','.join(missing)
					Logs.pprint('GREEN', 'Available%s' % missing)

			except Exception, e:
				if Logs.verbose == 0:
					Logs.pprint('YELLOW', 'Not Available - %s' % e)
				else:
					if str(e).startswith('msvc:'):
						Logs.warn('Could not find %s: %s' % (ctx.env['MSVC_VERSIONS'][0], ctx.env['MSVC_TARGETS'][0]))
						Logs.warn('Available compilers:')
						for msvc_ver,msvc_tgts in ctx.env['MSVC_INSTALLED_VERSIONS']:
							msvs_tgt = ' '.join([ k for k,v in msvc_tgts])
							Logs.warn("\t%s: %s" % (msvc_ver, msvs_tgt))
					if Logs.verbose > 1:
						import traceback
						traceback.print_exc()
					Logs.warn('%s is not available: %s' % (name, e))

	if not base_env.variants and Logs.verbose == 0:
		Logs.warn('No available variants detected. Re-run configure with the \'-v\' option for more info.')

def verify_external(bld):
	if bld.env.MCS and Logs.verbose > 0:
		externals = []
		for g in bld.groups:
			for tg in g:
				tsk = getattr(tg, 'link_task', None)
				if tsk and tsk.__class__.__name__ == 'fake_csshlib':
					externals.extend(tsk.outputs)
		for g in bld.groups:
			for tg in g:
				tsk = getattr(tg, 'cs_task', None)
				if tsk:
					externals = [ x for x in externals if x not in tsk.dep_nodes ]
		# for e in externals:
		# 	Logs.warn('Unreferenced Assembly: %s' % e)

def run_makexsd(bld):
	if bld.is_install and bld.variant != 'doc' and bld.env.BASENAME != 'web':
		makexsd = '%s PitTool.exe makexsd' % bld.env.RUN_NETFX
		env = os.environ.copy()
		env['TERM'] = 'xterm'
		ret = bld.exec_command(makexsd, cwd=bld.env.BINDIR, env=env)
		if ret:
			raise Errors.WafError('makexsd returned %s' % str(ret))

		if hasattr(bld, 'is_pkg'):
			shutil.copy(
				os.path.join(bld.env.BINDIR, 'peach.xsd'),
				os.path.join(bld.env.PKGDIR, 'peach.xsd')
			)

def build(bld):
	subdirs = getattr(bld, 'subdirs', None)

	if not Options.options.destdir:
		Options.options.destdir = bld.path.abspath()

	if subdirs:
		bld.recurse(subdirs)
		return

	# Find the topmost directories that contain wscript_build, up to maxpath in depth
	ignore = getattr(Context.g_module, 'ignore', [])
	maxdepth = getattr(Context.g_module, 'maxdepth', 1)
	dirs = [ x.parent for x in bld.path.ant_glob('**/wscript_build', maxdepth=maxdepth ) ]
	subdirs = [ x.path_from(bld.launch_node()) for x in dirs if x.parent not in dirs ]

	# Ignore blacklisted subdirectories
	for x in ignore:
		x = os.path.normpath(x)
		if Logs.verbose > 0:
			Logs.warn("Skipping directory '%s'" % x)
		subdirs.remove(x)

	what = Options.options.variant or ''
	variants = what.split(',')

	success = False
	for opt in variants:
		for variant in bld.env.variants:
			if opt not in variant:
				continue

			ctx = Context.create_context(bld.cmd)
			ctx.cmd = bld.cmd
			ctx.fun = 'build'
			ctx.subdirs = subdirs
			ctx.options = Options.options
			ctx.variant = variant
			ctx.add_post_fun(verify_external)
			ctx.add_post_fun(run_makexsd)
			ctx.execute()
			success = True

	if not success:
		raise Errors.WafError('"%s" is not a supported variant' % what)

	# Suppress missing target warnings
	bld.targets = '*'
