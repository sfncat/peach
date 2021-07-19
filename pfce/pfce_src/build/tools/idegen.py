import os
import os.path
import sys
import re
import random

from collections import OrderedDict, namedtuple

from waflib.extras import msvs
from waflib.Build import BuildContext
from waflib import Utils, TaskGen, Logs, Task, Context, Node, Options, Errors

msvs.msvs_generator.cmd = None
msvs.msvs_2008_generator.cmd = None

form_re = re.compile('Windows Form Designer generated code')

MONO_PROJECT_TEMPLATE = r'''<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">${project.build_properties[0].configuration}</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">${project.build_properties[0].platform_tgt}</Platform>
    <ProductVersion>10.0.0</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{${project.uuid}}</ProjectGuid>
    <Compiler>
      <Compiler ctype="GppCompiler" />
    </Compiler>
    <Language>CPP</Language>
    <Target>Bin</Target>
  </PropertyGroup>


  ${for b in project.build_properties}
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == '${b.configuration}|${b.platform}' ">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>${b.outdir}</OutputPath>
    <OutputName>${b.output_file}</OutputName>
    <CompileTarget>Bin</CompileTarget>
    <DefineSymbols>${xml:b.preprocessor_definitions}</DefineSymbols>
    <SourceDirectory>.</SourceDirectory>
    <CustomCommands>
      <CustomCommands>
        <Command type="Clean" command="${xml:project.get_clean_command(b)}" workingdir="${project.ctx.path.abspath()}" />
        <Command type="Build" command="${xml:project.get_build_command(b)}" workingdir="${project.ctx.path.abspath()}" />
      </CustomCommands>
    </CustomCommands>
  </PropertyGroup>
  ${endfor}

  <ItemGroup>
    ${for x in project.source}
    <${project.get_mono_key(x)} Include='${project.relpath(x)}'>
      <Link>${x.path_from(project.tg.path)}</Link>
    </${project.get_mono_key(x)}>
    ${endfor}
  </ItemGroup>

</Project>'''

CS_PROJECT_TEMPLATE = r'''<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  ${for x in project.csproj_imports}
  ${x}
  ${endfor}

  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">${project.build_properties[0].configuration}</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">${project.build_properties[0].platform_tgt}</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    ${for k, v in project.globals.iteritems()}
    ${if v is None}
    <${k} />
    ${else}
    <${k}>${str(v)}</${k}>
    ${endif}
    ${endfor}
  </PropertyGroup>

  ${for props in project.build_properties}
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == '${props.configuration}|${props.platform_tgt}' ">
    ${for k, v in props.properties.iteritems()}
    <${k}>${str(v)}</${k}>
    ${endfor}
    ${if props.custom_cmds}
    <CustomCommands>
      <CustomCommands>
        ${for cmd in props.custom_cmds}
        <Command type="${cmd.type}" command="${cmd.command}" workingdir="${cmd.workingdir}" />
        ${endfor}
      </CustomCommands>
    </CustomCommands>
    ${endif}

  </PropertyGroup>
  ${endfor}

  ${if project.references}
  <ItemGroup>
    ${for k,v in project.references.iteritems()}
    ${if v.path}
    <Reference Include="${v.name}">
      <HintPath>${v.path}</HintPath>
    </Reference>
    ${else}
    <Reference Include="${v.name}" />
    ${endif}
    ${endfor}
  </ItemGroup>
  ${endif}

  ${for props in project.build_properties}
  ${if getattr(props, 'references', [])}
  <ItemGroup>
    ${for v in props.references}
    ${if v.path}
    <Reference Include="${v.name}"  Condition=" '$(Configuration)|$(Platform)' == '${props.configuration}|${props.platform_tgt}' ">
      <HintPath>${v.path}</HintPath>
    </Reference>
    ${else}
    <Reference Include="${v.name}"  Condition=" '$(Configuration)|$(Platform)' == '${props.configuration}|${props.platform_tgt}' " />
    ${endif}
    ${endfor}
  </ItemGroup>
  ${endif}
  ${endfor}

  ${if project.project_refs}
  <ItemGroup>
    ${for r in project.project_refs}
    <ProjectReference Include="${r.path}">
      <Project>{${r.uuid}}</Project>
      <Name>${r.name}</Name>
    </ProjectReference>
    ${endfor}
  </ItemGroup>
  ${endif}

  ${if project.source_files}
  <ItemGroup>
    ${for src in project.source_files.itervalues()}
    <${src.how} Include="${src.name}"${if not src.attrs} />${else}>
      ${for k,v in src.attrs.iteritems()}
      <${k}>${str(v)}</${k}>
      ${endfor}
    </${src.how}>${endif}
    ${endfor}
  </ItemGroup>
  ${endif}

  ${for p in project.build_properties}
  ${if getattr(p, 'source_files', [])}
  <ItemGroup Condition=" '$(Configuration)|$(Platform)' == '${p.configuration}|${p.platform_tgt}' ">
    ${for src in p.source_files}
    <${src.how} Include="${src.name}" ${if not src.attrs} />${else}>
      ${for k,v in src.attrs.iteritems()}
      <${k}>${str(v)}</${k}>
      ${endfor}
    </${src.how}>${endif}
    ${endfor}
  </ItemGroup>
  ${endif}
  ${endfor}

  ${if getattr(project.tg, 'ide_aspnet', False)}
  <PropertyGroup>
    <VisualStudioVersion Condition="'$(VisualStudioVersion)' == ''">10.0</VisualStudioVersion>
    <VSToolsPath Condition="'$(VSToolsPath)' == ''">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
  </PropertyGroup>
  ${endif}

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />

  ${if getattr(project.tg, 'ide_aspnet', False)}
  <Import Project="$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets" Condition="'$(VSToolsPath)' != ''" />
  <Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v10.0\WebApplications\Microsoft.WebApplication.targets" Condition="false" />
  <ProjectExtensions>
    <VisualStudio>
      <FlavorProperties GUID="{349c5851-65df-11da-9384-00065b846f21}">
        <WebProjectProperties>
          <UseIIS>False</UseIIS>
          <AutoAssignPort>True</AutoAssignPort>
          <DevelopmentServerPort>${project.aspnet_port}</DevelopmentServerPort>
          <DevelopmentServerVPath>/</DevelopmentServerVPath>
          <IISUrl>
          </IISUrl>
          <NTLMAuthentication>False</NTLMAuthentication>
          <UseCustomServer>False</UseCustomServer>
          <CustomServerUrl>
          </CustomServerUrl>
          <SaveServerSettingsInUserFile>False</SaveServerSettingsInUserFile>
        </WebProjectProperties>
      </FlavorProperties>
    </VisualStudio>
  </ProjectExtensions>
  ${endif}

  ${if any([x for x in project.build_properties if x.post_build])}
  <PropertyGroup>
    ${for p in project.build_properties}
    ${if p.post_build}
    <PostBuildEvent Condition=" '$(Configuration)|$(Platform)' == '${p.configuration}|${p.platform_tgt}' ">${p.post_build}</PostBuildEvent>
    ${endif}
    ${endfor}
  </PropertyGroup>
  ${endif}

  ${if project.global_props}
  <PropertyGroup>
  ${for k,v in project.global_props.iteritems()}
    <${k}>${str(v)}</${k}>
  ${endfor}
  </PropertyGroup>
  ${endif}

  ${for x in project.csproj_post_imports}
  ${x}
  ${endfor}

</Project>'''

