namespace Peach.Pro.Core.Storage
{
	class Sql
	{
		public const string GetLastRowId = "SELECT last_insert_rowid();";

		public const string InsertNinjaSample = @"
INSERT INTO Sample (
	File, 
	Hash
) VALUES (
	@File,
	@Hash
);" + GetLastRowId;

		public const string InsertNinjaElement = @"
INSERT INTO Element (
	Name
) VALUES (
	@Name
);" + GetLastRowId;

		public const string InsertNinjaSampleElement = @"
INSERT INTO SampleElement (
	SampleId, 
	ElementId, 
	Data
) VALUES (
	@SampleId,
	@ElementId,
	@Data
);" + GetLastRowId;

		public const string DeleteNinjaSample = @"
DELETE FROM SampleElement
WHERE SampleId = @SampleId;

DELETE FROM Sample
WHERE SampleId = @SampleId;
";

		public const string SelectNinjaSample = @"
SELECT * 
FROM Sample 
WHERE File = @File
";

		public const string SelectNinjaElement = @"
SELECT ElementId
FROM Element
WHERE Name = @Name
";

		public const string SelectNinjaElementCount = @"
SELECT
	COUNT(*) as Count,
	e.ElementId
FROM
	Element e,
	SampleElement se
WHERE
	se.ElementId = e.ElementId AND
	e.Name = @Name
GROUP BY
	se.ElementId
";

		public const string SelectNinjaData = @"
SELECT Data 
FROM SampleElement
WHERE ElementId = @ElementId
LIMIT 1 OFFSET @Offset 
";
		
		public const string InsertJobLog = @"
INSERT INTO [JobLog] (
	JobId,
	Message
) VALUES (
	@JobId,
	@Message
);";

		public const string SelectTestEvents = @"
SELECT * 
FROM [TestEvent] 
WHERE JobId = @JobId;
";

		public const string SelectJobLogs = @"
SELECT * 
FROM [JobLog] 
WHERE JobId = @Id;
";

		public const string SelectJob = @"
SELECT * 
FROM [Job] 
WHERE Id = @Id;
";

		public const string InsertJob = @"
INSERT INTO [Job] (
	Id,
	Status,
	Mode,
	PitFile,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	HeartBeat,
	Pid,
	Runtime,
	FaultCount,
	RangeStart,
	RangeStop,
	Duration,
	DryRun,
	PitUrl,
	LogPath,
	MetricKind,
	PeachVersion
) VALUES (
	@Id,
	@Status,
	@Mode,
	@PitFile,
	@Result,
	@Notes,
	@User,
	@Seed,
	@IterationCount,
	@StartDate,
	@StopDate,
	@HeartBeat,
	@Pid,
	@Runtime,
	@FaultCount,
	@RangeStart,
	@RangeStop,
	@Duration,
	@DryRun,
	@PitUrl,
	@LogPath,
	@MetricKind,
	@PeachVersion
);";

		public const string UpdateRunningJob = @"
UPDATE [Job]
SET 
	IterationCount = @IterationCount,
	FaultCount = @FaultCount,
	Status = @Status,
	Mode = @Mode,
	Runtime = @Runtime,
	HeartBeat = @HeartBeat
WHERE
	Id = @Id
";

		public const string UpdateJob = @"
UPDATE [Job]
SET 
	Status = @Status,
	Mode = @Mode,
	PitFile = @PitFile,
	Result = @Result,
	Notes = @Notes,
	User = @User,
	Seed = @Seed,
	IterationCount = @IterationCount,
	StartDate = @StartDate,
	StopDate = @StopDate,
	HeartBeat = @HeartBeat,
	Pid = @Pid,
	Runtime = @Runtime,
	FaultCount = @FaultCount,
	RangeStart = @RangeStart,
	RangeStop = @RangeStop,
	Duration = @Duration,
	LogPath = @LogPath,
	MetricKind = @MetricKind,
	PeachVersion = @PeachVersion
WHERE
	Id = @Id
;";

