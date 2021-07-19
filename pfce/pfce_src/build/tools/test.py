import os, os.path, sys
from waflib.Build import InstallContext
from waflib.TaskGen import feature, after_method
from waflib import Utils, Task, Logs, Options, Errors

testlock = Utils.threading.Lock()

def prepare_nunit_test(self):
	bindir = Utils.subst_vars('${BINDIR}', self.env)
	self.ut_nunit = self.generator.bld.path.make_node([bindir, 'nunit3-console.exe'])
	self.ut_cwd = self.ut_nunit.parent.abspath()
	self.ut_exec = []

	if self.env.CS_NAME == 'mono':
		self.ut_exec = [ 'mono', '--debug' ]

	self.ut_exec.extend([
		self.ut_nunit.abspath(),
		'--labels=All',
		'--noresult',
	])

	opts = self.generator.bld.options
	if opts.testcase:
		self.ut_exec.append('--test=%s' % opts.testcase)
	if opts.where:
		self.ut_exec.append('--where=%s' % opts.where)

	self.ut_exec.extend([ x.name for x in self.inputs ])

def get_inst_node(self, dest, name):
	dest = Utils.subst_vars(dest, self.env)
	dest = dest.replace('/', os.sep)
	if Options.options.destdir:
		dest = os.path.join(Options.options.destdir, os.path.splitdrive(dest)[1].lstrip(os.sep))

	return self.bld.root.make_node([dest, name])

def run_after_last_test(self, test):
	last_test = getattr(self.bld, 'last_utest_task', None)
	if last_test:
		test.set_run_after(last_test)
	setattr(self.bld, 'last_utest_task', test)

@feature('test')
@after_method('apply_link')
def make_test(self):
	inputs = []
	outputs = []
	test = None

	if getattr(self, 'link_task', None):
		dest = getattr(self, 'install_path', self.link_task.__class__.inst_to)
		inputs = [ get_inst_node(self, dest, self.link_task.outputs[0].name) ]

	if getattr(self, 'cs_task', None):
		bintype = getattr(self, 'bintype', self.gen.endswith('.dll') and 'library' or 'exe')
		dest = getattr(self, 'install_path', bintype == 'exe' and '${BINDIR}' or '${LIBDIR}')
		inputs = [ get_inst_node(self, dest, self.cs_task.outputs[0].name) ]

		test = getattr(self.bld, 'nunit_task', None)
		if not test:
			tg = self.bld(name = '')
			test = tg.create_task('utest', inputs, [])
			run_after_last_test(self, test)
			setattr(self.bld, 'nunit_task', test)
			tg.ut_fun = prepare_nunit_test
		else:
			test.inputs.extend(inputs)

	if not inputs:
		raise Errors.WafError('No test to run at: %r' % self)

	if not test:
		test = self.create_task('utest', inputs, outputs)
		run_after_last_test(self, test)

class utest(Task.Task):
	vars = []

	after = ['vnum', 'inst']

	def runnable_status(self):
		ret = Task.SKIP_ME

		if getattr(self.generator.bld, 'is_test', None):
			ret = super(utest, self).runnable_status()
			if ret == Task.SKIP_ME:
				ret = Task.RUN_ME

		return ret

	def run(self):
		self.ut_exec = getattr(self, 'ut_exec', [ self.inputs[0].abspath() ])
		if getattr(self.generator, 'ut_fun', None):
			self.generator.ut_fun(self)
		args = Utils.to_list(getattr(self.generator, 'ut_args', ''))
		if args:
			self.ut_exec.extend(args)

		Logs.debug('runner: %r' % self.ut_exec)
		cwd = getattr(self.generator, 'ut_cwd', '') or self.inputs[0].parent.abspath()

		opts = self.generator.bld.options

		env = {}
		env.update(os.environ)
		del env['TERM'] # fixes "System.ArgumentNullException: Value cannot be null." when running under mono

		if opts.mono_debug:
			env.update({ 'MONO_LOG_LEVEL' : 'debug' })
		if opts.trace:
			env.update({ 'PEACH_TRACE' : '1' })

		print self.ut_exec
		retcode = Utils.subprocess.call(self.ut_exec, cwd=cwd, env=env)

		tup = (getattr(self, 'ut_nunit', self.inputs[0]).name, retcode)
		self.generator.test_result = tup

		testlock.acquire()
		try:
			bld = self.generator.bld
			Logs.debug("ut: %r", tup)
			try:
				bld.utest_results.append(tup)
			except AttributeError:
				bld.utest_results = [tup]
		finally:
			testlock.release()

def options(opt):
	opt.add_option('--testcase', action='store', help='Name of test case/fixture to execute')
	opt.add_option('--where', action='store', help="NUnit --where filter (see https://github.com/nunit/docs/wiki/Test-Selection-Language)")
	opt.add_option('--trace', action='store_true', help='Enable trace log level')
	opt.add_option('--mono_debug', action='store_true', help='Enable mono debug logging')

class TestContext(InstallContext):
	'''runs the unit tests'''

	cmd = 'test'

	def __init__(self, **kw):
		super(TestContext, self).__init__(**kw)
		self.is_test = True
		self.add_post_fun(TestContext.summary)

	def summary(self):
		lst = getattr(self, 'utest_results', [])
		err = ', '.join(t[0] for t in filter(lambda x: x[1] != 0, lst))
		if err:
			raise Errors.WafError('Failures detected in test suites: %s' % err)
