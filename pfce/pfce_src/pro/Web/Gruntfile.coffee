'use strict'

module.exports = (grunt) ->
	grunt.util.linefeed = '\r\n'
	
	path = require('path')
	proxy = require('grunt-connect-proxy/lib/utils').proxyRequest
	require('load-grunt-tasks')(grunt);

	variant = grunt.option('variant') || 'win_debug_x64'
	repo = '../..'
	e2e = '.e2e'
	waf_bindir = path.join(repo, 'output', variant, 'bin')
	vs_bindir = path.join(repo, '.depproj', 'bin', variant)
	peach_args = [ '--nobrowser',  '--pits', path.resolve(e2e) ]
	e2e_specs = [
		'public/js/test/e2e.js'
	]

	grunt.initConfig
		pkg: grunt.file.readJSON 'package.json'

		bowercopy:
			libs:
				files:
					'public/lib/angular'             : 'angular:main'
					'public/lib/angular-bootstrap'   : 'angular-bootstrap:main'
					'public/lib/angular-breadcrumb'  : 'angular-breadcrumb:main'
					'public/lib/angular-chart'       : 'angular-chart.js:main'
					'public/lib/angular-loading-bar' : 'angular-loading-bar:main'
					'public/lib/angular-mocks'       : 'angular-mocks:main'
					'public/lib/angular-messages'    : 'angular-messages:main'
					'public/lib/angular-sanitize'    : 'angular-sanitize:main'
					'public/lib/angular-smart-table' : 'angular-smart-table:main'
					'public/lib/angular-ui-router'   : 'angular-ui-router:main'
					'public/lib/angular-ui-select'   : 'angular-ui-select:main'
					'public/lib/angular-visjs'       : 'angular-visjs:main'
					'public/lib/jquery'              : 'jquery:main'
					'public/lib/moment'              : 'moment:main'
					'public/lib/ngstorage'           : 'ngstorage:main'

			mainless:
				options:
					destPrefix: 'public/lib'
				files:
					'angular-chart'               : 'angular-chart.js/dist/angular-chart.css.map'
					'bootstrap/css'               : 'bootstrap/dist/css/bootstrap.css*'
					'bootstrap/fonts'             : 'bootstrap/dist/fonts/*'
					'bootstrap/js'                : 'bootstrap/dist/js/bootstrap.js'
					'chartjs'                     : 'Chart.js/Chart.js'
					'fontawesome/css'             : 'fontawesome/css/font-awesome.css'
					'fontawesome/fonts'           : 'fontawesome/fonts/*'
					'lodash'                      : 'lodash/lodash.js'
					'pithy'                       : 'pithy/lib/pithy.js'
					'vis/img'                     : 'vis/dist/img/*'
					'vis/vis.css'                 : 'vis/dist/vis.css'
					'vis/vis.js'                  : 'vis/dist/vis.js'

		clean:
			app: [
				'public/js/app/*'
				'public/js/test/*'
			]
			lib: [
				'public/lib'
			]
			e2e: [
				e2e
			]

		ts:
			options:
				module: 'commonjs'
				sourceMap: true
				sourceRoot: '/ts/app'
				removeComments: false
			app:
				src: ['public/ts/app/**/*.ts']
				reference: 'public/ts/app/reference.ts'
				out: 'public/js/app/app.js'
			unit:
				src: ['public/ts/test/unit/**/*.ts']
				reference: 'public/ts/test/unit/reference.ts'
				out: 'public/js/test/unit.js'
			e2e:
				src: ['public/ts/test/e2e/**/*.ts']
				reference: 'public/ts/test/e2e/reference.ts'
				out: 'public/js/test/e2e.js'

		jasmine:
			test:
				host: 'http://localhost:9999/'
				src: [
					'public/js/test/unit.js'
				]
				options:
