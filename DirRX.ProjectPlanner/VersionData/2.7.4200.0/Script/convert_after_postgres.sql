do $$
  begin
    if exists (select Lag from DirRX_Projec1_ProjectActivit)
    THEN
      update DirRX_Projec1_ProjectActivit
      set Lag = 0
      where Lag is null;
    end if;
  exception
    when SQLSTATE '42703' then
    null;
end $$;