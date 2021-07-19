import os.path, shutil, re, sys
from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs, Task
from waflib.Node import Node
from waflib import Utils, Errors, Logs, Context

def configure(conf):
	j = os.path.join
	v = conf.env

	if 'asciidoctor-pdf' not in v.supported_features:
		raise Errors.WafError("asciidoctor-pdf feature is missing")

	conf.find_program('java')
	conf.find_program('xmllint')
	conf.find_program('xsltproc')

	v['XMLLINT_OPTS'] = [
		'--noout',
		'--schema',
		j(conf.get_third_party(), 'docbook-5.0', 'xsd', 'docbook.xsd')
	]

	xsl = j(conf.get_third_party(), 'docbook-xsl-ns-1.78.1')

	v['WEBHELP_DIR'] = j(xsl, 'webhelp')
	v['WEBHELP_XSL'] = j(xsl, 'webhelp', 'xsl', 'webhelp.xsl')

	extensions = j(conf.get_third_party(), 'docbook-xsl-ns-1.78.1', 'extensions')

	classes = [
		j(extensions, 'webhelpindexer.jar'),
		j(extensions, 'tagsoup-1.2.1.jar'),
		j(extensions, 'lucene-analyzers-3.0.0.jar'),
		j(extensions, 'lucene-core-3.0.0.jar'),
	]

	conf.env['WEBINDEX_OPTS'] = [
		'-DindexerLanguage=en',
		'-DhtmlExtension=html',
		'-DdoStem=true',
		'-DindexerExcludedFiles=""',
		'-Dorg.xml.sax.driver=org.ccil.cowan.tagsoup.Parser',
		'-Djavax.xml.parsers.SAXParserFactory=org.ccil.cowan.tagsoup.jaxp.SAXFactoryImpl',
		'-classpath',
		os.pathsep.join(classes),
		'com.nexwave.nquindexer.IndexerMain',
	]

@conf
def set_webhelp_theme(self, path):
	if isinstance(path, str):
		node = self.path.find_dir(path)
	else:
		node = path
	if not node:
		raise Errors.WafError("webhelp theme directory not found: %r in %r" % (path, self))

	self.env.WEBHELP_THEME_PATH = node
	self.env.WEBHELP_THEME_DEPS = node.ant_glob('**/*')

def runnable_status(self):
	for t in self.run_after:
		if not t.hasrun:
			return ASK_LATER

	if not self.inputs:
		# Get the list of files to install
		self.source = self.inputs = self.path.ant_glob('**/*', quiet=True)

	ret = Task.runnable_status(self)
	if ret == SKIP_ME:
		return RUN_ME
	return ret

def install_webhelp(self, inst_to, srcs, cwd, tsk = None):
	inst = self.bld.install_files(inst_to, srcs, cwd = cwd, relative_trick = True, chmod = Utils.O644)

	if inst:
		if tsk:
			#inst.set_run_after(tsk)
			inst.runnable_status = lambda inst=inst: runnable_status(inst)

		# Store inst task in install_extras for packaging
		try:
			self.install_extras.append(inst)
		except AttributeError:
			self.install_extras = [inst]

@feature('webhelp')
@before_method('process_source')
def apply_webhelp(self):
	srcs = self.to_nodes(getattr(self, 'source', []))
	if not srcs:
		return

	if len(srcs) != 1:
		raise Errors.WafError("webhelp feature only supports a single source")

	# Clear source so we don't try and create a compiled task
	self.source = []

	name = getattr(self, 'target', None)
	if not name: name = self.name + '.xml'
	xml = self.path.find_or_declare(name)
	out = xml.parent.get_bld().make_node(self.name)
	out.mkdir()

	doc = self.create_task('asciidoctor', srcs, xml)
	lnt = self.create_task('xmllint', xml)
	hlp = self.create_task('webhelp', xml)
	idx = self.create_task('webindex', xml)

	idx.set_run_after(hlp)
	hlp.set_run_after(lnt)

	self.output_dir = out

	self.env.append_value('ASCIIDOCTOR_OPTS', [
		'-v',
		'-d',
		'article',
		'-b',
		'docbook',
		'-a'
		'images=images',
	])

	hlp.env['OUTPUT_DIR'] = out.path_from(self.bld.bldnode).replace('\\', '/') + '/'
	idx.env['OUTPUT_DIR'] = '-DhtmlDir=%s' % out.path_from(self.bld.bldnode)

	# Set a reasonable default install_path
	inst_to = getattr(self, 'install_path', '${BINDIR}/%s' % self.name)

	install_webhelp(self, inst_to, [], out, idx)

	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		self.images = img

		# Install image files
		install_webhelp(self, '%s/images' % inst_to, img.ant_glob('**/*'), img)

	root = self.bld.launch_node()

	# Install user template files
	if self.env.WEBHELP_THEME_DEPS:
		install_webhelp(self, inst_to, self.env.WEBHELP_THEME_DEPS, self.env.WEBHELP_THEME_PATH)

	# Install template files not included by the user
	template = os.path.relpath(os.path.join(self.env.WEBHELP_DIR, 'template'), self.path.abspath())
	node = self.path.find_dir(template)
	if not node:
		raise Errors.WafError("webhelp template directory not found at %r in %r" % (template, self))

	theme = [ x.path_from(self.env.WEBHELP_THEME_PATH) for x in self.env.WEBHELP_THEME_DEPS ]
	files = [ x for x in node.ant_glob('**/*') if x.path_from(node) not in theme ]

	if files:
		install_webhelp(self, inst_to, files, node)

