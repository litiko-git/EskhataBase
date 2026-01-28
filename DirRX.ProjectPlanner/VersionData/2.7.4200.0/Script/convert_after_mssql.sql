IF COL_LENGTH('DirRX_Projec1_ProjectActivit','Lag') IS NOT NULL
  BEGIN
    UPDATE DirRX_Projec1_ProjectActivit
    SET Lag = 0
    WHERE Lag IS NULL;
  END