from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs
from waflib import Utils, Errors, Logs
import os, shutil, re

def configure(conf):
	j = os.path.join
	v = conf.env
	pub = j(conf.path.abspath(), 'docs', 'publishing')
	fopub = j(pub, 'asciidoctor-fopub')

	conf.find_program('ruby')
	conf.find_program('perl')
	conf.find_program('java')
	conf.find_program('xmllint')
	conf.find_program('xsltproc')

	conf.find_program('asciidoctor', path_list = [ j(pub, 'asciidoctor', 'bin') ], exts = '')
	conf.find_program('fopub', path_list = [ fopub ])

	# Use ghostscript 9.15 binary from www.ghostscript.com
	# so bookmarks are preserved when merging pdf files
	# see doc.py for download locations

	gs_path = []
	gs_prog = 'gs-915-linux_x86_64'

	if Utils.unversioned_sys_platform() == 'win32':
		gs_prog = 'gswin64c'
		for p in [ 'ProgramFiles', 'ProgramFiles(x86)', 'ProgramW6432' ]:
			gs_path.append(j(os.environ.get(p), 'gs', 'gs9.15', 'bin'))

	try:
		conf.find_program(gs_prog, var='GS', path_list=gs_path)
		v.append_value('supported_features', 'gs')
	except Exception, e:
		v.append_value('missing_features', 'gs')
		if Logs.verbose > 0:
			Logs.warn('Ghostscript is not available: %s' % (e))

	# Ensure fopub is initialized
	test = conf.bldnode.make_node('docbook_test.xml')
	if not os.path.isfile(test.abspath()):
		test.write('''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE article PUBLIC "-//OASIS//DTD DocBook XML V4.5//EN" "http://www.oasis-open.org/docbook/xml/4.5/docbookx.dtd">
<article lang="en">
<simpara>Test</simpara>
</article>''')

		conf.cmd_and_log(Utils.to_list(conf.env.FOPUB) + [test.abspath()], cwd = fopub)

	conf.env['ASCIIDOCTOR_OPTS'] = [
		'-v',
		'-b', 'docbook45',
		'-d', 'article',
	]

	conf.env['FOPUB_OPTS'] = [
		'-param', 'paper.type', 'USletter',
		'-param', 'header.column.widths', '"0 1 0"',
		'-param', 'footer.column.widths', '"0 1 0"',
	]

	conf.env['JAVA_OPTS'] = [
		'-Xmx3000M',
	]

	conf.env.append_value('SGML_CATALOG_FILES', [ j(pub, 'docbook-xml-4.5', 'catalog.xml') ])

	conf.env['XMLLINT_OPTS'] = [
		'--catalogs',
		'--nonet',
		'--noout',
		'--valid',
	]

	docbook = j('docs', 'publishing', 'docbook-xsl-1.78.1')
	conf.env['WEBHELP_DIR'] = j(docbook, 'webhelp')
	conf.env['WEBHELP_XSL'] = j(conf.path.abspath(), docbook, 'webhelp', 'xsl', 'webhelp.xsl')

	extensions = j(conf.path.abspath(), docbook, 'extensions')
	xerces = j(pub, 'xerces-2_11_0')

	classes = [
		j(extensions, 'webhelpindexer.jar'),
		j(extensions, 'lucene-analyzers-3.0.0.jar'),
		j(extensions, 'lucene-core-3.0.0.jar'),
		j(extensions, 'tagsoup-1.2.1.jar'),
		j(extensions, 'saxon-65.jar'),
		j(xerces, 'xercesImpl.jar'),
		j(xerces, 'xml-apis.jar '),
	]

	conf.env['WEBINDEX_OPTS'] = [
		'-DhtmlDir=docs',
		'-DindexerExcludedFiles=""',
		'-Dorg.xml.sax.driver=org.ccil.cowan.tagsoup.Parser',
		'-Djavax.xml.parsers.SAXParserFactory=org.ccil.cowan.tagsoup.jaxp.SAXFactoryImpl',
		'-cp',
		os.pathsep.join(classes),
		'com.nexwave.nquindexer.IndexerMain',
	]

	conf.env['GS_OPTS'] = [
		'-dBATCH',
		'-dNOPAUSE',
		'-q',
		'-sDEVICE=pdfwrite',
	]

