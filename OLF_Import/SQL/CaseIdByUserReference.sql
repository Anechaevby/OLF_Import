﻿SELECT case_id FROM VW_CASE_NUMBER 
WHERE VW_CASE_NUMBER.OLD_CASE_ID LIKE '@UserRef%'