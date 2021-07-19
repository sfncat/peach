-- States >>>
DROP VIEW IF EXISTS ViewStates;
CREATE VIEW ViewStates AS 
SELECT
	CASE WHEN s.RunCount = 0 THEN
		-- If RunCount is 0, it is Human (All runs collapse into single entry)
		1
	ELSE
		0
	END AS Kind,
	CASE WHEN s.RunCount = 0 THEN
		n.Name
	ELSE
		n.Name || '_' || s.RunCount
	END AS [State],
	s.[Count] AS [ExecutionCount]
FROM [State] AS s
JOIN NamedItem n ON s.NameId = n.Id
WHERE
	length(n.Name) > 0
ORDER BY
	ExecutionCount DESC,
	[State]
;

-- States <<<

-- Iterations >>>
DROP VIEW IF EXISTS ViewIterations;
CREATE VIEW ViewIterations AS 
SELECT
	x.Kind,
	CASE WHEN x.Kind = 0 THEN
		sn.Name || '_' || s.RunCount
	ELSE
		sn.Name
	END AS [State],
	a.Name AS [Action],
	p.Name AS Parameter,
	e.Name AS Element,
	m.Name AS Mutator,
	d.Name AS Dataset,
	x.IterationCount
FROM Mutation  AS x
JOIN [State]   AS s  ON s.Id  = x.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS a  ON a.Id  = x.ActionId
JOIN NamedItem AS p  ON p.Id  = x.ParameterId
JOIN NamedItem AS e  ON e.Id  = x.ElementId
JOIN NamedItem AS m  ON m.Id  = x.MutatorId
JOIN NamedItem AS d  ON d.Id  = x.DatasetId
;

-- Iterations <<<

-- Faults >>>
DROP VIEW IF EXISTS ViewFaults;
CREATE VIEW ViewFaults AS 
SELECT
	x.Kind,
	CASE WHEN x.Kind = 0 THEN
		sn.Name || '_' || s.RunCount
	ELSE
		sn.Name
	END AS [State],
	a.Name AS [Action],
	CASE WHEN LENGTH(p.Name) > 0 THEN
		p.Name || '.' || 
		e.Name
	ELSE
		e.Name
	END AS [Element],
	m.Name AS Mutator,
	d.Name AS Dataset,
	x.FaultDetailId,
	x.Iteration
FROM FaultMetric  AS x
JOIN [State]   AS s  ON s.Id  = x.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS a  ON a.Id  = x.ActionId
JOIN NamedItem AS p  ON p.Id  = x.ParameterId
JOIN NamedItem AS e  ON e.Id  = x.ElementId
JOIN NamedItem AS m  ON m.Id  = x.MutatorId
JOIN NamedItem AS d  ON d.Id  = x.DatasetId
;

-- Faults <<<

-- Buckets >>>
DROP VIEW IF EXISTS ViewBuckets;
CREATE VIEW ViewBuckets AS
SELECT
	x.MajorHash || '_' || x.MinorHash AS Bucket,
	m.Name AS Mutator,
	CASE WHEN x.Kind = 0 THEN
		CASE WHEN LENGTH(p.Name) > 0 THEN
			sn.Name || '_' || s.RunCount || '.' || 
			a.Name || '.' || 
			p.Name || '.' || 
			e.Name
		ELSE
			sn.Name || '_' || s.RunCount || '.' || 
			a.Name || '.' || 
			e.Name
		END
	ELSE
		CASE WHEN LENGTH(sn.Name) > 0 THEN
			CASE WHEN LENGTH(a.Name) > 0 THEN
				CASE WHEN LENGTH(e.Name) > 0 THEN
					sn.Name || '.' || a.Name || '.' || e.Name
				ELSE
					sn.Name || '.' || a.Name
				END
			ELSE
				CASE WHEN LENGTH(e.Name) > 0 THEN
					sn.Name || '.' || e.Name
				ELSE
					sn.Name
				END
			END
		ELSE
			CASE WHEN LENGTH(a.Name) > 0 THEN
				CASE WHEN LENGTH(e.Name) > 0 THEN
					a.Name || '.' || e.Name
				ELSE
					a.Name
				END
			ELSE
				e.Name
			END
		END
	END AS Element,
	(
		SELECT 
			SUM(u.IterationCount)
		FROM 
			Mutation AS u 
		WHERE 
			u.StateId     = y.StateId AND
			u.ActionId    = y.ActionId AND
			u.ParameterId = y.ParameterId AND
			u.ElementId   = y.ElementId AND
			u.MutatorId   = y.MutatorId AND
			u.Kind        = y.Kind
	) as IterationCount,
	x.Kind,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