PROJECT_TEMPLATE = r'''<?xml version="1.0" encoding="UTF-8"?>
<Project DefaultTargets="Build" ToolsVersion="4.0"
	xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

	<ItemGroup Label="ProjectConfigurations">
		${for b in project.build_properties}
		<ProjectConfiguration Include="${b.configuration}|${b.platform}">
			<Configuration>${b.configuration}</Configuration>
			<Platform>${b.platform}</Platform>
		</ProjectConfiguration>
		${endfor}
	</ItemGroup>

	<PropertyGroup Label="Globals">
		<ProjectGuid>{${project.uuid}}</ProjectGuid>
		<Keyword>MakeFileProj</Keyword>
		<ProjectName>${project.name}</ProjectName>
	</PropertyGroup>
	<Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />

	${for b in project.build_properties}
	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='${b.configuration}|${b.platform}'" Label="Configuration">
		<ConfigurationType>Makefile</ConfigurationType>
		<OutDir>${b.outdir}</OutDir>
		${if getattr(project, 'platform_toolset', None)}
		<PlatformToolset>${project.platform_toolset}</PlatformToolset>
		${endif}
	</PropertyGroup>
	${endfor}

	<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
	<ImportGroup Label="ExtensionSettings">
	</ImportGroup>

	${for b in project.build_properties}
	<ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='${b.configuration}|${b.platform}'">
		<Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
	</ImportGroup>
	${endfor}

	${for b in project.build_properties}
	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='${b.configuration}|${b.platform}'">
		<NMakeBuildCommandLine>${xml:project.get_build_command(b)}</NMakeBuildCommandLine>
		<NMakeReBuildCommandLine>${xml:project.get_rebuild_command(b)}</NMakeReBuildCommandLine>
		<NMakeCleanCommandLine>${xml:project.get_clean_command(b)}</NMakeCleanCommandLine>
		<NMakeIncludeSearchPath>${xml:b.includes_search_path}</NMakeIncludeSearchPath>
		<NMakePreprocessorDefinitions>${xml:b.preprocessor_definitions};$(NMakePreprocessorDefinitions)</NMakePreprocessorDefinitions>
		<IncludePath>${xml:b.includes_search_path}</IncludePath>
		<ExecutablePath>$(ExecutablePath)</ExecutablePath>

		${if getattr(b, 'output_file', None)}
		<NMakeOutput>${xml:b.output_file}</NMakeOutput>
		${endif}
		${if getattr(b, 'deploy_dir', None)}
		<RemoteRoot>${xml:b.deploy_dir}</RemoteRoot>
		${endif}
	</PropertyGroup>
	${endfor}

	${for b in project.build_properties}
		${if getattr(b, 'deploy_dir', None)}
	<ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='${b.configuration}|${b.platform}'">
		<Deploy>
			<DeploymentType>CopyToHardDrive</DeploymentType>
		</Deploy>
	</ItemDefinitionGroup>
		${endif}
	${endfor}

	<ItemGroup>
		${for x in project.source}
		<${project.get_key(x)} Include='${x.win32path()}' />
		${endfor}
	</ItemGroup>
	<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
	<ImportGroup Label="ExtensionTargets">
	</ImportGroup>
</Project>'''

SOLUTION_TEMPLATE = '''Microsoft Visual Studio Solution File, Format Version ${project.numver}
# Visual Studio ${project.vsver}
${for p in project.all_projects}
Project("{${p.ptype()}}") = "${p.name}", "${p.title}", "{${p.uuid}}"
${if getattr(p, 'project_sections', None)}
${for sec,opts in p.project_sections.iteritems()}
	${if opts}
	ProjectSection(${sec[0]}) = ${sec[1]}
		${for k,v in opts.iteritems()}
		${k} = ${v}
		${endfor}
	EndProjectSection
	${endif}
${endfor}
${endif}
EndProject${endfor}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		${if project.all_projects}
		${for (configuration, platform) in project.all_projects[0].ctx.project_configurations()}
		${configuration}|${platform} = ${configuration}|${platform}
		${endfor}
		${endif}
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		${for p in project.all_projects}
			${if hasattr(p, 'source')}
			${for b in p.build_properties}
		{${p.uuid}}.${b.configuration}|${b.platform_sln}.ActiveCfg = ${b.configuration_bld}|${b.platform}
			${if getattr(b, 'is_active', getattr(p, 'is_active', None))}
		{${p.uuid}}.${b.configuration}|${b.platform_sln}.Build.0 = ${b.configuration_bld}|${b.platform}
			${endif}
			${if getattr(b, 'is_deploy', getattr(p, 'is_deploy', None))}
		{${p.uuid}}.${b.configuration}|${b.platform_sln}.Deploy.0 = ${b.configuration_bld}|${b.platform}
			${endif}
			${endfor}
			${endif}
		${endfor}
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
	${for p in project.all_projects}
		${if p.parent}
		{${p.uuid}} = {${p.parent.uuid}}
		${endif}
	${endfor}
	EndGlobalSection
EndGlobal
'''

msvs.SOLUTION_TEMPLATE = SOLUTION_TEMPLATE
msvs.PROJECT_TEMPLATE = PROJECT_TEMPLATE

def default_collect_properties(self):
	ret = []
	for c in self.ctx.configurations:
		for p in self.ctx.platforms:
			x = msvs.build_property()
			x.outdir = ''

			x.configuration = c
			x.platform = p
			x.platform_sln = p

			x.preprocessor_definitions = ''
			x.includes_search_path = ''

			# can specify "deploy_dir" too
			ret.append(x)
	self.build_properties = ret

