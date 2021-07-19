from waflib.Build import InstallContext
from waflib import Task, Utils, Logs, Configure, Context, Options, Errors
import os, zipfile, sys

class PkgContext(InstallContext):
	'''create product installers'''

	cmd = 'pkg'

	def __init__(self, **kw):
		super(PkgContext, self).__init__(**kw)
		self.is_pkg = True

class ZipContext(PkgContext):
	'''zip contents of output directory'''

	cmd = 'zip'

	def __init__(self, **kw):
		super(ZipContext, self).__init__(**kw)

	def execute(self):
		super(ZipContext, self).execute()

		files = []

		for g in self.groups:
			for tg in g:
				tsk = getattr(tg, 'install_task', None)
				if tsk:
					files.extend(tsk.outputs)

		if files:
			self.archive(files)

	def archive(self, files):
		env = self.env

		base_path = self.path.make_node(env.PREFIX)

		arch = self.path.make_node(env.PREFIX + '.zip')

		Logs.warn('Creating archive: %s' % arch)

		try:
			arch.delete()
		except Exception:
			pass

		zip = zipfile.ZipFile(arch.abspath(), 'w', compression=zipfile.ZIP_DEFLATED)

		for n in files:
			if not n.is_child_of(base_path):
				continue
			archive_name = n.path_from(base_path)
			if Logs.verbose > 0:
				Logs.info(' + add %s (from %s)' % (archive_name, n))
			else:
				sys.stdout.write('.')
				sys.stdout.flush()
			zip.write(n.abspath(), archive_name, zipfile.ZIP_DEFLATED)

		zip.close()


		if Logs.verbose == 0:
			sys.stdout.write('\n')

		try:
			from hashlib import sha1 as sha
		except ImportError:
			from sha import sha

		digest = sha(arch.read()).hexdigest()
		dgst = arch.change_ext('.zip.sha1')
		try:
			dgst.delete()
		except Exception:
			pass
		dgst.write('SHA1(%s)= %s\n' % (arch, digest))

		Logs.warn('New archive created: %s (sha1=%s)' % (arch, digest))

class PkgTask(Task.Task):
	def runnable_status(self):
		if getattr(self.generator.bld, 'is_pkg', None):
			return super(PkgTask, self).runnable_status()
		else:
			return Task.SKIP_ME
