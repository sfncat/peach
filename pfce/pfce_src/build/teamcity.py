#!/usr/bin/env python

import os
import re
import subprocess
import argparse

def tag(roots):
	for root in roots:
		repo = os.path.basename(root)
		remote = 'git@github_%s:dejavu/%s.git' % (repo, repo)
		subprocess.check_call([ 'git', 'tag', '-a', '-m', 'Tagging build', 'v%s' % buildtag ], cwd=root)
		subprocess.check_call([ 'git', 'push', '--tags', remote ], cwd=root)

if __name__ == "__main__":
	p = argparse.ArgumentParser(description='teamcity init')
	p.add_argument('--promote', action='store_true')
	p.add_argument('--tag', action='store_true')
	p.add_argument('--match', default="v*")
	p.add_argument('--root', action='append')

	args = p.parse_args()

	if args.tag:
		advance = 1
	else:
		advance = 0

	buildtag = '0.0.0'
	branch = subprocess.check_output(['git', 'rev-parse', '--abbrev-ref', 'HEAD']).strip()
	desc = subprocess.check_output(['git', 'describe', '--match', args.match]).strip()

	if branch == 'master' or branch.startswith('prod-'):
		match = re.match(r'v(\d+)\.(\d+)\.(\d+).*', desc)
		if args.promote and match:
			buildtag = '%s.%s.%d' % (match.group(1), match.group(2), int(match.group(3)) + advance)
			if args.tag:
				tag(args.root)

	print("##teamcity[setParameter name='BuildTag' value='%s']" % buildtag)
	print("##teamcity[buildNumber '%s']" % desc)
