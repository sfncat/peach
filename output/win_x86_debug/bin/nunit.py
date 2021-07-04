#!/usr/bin/env python

import os
import sys
import time
import signal
import tempfile
import argparse
import threading
import subprocess
import xml.etree.ElementTree as ET

bindir = os.path.dirname(os.path.realpath(__file__))
nunit = os.path.join(bindir, 'nunit3-console.exe')
trampoline = os.path.join(bindir, 'trampoline.py')

def win32_kill(pid):
	subprocess.call([
		'taskkill',
		'/t',               # Tree (including children)
		'/f',               # Force 
		'/pid', str(pid),
	])

def unix_kill(pid):
	os.killpg(pid, signal.SIGKILL)

def kill(pid):
	if sys.platform == 'win32':
		win32_kill(pid)
	else:
		unix_kill(pid)

def dotnet(cmd, newpg=True):
	if sys.platform != 'win32':
		cmd.insert(0, 'mono')
		if newpg:
			cmd.insert(0, trampoline)
			cmd.insert(0, 'python')
	return cmd

TEST_TEMPLATE = '''      <test-case name="{name}" duration="{duration}" result="{result}">
        <reason>
          <message>{message}</message>
        </reason>
      </test-case>'''

FILE_TEMPLATE = '''<?xml version="1.0" encoding="utf-8" standalone="no"?>
<test-run>
  <test-suite type="Assembly" total="{total}" duration="{duration}" failed="{failed}" skipped="{skipped}" name="{asm_name}">
    <test-suite type="TestFixture" fullname="{fixture_fullname}">
{testcases}
    </test-suite>
  </test-suite>
</test-run>
'''

def save_failed(asm, test, fixture, filename, duration, message):
	cases = []
	for testcase in fixture.findall('.//test-case'):
		cases.append(TEST_TEMPLATE.format(
			name=testcase.attrib['name'],
			duration=duration,
			result='Failed',
			message=message,
		))

	xml = FILE_TEMPLATE.format(
		total=fixture.attrib['testcasecount'],
		duration=duration,
		failed=fixture.attrib['testcasecount'],
		skipped=0,
		asm_name=asm,
		fixture_fullname=test,
		testcases='\n'.join(cases)
	)

	with open(filename, 'w') as fout:
		fout.write(xml)

def category_filter(comma_sep_list):
	categories = ["cat == %s" % x.strip() for x in comma_sep_list.split(',')]
	return " || ".join(categories)

def run_nunit(args, asm, fixture, outdir):
	test = fixture.attrib['fullname']
	result = '.'.join([
		os.path.splitext(args.result)[0],
		test.replace('<', '_').replace('>', '_'),
		'xml',
	])

	cmd = dotnet([
		nunit,
		'--labels=All',
		"--where:%s" % category_filter(args.include),
		'--result=%s' % result,
		asm,
		'--test=%s' % test,
	])

	print ' '.join(cmd)
	sys.stdout.flush()

	start = time.time()
	status = dict(aborted=None)
	proc = subprocess.Popen(cmd, stdout=subprocess.PIPE)

	def on_inactive():
		status['aborted'] = 'Timeout due to inactivity'
		print '%s: %s' % (test, status['aborted'])
		sys.stdout.flush()
		kill(proc.pid)

	def on_abort(signum, frame):
		status['aborted'] = 'SIGTERM'
		print '%s: %s' % (test, status['aborted'])
		sys.stdout.flush()
		kill(proc.pid)
		sys.exit(signal.SIGTERM)

	try:
		old_handler = signal.signal(signal.SIGTERM, on_abort)

		while proc.poll() is None:
			timer = threading.Timer(args.timeout, on_inactive)
			timer.start()
			line = proc.stdout.readline()
			timer.cancel()

			with open(os.path.join(outdir, '%s.txt' % test), 'a') as fout:
				sys.stdout.write(line)
				sys.stdout.flush()
				fout.write(line)
	except KeyboardInterrupt:
		status['aborted'] = 'KeyboardInterrupt'
		print 'kill(%d)' % proc.pid
		sys.stdout.flush()
		kill(proc.pid)
		raise
	finally:
		try:
			signal.signal(signal.SIGTERM, old_handler)
		except Exception, e:
			print 'Could not restore SIGTERM handler: %s' % e
			sys.stdout.flush()

		try:
			timer.cancel()
		except Exception, e:
			print 'Could not cancel timer: %s' % e
			sys.stdout.flush()

		try:
			print 'Wait for process to finish...'
			sys.stdout.flush()

			stdout = proc.stdout.read()
			sys.stdout.write(stdout)
			sys.stdout.flush()

			rc = proc.wait()
			print 'exit code: %d' % rc
			print ''
			print ''
			sys.stdout.flush()

			if status['aborted'] or rc < 0:
				msg = status['aborted'] or 'nunit failed with exit code: %d' % rc
				duration = time.time() - start
				save_failed(asm, test, fixture, result, duration, msg)
		except Exception, e:
			print e
			sys.stdout.flush()

def main():
	p = argparse.ArgumentParser(description='nunit-runner')
	p.add_argument('--timeout', type=int, default=600)
	p.add_argument('--result', required=True)
	p.add_argument('--include', required=True)
	p.add_argument('input', nargs=argparse.REMAINDER)
	args = p.parse_args()

	tmpdir = os.getenv('TMPDIR')
	if tmpdir is not None and not os.path.exists(tmpdir):
		print 'Creating $TMDIR: %s' % tmpdir
		os.makedirs(tmpdir)

	outdir = os.path.dirname(args.result)
	if not os.path.exists(outdir):
		print 'Creating output dir: %s' % outdir
		os.makedirs(outdir)

	with tempfile.NamedTemporaryFile() as tmp:
		explore = dotnet([
			nunit,
			'--explore=%s' % tmp.name,
			"--where:%s" % category_filter(args.include),
		], newpg=False) + args.input

	subprocess.check_call(explore)

	xml_root = ET.parse(tmp.name).getroot()

	for asm in xml_root.findall('test-suite[@type="Assembly"]'):
		path = asm.attrib['fullname']

		for fixture in asm.findall('.//test-suite[@type="TestFixture"]'):
			run_nunit(args, path, fixture, outdir)

if __name__ == "__main__":
	try:
		main()
	except KeyboardInterrupt:
		print 'KeyboardInterrupt'