JOIN Mutation AS y ON 
	x.StateId     = y.StateId AND
	x.ActionId    = y.ActionId AND
	x.ParameterId = y.ParameterId AND
	x.ElementId   = y.ElementId AND
	x.MutatorId   = y.MutatorId AND
	x.DatasetId   = y.DatasetId AND
	x.Kind        = y.Kind
JOIN [State]   AS s  ON s.Id  = x.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS a  ON a.Id  = x.ActionId
JOIN NamedItem AS p  ON p.Id  = x.ParameterId
JOIN NamedItem AS e  ON e.Id  = x.ElementId
JOIN NamedItem AS m  ON m.Id  = x.MutatorId
JOIN NamedItem AS d  ON d.Id  = x.DatasetId
GROUP BY 
	x.MajorHash,
	x.MinorHash,
	x.MutatorId,
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.ElementId,
	x.Kind
ORDER BY
	FaultCount DESC,
	IterationCount DESC,
	Mutator,
	Bucket,
	Element
;

-- Buckets <<<

-- Bucket Details >>>
DROP VIEW IF EXISTS ViewBucketDetails;
CREATE VIEW ViewBucketDetails AS
SELECT
	COUNT(*) as FaultCount,
	MIN(Iteration) as Iteration,
	*
FROM FaultDetail
GROUP BY
	MajorHash,
	MinorHash
ORDER BY 
	FaultCount DESC,
	MajorHash,
	MinorHash,
	Exploitability
;
-- Bucket Details <<<

-- BucketTimeline >>>
DROP VIEW IF EXISTS ViewBucketTimeline;
CREATE VIEW ViewBucketTimeline AS
SELECT
	x.MajorHash || '_' || x.MinorHash AS Label,
	MIN(x.[Iteration]) AS [Iteration],
	MIN(x.[Timestamp]) AS [Time],
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
GROUP BY
	x.MajorHash,
	x.MinorHash
;
-- BucketTimeline <<<

-- FaultTimeline >>>
DROP VIEW IF EXISTS ViewFaultTimeline;
CREATE VIEW ViewFaultTimeline AS
SELECT
	x.[Timestamp] AS [Date],
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric x
GROUP BY x.[Hour];
-- FaultTimeline <<<

-- Mutators >>>
DROP VIEW IF EXISTS ViewDistinctElements;
CREATE VIEW ViewDistinctElements AS
SELECT DISTINCT
	MutatorId,
	StateId,
	ActionId,
	ParameterId,
	ElementId
FROM Mutation
WHERE Kind = 0;

DROP VIEW IF EXISTS ViewMutatorsByElement;
CREATE VIEW ViewMutatorsByElement AS
SELECT 
	MutatorId,
	COUNT(*) AS ElementCount
FROM ViewDistinctElements
GROUP BY MutatorId;

DROP VIEW IF EXISTS ViewMutatorsByIteration;
CREATE VIEW ViewMutatorsByIteration AS
SELECT 
	vme.MutatorId,
	vme.ElementCount,
	SUM(x.IterationCount) AS IterationCount
FROM ViewMutatorsByElement AS vme
JOIN Mutation AS x ON vme.MutatorId = x.MutatorId AND x.Kind = 0
GROUP BY x.MutatorId;

DROP VIEW IF EXISTS ViewMutatorsByFault;
CREATE VIEW ViewMutatorsByFault AS
SELECT 
	x.MutatorId,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
WHERE x.Kind = 0
GROUP BY x.MutatorId;
	
DROP VIEW IF EXISTS ViewMutators;
CREATE VIEW ViewMutators AS
SELECT
	n.Name AS Mutator,
	vmi.ElementCount,
	vmi.IterationCount,
	vmf.BucketCount,
	vmf.FaultCount
FROM ViewMutatorsByIteration AS vmi
LEFT JOIN ViewMutatorsByFault AS vmf ON vmi.MutatorId = vmf.MutatorId
JOIN NamedItem AS n ON vmi.MutatorId = n.Id
ORDER BY
	BucketCount DESC,
	FaultCount DESC,
	IterationCount DESC,
	ElementCount DESC,
	Mutator
;
-- Mutators <<<

-- Elements >>>
DROP VIEW IF EXISTS ViewElementsByIteration;
CREATE VIEW ViewElementsByIteration AS
SELECT
	x.StateId,
	x.Actionid,
	x.ParameterId,
	x.ElementId,
	x.Kind,
	SUM(x.IterationCount) AS IterationCount
