# -*- coding: utf-8 -*-

from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, extension, taskgen_method
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs
from waflib import Utils, Errors, Logs, Options
import os, shutil, re, sys, zipfile, json
import xml.etree.ElementTree as ET

re_name = re.compile('^:Doctitle: (.*)', re.M)
re_desc = re.compile('^:Description: (.*)', re.M)

def configure(conf):
	v = conf.env
	j = os.path.join

	v.PIT_DOC_TEMPLATE     = '''= ${PIT_TITLE}
Peach Fuzzer, LLC
v{BUILDTAG}
:doctype: book
:compat-mode:
:experimental:
:icons: font
:listing-caption:
:toclevels: 3
:chapter-label:
ifdef::backend-pdf[]
:pagenums:
:source-highlighter: coderay
endif::[]

[abstract]
Copyright © 2015 Peach Fuzzer, LLC. All rights reserved.

This document may not be distributed or used for commercial purposes without the explicit consent of the copyright holders.

Peach Fuzzer® is a registered trademark of Peach Fuzzer, LLC.

Peach Fuzzer contains Patent Pending technologies.

While every precaution has been taken in the preparation of this book, the publisher and authors assume no responsibility for errors or omissions, or for damages resulting from the use of the information contained herein. 

Peach Fuzzer, LLC +
1122 E Pike St +
Suite 1064 +
Seattle, WA 98122

== ${PIT_DESCRIPTION}
${PIT_USAGE}
${EXTRA}
'''

def strict_error(msg):
	if Options.options.strict:
		raise Errors.WafError(msg)
	else:
		Logs.warn(msg)

@conf
def pit_common_export(self):
	return self.path.ant_glob('Assets/_Common/**')

@conf
def pit_common_doc(self):
	return self.path.ant_glob('Assets/*/*.adoc')

@conf
def pit_common_source(self):
	return self.path.ant_glob('Assets/*/*.xml')

def pit_subcategory(node):
	name = node.name
	k = name.find('.')
	if k >= 0:
		name = name[:k]
	return name.split('_')

@conf
def pit_builder(bld, name, **kw):
	source = Utils.to_list(kw.get('source', bld.pit_common_source()))
	category = kw.get('category', 'Network')

	# Make a builder for using the common models that doesn't make a pit zip
	bld(
		name     = name,
		features = 'pit',
		export   = kw.get('export', bld.pit_common_export()),
		doc      = kw.get('doc', bld.pit_common_doc()),
		category = category,
		catdir   = kw.get('catdir', 'Net'),
		use      = [],
	)

	# Make a pit zip for each pit source files
	# that will depend on a common set of models
	use = Utils.to_list(kw.get('use', [])) + [ name ]

	for s in source:
		# Name of zip is name of pit fle w/o extension
		childname,ext = os.path.splitext(str(s))

		# If there are multiple pit files all part of protocol 'XXX'
		# it is invalid to have XXX.xml as a pit, need more descriptive
		# names lile 'XXX_Client.xml' and 'XXX_Server.xml'
		# If there is only one pit file, 'XXX.xml' is considered valid
		if childname == name:
			if len(source) != 1:
				raise Errors.WafError("Error, '%s%s' must have a name starting with '%s_'." % (childname,ext,name))
		elif not childname.startswith(name + '_'):
			Logs.warn("Pit inconsistency in '%s.zip' - '%s%s' should have a name starting with '%s_'" % (name, childname, ext, name))

		bld(
			name     = '%s.zip' % (childname),
			target   = '%s.zip' % (childname),
			features = 'pit',
			source   = [ s ],
			use      = use,
			category = category,
			pit      = kw.get('pit', name),
			parent   = name,
		)

