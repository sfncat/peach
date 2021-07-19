#!/usr/bin/env python

import os
import sys
import subprocess

nunit_dir = os.path.dirname(os.path.realpath(__file__))
bin_dir = os.getenv('BINDIR', nunit_dir)

j = os.path.join
nunit = j(nunit_dir, 'nunit3-console.exe')

def dotnet(cmd):
	if sys.platform != 'win32':
		cmd.insert(0, 'mono')
	return cmd

def main():
	cmd = dotnet([
		nunit,
		'--labels=All',
		'--result=output/nunit-web.xml',
		j(bin_dir, 'Peach.Web.Test.Proxy.exe'),
		j(bin_dir, 'Peach.Web.Test.Unit.exe'),
		j(bin_dir, 'Peach.Web.Test.Integration.exe'),
	])

	retcode = subprocess.call(cmd)
	sys.exit(retcode)

if __name__ == "__main__":
	try:
		main()
	except KeyboardInterrupt:
		print 'KeyboardInterrupt'