msvs.vsnode_project.collect_properties = default_collect_properties

# Note, no newline at end of template file!

class reference(object):
	def __init__(self, cwd, name, node):
		self.name = name
		if node:
			self.path = node.path_from(cwd)
			self.key = node.abspath()
		else:
			self.path = None
			self.key = name

class source_file(object):
	def __init__(self, how, ctx, node, cwd=None):
		self.how = how
		self.node = node
		self.attrs = OrderedDict()

		if not cwd:
			cwd = ctx.tg.path

		if not node.is_child_of(cwd):
			proj_path = node.name
		else:
			proj_path = node.path_from(cwd)

		rel_path = node.path_from(ctx.base)
		self.name = rel_path

		if proj_path != rel_path:
			self.attrs['Link'] = proj_path

class source_link(object):
	def __init__(self, how, node, name, link=None):
		self.how = how
		self.node = node
		self.name = name
		self.attrs = OrderedDict()
		if link:
			self.attrs['Link'] = link

class embed_resource(object):
	def __init__(self, ctx, name, tsk):
		self.node = tsk.outputs[0]
		self.name = self.node.path_from(ctx.tg.path)
		self.how = 'EmbeddedResource'
		self.attrs = OrderedDict()
		self.attrs['Link'] = 'Resources\\' + name

class vsnode_target(msvs.vsnode_target):
	def __init__(self, ctx, tg):
		msvs.vsnode_target.__init__(self, ctx, tg)
		tg.uuid = self.uuid
		self.proj_configs = OrderedDict() # Variant -> build_property
		self.platform_toolset = getattr(ctx, 'platform_toolset', None)

	def collect_properties(self):
		msvs.vsnode_target.collect_properties(self)

	def collect_source(self):
		# Try and find the wscript_build
		wscript = self.tg.path.find_resource('wscript_build')
		if wscript:
			self.source.append(wscript)

		msvs.vsnode_target.collect_source(self)

	def relpath(self, item):
		# Have to use os.path.relpath since self.ctx.path isn't always a parent of item
		return os.path.relpath(item.abspath(), self.ctx.path.abspath())

	def get_mono_key(self, node):
		"""
		required for writing the source files
		"""
		name = node.name
		if name.endswith('.cpp') or name.endswith('.c'):
			return 'Compile'
		return 'None'

	def get_waf(self):
		return 'cd /d "%s" & "%s" %s' % (
			self.ctx.ctx.srcnode.abspath(), 
			sys.executable, 
			getattr(self.ctx, 'waf_command', 'waf')
		)

	def get_waf_mono(self):
		return '%s %s' % (sys.executable, getattr(self.ctx, 'waf_command', 'waf'))

	def get_build_params(self, props):
		(waf, opt) = msvs.vsnode_target.get_build_params(self, props)
		opt += " --variant=%s" % props.variant
		return (waf, opt)

class vsnode_web_target(msvs.vsnode_project):
	VS_GUID_WEBPROJ = "E24C65DC-7377-472B-9ABA-BC803B73C61A"
	def ptype(self):
		return self.VS_GUID_WEBPROJ

	def __init__(self, ctx, tg):
		self.base = getattr(ctx, 'projects_dir', None) or tg.path
		node = tg.path.make_node(tg.name + '.webproj')
		msvs.vsnode_project.__init__(self, ctx, node)
		self.name = tg.name
		self.tg = tg # task generators
		self.target_framework = tg.env['TARGET_FRAMEWORK']
		self.proj_configs = OrderedDict() # Variant -> build_property
		self.project_sections = OrderedDict()

		# Keep ports random but deterministic, use uuid as seed
		random.seed(str(self.uuid))
		port = str(random.randint(40000, 49999))
		self.title = 'http://localhost:%s' % port

	def write(self):
		# Web projects have no project file, just populate the project section
		web_root = self.tg.ide_website
		sln_root = self.ctx.path
		rel_path = os.path.relpath(web_root.abspath(), sln_root.abspath()) + os.path.sep

		p = OrderedDict()
		self.project_sections[('WebsiteProperties', 'preProject')] = p

		p['UseIISExpress'] = "true"
		p['TargetFrameworkMoniker'] = ".NETFramework,Version%%3D%s" % self.target_framework
		for cfg in ['Debug', 'Release']:
			p['%s.AspNetCompiler.VirtualPath' % cfg] = "/%s" % web_root.name
			p['%s.AspNetCompiler.PhysicalPath' % cfg] = rel_path
			p['%s.AspNetCompiler.TargetPath' % cfg] = rel_path
			p['%s.AspNetCompiler.Updateable' % cfg] = "true"
			p['%s.AspNetCompiler.ForceOverwrite' % cfg] = "true"
			p['%s.AspNetCompiler.FixedNames' % cfg] = "false"
			p['%s.AspNetCompiler.Debug' % cfg] = "True"

		p['SlnRelativePath'] = rel_path
		p['DefaultWebSiteLanguage'] = "Visual C#"

	def collect_source(self):
		pass

	def collect_properties(self):
		pass

