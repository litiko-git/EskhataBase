

DO $$
DECLARE
  activity_task uuid := '1dfb1055-aaf3-4722-8b6f-9d85c64b32aa';
  activity_assignment uuid := 'ca40bc93-e562-4a4f-9db0-23950fd61584';
  activity_review_assignment uuid := '9874745c-b061-43bb-9378-013b60e551fb';
  activity_notice uuid := '0e15f86c-ab7f-4466-8b49-08773985ecaa';
  project_plan uuid := 'a319de81-7897-43e0-8f52-3eefbc011f91';
BEGIN

  UPDATE sungero_content_edoc
  SET isenableautose_projec1_dirrx = false
  WHERE discriminator = project_plan AND isenableautose_projec1_dirrx IS NULL;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='convert_task_responsibles')
  THEN
    BEGIN
      UPDATE sungero_wf_task
      SET Responsible_Projec1_DirRX = (SELECT tmp.Responsible FROM convert_task_responsibles tmp WHERE tmp.Task = sungero_wf_task.id)
      WHERE discriminator = activity_task
      AND sungero_wf_task.id IN (SELECT tmp.Task FROM convert_task_responsibles tmp);

      DROP TABLE convert_task_responsibles;
    END;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='convert_activity_task_temp')
  THEN
    BEGIN
      UPDATE sungero_wf_task
      SET activityrefid_projec1_dirrx = (SELECT tmp.ref_id FROM convert_activity_task_temp tmp WHERE tmp.task_id = sungero_wf_task.id AND tmp.task_type = sungero_wf_task.discriminator)
      WHERE discriminator = activity_task
      AND sungero_wf_task.id IN (SELECT tmp.task_id FROM convert_activity_task_temp tmp WHERE tmp.task_type = activity_task);
      
      UPDATE sungero_wf_assignment
      SET
      activityrefid_projec1_dirrx =
        (SELECT tmp.ref_id
         FROM convert_activity_task_temp tmp
         WHERE tmp.task_id = sungero_wf_assignment.id
         AND tmp.task_type = sungero_wf_assignment.discriminator
         AND sungero_wf_assignment.discriminator = activity_assignment),
      activityrefid1_projec1_dirrx =
        (SELECT tmp.ref_id
         FROM convert_activity_task_temp tmp
         WHERE tmp.task_id = sungero_wf_assignment.id
         AND tmp.task_type = sungero_wf_assignment.discriminator
         AND sungero_wf_assignment.discriminator = activity_review_assignment),
      activityrefid2_projec1_dirrx =
        (SELECT tmp.ref_id
         FROM convert_activity_task_temp tmp
         WHERE tmp.task_id = sungero_wf_assignment.id
         AND tmp.task_type = sungero_wf_assignment.discriminator
         AND sungero_wf_assignment.discriminator = activity_notice)
      WHERE sungero_wf_assignment.id IN (SELECT tmp.task_id FROM convert_activity_task_temp tmp WHERE tmp.task_type <> activity_task);

      DROP TABLE convert_activity_task_temp;
    END;
  END IF;
  
  WITH greatest_version_activity AS
  (
  SELECT
    actualworkload
    , factualcosts
    , executionperce
    , refid
    , id
    , projectplan
  FROM dirrx_projec1_prjctactivity base where numberversion = (SELECT max(numberversion) FROM dirrx_projec1_prjctactivity WHERE refid = base.refid AND projectplan = base.projectplan)
  )
  UPDATE sungero_wf_task
  SET actualworkload_projec1_dirrx = (SELECT actualworkload FROM greatest_version_activity WHERE refid = sungero_wf_task.activityrefid_projec1_dirrx and projectplan = sungero_wf_task.projectplan_projec1_dirrx),
      factualcosts_projec1_dirrx = (SELECT factualcosts FROM greatest_version_activity WHERE refid = sungero_wf_task.activityrefid_projec1_dirrx and projectplan = sungero_wf_task.projectplan_projec1_dirrx),
      executionperce_projec1_dirrx = (SELECT executionperce FROM greatest_version_activity WHERE refid = sungero_wf_task.activityrefid_projec1_dirrx and projectplan = sungero_wf_task.projectplan_projec1_dirrx)
  WHERE discriminator = activity_task;
  
  UPDATE sungero_wf_assignment
  SET actualworkload_projec1_dirrx = (SELECT actualworkload_projec1_dirrx FROM sungero_wf_task t WHERE t.id = sungero_wf_assignment.task),
      factualcosts_projec1_dirrx = (SELECT factualcosts_projec1_dirrx FROM sungero_wf_task t WHERE t.id = sungero_wf_assignment.task),
      executionperce_projec1_dirrx = (SELECT executionperce_projec1_dirrx FROM sungero_wf_task t WHERE t.id = sungero_wf_assignment.task)
  WHERE discriminator = activity_assignment;
END $$;