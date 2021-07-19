#!/usr/bin/env python

import subprocess, re, os, sys, argparse
import distutils.util

class Changes(object):
	def __init__(self, begin, end, version):

		print "Changes.__init__: begin '%s', end '%s'" % (begin, end)

		logs = subprocess.check_output([
			'git', 'log', '--pretty=format:%s', '%s..%s' % (begin, end), '--'
		]).split('\n')

		tag_new = "new:"
		tag_fix = "fix:"
		tag_chg = "chg:"

		self.version = version
		self.begin = begin
		self.end = end
		self.new = []
		self.chg = []
		self.fix = []
		self.bad = []

		for l in logs:
			l = l.strip()

			if not l or l.startswith('dev:'):
				continue

			# remove github PR # e.g This is a PR commit (#112)
			l = re.sub(r'(\s\(\#[0-9]+\))', '', l)

			# find prefix
			if l.startswith(tag_new):
				self.new.append(l[len(tag_new)+1:])
			elif l.startswith(tag_chg):
				self.chg.append(l[len(tag_chg)+1:])
			elif l.startswith(tag_fix):
				self.fix.append(l[len(tag_fix)+1:])
			else:
				self.bad.append(l)

	def write(self, fp):
		if not self.new and not self.chg and not self.fix:
			return

		fp.write('\n')
		fp.write('=== %s\n\n' % self.version)

		for x in self.new:
			fp.write(' * {new} %s\n' % x)
		for x in self.chg:
			fp.write(' * {chg} %s\n' % x)
		for x in self.fix:
			fp.write(' * {fix} %s\n' % x)

		fp.flush()

class Generator(object):
	def __init__(self):

		# tags to skip if found
		self._skip_tags = []

		# earliest version
		self._start_tag = 'v4.3.147'

		self.filename = 'docs/src.pro/Common/WhatsNew.adoc'

		with open(self.filename, 'r') as f:
			lines = f.readlines()

		for idx,val in enumerate(lines):
			if val.startswith('// START-WHATSNEW'):
				self.whatsnew_start = lines[0:idx+1]
			if val.startswith('// END-WHATSNEW'):
				self.whatsnew_end = lines[idx:]

		props = {}

		with open('peach-pro.properties', 'r') as f:
			for line in list(f):
				k,v = line.split('=')
				props[k.strip()] = v.strip()

		self._version = '%(TAG_PREFIX)s{BUILDTAG}' % props
		self._pub_prefix = props['PUB_PREFIX']
		self._tag_search = '%(PUB_PREFIX)s%(TAG_PREFIX)s%(RELEASE_VERSION)s.*' % props

		print ""
		print " * version:", self._version
		print " * pub_prefix:", self._pub_prefix
		print " * tag_search:", self._tag_search
		print ""

	def changes(self, strict):
		changes = []
		begin = self._prev_tag('HEAD')
		end = 'HEAD'
		version = self._version

		while True:

			if begin == end:
				print "Error, trying to get changes between same tag: %s..%s" % (begin, end)
				exit(1)
			
			c = Changes(begin, end, version)

			if strict and c.bad:
				print "Error, unknown git commit prefix in range %s..%s:" % (begin, end)
				for l in c.bad:
					print " * %s" % l
				exit(1)

			changes.append(c)

			if self._has_generated(begin):
				break

			if self._start_tag == begin:
				break

			version = begin
			end = begin
			begin = self._prev_tag(end + '^1')

		return changes

	def write(self, changes):

		# 1) Output everything as-is up to '// START-WHATSNEW'
		# 2) Output the changelog
		# 3) Eat all lines up to '// END-WHATSNEW'
		# 4) Output the remainder of the file

		with open(gen.filename, 'w+') as f:
			f.writelines(self.whatsnew_start)

			for c in changes:
				c.write(sys.stdout)
				c.write(f)

			sys.stdout.write('\n')
			f.write('\n')

			f.writelines(self.whatsnew_end)

	def _prev_tag(self, cur):
		
		tag = subprocess.check_output([
			'git', 'describe', '--match', self._tag_search, '--abbrev=0', cur
		]).strip()[len(self._pub_prefix):]

		while tag in self._skip_tags:
			tag = subprocess.check_output([
				'git', 'describe', '--match', self._tag_search, '--abbrev=0', "%s%s^" % (self._pub_prefix, tag)
			]).strip()[len(self._pub_prefix):]

		return tag

	def _has_generated(self, version):
		if version.endswith('.0'):
			return True

		for l in self.whatsnew_end:
			if version in l:
				return True

		return False

parser = argparse.ArgumentParser()
parser.add_argument('--strict', metavar='BOOL',
	type=lambda x:bool(distutils.util.strtobool(x)),
	default='false', help='enforce valid commit prefix')

args = parser.parse_args()

print "--] Whats New Generator [--"

gen = Generator()

changes = gen.changes(args.strict)

gen.write(changes)

print "--] done [--"

# end