class vsnode_cs_target(msvs.vsnode_project):
	VS_GUID_CSPROJ = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"
	def ptype(self):
		return self.VS_GUID_CSPROJ

	def __init__(self, ctx, tg):
		self.base = getattr(ctx, 'projects_dir', None) or tg.path
		if getattr(ctx, 'csproj_in_tree', True):
			self.base = tg.path

		name = getattr(tg, 'ide_name', None)
		if not name:
			if hasattr(tg, 'gen'):
				name = os.path.splitext(tg.gen)[0]
			else:
				name = tg.name

		# If taskgen is in multiple solutions, prepend sln name to csproj file
		if len(getattr(tg, 'solutions', [])) > 1:
			name = ctx.sln_name + '-' + name

		node = self.base.make_node(name + '.csproj') # the project file as a Node

		msvs.vsnode_project.__init__(self, ctx, node)
		self.name = name
		self.tg = tg # task generators

		# Note: Must use ordered dict so order is preserved
		self.globals      = OrderedDict()
		self.properties   = OrderedDict()
		self.references   = OrderedDict() # Name -> HintPath
		self.embeds       = OrderedDict() # Abspath -> Record
		self.source_files = OrderedDict() # Abspath -> Record
		self.global_props = OrderedDict()
		self.project_refs = [] # uuid
		self.proj_configs = OrderedDict() # Variant -> build_property
		self.project_dependencies = OrderedDict() # List of UUID
		self.project_sections = OrderedDict() # sln sections, like 'ProjectDependencies'
		self.project_sections[('ProjectDependencies', 'postProject')] = self.project_dependencies

		self.csproj_imports = []
		self.csproj_post_imports = []
		if hasattr(tg, 'tsc'):
			self.csproj_imports = [
				'''<Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\\v$(VisualStudioVersion)\TypeScript\Microsoft.TypeScript.Default.props" Condition="Exists('$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\\v$(VisualStudioVersion)\TypeScript\Microsoft.TypeScript.Default.props')" />''',
			]
			self.csproj_post_imports = [
				'''<Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\\v$(VisualStudioVersion)\TypeScript\Microsoft.TypeScript.targets" Condition="Exists('$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\\v$(VisualStudioVersion)\TypeScript\Microsoft.TypeScript.targets')" />''',
			]

	def combine_flags(self, flag):
		tg = self.tg
		final = OrderedDict()
		for item in tg.env.CSFLAGS:
			if item.startswith(flag):
				opt = item[len(flag):].replace(',', ';')
				for x in opt.split(';'):
					final.setdefault(x)
		return ';'.join(final.keys())

	def collect_use(self):
		tg = self.tg
		get = tg.bld.get_tgen_by_name

		names = tg.to_list(getattr(tg, 'use', []))

		for x in names:
			asm_name = os.path.splitext(x)[0]
			try:
				y = get(x)
			except Errors.WafError:
				if asm_name.startswith('Facades/'):
					continue
				r = reference(self.base, asm_name, None)
				self.references[r.key] = r
				continue
			y.post()

			tsk = getattr(y, 'cs_task', None) or getattr(y, 'link_task', None)
			if not tsk:
				self.bld.fatal('cs task has no link task for use %r' % self)

			if 'fake_lib' in y.features:
				r = reference(self.base, asm_name, y.link_task.outputs[0])
				self.references[r.key] = r
				continue

			if 'nuget_lib' in y.features:
				asm_name = os.path.splitext(x.split(':')[3])[0]
				r = reference(self.base, asm_name, y.link_task.outputs[0])
				self.references[r.key] = r
				continue

			base = self.base == tg.path and y.path or self.base
			name = os.path.splitext(y.name)[0]
			if len(getattr(y, 'solutions', [])) > 1:
				csproj = self.ctx.sln_name + '-' + name + '.csproj'
			else:
				csproj = name + '.csproj'
			other = base.make_node(csproj)
			
			dep = msvs.build_property()
			dep.path = other.path_from(self.base)
			dep.uuid = msvs.make_uuid(other.abspath())
			dep.name = name

			self.project_refs.append(dep)
	
	def collect_better_install(self, lst):
		installs = getattr(self.tg, 'better_install', [])
		for item in installs:
			source_dir = item.get('source_dir', self.tg.path)
			source_glob = item.get('source_glob', '**')
			source_nodes = source_dir.ant_glob(source_glob)
			target = item.get('target')
			if target:
				target = self.base.make_node(target)
			for node in source_nodes:
				entry = lst.get(node.abspath(), None)
				if entry is None:
					include = node.path_from(self.base)
					rel_path = node.path_from(source_dir)
					if target:
						link = target.make_node(rel_path)
					else:
						link = self.base.make_node(rel_path)
					link = link.path_from(self.base)
					if link == include:
						link = None
					entry = source_link('Content', node, include, link)
					lst[node.abspath()] = entry
				entry.attrs['CopyToOutputDirectory'] = 'PreserveNewest'

	def collect_install(self, lst, attr):
		val = getattr(self.tg, attr, [])

		if isinstance(val, dict):
			for cwd, items in val.iteritems():
				if isinstance(cwd, str):
					cwd = self.path.find_node(cwd)
				self.collect_install2(lst, items, cwd)
		else:
			self.collect_install2(lst, val, self.tg.path)

	def collect_install2(self, lst, items, path):
		extras = self.tg.to_nodes(items, path=path)

		for x in extras:
			r = lst.get(x.abspath(), None)
			if not r:
				r = source_file('Content', self, x, path)
			r.attrs['CopyToOutputDirectory'] = 'PreserveNewest'
			lst[x.abspath()] = r

	def collect_source(self):
		tg = self.tg
		lst = self.source_files

		# Process compiled sources
		if hasattr(tg, 'cs_task'):
			srcs = tg.to_nodes(tg.cs_task.inputs, [])
			for x in srcs:
				lst[x.abspath()] = source_file('Compile', self, x)

		if hasattr(tg, 'tsc'):
			for tsc in tg.tsc:
				srcs = tg.to_nodes(tsc.inputs, [])
				for x in srcs:
					lst[x.abspath()] = source_file('TypeScriptCompile', self, x)

				for x in tsc.tsc_deps[0]:
					lst[x.abspath()] = source_file('TypeScriptCompile', self, x)				

		# extra sources if you want
		srcs = tg.to_nodes(getattr(tg, 'ide_source', []))
		for x in srcs:
			lst[x.abspath()] = source_file('Compile', self, x)

		# Process compiled resx files
		for tsk in filter(lambda x: x.__class__.__name__ is 'resgen', tg.tasks):
			r = source_file('EmbeddedResource', self, tsk.inputs[0])
			lst[r.node.abspath()] = r

		# Process embedded resources
		srcs = tg.to_nodes(getattr(tg, 'resource', []))
		for x in srcs:
			r = source_file('EmbeddedResource', self, x)
			lst[x.abspath()] = r

		# Process embedded assemblies
		srcs = tg.to_list(getattr(tg, 'embed', []))
		get = tg.bld.get_tgen_by_name
		for x in srcs:
			y = get(x)
			y.post()
			tsk = getattr(y, 'link_task', None)
			r = embed_resource(self, x, tsk)
			self.embeds[r.node.abspath()] = r

		# Process ide_content attribute
		srcs = tg.to_nodes(getattr(tg, 'ide_content', []))
		for x in srcs:
			r = source_file('Content', self, x)
			lst[x.abspath()] = r

			if x.name.endswith('.asax'):
				cs = x.parent.find_resource(x.name + '.cs')
				if cs: 
					cs = lst.get(cs.abspath(), None)
					cs.attrs['DependentUpon'] = x.name

		# Process installed files
		self.collect_install(lst, 'install_644')
		self.collect_install(lst, 'install_755')
		self.collect_better_install(lst)

		# Try and find the wscript_build
		wscript = tg.path.find_resource('wscript_build')
		if wscript:
			lst[wscript.abspath()] = source_file('None', self, wscript)

		# Add app.manifest
		manifest = getattr(tg, 'app_manifest', None)
		if manifest:
			lst[manifest.abspath()] = source_file('None', self, manifest)

		# Add app.config
		cfg = getattr(tg, 'app_config', None)
		if cfg:
			lst[cfg.abspath()] = source_file('None', self, cfg)

		# Check for Web.config
		# if getattr(tg, 'ide_aspnet', False):
		# 	cfg = tg.path.find_resource('Web.config')
		# 	if not cfg:
		# 		self.ctx.fatal('Could not find Web.config for ide_aspnet taskgen: %r' % tg)
		# 	r = source_file('Content', self, cfg)
		# 	r.attrs['SubType'] = 'Designer'
		# 	lst[cfg.abspath()] = r

		# 	config = self.ctx.get_config(tg.bld, tg.env)

		# 	cfg_config = tg.path.find_resource('Web.%s.config' % config)
		# 	if cfg_config:
		# 		r = source_file('None', self, cfg_config)
		# 		r.attrs['DependentUpon'] = cfg.name
		# 		lst[cfg_config.abspath()] = r

		settings = []

		# Try and glue up Designer files
		for k,v in lst.iteritems():
			n = v.node

			if not n.name.lower().endswith('.designer.cs'):
				# basename = os.path.splitext(n.abspath())
				# if basename[1]:
				# 	dep = lst.get(basename[0], None)
				# 	if dep:
				# 		v.attrs['DependentUpon'] = dep.name
				continue

			name = n.name[:-12]

			aspx = name.endswith('.aspx') and n.parent.find_resource(name)
			if aspx: aspx = lst.get(aspx.abspath(), None)
			cs = n.parent.find_resource(name + '.cs')
			if cs: cs = lst.get(cs.abspath(), None)
			resx = n.parent.find_resource(name + '.resx')
			if resx: resx = lst.get(resx.abspath(), None)
			setting = n.parent.find_resource(name + '.settings')

			if aspx:
				subtype = 'ASPXCodeBehind'
			elif form_re.search(n.read()):
				subtype = 'Form'
			else:
				subtype = 'Component'

			if aspx:
				if Logs.verbose > 0:
					print 'Designer: aspx - %s' % k
				v.attrs['DependentUpon'] = aspx.node.name
				if cs:
					cs.attrs['DependentUpon'] = aspx.node.name
					cs.attrs['SubType'] = subtype
			elif cs and resx:
				# If cs & resx, 's' & 'resx' are dependent upon 'cs'
				if Logs.verbose > 0:
					print 'Designer: cs & resx - %s %s' % (subtype, k)
				v.attrs['DependentUpon'] = cs.node.name
				cs.attrs['SubType'] = subtype
				resx.attrs['DependentUpon'] = cs.node.name
			elif cs:
				# If cs only, 's' is dependent upon 'cs'
				if Logs.verbose > 0:
					print 'Designer: cs - %s %s' % (subtype, k)
				v.attrs['DependentUpon'] = cs.node.name
				cs.attrs['SubType'] = subtype
			elif resx:
				# If resx only, 's' is autogen
				if Logs.verbose > 0:
					print 'Designer: resx - %s' % k
				v.attrs['AutoGen'] = True
				v.attrs['DependentUpon'] = resx.node.name
				v.attrs['DesignTime'] = True
				resx.attrs['Generator'] = 'ResXFileCodeGenerator'
				resx.attrs['LastGenOutput'] = n.name
			elif setting:
				# If settings, add to source file list
				if Logs.verbose > 0:
					print 'Designer: settings - %s' % k
				f = source_file('None', self, setting)
				f.attrs['Generator'] = 'SettingsSingleFileGenerator'
				f.attrs['LastGenOutput'] = n.name
				v.attrs['AutoGen'] = True
				v.attrs['DependentUpon'] = f.node.name
				v.attrs['DesignTimeSharedInput'] = True

				# Defer adding until we are done iterating
				settings.append(f)

		for x in settings:
			lst[x.node.abspath()] = x

		self.collect_use()

	def write(self):
		Logs.debug('msvs: creating %r' % self.path)

		# first write the project file
		template1 = msvs.compile_template(CS_PROJECT_TEMPLATE)
		proj_str = template1(self)
		proj_str = msvs.rm_blank_lines(proj_str)
		self.path.stealth_write(proj_str)

	def collect_properties(self):
		tg = self.tg
		g = self.globals

		if hasattr(tg, 'cs_task'):
			asm_name = os.path.splitext(tg.cs_task.outputs[0].name)[0]
		else:
			asm_name = tg.name
		base = getattr(self.ctx, 'projects_dir', None) or tg.path

		env = tg.env
		platform = tg.env.CSPLATFORM
		config = self.ctx.get_config(tg.bld, tg.env)

		out_node = base.make_node(['bin', '%s_%s' % (config, platform)])
		out = out_node.path_from(self.base)

		# Order matters!
		g['ProjectGuid'] = '{%s}' % self.uuid
		if getattr(tg, 'ide_aspnet', False):
			g['ProjectTypeGuids'] = '{349c5851-65df-11da-9384-00065b846f21};{fae04ec0-301f-11d3-bf4b-00c04f79efbc}'
			g['OutputType'] = 'library'
		else:
			g['OutputType'] = getattr(tg, 'bintype', tg.gen.endswith('.dll') and 'library' or 'exe')
		g['BaseIntermediateOutputPath'] = base.make_node('obj').path_from(self.base) + os.sep

		# This should get rid of the obj/<arch>/<cfg>/TempPE folder
		# but it still exists.  More info available here:
		# http://social.msdn.microsoft.com/Forums/vstudio/en-US/eb1b5a4c-7348-4926-89eb-b57a9d811863/vs-inproc-compiler-msbuild-and-the-obj-subdir
		# g['UseHostCompilerIfAvailable'] = base == tg.path

		g['AppDesignerFolder'] = 'Properties'
		g['RootNamespace'] = getattr(tg, 'namespace', asm_name)
		g['AssemblyName'] = asm_name
		g['TargetFrameworkVersion'] = tg.env.TARGET_FRAMEWORK
		g['TargetFrameworkProfile'] = os.linesep + '    '
		g['FileAlignment'] = '512'

		# Uncomment the following line to ignore framework mismatch errors
		#g['ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch'] = True

		keyfile = tg.to_nodes(getattr(tg, 'keyfile', []))
		if keyfile:
			f = source_file('None', self, keyfile[0])
			g['SignAssembly'] = True
			g['AssemblyOriginatorKeyFile'] = f.name
			self.source_files[f.node.abspath()] = f

		if getattr(tg, 'ide_aspnet', False):
			g['UseIISExpress'] = 'true'
			g['IISExpressSSLPort'] = None
			g['IISExpressAnonymousAuthentication'] = None
			g['IISExpressWindowsAuthentication'] = None
			g['IISExpressUseClassicPipelineMode'] = None
			# Keep ports random but deterministic, use uuid as seed
			random.seed(str(self.uuid))
			self.aspnet_port = str(random.randint(60000, 65535))

		p = self.properties

		# Order matters!
		p['PlatformTarget'] = getattr(tg, 'platform', 'AnyCPU')
		p['DebugSymbols'] = getattr(tg, 'csdebug', tg.env.CSDEBUG) and True or False
		p['DebugType'] = getattr(tg, 'csdebug', tg.env.CSDEBUG)
		p['Optimize'] = '/optimize+' in tg.env.CSFLAGS
		if getattr(tg, 'ide_aspnet', False):
			p['OutputPath'] = 'bin'
		else:
			p['OutputPath'] = out
		p['DefineConstants'] = self.combine_flags('/define:')
		p['ErrorReport'] = 'prompt'
		p['WarningLevel'] = self.combine_flags('/warn:')
		p['NoWarn'] = self.combine_flags('/nowarn:')
		p['TreatWarningsAsErrors'] = '/warnaserror' in tg.env.CSFLAGS
		p['DocumentationFile'] = getattr(tg, 'csdoc', tg.env.CSDOC) and out + os.sep + asm_name + '.xml' or ''
		p['AllowUnsafeBlocks'] = getattr(tg, 'unsafe', False)

		if getattr(tg, 'tsc', False):
			self.global_props['TypeScriptOutFile'] = tg.tsc_out[0]

		# Add ide_use task generator outputs as post build copy
		# Using abspath since macros like $(ProjectDir) don't seem to work

		ide_use = []
		names = names = tg.to_list(getattr(tg, 'ide_use', []))
		for x in names:
			y = tg.bld.get_tgen_by_name(x)
			y.post()
			tsk = getattr(y, 'link_task', None)
			if not tsk:
				self.tg.bld.fatal('cs task has no link task for ide_use %r' % self.tg)

			src = y.link_task.outputs[0]
			dst = out_node.make_node(src.name)
			ide_use.append((src, dst))

			# We might not have run collect_sources() yet
			if not hasattr(y, 'uuid'):
				y = self.ctx.make_project(y)
				if not y and not self.ctx.enable_cproj:
					continue

				if not hasattr(y, 'uuid'):
					self.tg.bld.fatal('cs tgen link task has no uuid for ide_use %r' % self.tg)

			guid = '{%s}' % y.uuid
			self.project_dependencies[guid] = guid

		(inst_cmd, inst_args) = self.ctx.get_ide_use(ide_use)
		if inst_args:
			p[inst_cmd] = inst_args