@conf
def pit_file_builder(bld, name, **kw):
	source = Utils.to_list(kw.get('source', bld.pit_common_source()))

	if len(source) == 1:
		# Expet pit file to be the same as name of this pit builder
		# IE: name='PNG' and source='PNG.xml'
		childname,ext = os.path.splitext(str(source[0]))
		if childname != name:
			Logs.warn("Pit inconsistency in '%s.zip' - '%s%s' should be named '%s%s'" % (name, childname, ext, name, ext))

	return bld(
		name     = name,
		target   = name + '.zip',
		features = 'pit',
		category = kw.get('category', 'File'),
		catdir   = kw.get('category', 'File'),
		source   = source,
		use      = kw.get('use', []),
		export   = kw.get('export', bld.pit_common_export()),
		doc      = kw.get('doc', bld.pit_common_doc()),
		pit      = kw.get('pit', name),
	)

@conf
def pit_net_builder(bld, name, **kw):
	kw['catdir']   = 'Net'
	kw['category'] = 'Network'
	return bld.pit_builder(name, **kw)

class pit_idx(Task):
	vars    = [ 'BUILDTAG', 'PIT_CATEGORY' ]
	ext_in  = [ '.config' ]
	ext_out = [ '.json' ]

	def run(self):
		meta = []
		cat = self.env.PIT_CATEGORY

		for s in self.inputs:
			try:
				tree = ET.parse(s.abspath())
				root = tree.getroot()

				config = []

				for section in root:
					for item in section:
						item.attrib['type'] = item.tag
						if item.attrib.has_key('min'):
							item.attrib['min'] = int(item.attrib['min'])
						if item.attrib.has_key('max'):
							item.attrib['max'] = int(item.attrib['max'])
						config.append(item.attrib)

				meta.append({
					'name'   : [ cat ] + pit_subcategory(s),
					'pit'    : s.path_from(s.parent.parent),
					'build'  : self.env.BUILDTAG,
					'config' : config,
					'calls'  : [ 'StartIterationEvent', 'ExitIterationEvent' ],
				})
			except Exception, e:
				raise Errors.WafError("Error in pit_idx task on: %r\n%s" % (s, e))

		with open(self.outputs[0].abspath(), "w+") as fd:
			json.dump(meta, fd, indent = 1, sort_keys = True)

@feature('pit')
@before_method('process_source')
def process_pit_source(self):
	srcs = self.to_nodes(getattr(self, 'source', []))

	# Install exports always
	self.install_pits(self.to_nodes(getattr(self, 'export', [])))

	# TaskGen only exists for exports
	if not srcs:
		return

	# Clear sources sinde we don't want waf to try and build them
	self.source = []

	cfgs = []
	extras = []
	exts = [ '.json', '.test' ]

	# Collect .xml.config files as well a extra files to install
	for s in srcs:
		cfgs.append(s.change_ext('.xml.config'))
		for x in exts:
			n = s.parent.find_resource(s.name + x)
			if n: extras.append(n)

	# Generate index.json from all the .config files
	self.idx_task = self.create_task('pit_idx', cfgs, self.path.find_or_declare(self.name + '.index.json'))

	try:
		self.idx_task.env.PIT_CATEGORY = self.category
	except AttributeError:
		raise Errors.WafError("TaskGen missing category attribute: %r" % self)

	assets = self.pit_assets_dir()
	chmod = Utils.O644 << 16L

	# Collect triple of (Node, Name, Attr) for files to zip
	self.zip_inputs = [ (self.idx_task.outputs[0], 'index.json', chmod) ]
	for x in srcs + cfgs:
		self.zip_inputs.append((x, x.path_from(assets), chmod))

	# Install our sources to flattened output folder
	self.install_pits(srcs + cfgs + extras)


