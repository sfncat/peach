from waflib.TaskGen import before_method, after_method, feature, taskgen_method
from waflib import Errors, Task, Logs, Utils

wlock = Utils.threading.Lock()

def configure(conf):
	conf.find_program('mkbundle')

@taskgen_method
def use_bundle_rec(self, name, **kw):
	if name in self.tmp_use_bundle_not or name in self.tmp_use_bundle_seen:
		return

	try:
		y = self.bld.get_tgen_by_name(name)
		self.tmp_use_bundle_seen.append(name)
	except Errors.WafError:
		self.tmp_use_bundle_not.append(name)
		return

	y.post()

	self.bundle_use.append(y)

	for x in self.to_list(getattr(y, 'use', [])):
		self.use_bundle_rec(x)

@feature('cs')
@after_method('apply_cs')
def cs_bundle(self):
	if not getattr(self, 'mkbundle', False):
		return
	if 'mkbundle' not in self.env['supported_features']:
		return
	if self.gen.endswith('.dll'):
		return

	self.bundle_use = []
	self.tmp_use_bundle_not = []
	self.tmp_use_bundle_seen = []

	for x in self.to_list(getattr(self, 'use', [])):
		self.use_bundle_rec(x)

	exe = self.cs_task.outputs[0]
	srcs = [ exe ]

	# for y in self.bundle_use:
	# 	tsk = getattr(y, 'cs_task', None) or getattr(y, 'link_task', None)
	# 	srcs.append(tsk.outputs[0])

	mkbundle = self.create_task('mkbundle', srcs, exe.change_ext(''))
	mkbundle.env.env = {
		'AS'              : self.env['MKBUNDLE_AS'],
		'CC'              : self.env['MKBUNDLE_CC'],
		'PKG_CONFIG_PATH' : self.env['MKBUNDLE_PKG_CONFIG_PATH'],
	}

	inst_to = getattr(self, 'install_path', '${BINDIR}')

	self.install_files(inst_to, mkbundle.outputs, chmod=Utils.O755)

class mkbundle(Task.Task):
	run_str = '${MKBUNDLE} --static -z -o ${TGT} ${SRC} ${ASSEMBLIES}'

	def exec_command(self, cmd, **kw):
		# Can only run one at a time
		try:
			wlock.acquire()
			return super(mkbundle, self).exec_command(cmd, **kw)
		finally:
			wlock.release()
