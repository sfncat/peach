from waflib.Build import InstallContext
from waflib.TaskGen import feature, after_method
from waflib import Task, Utils, Logs, Configure, Context, Options, Errors
import os

class GumpContext(InstallContext):
	'''run gump integration tests'''

	cmd = 'gump'

	def __init__(self, **kw):
		super(GumpContext, self).__init__(**kw)
		self.is_gump = True

def generate_test_yml(self):
	role_template = '    - { role: gump, pit_category: ${catdir}, pit_name: ${name}, tags: [ "${name}" ] }\n'
	test_template = '''---
- hosts: all
  sudo: true

  roles: 
${roles}'''

	roles = ''
	for part in self.path.ant_glob('Assets/*/*.xml'):
		name = os.path.splitext(part.name)[0]
		roles += Utils.subst_vars(role_template, dict(catdir=self.catdir, name=name))

	target = self.path.find_or_declare('test.yml')
	tsk = self.create_task('emit', None, [ target ])
	tsk.env.EMIT_SOURCE = Utils.subst_vars(test_template, dict(roles=roles))
	return tsk.outputs

@feature('pit')
@after_method('make_pit_zip')
def prepare_gump(self):
	if hasattr(self, 'parent'):
		return

	test_dir = self.path.find_node('Test/Local')
	if not test_dir:
		return

	if not hasattr(self, 'category'):
		raise Errors.WafError('Missing category for pit: %r' % self.name)
	
	inst_to = os.path.join('${PREFIX}', 'gump', self.category, self.name)
	src = self.path.ant_glob('Test/Local/**/*')

	self.install_files(inst_to, src, cwd=test_dir, relative_trick=True)
	self.install_files(inst_to, self.path.find_node('../../Vagrantfile.j2'))
	self.install_files(inst_to, self.path.ant_glob('Assets/*/*.xml.json'))
	self.install_files(inst_to, self.path.ant_glob('Assets/*/*.xml.test'))

	test_yml = generate_test_yml(self)
	self.install_files(inst_to, test_yml)

@feature('pit')
@after_method('make_pit_zip')
def prepare_gump_zip(self):
	zip_task = getattr(self, 'zip_task', None)
	if not zip_task:
		return

	if hasattr(self, 'parent'):
		name = self.parent
	else:
		name = self.name

	inst_to = os.path.join('${PREFIX}', 'gump', self.category, name)
	self.install_files(inst_to, self.zip_task.outputs)