@feature('pit')
@before_method('process_pit_source')
def process_pit_docs(self):
	v = self.env

	if not 'asciidoctor-pdf' in v['supported_features']:
		return

	docs = self.to_nodes(getattr(self, 'doc', []))
	if not docs:
		return

	doc_names = map(lambda x: x.name, docs)

	# there should be:
	# PIT.adoc
	# PIT_Usage.adoc
	# PIT_Datasheet.adoc
	# -- or --
	# PIT.adoc
	# PIT_Part1_Usage.adoc
	# PIT_Part2_Usage.adoc
	# PIT_Part1_DataSheet.adoc
	# PIT_Part2_DataSheet.adoc

	source = Utils.to_list(self.path.ant_glob('Assets/*/*.xml'))

	sheet = None
	master = None
	valid_usages = []
	pit_usage = ''
	target = self.path.find_or_declare(self.name + '.adoc')

	for src in source:
		childname, ext = os.path.splitext(str(src))
		usage = '%s_Usage.adoc' % childname
		valid_usages.append(usage)

		if usage not in doc_names:
			strict_error("No Usage guide for pit %r" % childname)

	for x in docs:
		if x.name.endswith('_DataSheet.adoc'):
			expected = '%s_DataSheet.adoc' % self.name
			if x.name == expected:
				sheet = x
			else:
				Logs.warn("Inconsistent file name, found %r but expected %r" % (x.name, expected))
		elif x.name.endswith('_Usage.adoc'):
			if x.name in valid_usages:
				pit_usage += 'include::%s[]\n' % x.path_from(target.parent)
			else:
				Logs.warn("Inconsistent file name, found %r but expected one of %r" % (x.name, valid_usages))
		else:
			if x.name == '%s.adoc' % self.name:
				master = x
			else:
				Logs.warn("Ignoring unrecognized documentation file %r" % (x.name))

	if master is None:
		raise Errors.WafError("No Master for pit %r" % self.name)

	contents = master.read()

	try:
		v.PIT_TITLE = re_name.search(contents).group(1)
	except AttributeError, e:
		raise Errors.WafError("Missing :Doctitle: in master %r" % master.abspath(), e)

	try:
		v.PIT_DESCRIPTION = re_desc.search(contents).group(1)
	except AttributeError, e:
		raise Errors.WafError("Missing :Description: in master %r" % master.abspath(), e)


	doc_additions = '../../../../../peach/doc_additions'
	if self.category == 'Network':
		v.EXTRA = 'include::%s/%s[]\n' % (doc_additions, 'Getting_machine_info.adoc')
	else:
		v.EXTRA = ''

	v.PIT_USAGE = pit_usage
	v.EMIT_SOURCE = Utils.subst_vars(v.PIT_DOC_TEMPLATE, v)

	tsk = self.create_task('emit', None, [ target ])
	adoc = tsk.outputs[0]

	# Make html version of datasheet
	if sheet is None:
		strict_error("No DataSheet for pit %r" % self.name)
	else:
		v.append_value('ASCIIDOCTOR_OPTS', [ '-d', 'article', '-a', 'last-update-label!' ])
		tsk = self.create_task('asciidoctor_html', sheet, sheet.change_ext('.html'))
		self.install_files('${BINDIR}/docs/datasheets', tsk.outputs)

	v.append_value('ASCIIDOCTOR_PDF_OPTS', [ '-a', 'imagesdir=%s' % doc_additions ])
	self.pdf_task = self.create_task('asciidoctor_pdf', adoc, adoc.change_ext('.pdf'))
	self.install_files('${BINDIR}/docs', self.pdf_task.outputs)

	# Don't save pdf task outputs in self.zip_inputs just yet
	# That will happen when we process the use parameter

@feature('pit')
@after_method('process_source')
def make_pit_zip(self):
	v = self.env

	# Only make the zip if we made a .json manifest
	if not hasattr(self, 'idx_task'):
		return

	chmod = Utils.O644 << 16L

	# Collect triple of (Node, Name, Attr) for dependencies to zip
	self.collect_pit_deps(self.name, [], chmod)

	# Collecting the deps might have filled in our pdf_task
	pdf = getattr(self, 'pdf_task', None)
	if not pdf:
		if self.name in v.SHIPPING_PITS:
			Logs.warn("No documentation for shipping pit %r" % self.name)
	else:
		self.zip_inputs.append((pdf.outputs[0], pdf.outputs[0].name, chmod))

	# Create the zip
	zip_srcs = [ x for x,d,a in self.zip_inputs ]
	zip_target = self.path.find_or_declare(self.target)
	self.zip_task = self.create_task('zip', zip_srcs, zip_target)
	self.install_files('${BINDIR}', self.zip_task.outputs)

