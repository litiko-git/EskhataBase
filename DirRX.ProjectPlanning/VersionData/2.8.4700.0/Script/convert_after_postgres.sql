do $$ 
begin

  if exists (select * from information_schema.columns where table_name = 'sungero_docflow_project' and column_name = 'baselineworkty_project_dirrx') then
        update sungero_docflow_project 
        set baselineworkty_project_dirrx = 'Money'
        where discriminator = '4383f2ff-56e6-46f4-b4ef-cc17e6aeef40' and baselineworkty_project_dirrx is null;
  end if;
  
  if exists (select * from information_schema.columns where table_name = 'sungero_docflow_project' and column_name = 'statusissues_project_dirrx') then
        update sungero_docflow_project 
        set statusissues_project_dirrx = 'NotSpecified'
        where statusissues_project_dirrx is null;
  end if;
  
end $$;