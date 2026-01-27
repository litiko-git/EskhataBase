DO $$
BEGIN

IF EXISTS (SELECT 1 FROM pg_tables WHERE LOWER(tablename) = 'resourcelinks') THEN
  IF (SELECT  data_type FROM INFORMATION_SCHEMA.COLUMNS
	WHERE LOWER(table_name) = 'resourcelinks' AND LOWER(column_name) = 'project_activity_id') = 'integer' OR
	(SELECT  data_type FROM INFORMATION_SCHEMA.COLUMNS
	WHERE LOWER(table_name) = 'resourcelinks' AND LOWER(column_name) = 'resource_id') = 'integer' THEN
    ALTER TABLE ResourceLinks ALTER COLUMN project_activity_id TYPE bigint;
	  ALTER TABLE ResourceLinks ALTER COLUMN resource_id TYPE bigint;
  END IF;
END IF;

  if exists (select * from information_schema.columns where table_name = 'dirrx_projec1_prjctactivity' and column_name = 'refid') then
        update dirrx_projec1_prjctactivity
        set refid = id
        where refid is null;
  end if;
  
END$$;