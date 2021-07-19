from waflib import Utils, Errors
from waflib.TaskGen import feature
import os.path

'''
For Windows 7 x64:
 * Strawberry Perl 5.20.1.1 64bit (strawberry-perl-5.20.1.1-64bit.msi)
 * Ruby 1.9.3-p545 (rubyinstaller-1.9.3-p545.exe)
 * Java SE Development Kit 8u11 x64 (jdk-8u25-windows-x64.exe)
 * Doxygen (doxygen-1.8.8-setup.exe)
 * Ghostscript (gs915w64.exe)

For Ubuntu 14.04 x64:
 * ruby1.9.1 perl xsltproc libxml2-utils ghostscript
 * Oracle JDK 1.8
 * http://www.webupd8.org/2012/09/install-oracle-java-8-in-ubuntu-via-ppa.html
 * wget http://downloads.ghostscript.com/public/binaries/ghostscript-9.15-linux-x86_64.tgz
'''

host_plat = [ 'win32', 'linux', 'darwin' ]

archs = [ ]

tools = [
	'tools.utils', # emit
]

optional_tools = [
	'tools.asciidoctor-pdf',
	'tools.doxygen',
	'tools.webhelp',
]

def prepare(conf):
	pass

def configure(conf):
	env = conf.env

	env.append_value('supported_features', [
		'doc',
		'emit',
		'subst',
		'zip',
		'install_task',
	])
