INSERT INTO dbo.PAT_DOC_LOG
(
    DOC_LOG_ID,
    CASE_ID,    
    DOC_LOG_ORIGIN,
    LOGIN_ID,
    LOG_DATE,
    DOC_TYPE,
    DOC_NAME,
    DOC_FILE_NAME,
    DOC_FILE_DES,
    DOC_SENT_DATE,
    DOC_REC_DATE,
    CATEGORY_ID,
    DOC_EDITABLE,    
    PUBLISH_WEB,    
    IS_FAMILY     
)
VALUES
(
    @DocId, -- DOC_LOG_ID - int
    @Case_id, -- CASE_ID - int  
    1, -- DOC_LOG_ORIGIN - int
    N'SU', -- LOGIN_ID - nvarchar
    @LogDate, -- LOG_DATE - datetime
    99, -- DOC_TYPE - int
    @DocName, -- DOC_NAME - nvarchar
    @DocFileName, -- DOC_FILE_NAME - nvarchar
    @Description, -- DOC_FILE_DES - nvarchar
    @DateSent, -- DOC_SENT_DATE - datetime
    @DOC_REC_DATE, -- DOC_REC_DATE - datetime
    @Category_id, -- CATEGORY_ID - int
    0, -- DOC_EDITABLE - int    
    0, -- PUBLISH_WEB - int    
    0 -- IS_FAMILY - int       
)