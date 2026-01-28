if not exists (select * from sysobjects where name='ResourceLinks')
    CREATE TABLE ResourceLinks (
   project_activity_id int NOT NULL,
   resource_id int NOT NULL,
   average_busy real NOT NULL
);

if exists (select * from sysobjects where name='DirRX_Projec1_ProjectsResour')
insert into ResourceLinks
select Distinct Activity, Recipient, Busy from DirRX_Projec1_ProjectsResour pr
left join ResourceLinks rl on (pr.Recipient = rl.resource_id AND pr.Activity = rl.project_activity_id)
where rl.project_activity_id IS NULL AND
pr.Activity IS NOT NULL


if not exists (select * from sysobjects where name='temp_projectplanner')
    CREATE TABLE temp_projectplanner (
    id  int NOT NULL,
   projectplan_name int NULL
);

truncate table temp_projectplanner;

if exists(select * from sysobjects where name='DirRX_Projec1_PrjctActivity')
BEGIN
INSERT INTO temp_projectplanner
SELECT id, ProjectPlan FROM DirRX_Projec1_PrjctActivity
END

if not exists (select * from sysobjects where name='temp_projectplanner_projects')
    CREATE TABLE temp_projectplanner_projects (
    id  int NOT NULL,
   projectplan_name int NULL
);

truncate table temp_projectplanner_projects;

IF COL_LENGTH('Sungero_Docflow_Project','ProjectPlanDir_Project_DirRX') IS NOT NULL
BEGIN;  
EXEC('INSERT INTO temp_projectplanner_projects SELECT id, ProjectPlanDir_Project_DirRX FROM Sungero_Docflow_Project')
END;
