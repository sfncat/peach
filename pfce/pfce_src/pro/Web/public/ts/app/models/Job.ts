﻿/// <reference path="../reference.ts" />

namespace Peach {
	export interface IJobCommands {
		stopUrl: string;
		continueUrl: string;
		pauseUrl: string;
		killUrl: string;
	}

	export interface IJobMetrics {
		[name: string]: string;
	}

	export interface IJobRequest {
		// URL to configured pit
		// /p/pits/ID
		pitUrl: string;

		seed?: number;
		rangeStart?: number;
		rangeStop?: number;

		dryRun?: boolean;
	}

	export interface IJob extends IJobRequest {
		id: string;

		jobUrl?: string;

		// The URL for getting test results
		firstNodeUrl?: string;

		// all faults generated by this job
		// "/p/jobs/0123456789AB/faults"
		faultsUrl?: string;

		// target we are fuzzing
		targetUrl?: string;

		// target configuration being used
		targetConfigUrl?: string;

		// fuzzing nodes used by job
		// "/p/jobs/0123456789AB/nodes";
		nodesUrl?: string;

		// version of peach used by job, fully qualified
		peachUrl?: string;

		//"/p/files/ID",
		reportUrl?: string;

		// grid fs url to package
		// "/p/files/...",
		packageFileUrl?: string;

		// URLs to associated metrics
		metrics?: IJobMetrics;

		// URLs used to control a running job.
		commands?: IJobCommands;

		status?: string;
		mode?: string;

		// display name for job
		// "0123456789AB"
		name?: string;

		// ":"notes from user about job, optional",
		notes?: string;

		// Set to null for now
		user?: string;

		// current iteration count
		iterationCount?: number;

		startDate?: Date;
		stopDate?: Date;

		// seconds we have been running
		runtime?: number;

		// iterations per hour
		speed?: number;

		// total number of faults
		faultCount?: number;

		// Always 1
		nodeCount?: number;

		// Set to 127.0.0.1
		ipAddress?: string;

		// Empty list
		tags?: ITag[];

		groups?: any[];

		result?: string;
	}

	export var JobStatus = {
		// client-side states
		StartPending    : '',
		PausePending    : '',
		ContinuePending : '',
		StopPending     : '',
		KillPending     : '',

		// server-side states
		Stopped  : '',
		Starting : '',
		Running  : '',
		Paused   : '',
		Stopping : ''
	}
	MakeLowerEnum(JobStatus);

	export var JobMode = {
		Preparing   : '',
		Fuzzing     : '',
		Searching   : '',
		Reproducing : '',
		Reporting   : ''
	};
	MakeLowerEnum(JobMode);
}