def runnable_status(self):
	for t in self.run_after:
		if not t.hasrun:
			return ASK_LATER

	if not self.inputs:
		# Get the list of files to install
		# self.path is the same as our task generator's output_dir
		self.source = self.inputs = self.path.ant_glob('**/*', quiet=True)

	ret = Task.runnable_status(self)
	if ret == SKIP_ME:
		return RUN_ME
	return ret

@feature('asciidoc')
@after_method('process_source')
def apply_asciidoc(self):
	# Turn all docbook xml to pdf
	for adoc in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]
		pdf = xml.change_ext('.pdf')
		fopub = self.create_task('fopub', xml, pdf)

		cover = self.to_nodes(getattr(self, 'cover', []))
		if cover and 'gs' in self.env['supported_features']:
			merge = self.create_task('pdfmerge', cover + [ pdf ], pdf.change_ext('.merged.pdf'))
			pdf = merge.outputs[0]

		# Install pdf to bin directory
		inst_to = getattr(self, 'install_path', '${BINDIR}')
		inst = self.install_as('%s/%s' % (inst_to, self.name), pdf, chmod=Utils.O644)

		# Store inst task in install_extras for packaging
		try:
			self.install_extras.append(inst)
		except AttributeError:
			self.install_extras = [inst]

	# Set path to images relative to xml file
	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		adoc.env.append_value('ASCIIDOCTOR_OPTS', [ '-a', 'images=%s' % img.path_from(xml.parent) ])

@feature('webhelp')
@after_method('process_source')
def apply_webhelp(self):
	inst_to = getattr(self, 'install_path', '${BINDIR}/%s' % self.name)

	for adoc in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]

		# xsltproc outputs to cwd
		self.output_dir = xml.parent.find_dir(self.name)
		if not self.output_dir:
			self.output_dir = xml.parent.find_or_declare(self.name)

		xsl = self.create_task('webhelp', xml)
		idx = self.create_task('webindex', xml)

		# docbook-xsl will puts all docs in a 'docs' subfolder
		# so include this in our cwd so it is stripped in the BINDIR
		cwd = self.output_dir.find_or_declare('docs/file').parent

		inst = self.bld.install_files(inst_to, [], cwd = cwd, relative_trick = True, chmod = Utils.O644)

		if inst:
			inst.set_run_after(idx)
			inst.runnable_status = lambda inst=inst: runnable_status(inst)

			# Store inst task in install_extras for packaging
			try:
				self.install_extras.append(inst)
			except AttributeError:
				self.install_extras = [inst]

	# Set path to images relative to webroot
	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		adoc.env.append_value('ASCIIDOCTOR_OPTS', [ '-a', 'images=images' ])

		# Install images to bin directory
		inst = self.bld.install_files('%s/images' % inst_to, img.ant_glob('**/*'), cwd = img, relative_trick = True, chmod = Utils.O644)
		if inst:
			self.install_extras.append(inst)

	root = self.bld.launch_node()

	# Install template files to BINDIR
	template = root.find_dir(os.path.join(self.env.WEBHELP_DIR, 'template'))
	inst = self.bld.install_files(inst_to, template.ant_glob('**/*', excl='favicon.ico'), cwd = template, relative_trick = True, chmod = Utils.O644)
	if inst:
		self.install_extras.append(inst)

@extension('.adoc')
def adoc_hook(self, node):
	xml = node.change_ext('.%d.xml' % self.idx)

	adoc = self.create_task('asciidoctor', node, xml)
	lint = self.create_task('xmllint', xml)

	try:
		self.compiled_tasks.append(adoc)
	except AttributeError:
		self.compiled_tasks = [adoc]
	return adoc