class vsnode_cs_target2012(vsnode_cs_target):
	def __init__(self, ctx, tg):
		vsnode_cs_target.__init__(self, ctx, tg)
		self.csproj_imports.append(
			'''<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />'''
		)

def copyattr(obj, src, dst):
	setattr(obj, dst, getattr(obj, src, []))

class Variant(object):
	def __init__(self, name, prop, projects):
		self.name = name
		self.prop = prop
		self.projects = projects
		self.key = '%s|%s' % (prop.configuration, prop.platform_sln)

class idegen(msvs.msvs_generator):
	'''generates a visual studio 2010 solution'''

	copy_cmd = 'copy'
	enable_cproj = True

	def __init__(self, ctx):
		self.ctx = ctx

	def init(self, sln):
		if not getattr(self, 'configurations', None):
			self.configurations = ['Release'] # LocalRelease, RemoteDebug, etc
		if not getattr(self, 'platforms', None):
			self.platforms = ['Win32']
		if not getattr(self, 'all_projects', None):
			self.all_projects = []
		if not getattr(self, 'project_extension', None):
			self.project_extension = '.vcxproj'
		if not getattr(self, 'projects_dir', None):
			self.projects_dir = self.ctx.srcnode.make_node('.depproj').make_node(os.path.splitext(sln)[0])
			self.projects_dir.mkdir()

		# bind the classes to the object, so that subclass can provide custom generators
		if not getattr(self, 'vsnode_vsdir', None):
			self.vsnode_vsdir = msvs.vsnode_vsdir
		if not getattr(self, 'vsnode_target', None):
			self.vsnode_target = msvs.vsnode_target
		if not getattr(self, 'vsnode_build_all', None):
			self.vsnode_build_all = msvs.vsnode_build_all
		if not getattr(self, 'vsnode_install_all', None):
			self.vsnode_install_all = msvs.vsnode_install_all
		if not getattr(self, 'vsnode_project_view', None):
			self.vsnode_project_view = msvs.vsnode_project_view

		self.sln_name = os.path.splitext(sln)[0]
		self.numver = '11.00'
		self.vsver  = '2010'

		#self.projects_dir = None
		#self.csproj_in_tree = False

		# Make monodevelop csproj
		if Utils.unversioned_sys_platform() != 'win32':
			msvs.PROJECT_TEMPLATE = MONO_PROJECT_TEMPLATE
			msvs.vsnode_project.VS_GUID_VCPROJ = '2857B73E-F847-4B02-9238-064979017E93'
			vsnode_target.get_waf = vsnode_target.get_waf_mono
			self.project_extension = '.cproj'
			self.get_platform = self.get_platform_mono
			self.get_config = self.get_config_mono
			self.get_ide_use = self.get_ide_use_mono
			idegen.copy_cmd = 'cp'

		self.vsnode_target = vsnode_target
		self.vsnode_cs_target = vsnode_cs_target
		self.vsnode_web_target = vsnode_web_target

	def collect_projects(self, projects):
		self.all_projects = projects
		self.add_aliases()
		self.collect_dirs()
		default_project = getattr(self, 'default_project', None)
		def sortfun(x):
			if x.name == default_project:
				return ''
			return getattr(x, 'path', None) and x.path.win32path() or x.name
		self.all_projects.sort(key=sortfun)

	def get_config(self, bld, env):
		return '%s_%s' % (env.TARGET, env.VARIANT)

	def get_config_mono(self, bld, env):
		return bld.variant

	def get_platform(self, env):
		if env.SUBARCH == []:
			return 'Win32'
		return env.SUBARCH.replace('x86', 'Win32')

	def get_platform_mono(self, env):
		return 'Win32'

	def get_ide_use(self, items):
		args = []
		for (src, dst) in items:
			args.append('copy "%s" "%s"' % (src.abspath(), dst.abspath()))
		return ('PostBuildEvent', os.linesep.join(args))

	def get_ide_use_mono(self, items):
		args = []
		for (src, dst) in items:
			args.append('        <Command type="AfterBuild" command="cp &quot;%s&quot; &quot;%s&quot;" />' % (src.abspath(), dst.abspath()))

		if args:
			args.insert(0, '')
			args.insert(1, '      <CustomCommands>')
			args.append('      </CustomCommands>')
			args.append('    ')

		return ('CustomCommands', os.linesep.join(args))

	def get_solution_node(self):
		return self.ctx.srcnode.make_node(self.sln)

	def add_variant(self):
		if self.all_projects:
			# Generate the sln config|plat for this variant
			prop = msvs.build_property()
			prop.platform_tgt = self.ctx.env.CSPLATFORM
			prop.platform = self.get_platform(self.ctx.env)
			prop.platform_sln = prop.platform_tgt.replace('AnyCPU', 'Any CPU')
			prop.configuration = self.get_config(self.ctx, self.ctx.env)
			prop.variant = self.ctx.variant
			return Variant(prop.variant, prop, self.all_projects)

	def write_files(self, sln, variants):
		self.sln = sln
		self.variants = variants
		self.all_projects = self.flatten_projects(variants)

		if Logs.verbose == 0:
			sys.stderr.write('\n')

		for p in self.all_projects:
			p.write()

		self.make_sln_configs(variants)

		# and finally write the solution file
		node = self.get_solution_node()
		node.parent.mkdir()
		Logs.warn('Creating %r' % node)
		template1 = msvs.compile_template(msvs.SOLUTION_TEMPLATE)
		sln_str = template1(self)
		sln_str = msvs.rm_blank_lines(sln_str)
		node.stealth_write(sln_str)

	def collect_dirs(self):
		"""
		Create the folder structure in the Visual studio project view
		"""
		seen = {}
		def make_parents(proj):
			# look at a project, try to make a parent
			if getattr(proj, 'parent', None):
				# aliases already have parents
				return
			x = proj.iter_path
			if x in seen:
				proj.parent = seen[x]
				return

			# There is not vsnode_vsdir for x.
			# So create a project representing the folder "x"
			n = proj.parent = seen[x] = self.vsnode_vsdir(self, msvs.make_uuid(x.abspath()), x.name)
			n.iter_path = x.parent
			self.all_projects.append(n)

			# recurse up to the project directory
			if x.height() > self.ctx.srcnode.height() + 1:
				make_parents(n)

		for p in self.all_projects[:]: # iterate over a copy of all projects
			if not getattr(p, 'tg', None):
				# but only projects that have a task generator
				continue

			# make a folder for each task generator
			path = p.tg.path.parent
			ide_path = getattr(p.tg, 'ide_path', '.')
			if os.path.isabs(ide_path):
				p.iter_path = self.path.make_node(ide_path)
			else:
				p.iter_path = path.make_node(ide_path)

			if p.iter_path.height() > self.ctx.srcnode.height():
				make_parents(p)

	def project_configurations(self):
		return map(lambda x: (x.prop.configuration, x.prop.platform_sln), self.variants.itervalues())

	def make_sln_configs(self, variants):
		for p in self.all_projects:
			if not hasattr(p, 'tg'):
				continue

			if len(variants) == len(p.build_properties):
				continue

			props = []

			for key, variant in variants.iteritems():
				other = p.proj_configs.get(key, None)

				prop = msvs.build_property()
				prop.configuration = variant.prop.configuration
				prop.platform_sln = variant.prop.platform_sln

				if other:
					prop.configuration_bld = other.configuration_bld
					prop.platform = other.platform
					prop.is_active = True
				else:
					prop.configuration_bld = p.build_properties[0].configuration_bld
					prop.platform = p.build_properties[0].platform
					prop.is_active = False

				#print '%s %s %s|%s -> %s|%s %s' % (p.name, k, prop.configuration, prop.platform_sln, prop.configuration, prop.platform, prop.is_active)

				props.append(prop)

			p.build_properties = props

	def check_conditionals(self, left, right, attr, prop):
		lhs = getattr(left, attr)
		rhs = getattr(right, attr)

		cur_keys = set(lhs.iterkeys())
		new_keys = set(rhs.iterkeys())

		in_both = new_keys & cur_keys
		from_old = cur_keys.difference(in_both)
		from_new = new_keys.difference(in_both)

		for key in from_old:
			# Items in from_old need to be removed from source_list
			# and tracked per variant
			item = lhs.pop(key)
			for other_prop in left.build_properties:
				v = getattr(other_prop, attr, [])
				v.append(item)
				setattr(other_prop, attr, v)

		for key in from_new:
			# Item's in from_new need to be tracked per variant
			v = getattr(prop, attr, [])
			v.append(rhs[key])
			setattr(prop, attr, v)

	def flatten_projects(self, variants):
		ret = OrderedDict()

		# TODO: Might need to implement conditional project refereces
		# as well as assembly references based on the selected
		# configuration/platform

		for variant in variants.itervalues():
			for p in variant.projects:
				p.ctx = self
				ret.setdefault(p.uuid, p)

				if not getattr(p, 'tg', []):
					continue

				main = ret[p.uuid]

				env = p.tg.env
				config = self.get_config(p.tg.bld, p.tg.env)

				if isinstance(p, vsnode_cs_target):
					prop = msvs.build_property()
					prop.platform_tgt = p.tg.env.CSPLATFORM
					# Solution needs 'Any CPU' not 'AnyCPU'
					prop.platform = prop.platform_tgt.replace('AnyCPU', 'Any CPU')
					prop.platform_sln = prop.platform
					prop.properties = p.properties
					prop.configuration_bld = config
					prop.sources = []

					# for MonoDevelop support
					prop.custom_cmds = map(
						lambda x: namedtuple('CustomCommand', x.keys())(*x.values()),
						getattr(p.tg, 'ide_custom_commands', [])
					)

					# for MSVS support, convert custom commands into post build events
					post_builds = []
					for x in prop.custom_cmds:
						workingdir = x.workingdir.replace('{', '(').replace('}', ')')
						if x.type == 'AfterBuild':
							post_builds.append('cd %s && %s' % (workingdir, x.command))
					prop.post_build = ' && '.join(post_builds).replace('&', '&amp;')

					# Ensure all files are accounted for
					if main != p:
						main.source_files = OrderedDict(main.source_files.items() + p.source_files.items())

						# MonoDevelop doesn't let us do conditions on a per
						# source basis. Condition only works on PropertyGroup
						# and Reference elements.
						self.check_conditionals(main, p, 'references', prop)
						self.check_conditionals(main, p, 'embeds', prop)
						for other_prop in main.build_properties:
							copyattr(other_prop, 'embeds', 'source_files')
						copyattr(prop, 'embeds', 'source_files')

				elif isinstance(p, vsnode_web_target):
					prop = msvs.build_property()
					prop.platform_tgt = env.CSPLATFORM
					prop.platform = 'Any CPU'
					prop.platform_sln = prop.platform_tgt.replace('AnyCPU', 'Any CPU')
					prop.configuration_bld = 'Debug'
				else:
					prop = p.build_properties[0]
					prop.platform_tgt = env.CSPLATFORM
					prop.platform = self.get_platform(env)
					prop.platform_sln = prop.platform_tgt.replace('AnyCPU', 'Any CPU')
					prop.configuration_bld = config

				prop.configuration = config
				prop.variant = variant.name
				prop.is_active = True

				main.proj_configs[variant.name] = prop

				if not any([ x for x in main.build_properties if x.platform == prop.platform and x.configuration == config ]):
					main.build_properties.append(prop)

		return ret.values()

	def add_aliases(self):
		pass

	def make_project(self, tg):
		features = getattr(tg, 'features', '')
		if 'fake_lib' in features or 'nuget_lib' in features:
			return None
		elif hasattr(tg, 'link_task') and self.enable_cproj:
			return self.vsnode_target(self, tg)
		elif hasattr(tg, 'cs_task') or hasattr(tg, 'tsc'):
			return self.vsnode_cs_target(self, tg)
		elif hasattr(tg, 'ide_website'):
			return self.vsnode_web_target(self, tg)
		else:
			return None

