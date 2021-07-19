#! /usr/bin/env python

from waflib.extras import doxygen

def configure(conf):
	conf.find_program('doxygen', var='DOXYGEN')