re_xi = re.compile('''^(include|image)::([^.]*.(adoc|png))\[''', re.M)

def asciidoc_scan(self):
	depnodes = []

	root = self.inputs[0]
	p = root.parent

	docinfo = p.find_resource('%s-docinfo.xml' % os.path.splitext(root.name)[0])
	if docinfo:
		depnodes.append(docinfo)

	node_lst = [self.inputs[0]]
	seen = []
	while node_lst:
		nd = node_lst.pop(0)
		if nd in seen: continue
		seen.append(nd)

		code = nd.read()
		for m in re_xi.finditer(code):
			name = m.group(2)
			k = p.find_resource(name)
			if k:
				depnodes.append(k)
				node_lst.append(k)
	return [depnodes, ()]

class asciidoctor(Task):
	run_str = '${RUBY} -I. ${ASCIIDOCTOR} ${ASCIIDOCTOR_OPTS} -o ${TGT} ${SRC}'
	color   = 'PINK'
	vars    = ['ASCIIDOCTOR_OPTS']
	scan    = asciidoc_scan

class xmllint(Task):
	run_str = '${XMLLINT} ${XMLLINT_OPTS} ${SRC}'
	color   = 'PINK'
	before  = [ 'fopub', 'webhelp' ]
	vars    = [ 'XMLLINT_OPTS' ]

	def exec_command(self, cmd, **kw):
		env = dict(self.env.env or os.environ)
		env.update(SGML_CATALOG_FILES = ';'.join(self.env['SGML_CATALOG_FILES']))
		kw['env'] = env

		return super(xmllint, self).exec_command(cmd, **kw)

class pdfmerge(Task):
	run_str = '${GS} ${GS_OPTS} -sOutputFile= ${TGT} ${SRC}'
	color   = 'PINK'
	vars    = [ 'GS_OPTS' ]

	def exec_command(self, cmd, **kw):
		if isinstance(cmd, list):
			lst = []
			carry = ''
			for a in cmd:
				if a == '-sOutputFile=':
					carry = a
				else:
					lst.append(carry + a)
					carry = ''
			cmd = lst

		return super(pdfmerge, self).exec_command(cmd, **kw)

class webhelp(Task):
	run_str = '${XSLTPROC} ${WEBHELP_XSL} ${SRC[0].abspath()}'
	color   = 'PINK'
	vars    = [ 'WEBHELP_XSL' ]
	after   = [ 'xmllint' ]

	def exec_command(self, cmd, **kw):
		# webhelp outputs all files in cwd
		self.cwd = self.generator.output_dir.abspath()

		if os.path.exists(self.cwd):
			try:
				shutil.rmtree(self.cwd)
			except OSError:
				pass

		os.makedirs(self.cwd)

		kw['cwd'] = self.cwd

		return super(webhelp, self).exec_command(cmd, **kw)

@update_outputs
class webindex(Task):
	run_str = '${JAVA} ${WEBINDEX_OPTS}'
	color   = 'PINK'
	vars    = [ 'WEBINDEX_OPTS' ]
	after   = [ 'webhelp' ]

	def exec_command(self, cmd, **kw):
		# Force 'cwd' to be output_dir
		kw['cwd'] = self.generator.output_dir.abspath()
		ret = super(webindex, self).exec_command(cmd, **kw)

		if not ret:
			# gather the list of output files from webhelp and webindex
			self.outputs = self.generator.output_dir.ant_glob('**/*', quiet=True)

		return ret

class fopub(Task): 
	run_str = '${FOPUB} ${SRC} ${FOPUB_OPTS}'
	color   = 'PINK'
	vars    = [ 'FOPUB_OPTS', 'JAVA_OPTS' ]
	after   = [ 'xmllint' ]

	def exec_command(self, cmd, **kw):
		env = dict(self.env.env or os.environ)
		env.update(JAVA_OPTS = ' '.join(self.env['JAVA_OPTS']))
		kw['env'] = env

		return super(fopub, self).exec_command(cmd, **kw)
