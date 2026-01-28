begin

  IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE LOWER(table_name) = 'resourcelinks')
  IF (SELECT  DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
	  WHERE LOWER(table_name) = 'resourcelinks' AND LOWER(column_name) = 'project_activity_id') = 'int' OR
		(SELECT  DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
		WHERE LOWER(table_name) = 'resourcelinks' AND LOWER(column_name) = 'resource_id') = 'int'
		  BEGIN
		    ALTER TABLE ResourceLinks ALTER COLUMN project_activity_id bigint;
		    ALTER TABLE ResourceLinks ALTER COLUMN resource_id bigint;
      END
      
    if exists (select * from information_schema.columns where table_name = 'dirrx_projec1_prjctactivity' and column_name = 'refid')
    begin
        update dirrx_projec1_prjctactivity
        set refid = id
        where refid is null;
    end
    
end;    