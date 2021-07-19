from waflib.TaskGen import feature, after_method
from waflib import Logs

def configure(conf):
	v = conf.env
	conf.find_program(v['ASAN_CC'])

@feature('asan')
@after_method('apply_link')
def process_asan(self):
	self.env['CC'] = self.env['ASAN_CC']
	self.env['CXX'] = self.env['ASAN_CXX']
	self.env['LINK_CC'] = self.env['ASAN_CC']
	self.env['LINK_CXX'] = self.env['ASAN_CXX']