		public const string DeleteJob = @"
DELETE FROM [TestEvent]
WHERE JobId = @Id;

DELETE FROM [JobLog]
WHERE JobId = @Id;

DELETE FROM [Job]
WHERE Id = @Id;
";

		public const string InsertTestEvent = @"
INSERT INTO TestEvent (
	JobId,
	Status, 
	Short, 
	Description,
	Resolve
) VALUES (
	@JobId,
	@Status, 
	@Short, 
	@Description,
	@Resolve
);" + GetLastRowId;

		public const string PassPendingTestEvents = @"
UPDATE TestEvent
SET
	Status = 1
WHERE
	Status = 0 AND
	JobId = @JobId 
;";

		public const string	UpdateTestEvent = @"
UPDATE TestEvent
SET
	Status = @Status,
	Short = @Short,
	Description = @Description,
	Resolve = @Resolve
WHERE
	Id = @Id
;";
		
		public const string UpsertMutation = @"
INSERT OR REPLACE INTO Mutation (
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId,
	Kind,
	IterationCount
) VALUES (
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId,
	@Kind,
	COALESCE((
		SELECT IterationCount + 1
		FROM Mutation
		WHERE
			StateId = @StateId AND
			ActionId = @ActionId AND
			ParameterId = @ParameterId AND
			ElementId = @ElementId AND
			MutatorId = @MutatorId AND
			DatasetId = @DatasetId AND
			Kind = @Kind
	), 1)
);";

		public const string UpsertState = @"
INSERT OR REPLACE INTO [State] (
	Id, 
	NameId, 
	RunCount,
	Count
) VALUES (
	@Id, 
	@NameId, 
	@RunCount,
	@Count
);";

		public const string InsertFaultMetric = @"
INSERT INTO FaultMetric (
	Iteration,
	MajorHash,
	MinorHash,
	Timestamp,
	Hour,
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId,
	FaultDetailId,
	Kind
) VALUES (
	@Iteration,
	@MajorHash,
	@MinorHash,
	@Timestamp,
	@Hour,
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId,
	@FaultDetailId,
	@Kind
);";

		public const string InsertFaultDetail = @"
INSERT INTO FaultDetail (
	Reproducible,
	Iteration,
	TimeStamp,
	Source,
	Exploitability,
	MajorHash,
	MinorHash,
	Title,
	Description,
	Seed,
	IterationStart,
	IterationStop,
	Flags,
	FaultPath
) VALUES (
	@Reproducible,
	@Iteration,
	@TimeStamp,
	@Source,
	@Exploitability,
	@MajorHash,
	@MinorHash,
	@Title,
	@Description,
	@Seed,
	@IterationStart,
	@IterationStop,
	@Flags,
	@FaultPath
);" + GetLastRowId;

		public const string InsertFaultFile = @"
INSERT INTO FaultFile (
	FaultDetailId,
	Name,
	FullName,
	Initial,
	Type,
	AgentName,
	MonitorName,
	MonitorClass,
	Size
) VALUES (
	@FaultDetailId,
	@Name,
	@FullName,
	@Initial,
	@Type,
	@AgentName,
	@MonitorName,
	@MonitorClass,
	@Size
);" + GetLastRowId;

		public const string SelectFaultDetailById = @"
SELECT * 
FROM FaultDetail 
WHERE Id = @Id;
";

		public const string SelectFaultFilesByFaultId = @"
SELECT * 
FROM FaultFile 
WHERE FaultDetailId = @Id;
";

		public const string SelectFaultFilesById = @"
SELECT * 
FROM FaultFile 
WHERE Id = @Id;
";

		public const string SelectMutationByIterationAndKind = @"
SELECT * 
FROM ViewFaults 
WHERE Iteration = @Iteration AND Kind = @Kind;
";