#					display: 'short'
#					summary: true
					vendor: [
						# ordered libraries
						'public/lib/jquery/jquery.js'
						'public/lib/chartjs/Chart.js'
						'public/lib/angular/angular.js'
						# unordered libraries
						'public/lib/**/*.js'
						# extra stuff
						'public/js/angular-vis.js'
					]

		protractor:
			options:
				configFile: 'protractor.conf.js'
			waf:
				options:
					keepAlive: false
					args:
						baseUrl: 'http://localhost:8888/'
						specs: e2e_specs
			vs:
				options:
					keepAlive: true
					args:
						baseUrl: 'http://localhost:9001/'
						specs: e2e_specs
		watch:
			ts:
				files: ['public/ts/app/**/*.ts']
				tasks: ['ts:app']
				options:
					livereload: true
			html:
				files: ['public/**/*.html', '!public/tests.html']
				options:
					livereload: true
			css:
				files: ['public/**/*.css']
				options:
					livereload: true
			unit:
				files: ['public/ts/app/**/*.ts', 'public/ts/test/unit/**/*.ts']
				tasks: ['ts:unit', 'jasmine:test']
				options:
					atBegin: true
			e2e:
				files: ['public/ts/app/**/*.ts', 'public/ts/test/e2e/**/*.ts']
				tasks: ['e2e-cycle']
				options:
					atBegin: true

		focus:
			app:
				include: ['ts', 'html', 'css']

		connect:
			options:
				hostname: 'localhost'
				port: 9000
				base: 'public'
				middleware: (connect, options) -> 
					[ proxy, connect.static(options.base[0]) ]
			proxies: [
				{context: '/p/', host: 'localhost', port: 8888}
			]
			livereload:
				options:
					livereload: true
			test:
				options:
					port: 9001
					livereload: false

		http:
			accept_eula:
				options:
					url: 'http://localhost:8888/eula'
					method: 'POST'
					form:
						accept: 'true'
			reject_eula:
				options:
					url: 'http://localhost:8888/eula'
					method: 'POST'
					form:
						accept: 'false'

		open:
			dev:
				path: 'http://localhost:<%= connect.options.port%>'
		
		copy:
			pits: 
				files: [
					{
						expand: true
						cwd: path.join(repo, 'pits', 'pro')
						src: ['Test/*']
						dest: e2e
					}
				]
			eula_vs:
				src: 'eula.config'
				dest: path.join(vs_bindir, 'Peach.exe.user.config')
			eula_waf:
				src: 'eula.config'
				dest: path.join(waf_bindir, 'Peach.exe.user.config')
				
		run:
			options:
				failOnError: true
				wait: false
				ready: /Web site running/
			waf:
				cmd: 'Peach.exe'
				args: peach_args
				options:
					cwd: waf_bindir
			vs:
				cmd: 'Peach.exe'
				args: peach_args
				options:
					cwd: vs_bindir

	grunt.registerTask 'default', ['work']

	grunt.registerTask 'init', [
		'clean'
		'bowercopy'
		'ts:app'
	]

	grunt.registerTask 'server', [
		'configureProxies'
		# 'http:accept_eula'
		'connect:livereload'
	]

	grunt.registerTask 'work', [
		'clean:app'
		'ts:app'
		'server'
		'open'
		'focus:app'
	]

	grunt.registerTask 'unit', [
		'watch:unit'
	]

	grunt.registerTask 'test', [
		'ts:unit'
		'jasmine:test'
	]

	grunt.registerTask 'prepare-e2e', [
		'ts:app'
		'ts:e2e'
		'clean:e2e'
		'copy:pits'
		'copy:eula_vs'
	]

	grunt.registerTask 'e2e-cycle', [
		'prepare-e2e'
		'run:vs'
		'protractor:vs'
		'stop:vs'
	]

	grunt.registerTask 'e2e-dev', [
		'configureProxies'
		'connect:test'
		'watch:e2e'
	]

	grunt.registerTask 'e2e', [
		'prepare-e2e'
		'run:waf'
		'protractor:waf'
		'stop:waf'
	]