class xmllint(Task):
	run_str = '${XMLLINT} ${XMLLINT_OPTS} ${SRC}'
	color   = 'PINK'
	before  = [ 'webhelp' ]
	vars    = [ 'XMLLINT_OPTS' ]

fail_re = re.compile('^Error.*$', re.MULTILINE)

class webhelp(Task):
	run_str = '${XSLTPROC} --stringparam base.dir ${OUTPUT_DIR} ${WEBHELP_XSL} ${SRC}'
	color   = 'PINK'
	vars    = [ 'WEBHELP_XSL', 'OUTPUT_DIR' ]
	after   = [ 'xmllint' ]

	def exec_command(self, cmd, **kw):
		if os.path.exists(self.generator.output_dir.abspath()):
			try:
				shutil.rmtree(self.generator.output_dir.abspath())
			except OSError:
				pass

		os.makedirs(self.generator.output_dir.abspath())

		bld = self.generator.bld
		try:
			if not kw.get('cwd', None):
				kw['cwd'] = bld.cwd
		except AttributeError:
			bld.cwd = kw['cwd'] = bld.variant_dir

		if not isinstance(kw['cwd'], str):
			kw['cwd'] = kw['cwd'].abspath()

		subprocess = Utils.subprocess
		kw['shell'] = isinstance(cmd, str)
		Logs.debug('runner: %r' % (cmd,))
		Logs.debug('runner_env: kw=%s' % kw)

		if bld.logger:
			bld.logger.info(cmd)

		kw['stdout'] = subprocess.PIPE
		kw['stderr'] = subprocess.PIPE

		if Logs.verbose and not kw['shell'] and not Utils.check_exe(cmd[0]):
			raise Errors.WafError("Program %s not found!" % cmd[0])

		try:
			p = subprocess.Popen(cmd, **kw)
			(out, err) = p.communicate()
			ret = p.returncode
		except Exception as e:
			raise Errors.WafError('Execution failure: %s' % str(e), ex=e)

		if out:
			if not isinstance(out, str):
				out = out.decode(sys.stdout.encoding or 'iso8859-1')
			if bld.logger:
				bld.logger.debug('out: %s' % out)
			else:
				Logs.info(out, extra={'stream':sys.stdout, 'c1': ''})
			if ret == 0:
				m = fail_re.search(out)
				if m:
					ret = m.groups(1)
		if err:
			if not isinstance(err, str):
				err = err.decode(sys.stdout.encoding or 'iso8859-1')
			if bld.logger:
				bld.logger.error('err: %s' % err)
			else:
				Logs.info(err, extra={'stream':sys.stderr, 'c1': ''})
			if ret == 0:
				m = fail_re.search(err)
				if m:
					ret = m.group(0)

		return ret

class webindex(Task):
	run_str = '${JAVA} ${OUTPUT_DIR} ${WEBINDEX_OPTS} '
	color   = 'PINK'
	vars    = [ 'WEBINDEX_OPTS', 'OUTPUT_DIR' ]
	after   = [ 'webhelp' ]

	def runnable_status(self):
		ret = super(Task, self).runnable_status()
		if ret == SKIP_ME:
			# in case the files were removed
			nodes = self.generator.output_dir.ant_glob('**/*', quiet=True)
			self.add_install(nodes)
		return ret

	def add_install(self, nodes):
		# Install all webhelp html and search files once the webindex has been generated
		self.outputs += nodes
		if getattr(self.generator, 'install_path', None):
			self.generator.add_install_files(install_to=self.generator.install_path,
				install_from=self.outputs,
				postpone=False,
				cwd=self.generator.output_dir,
				relative_trick=True)

	def post_run(self):
		nodes = self.generator.output_dir.ant_glob('**/*', quiet=True)
		for x in nodes:
			self.generator.bld.node_sigs[x] = self.uid()
		self.add_install(nodes)
		return super(Task, self).post_run()

