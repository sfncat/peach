from waflib.TaskGen import feature, before_method, after_method, taskgen_method
from waflib import Task, Utils, Logs, Configure, Context, Options, Errors
import os, zipfile, sys, stat

def configure(conf):
	pass

@taskgen_method
def use_zip_rec(self, name, **kw):
	if name in self.tmp_use_zip_not or name in self.tmp_use_zip_seen:
		return

	try:
		y = self.bld.get_tgen_by_name(name)
		self.tmp_use_zip_seen.append(name)
	except Errors.WafError:
		self.tmp_use_zip_not.append(name)
		return

	y.post()

	self.zip_use.append(y)

	# MSI already has its dependencies, so don't recurse
	if 'msi' in y.features:
		return

	for x in self.to_list(getattr(y, 'use', [])):
		self.use_zip_rec(x)

@taskgen_method
def get_zip_src(self, tsk):
	zip_root = getattr(self, 'zip_root', '${BINDIR}')
	zip_rewrites = getattr(self, 'zip_rewrites', {})

	dest = tsk.dest.replace('${PKGDIR}', zip_root)
	destpath = Utils.subst_vars(dest, tsk.env).replace('/', os.sep)
	bindir = Utils.subst_vars(zip_root, tsk.env)
	destpath = os.path.relpath(destpath, bindir)

	if tsk.type == 'symlink_as':
		w = tsk.get_install_path()
		src = self.bld.root.make_node(w)
		self.zip_inputs.add((src, destpath, Utils.O755))
		return

	for src in self.to_nodes(tsk.install_from):
		if src.name.endswith('.pdb') or src.name.endswith('.mdb'):
			continue
		elif tsk.type == 'install_as':
			destfile = destpath
		elif tsk.relative_trick:
			destfile = os.path.join(destpath, src.path_from(tsk.relative_base))
			for k, v in zip_rewrites.items():
				if k in destfile:
					destfile = destfile.replace(k, v)
		else:
			destfile = os.path.join(destpath, src.name)

			for k, v in zip_rewrites.items():
				if k in destfile:
					destfile = destfile.replace(k, v)
		
		external_attr = tsk.chmod << 16L

		self.zip_inputs.add((src, destfile, external_attr))

@feature('zip')
@before_method('apply_zip_srcs')
def apply_zip_use(self):
	self.zip_use = []

	if not getattr(self.bld, 'is_pkg', False):
		return

	self.tmp_use_zip_not = []
	self.tmp_use_zip_seen = []

	for x in self.to_list(getattr(self, 'use', [])):
		self.use_zip_rec(x)

@feature('zip')
def apply_zip_srcs(self):
	self.zip_inputs = set()

	for y in self.zip_use:
		y.post()
		vnum = getattr(y, 'vnum_install_task', None)
		if vnum:
			for x in vnum:
				self.get_zip_src(x)
		else:
			for tsk in y.tasks:
				if tsk.__class__.__name__ == 'inst':
					self.get_zip_src(tsk)
		for tg in getattr(y, 'install_extras', []):
			tg.post()
			for tsk in tg.tasks:
				if tsk.__class__.__name__ == 'inst':
					self.get_zip_src(tsk)

	zip_extras = getattr(self, 'zip_extras', [])
	for y in zip_extras:
		self.zip_inputs.add(y)
	
	if self.zip_inputs:
		self.zip_inputs = sorted(self.zip_inputs, key=lambda x: x[1])
		srcs = [ x[0] for x in self.zip_inputs ]
		dest = self.path.find_or_declare(self.name + '.zip')
		self.zip_task = self.create_task('zip', srcs, dest)
		self.sha_task = self.create_task('sha', self.zip_task.outputs, dest.change_ext('.zip.sha1'))

		inst_to = getattr(self, 'install_path', '${PKGDIR}')
		self.install_files(inst_to, self.zip_task.outputs + self.sha_task.outputs)

class zip(Task.Task):
	color = 'PINK'

	def run(self):
		output = self.outputs[0]
		basename = os.path.splitext(output.name)[0]

		try:
			os.unlink(output.abspath())
		except Exception:
			pass

		zip = zipfile.ZipFile(output.abspath(), 'w', compression=zipfile.ZIP_DEFLATED)

                items = {}

		for src, dest, attr in self.generator.zip_inputs:
			dest = os.path.normpath(dest).replace('\\', '/')
			src_abspath = src.abspath()

                        prev = items.get(dest, None)
                        if prev:
                            raise Errors.WafError("Target '%s' has duplicate entries for '%s': %s and %s" % (output, dest, prev, src_abspath))

                        items[dest] = src_abspath

			if os.path.islink(src_abspath):
				zi = zipfile.ZipInfo(dest)
				zi.create_system = 3
				zi.external_attr = 2716663808L
				# '0xA1ED0000L' is symlink attr magic
				zip.writestr(zi, os.readlink(src_abspath))
			else:
				zip.write(src_abspath, dest)
				if attr is not None:
					zi = zip.getinfo(dest)
					zi.external_attr = attr

		zip.close()

class sha(Task.Task):
	color = 'PINK'

	def run(self):
		try:
			from hashlib import sha1 as sha
		except ImportError:
			from sha import sha

		src = self.inputs[0]
		dst = self.outputs[0]

		digest = sha(src.read()).hexdigest()

		dst.write('SHA1(%s)= %s\n' % (src.name, digest))

@feature('simple_zip')
def process_simple_zip(self):
	self.zip_inputs = set()

	for zip_spec in getattr(self, 'zip_spec', []):
		relative = zip_spec['relative']
		prefix = zip_spec['prefix']
		for src in zip_spec['src']:
			attr = stat.S_IMODE(os.stat(src.abspath()).st_mode)
			dest = os.path.join(prefix, src.path_from(relative))
			self.zip_inputs.add((src, dest, None))

	self.zip_inputs = sorted(self.zip_inputs, key=lambda x: x[1])
	srcs = [ x[0] for x in self.zip_inputs ]
	dest = self.path.find_or_declare(self.name + '.zip')

	self.zip_task = self.create_task('zip', srcs, dest)

	inst_to = getattr(self, 'install_path', '${PKGDIR}')
	self.install_files(inst_to, self.zip_task.outputs)