class multi_idegen(BuildContext):
	cmd = 'msvs2010'
	depth = 0
	sln_variants = {}
	is_idegen = True

	def init(self, generator):
		pass

	def execute(self):
		multi_idegen.depth += 1

		self.restore()
		if not self.all_envs:
			self.load_envs()
		self.recurse([self.run_dir])

		self.process_solutions()

		multi_idegen.depth -= 1
		if multi_idegen.depth == 0:
			for sln, variants in multi_idegen.sln_variants.iteritems():
				variants = OrderedDict(sorted(variants.iteritems(), key=lambda x: x[1].key))
				generator = self.create_generator(sln)
				generator.write_files(sln, variants)

	def create_generator(self, sln):
		generator = idegen(self)
		generator.init(sln)
		self.init(generator)
		return generator

	def process_solutions(self):
		solutions = self.collect_solutions()
		for sln, tgs in solutions.iteritems():
			generator = self.create_generator(sln)

			projects = []

			for tg in tgs:
				p = generator.make_project(tg)
				if not p:
					continue

				p.collect_source()
				p.collect_properties()
				projects.append(p)

				if Logs.verbose == 0:
					sys.stderr.write('.')
					sys.stderr.flush()

			if projects:
				generator.collect_projects(projects)
				variant = generator.add_variant()
				sln_variants = multi_idegen.sln_variants.setdefault(sln, {})
				sln_variants[variant.name] = variant

	def collect_solutions(self):
		ret = {}

		for g in self.groups:
			for tg in g:
				if not isinstance(tg, TaskGen.task_gen):
					continue

				if not getattr(tg, 'ide', True):
					continue

				if not hasattr(tg, 'msvs_includes'):
					tg.msvs_includes = tg.to_list(getattr(tg, 'includes', [])) + tg.to_list(getattr(tg, 'export_includes', []))

				tg.post()

				solutions = getattr(tg, 'solutions', [self.env.APPNAME + '.sln'])
				for sln in solutions:
					ret.setdefault(sln, []).append(tg)

		return ret

