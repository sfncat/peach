#!/usr/bin/env python

import os
import sys
import argparse
import fnmatch
import zipfile
import shutil
import json
import datetime
import ntpath

'''
This script expects the following directory structure:
output
  doc/doc.zip
  pits/${pit}.zip
  ${platform}_release/pkg/peach-pro-${buildtag}-${platform}_release.zip
  ${platform}_release/pkg/flexnetls-${platform}.zip

The result should be:    <--- archive to smb://nas/builds/peach-pro
output
  release
  ${buildtag}            <--- publish to ssh://dl.peachfuzzer.com
    release.json
    peach-pro-${buildtag}-${platform}_release.zip
    peach-pro-${buildtag}-sdk.zip
    flexnetls-${platform}.zip
    pits
      ${pit}.zip
    datasheets
      html
        ${pit}.html
      pdf
        ${pit}.pdf
'''

outdir   = 'output'
reldir   = os.path.join(outdir, 'release')
pitfile  = os.path.join(outdir, 'pits.zip')
docfile  = os.path.join(outdir, 'doc.zip')
tmpdir   = os.path.join(outdir, 'tmp')

peach_docs = [ 'docs/*' ]
publish_docs = [ 'docs/publish/*' ]
sdk_filter = [ 'sdk/*' ]

releases = [
	{
		'dirname' : '%(buildtag)s',
		'all'     : 'peach-pro-%(buildtag)s.zip',
		'product' : 'Peach Studio',
		'sdk'     : 'peach-pro-%(buildtag)s-sdk.zip',
	},
]

def filter_release(item):
	return item.startswith('peach-pro') and 'release' in item

def extract_pkg():
	# Copy output/$CFG_release/pkg/*.zip to release folder

	print ''
	print 'Extract packages'
	print ''

	pkgs = []

	for cfg in os.listdir(outdir):
			if not cfg.endswith('release'):
					print 'IGNORING   %s' % cfg
					continue

			path = os.path.join(outdir, cfg, 'pkg')
			if not os.path.exists(path):
					continue

			print 'PROCESSING %s' % cfg

			for item in os.listdir(path):
					if not item.endswith('.zip'):
						continue
					src = os.path.join(path, item)
					print '  - %s' % item
					shutil.copy(src, reldir)
					pkgs.append((
						os.path.join(reldir, item),
						os.path.join(path, 'peach.xsd')
					))

	return pkgs

def extract_doc():
	# Look for output/doc.zip
	# Extract to release/tmp/doc and make list of all files

	print ''
	print 'Extract documentation'
	print ''

	files = []

	docdir = os.path.join(tmpdir, 'doc')

	print 'PROCESSING %s' % docfile

	with zipfile.ZipFile(docfile, 'r') as z:
		for i in z.infolist():
			# print ' - %s' % i.filename
			z.extract(i, docdir)
			files.append((docdir, i.filename))

	return files

def extract_pits():
	# Lookfor output/pits.zip
	# Extract to release/tmp/pits and make list of all files

	print ''
	print 'Extract pits'
	print ''

	files = []
	packs = None
	archives = None
	manifest = None

	pitdir = os.path.join(tmpdir, 'pits')

	print 'PROCESSING %s' % pitfile

	with zipfile.ZipFile(pitfile, 'r') as z:
		for i in z.infolist():
			if os.path.basename(i.filename) == 'shipping_packs.json':
				packs = z.read(i)
			if os.path.basename(i.filename) == 'shipping_pits.json':
				archives = z.read(i)
			if os.path.basename(i.filename) == 'manifest.json':
				manifest = z.read(i)
			if os.path.basename(i.filename) == 'Peach.Pro.Pits.dll':
				z.extract(i, pitdir)
				files.append(i.filename)
			if i.filename.endswith('.zip'):
				print ' - %s' % i.filename
				z.extract(i, pitdir)
				files.append(i.filename)
			if i.filename.startswith('docs/datasheets') and not i.filename.endswith('/'):
				print ' - %s' % i.filename
				z.extract(i, pitdir)
				files.append(i.filename)

	# TODO Filter docs based on shipping pits

	packs = json.loads(packs)
	archives = json.loads(archives)
	manifest = json.loads(manifest)

	return (files, packs, archives, manifest)

