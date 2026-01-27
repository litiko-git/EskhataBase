CREATE TABLE IF NOT EXISTS ResourceLinks (
   project_activity_id int NOT NULL,
   resource_id int NOT NULL,
   average_busy real NOT NULL
);

do $$
begin
IF EXISTS (SELECT * FROM DirRX_Projec1_ProjectsResour)
  then
	insert into ResourceLinks
  	select Distinct Activity, Recipient, Busy from DirRX_Projec1_ProjectsResour pr
  	left join ResourceLinks rl on (pr.Recipient = rl.resource_id AND pr.Activity = rl.project_activity_id)
  	where rl.project_activity_id IS null AND
	pr.Activity is not null;
END IF;
exception
	WHEN SQLSTATE '42P01' then
	NULL;
end $$;


CREATE TABLE IF NOT EXISTS temp_projectplanner (
id  int NOT NULL,
projectplan_name int NULL
);

truncate table temp_projectplanner;

do $$
begin
	IF EXISTS (SELECT * FROM DirRX_Projec1_PrjctActivity)
  then
  INSERT INTO temp_projectplanner
  SELECT id, ProjectPlan FROM DirRX_Projec1_PrjctActivity;
END IF;
exception
	WHEN SQLSTATE '42P01' then
	NULL;
end $$;


CREATE TABLE IF NOT EXISTS temp_projectplanner_projects (
id  int NOT NULL,
projectplan_name int NULL
);

truncate table temp_projectplanner_projects;

do $$
begin
	IF EXISTS (SELECT ProjectPlanDir_Project_DirRX FROM Sungero_Docflow_Project)
  then
  INSERT INTO temp_projectplanner_projects
  SELECT id, ProjectPlanDir_Project_DirRX FROM Sungero_Docflow_Project;
END IF;
exception
	WHEN SQLSTATE '42703' then
	NULL;
	WHEN SQLSTATE '42P01' then
	NULL;
end $$;