FROM Mutation AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.ElementId,
	x.Kind
;

DROP VIEW IF EXISTS ViewElementsByFault;
CREATE VIEW ViewElementsByFault AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.ElementId,
	x.Kind,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount
FROM FaultMetric AS x
GROUP BY
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.ElementId,
	x.Kind
;

DROP VIEW IF EXISTS ViewElements;
CREATE VIEW ViewElements AS
SELECT 
	CASE WHEN vei.Kind = 0 THEN
		sn.Name || '_' || s.RunCount
	ELSE
		sn.Name
	END AS [State],
	a.Name as [Action],
	CASE WHEN LENGTH(p.Name) > 0 THEN
		p.Name || '.' || 
		e.Name
	ELSE
		e.Name
	END AS [Element],
	vei.IterationCount,
	vef.BucketCount,
	vef.FaultCount,
	vei.Kind
FROM ViewElementsByIteration AS vei
LEFT JOIN ViewElementsByFault AS vef ON
	vei.ElementId   = vef.ElementId AND 
	vei.StateId     = vef.StateId AND 
	vei.ActionId    = vef.ActionId AND 
	vei.ParameterId = vef.ParameterId AND
	vei.Kind        = vef.Kind
JOIN [State]   AS s  ON s.Id  = vei.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS e  ON e.Id  = vei.ElementId
JOIN NamedItem AS a  ON a.Id  = vei.ActionId
JOIN NamedItem AS p  ON p.Id  = vei.ParameterId
ORDER BY
	BucketCount DESC,
	FaultCount DESC,
	IterationCount DESC,
	[State],
	[Action],
	[Element]
;

-- Elements <<<


-- Datasets >>>
DROP VIEW IF EXISTS ViewDatasetsByIteration;
CREATE VIEW ViewDatasetsByIteration AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.Kind,
	SUM(x.IterationCount) AS IterationCount
FROM Mutation AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.Kind
;

DROP VIEW IF EXISTS ViewDatasetsByFault;
CREATE VIEW ViewDatasetsByFault AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.Kind,
	COUNT(DISTINCT(x.MajorHash)) as BucketCount,
	COUNT(DISTINCT(x.Iteration)) as FaultCount
FROM FaultMetric AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.Kind
;

DROP VIEW IF EXISTS ViewDatasets;
CREATE VIEW ViewDatasets AS
SELECT
	vdi.Kind AS Kind,
	CASE WHEN vdi.Kind = 0 THEN
		CASE WHEN length(p.Name) > 0 THEN
			sn.Name || '.' || a.Name || '.' || p.Name || '/' || d.Name
		ELSE
			sn.Name || '.' || a.Name || '/' || d.Name
		END
	ELSE
		--- Don't have to worry about not having a data set name
		--- since its part of the WHERE below. Can also ignore
		--- ParameterId since there is no field id on parameters
		CASE WHEN LENGTH(sn.Name) > 0 THEN
			CASE WHEN LENGTH(a.Name) > 0 THEN
				sn.Name || '.' || a.Name || '.' || d.Name
			ELSE
				sn.Name || '.' || d.Name
			END
		ELSE
			CASE WHEN LENGTH(a.Name) > 0 THEN
				a.Name || '.' || d.Name
			ELSE
				d.Name
			END
		END
	END AS Dataset,
	SUM(vdi.IterationCount) as IterationCount,
	SUM(vdf.BucketCount) as BucketCount,
	SUM(vdf.FaultCount) as FaultCount
FROM ViewDatasetsByIteration AS vdi
LEFT JOIN ViewDatasetsByFault as vdf ON 
	vdi.StateId = vdf.StateId AND
	vdi.ActionId = vdf.ActionId AND
	vdi.ParameterId = vdf.ParameterId AND
	vdi.DatasetId = vdf.DatasetId AND
	vdi.Kind = vdf.Kind
JOIN [State] AS s ON vdi.StateId = s.Id
JOIN NamedItem AS sn ON s.NameId = sn.Id
JOIN NamedItem AS a ON vdi.ActionId = a.Id
JOIN NamedItem AS p ON vdi.ParameterId = p.Id
JOIN NamedItem AS d ON vdi.DatasetId = d.Id
WHERE
	length(d.name) > 0
GROUP BY
	s.NameId,
	vdi.ActionId,
	vdi.ParameterId,
	vdi.DatasetId
ORDER BY
	BucketCount DESC,
	FaultCount DESC,
	IterationCount DESC,
	Dataset
;

-- Datasets <<<