def add_files(z, files):
	for b, f in files:
		src = os.path.join(b, f)
		mode = os.stat(src).st_mode
		print ' + %s (%s)' % (f, oct(mode))
		z.write(src, f)
		zi = z.getinfo(f)
		zi.external_attr = mode << 16L

def update_pkg(pkg, docs, xsd):
	# Add all files in docs to pkg zip

	print ''
	print 'Adding docs to %s' % pkg
	print ''

	with zipfile.ZipFile(pkg, 'a', compression=zipfile.ZIP_DEFLATED) as z:
		add_files(z, docs)
		z.write(xsd, 'peach.xsd')

def make_sdk(pkg, files):
	print ''
	print 'Creating %s' % pkg
	print ''

	with zipfile.ZipFile(pkg, 'w', compression=zipfile.ZIP_DEFLATED) as z:
		add_files(z, files)

def filter_docs(files, filters):
	ret = []
	for b, f in files:
		for v in filters:
			if fnmatch.fnmatch(f, v):
				ret.append((b, f))
	return ret

def convert_manifest(manifest):
	ret = []
	for k, v in manifest['Features'].items():
		ret.append(dict(
			feature = k,
			zip = v['Zip'],
			exclude = v['Assets'],
		))
	return ret

def filter_updates(pkg):
	return 'internal' not in pkg and 'flexnetls' not in pkg

def main():
	p = argparse.ArgumentParser(description = 'make release zips')
	p.add_argument('--buildtag', default = '0.0.0', help = 'buildtag')
	args = p.parse_args()

	if os.path.isdir(reldir):
		shutil.rmtree(reldir)

	if not os.path.isdir(reldir):
		os.makedirs(reldir)

	pkgs = extract_pkg()
	docs = extract_doc()
	(pit_files, packs, pit_archives, manifest) = extract_pits()

	for pkg, xsd in pkgs:
		if 'release.zip' in pkg:
			update_pkg(pkg, filter_docs(docs, peach_docs), xsd)

	now = datetime.datetime.now()

	names = [ os.path.basename(x) for x, y in pkgs ]

	for r in releases:
		dirname = r['dirname'] % vars(args)
		sdk = r['sdk'] % vars(args)

		print ''
		print 'Generating release folder %s' % dirname
		print ''

		manifest = dict(
			dist = [ x for x in names if filter_release(x) ],
			files = [ sdk ] + [ntpath.basename(v) for x,v in filter_docs(docs, publish_docs)],
			flexnetls = [ x for x in names if 'flexnetls' in x ],
			product = r['product'],
			build = args.buildtag,
			version = 3,
			date = '%s/%s/%s' % (now.day, now.month, now.year),
			pit_archives = pit_archives,
			packs = packs,
			pit_features = convert_manifest(manifest),
		)

		if not manifest['dist']:
			print 'No files found, skipping!'
			continue

		path = os.path.join(reldir, dirname)
		os.mkdir(path)
		os.mkdir(os.path.join(path, 'pits'))

		sdk_path = os.path.join(path, sdk)
		make_sdk(sdk_path, filter_docs(docs, sdk_filter))

		rel = os.path.join(path, 'release.json')

		for d, v in filter_docs(docs, publish_docs):
			shutil.copy(os.path.join(d, v), path)


		for f in manifest['dist']:
			src = os.path.join(reldir, f)
			dst = os.path.join(path, f)
			shutil.copy(src, dst)

		for f in manifest['flexnetls']:
			src = os.path.join(reldir, f)
			dst = os.path.join(path, f)
			shutil.copy(src, dst)

		for f in pit_files:
			src = os.path.join(tmpdir, 'pits', f)
			# Eat the 'docs/' prefix
			if f.startswith('docs/'):
				f = f[5:]
			dst = os.path.join(path, 'pits', f)
			d = os.path.dirname(dst)
			if not os.path.isdir(d):
				os.makedirs(d)
			shutil.copy(src, dst)

		data = json.dumps(manifest, sort_keys=True, indent=4)
		with open(rel, 'w') as f:
			f.write(data)

	if os.path.isdir(tmpdir):
		shutil.rmtree(tmpdir)

	for x, y in pkgs:
		try:
			os.unlink(x)
		except:
			pass

if __name__ == "__main__":
	main()
