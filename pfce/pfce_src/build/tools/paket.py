import os
from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, taskgen_method
from waflib import Utils, Logs, Task, Context, Errors, Options

def configure(conf):
	dotnet = []
	if Utils.unversioned_sys_platform() != 'win32':
		conf.find_program('mono')
		dotnet = conf.env['MONO']

	bootstrapper = os.path.abspath(os.path.join(
		'paket',
		'.paket', 
		'paket.bootstrapper.exe'
	))

	paket = os.path.abspath(os.path.join(
		'paket',
		'.paket', 
		'paket.exe'
	))

	if not Options.options.nopaket:
		conf.cmd_and_log(dotnet + [bootstrapper], cwd='paket')
		conf.cmd_and_log(dotnet + [paket, 'restore'], cwd='paket')

		# Replace user-overridden files after paket restore
		overrides = conf.path.find_dir('paket/overrides').ant_glob('*')
		packages = conf.path.find_dir('paket/packages').ant_glob('**/*')

		for p in packages:
			for o in overrides:
				if p.name == o.name:
					if Logs.verbose > 0:
						Logs.info('copy %s %s' % (o.abspath(), p.abspath()))
					p.write(o.read())


	conf.env.append_value('supported_features', 'paket')

def options(ctx):
	ctx.add_option('--nopaket', action='store_true', default=False, help='Don\'t restore paket packages')

class Package(object):
	def __init__(self, bld, group, name, node):
		self.group = group
		self.name = name
		self.deps = []
		self.byfx = {}

		self.create_tgs(bld, node.find_dir('lib'), False)
		self.create_tgs(bld, node.find_dir('ref'), True)

	def create_tgs(self, bld, node, is_ref):
		if node is None:
			return

		root = node.abspath()
		for fx in os.listdir(root):
			fx_path = os.path.join(root, fx)
			if os.path.isdir(fx_path):
				if fx in self.byfx:
					continue
				fx_node = node.make_node(fx)
				for file in os.listdir(fx_path):
					ext = os.path.splitext(file)[1]
					if ext == '.dll':
						self.make_nuget_lib(bld, fx, fx_node.make_node(file), is_ref)
			elif not is_ref:
				ext = os.path.splitext(fx)[1]
				if ext == '.dll':
					self.make_nuget_lib(bld, 'lib', node.make_node(fx), is_ref)

	def make_nuget_lib(self, bld, fx, node, is_ref):
		# print 'make: %s, %s, %s' % (fx, node, is_ref)
		# nuget_lib will make a fake_csshlib which will
		# cause install_outputs() to be called and it will
		# put the .dll, .pdb and .config into the ${BINDIR}
		tg = bld(
			name='%s:%s:%s:%s' % (self.group, self.name, fx, node.name),
			install_path = '${BINDIR}',
			features='nuget_lib',
			node=node,
			is_ref=is_ref
		)
		lst = self.byfx.setdefault(fx, [])
		lst.append(tg)

	def __repr__(self):
		return '%s/%s: %s' % (self.group, self.name, self.byfx)

@conf
def read_paket(self, lockfile):
	if not self.env.MCS:
		return

	pkgs = {}
	group = ''
	groups = { group : pkgs }
	parent = None
	root = self.path.find_dir('packages')
	node = root

	src = self.path.find_resource(lockfile)
	if src:
		ignore = False
		contents = src.read()
		for line in contents.splitlines():
			if line.startswith('GROUP'):
				group = line[6:]
				pkgs = {}
				groups[group] = pkgs
				parent = None
				node = root.find_dir(group)
			elif line.startswith('NUGET'):
				ignore = False
			elif line.startswith('HTTP'):
				ignore = True
			elif not ignore:
				suffix = line.lstrip(' ')
				depth = (len(line) - len(suffix)) / 2
				if depth == 2:
					name = suffix.split()[0]
					parent = pkgs.get(name)
					if parent is None:
						parent = Package(self, group, name, node.find_dir(name))
						pkgs[name] = parent
				elif depth == 3:
					pkg = suffix.split()[0]
					parent.deps.append(pkg)

	self.env.PAKET_PACKAGES = groups

@feature('cs', 'paket')
@before_method('install_packages')
def use_nuget(self):
	pkgs = getattr(self, 'use_packages', None)
	if not pkgs:
		return

	settings = dict(
		excludes = [],
		frameworks = [],
		group = ''
	)

	new_settings = getattr(self, 'paket_settings')
	if new_settings:
		settings.update(new_settings)

	use = self.to_list(getattr(self, 'use', []))

	tgs = set()

	for pkg in pkgs:
		use_packages_recurse(self, pkg, settings, tgs)

	for tg in sorted(tgs, key=lambda x: x.target):
		# print 'use: %s' % tg.name
		use.append(tg.name)

def use_packages_recurse(self, pkg, settings, into):
	if pkg in settings['excludes']:
		return

	group = self.env.PAKET_PACKAGES.get(settings['group'])
	if group is None:
		self.bld.fatal('%r uses unknown paket group %r' % (self.name, settings['group']))

	dep = group.get(pkg)
	if dep is None:
		self.bld.fatal('%r depends on unknown package %r' % (self.name, pkg))

	tgs = get_pkg_for_fx(dep, settings['frameworks'])
	if tgs is None:
		self.bld.fatal('%r depends on unknown framework for package %r, available frameworks: %r' % (
			self.name, pkg, dep.byfx.keys()
		))

	for tg in tgs:
		if not tg.is_ref:
			into.add(tg)

	for child in dep.deps:
		use_packages_recurse(self, child, settings, into)

def get_pkg_for_fx(dep, fxs):
	for fx in fxs:
		tgs = dep.byfx.get(fx)
		if tgs:
			return tgs
	return None

@feature('nuget_lib')
def process_nuget_lib(self):
	# self.node.sig = Utils.h_file(self.node.abspath())

	self.link_task = self.create_task('fake_csshlib', [], [self.node])
	self.target = self.node.name