class idegen2017(multi_idegen):
	'''generates a visual studio 2012 solution'''
	cmd = 'msvs2017'
	fun = multi_idegen.fun

	def init(self, generator):
		generator.numver = '12.00'
		generator.vsver  = '2017'
		generator.platform_toolset = 'v141'
		generator.vsnode_cs_target = vsnode_cs_target2012

class idegen2012(multi_idegen):
	'''generates a visual studio 2012 solution'''
	cmd = 'msvs2012'
	fun = multi_idegen.fun

	def init(self, generator):
		generator.numver = '12.00'
		generator.vsver  = '2012'
		generator.platform_toolset = 'v110'
		generator.vsnode_cs_target = vsnode_cs_target2012

class idegen_xamarin6(multi_idegen):
	'''generates a solution for Xamarin Studio/MonoDevelop 6'''
	cmd = 'xamarin6'
	fun = multi_idegen.fun

	def init(self, generator):
		generator.numver = '12.00'
		generator.vsver  = '2012'
		generator.platform_toolset = 'v110'
		generator.vsnode_cs_target = vsnode_cs_target2012
		generator.enable_cproj = False

def options(ctx):
	"""
	If the msvs option is used, try to detect if the build is made from visual studio
	"""
	ctx.add_option('--execsolution', action='store', help='when building with visual studio, use a build state file')

	old = BuildContext.execute
	def override_build_state(ctx):
		def lock(rm, add):
			uns = ctx.options.execsolution.replace('.sln', rm)
			uns = ctx.root.make_node(uns)
			try:
				uns.delete()
			except:
				pass

			uns = ctx.options.execsolution.replace('.sln', add)
			uns = ctx.root.make_node(uns)
			try:
				uns.write('')
			except:
				pass

		if ctx.options.execsolution:
			ctx.launch_dir = Context.top_dir # force a build for the whole project (invalid cwd when called by visual studio)
			lock('.lastbuildstate', '.unsuccessfulbuild')
			old(ctx)
			lock('.unsuccessfulbuild', '.lastbuildstate')
		else:
			old(ctx)
	BuildContext.execute = override_build_state