class PitList:
	def __str__(self):
		return json.dumps(self.__pits, indent=4)

	def __repr__(self):
		return self.__pits.__repr__() + 'x'

	def __iter__(self):
		return self.__pits.__iter__()

	def __init__(self):
		self.__pits = []
		self.all_parts = set()
		self.all_pits = set()

	def add_pit_zip(self, tg):
		rec = next( (x for x in self.__pits if x['name'] == tg.pit), None)
		if not rec:
			rec = { 'name' : tg.pit, 'archives' : [] }
			self.__pits.append(rec)
			self.__pits.sort(key=lambda x: x['name'])
		rec['archives'].append('pits/%s' % tg.zip_task.outputs[0].name)

		name = os.path.splitext(tg.name)[0]
		self.all_parts.add(name)
		self.all_pits.add(tg.pit)

def verify_shipping_packs(ctx):
	shipping_packs = getattr(ctx, 'shipping_packs', None)
	if not shipping_packs:
		raise Errors.WafError("No shipping packs registered. Add \"bld.shipping_packs='xxx.json'\" to a wscript.");

	shipping_pits = ctx.shipping_pits_task.env.EMIT_SOURCE
	shipping_packs = json.loads(shipping_packs.read())
	
	expected = ctx.shipping_pits_task.env.SHIPPING_PITS
	actual = shipping_pits.all_parts
	delta = expected - actual
	if delta:
		strict_error('The following shipping pits are missing: %r' % ', '.join(delta))

	referenced = set()
	for pack in shipping_packs:
		referenced.update(map(str, pack['pits']))

	delta = shipping_pits.all_pits - referenced
	if delta:
		strict_error("The following shipping pits are not referenced: %r" % ', '.join(delta))

	delta = referenced - shipping_pits.all_pits
	if delta:
		strict_error("The following pits are not shipping but defined in shipping_packs: %r" % ', '.join(delta))

@feature('pit')
@after_method('make_pit_zip')
def make_shipping_pits(self):
	v = self.env

	name = os.path.splitext(self.name)[0]
	if name not in v.SHIPPING_PITS:
		return

	zip_task = getattr(self, 'zip_task', None)
	if not zip_task:
		return

	tsk = getattr(self.bld, 'shipping_pits_task', None)
	if not tsk:
		tg = self.bld(name = 'shipping_pits')
		out = tg.path.find_or_declare('shipping_pits.json')
		tsk = tg.create_task('emit', [], out)
		tg.install_files('${BINDIR}', tsk.outputs)
		tsk.env.EMIT_SOURCE = PitList()
		self.bld.shipping_pits_task = tsk
		self.bld.add_post_fun(verify_shipping_packs)

	tsk.env.EMIT_SOURCE.add_pit_zip(self)

@taskgen_method
def install_pits(self, srcs):
	if srcs:
		self.bld.install_files('${BINDIR}', srcs, relative_trick = True, cwd = self.path)

@taskgen_method
def pit_assets_dir(self):
	return self.path.find_dir('Assets')

@taskgen_method
def collect_pit_deps(self, name, seen, chmod):
	# Prevent infinite looping
	seen.append(name)

	try:
		y = self.bld.get_tgen_by_name(name)
	except Errors.WafError:
		return

	y.post()

	# Only consider deps from pit builders
	if 'pit' not in y.features:
		return

	deps = y.to_nodes(getattr(y, 'export', []))
	assets = y.pit_assets_dir()

	# Save off the deps for inclusion in our pit zip
	for x in deps:
		self.zip_inputs.append((x, x.path_from(assets), chmod))

	other_doc = getattr(y, 'pdf_task', None)
	if other_doc and self.name.startswith(y.name):
		if not hasattr(self, 'pdf_task'):
			self.pdf_task = other_doc
		elif self.pdf_task.generator != self:
			raise Errors.WafError("Attempting to include multiple docs in pit zip %r: %r and %r" % (self.name, self.pdf_task, other_doc))

	# Recursivley collect dependencies
	for x in self.to_list(getattr(y, 'use', [])):
		if x not in seen:
			self.collect_pit_deps(x, seen, chmod)
