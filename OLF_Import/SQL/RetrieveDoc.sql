SELECT pat_doc_log.Doc_File_Name,
  pat_doc_log.Doc_Rec_Date,
  pat_doc_log.Doc_File_Des,
   cast( CASE_TYPE_ID as varchar) + '\' + cast(PAT_CASE.CASE_NUMBER as varchar)+ '\' + STATE_ID+ '\' + CASE_NUMBER_EXTENSION AS Doc_Path
FROM pat_doc_log
     JOIN vw_case_number ON vw_case_number.case_id = pat_doc_log.case_id
 join PAT_CASE on PAT_CASE.case_id = vw_case_number.case_id
WHERE vw_case_number.old_case_id LIKE '@MatterId%' --'BV17033BEEP'
      AND doc_type = 4 -- < PDF type
      AND pat_doc_log.CATEGORY_ID = @Category_Id
	  AND (doc_file_des LIKE '%validation%' OR doc_file_des LIKE '%2544%');