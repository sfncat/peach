import os
from waflib.Task import Task
from waflib.TaskGen import feature, before_method, after_method

FEATURE = 'flexnet_trial'

def configure(conf):
	j = os.path.join

	conf.find_program('java')

	flexnet_dir = j(conf.get_third_party(), 'flexnet_client-2016.08')
	identity = j(flexnet_dir, 'IdentityBackOffice.bin')
	tools = j(flexnet_dir, 'tools')
	class_path = [
		j(tools, 'flxTools.jar'),
		j(tools, 'flxBinary.jar'),
		j(tools, 'EccpressoAll.jar'),
		j(tools, 'commons-codec-1.9.jar'),
	]

	conf.env['FLEXNET_TRIAL_OPTS'] = [
		'-cp', os.pathsep.join(class_path),
		'com.flexnet.lm.tools.TrialFileUtil',
		'-id', identity
	]

	conf.env.append_value('supported_features', FEATURE)

@feature(FEATURE)
@before_method('process_source')
def generate_flexnet_trial(self):
	src = getattr(self, 'flexnet_input', None)
	if not src:
		self.generator.bld.fatal('flexnet_input must be specified')

	product = getattr(self, 'flexnet_product', None)
	if not product:
		self.generator.bld.fatal('flexnet_product must be specified')
	self.env['FLEXNET_PRODUCT'] = product

	inst_to = getattr(self, 'install_path', '${BINDIR}')
	node = self.path.find_resource(src)
	tgt = getattr(self, 'flexnet_output', None)
	if tgt is None:
		task = self.create_task('flexnet_trial', node, node.change_ext('.bin'))
		self.install_files(inst_to, task.outputs)
	else:
		task = self.create_task('flexnet_trial', node, tgt)
		if not tgt.is_src():
			self.install_files(inst_to, tgt)

class flexnet_trial(Task):
	run_str = '${JAVA} ${FLEXNET_TRIAL_OPTS} -product {FLEXNET_PRODUCT} ${SRC} ${TGT}'
	vars    = [ 'FLEXNET_TRIAL_OPTS', 'FLEXNET_PRODUCT' ]