		public const string SelectMutationByFaultIdAndKind = @"
SELECT * 
FROM ViewFaults
WHERE FaultDetailId = @Id AND Kind = @Kind;
";

		public const string InsertNames = @"
INSERT INTO NamedItem (
	Id, 
	Name
) VALUES (
	@Id, 
	@Name
);
";

		public const string UpdateStates = @"
UPDATE State 
SET Count = @Count 
WHERE Id = @Id;
";

		public const string SelectBucketCount = @"
SELECT COUNT(*)
FROM ViewBucketDetails;
";

		public const string JobMigrateV1 = @"
ALTER TABLE FaultDetail 
ADD COLUMN 
	Flags INTEGER NOT NULL DEFAULT 0
;
";

		public const string JobMigrateV2 = @"
DROP TABLE Job;
";

		public const string JobMigrateV3 = @"
Alter TABLE Mutation
ADD COLUMN
	Kind INTEGER NOT NULL DEFAULT 0
;

Alter TABLE FaultMetric
ADD COLUMN
	Kind INTEGER NOT NULL DEFAULT 0
;

Alter TABLE FaultMetric
ADD COLUMN
	FaultDetailId INTEGER
;

UPDATE FaultMetric
SET FaultDetailId = (
	SELECT Id
	FROM FaultDetail
	WHERE Iteration = FaultMetric.Iteration
);
";

		public const string JobMigrateV4 = @"
Alter TABLE FaultFile
ADD COLUMN
	Type INTEGER NOT NULL DEFAULT 0
;

Alter TABLE FaultFile
ADD COLUMN
	Initial INTEGER NOT NULL DEFAULT 0
;

Alter TABLE FaultFile
ADD COLUMN
	AgentName TEXT
;

Alter TABLE FaultFile
ADD COLUMN
	MonitorName TEXT
;

Alter TABLE FaultFile
ADD COLUMN
	MonitorClass TEXT
;
";

		public const string NodeMigrateV1 = @"
PRAGMA foreign_keys=OFF;

BEGIN TRANSACTION;

ALTER TABLE Job
RENAME TO tmp_Job;

CREATE TABLE Job (
	Id TEXT NOT NULL,
	LogPath TEXT,
	Status INTEGER NOT NULL,
	Mode INTEGER NOT NULL,
	PitFile TEXT,
	Result TEXT,
	Notes TEXT,
	User TEXT,
	IterationCount INTEGER NOT NULL,
	StartDate DATETIME NOT NULL,
	StopDate DATETIME,
	Runtime INTEGER NOT NULL,
	FaultCount INTEGER NOT NULL,
	Pid INTEGER NOT NULL,
	HeartBeat DATETIME,
	PeachVersion TEXT,
	PitUrl TEXT,
	Seed INTEGER,
	RangeStart INTEGER NOT NULL,
	RangeStop INTEGER,
	DryRun INTEGER NOT NULL,
	PRIMARY KEY (Id)
);

INSERT INTO Job (
	Id,
	Status,
	Mode,
	PitFile,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	HeartBeat,
	Pid,
	Runtime,
	FaultCount,
	RangeStart,
	RangeStop,
	DryRun,
	PitUrl,
	LogPath,
	PeachVersion
)
SELECT
	Id,
	Status,
	Mode,
	PitFile,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	HeartBeat,
	Pid,
	Runtime,
	FaultCount,
	RangeStart,
	RangeStop,
	IsControlIteration,
	PitUrl,
	LogPath,
	PeachVersion
FROM tmp_Job;

DROP TABLE tmp_Job;

PRAGMA foreign_key_check;

COMMIT;

PRAGMA foreign_keys=ON;
";

		public const string NodeMigrateV2 = @"
ALTER TABLE Job 
ADD COLUMN 
	Duration INTEGER
;";

		public const string NodeMigrateV3 = @"
ALTER TABLE Job 
ADD COLUMN 
	MetricKind INTEGER NOT NULL DEFAULT 0
;";
	}
}
