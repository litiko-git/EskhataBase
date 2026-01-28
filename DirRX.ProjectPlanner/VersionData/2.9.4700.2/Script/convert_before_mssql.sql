DECLARE @plan AS VARCHAR(100)='A319DE81-7897-43E0-8F52-3EEFBC011F91';
DECLARE @activity_task AS VARCHAR(100)='1dfb1055-aaf3-4722-8b6f-9d85c64b32aa';
DECLARE @activity_assignment AS VARCHAR(100)='ca40bc93-e562-4a4f-9db0-23950fd61584';
DECLARE @activity_review_assignment AS VARCHAR(100)='9874745c-b061-43bb-9378-013b60e551fb';
DECLARE @activity_notice AS VARCHAR(100)='0e15f86c-ab7f-4466-8b49-08773985ecaa';

IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DirRX_Projec1_ProjectActivi2'
  AND NOT column_name = 'Responsible_Projec1_DirRX')
BEGIN
  EXEC('
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = ''convert_task_responsibles'')
      DROP TABLE convert_task_responsibles
  ');

  CREATE TABLE convert_task_responsibles
  (
    Task bigint,
    Responsible bigint
  );

  INSERT convert_task_responsibles(ResponsibleLinks.Task, ResponsibleLinks.Responsible)
  SELECT ResponsibleLinks.Task, MIN(ResponsibleLinks.Responsible)
  FROM DirRX_Projec1_ProjectActivi2 ResponsibleLinks
  INNER JOIN Sungero_WF_Task Tasks
    ON ResponsibleLinks.Task = Tasks.Id
    AND Tasks.Discriminator = @activity_task
  GROUP BY ResponsibleLinks.Task;
END;


IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sungero_content_edoc' AND column_name = 'stage1_projec1_dirrx')
BEGIN
  EXEC('
    UPDATE sungero_content_edoc 
    SET lifecyclestate_docflow_sungero = (
      CASE
        WHEN (stage1_projec1_dirrx IN (''Initiation'', ''Planning'')) THEN ''Draft''
        WHEN (stage1_projec1_dirrx IN (''Execution'', ''Completion'', ''Completed'')) THEN ''Active''
        ELSE null
      END
    )
    WHERE discriminator=''' + @plan + ''';
  ');
END;

-- Если поле есть в sungero_wf_task, конвертации не было
IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sungero_wf_task' AND column_name = 'projectactivit_projec1_dirrx')
BEGIN
  --mssql не умеет компилировать скрипты, в которых идет обращение к несуществующим колонкам, даже если они обернуты в if exists. поэтому динамический код
  EXEC('
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = ''convert_activity_task_temp'')
      DROP TABLE convert_activity_task_temp
  ');
  
  CREATE TABLE convert_activity_task_temp
  (
    id bigint IDENTITY(1,1) PRIMARY KEY,
    task_id bigint,
    task_type uniqueidentifier,
    ref_id bigint
  );

  -- Заполняем временную таблицу значениями задач.
  EXEC('
    INSERT convert_activity_task_temp(task_id, task_type, ref_id)
    SELECT t.id, t.discriminator, a.refid
    FROM sungero_wf_task t join dirrx_projec1_prjctactivity a on t.projectactivit_projec1_dirrx = a.id
    WHERE t.discriminator = ''' + @activity_task + ''';
  ');
  
  -- Поля добавлялись все в разное время, на всякий проверяю, что они существуют
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sungero_wf_assignment' AND column_name = 'projectactivit_projec1_dirrx')
  BEGIN
    EXEC('
      INSERT convert_activity_task_temp(task_id, task_type, ref_id)
      SELECT t.id, t.discriminator, a.refid
      FROM sungero_wf_assignment t join dirrx_projec1_prjctactivity a on t.projectactivit_projec1_dirrx = a.id
      WHERE t.discriminator = ''' + @activity_assignment + ''';
    ');
  END;
  
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sungero_wf_assignment' AND column_name = 'projectactivi2_projec1_dirrx')
  BEGIN
    EXEC('
      INSERT convert_activity_task_temp(task_id, task_type, ref_id)
      SELECT t.id, t.discriminator, a.refid
      FROM sungero_wf_assignment t join dirrx_projec1_prjctactivity a on t.projectactivi2_projec1_dirrx = a.id
      WHERE t.discriminator = ''' + @activity_review_assignment + ''';
    ');
  END;
  
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sungero_wf_assignment' AND column_name = 'projectactivi1_projec1_dirrx')
  BEGIN
    EXEC('
      INSERT convert_activity_task_temp(task_id, task_type, ref_id)
      SELECT t.id, t.discriminator, a.refid
      FROM sungero_wf_assignment t join dirrx_projec1_prjctactivity a on t.projectactivi1_projec1_dirrx = a.id
      WHERE t.discriminator = ''' + @activity_notice + ''';
    ');
  END;
END;

UPDATE dirrx_projec1_prjctactivity SET status = 'Closed' WHERE status IN ('Deferred', 'Completed');
