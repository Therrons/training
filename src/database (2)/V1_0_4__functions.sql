
/**================================**==========================================================**/
--  fn_select_vmax_decline_reason to retrieve decline reasons from the db within a given timerange
/**================================**==========================================================**/

CREATE OR REPLACE FUNCTION "${Schema}".fn_select_vmax_decline_reason(
	IN dateFrom TIMESTAMP,
	IN dateTo TIMESTAMP)

RETURNS TABLE (id integer,
    correlation_id text,
    logical_recordId bigint,
    application_status text,
    application_policy_id bigint,
    decline_reason text,
    af_decline_reason text,
    crc_decline_reason text,
    cif_number text,
    isapproved boolean,
    producttype int,
    vmaxtransactionid bigint,
    vmaxtransactionname text,
    transactiontime text,
    time_logged timestamp without time zone)
LANGUAGE 'plpgsql'
AS $BODY$
DECLARE

BEGIN
   	RETURN QUERY 
	SELECT vdr.id,
    vdr.correlation_id,
    vdr.logical_recordId,
    vdr.application_status,
    vdr.application_policy_id,
    vdr.decline_reason,
    vdr.af_decline_reason,
    vdr.crc_decline_reason,
    vdr.cif_number,
    vdr.isapproved,
    vdr.producttype,
    vdr.vmaxtransactionid,
    vdr.vmaxtransactionname,
    vdr.transactiontime,
    vdr.time_logged
	FROM "${Schema}".vmax_decline_reason vdr
	WHERE vdr.time_logged BETWEEN dateFrom AND dateTo;
END;

$BODY$;
ALTER FUNCTION "${Schema}".fn_select_vmax_decline_reason(TIMESTAMP, TIMESTAMP)
OWNER TO "${db_user}";