/**================================**===================================================**/
--  sp_insert_dlq_error to capture entries that we were not able to put on our KAFKA queue
/**================================**===================================================**/
CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_dlq_error(
    IN topic_data text,
    IN topic_schema varchar(250),
    IN topic_name varchar(250),
    IN topic_dlq_name varchar(250),
    IN message_data text,
    IN error varchar(2000)
)
LANGUAGE 'plpgsql'
AS $BODY$
DECLARE

BEGIN
    INSERT INTO "${Schema}".dlq_kafka
    (topic_data, topic_schema, topic_name, topic_dlq_name, message_data, error)
    VALUES
    (topic_data, topic_schema, topic_name, topic_dlq_name, message_data, error);
END;

$BODY$;
ALTER PROCEDURE "${Schema}".sp_insert_dlq_error(text, varchar, varchar, varchar, text, varchar)
    OWNER TO "${db_user}";


/**================================**===================================================**/
--  sp_insert_vmax_decline_reason to capture entries read from the VMAX kafka queue
/**================================**===================================================**/

CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_vmax_decline_reason(
	IN correlation_id text,
	IN logical_recordid bigint,
	IN application_status text,
	IN application_policy_id bigint,
	IN decline_reason text,
	IN af_decline_reason text,
	IN crc_decline_reason text,
	IN cif_number text,
    IN isapproved boolean,
    IN producttype int,
    IN vmaxtransactionid bigint,
    IN vmaxtransactionname text,
    IN transactiontime text)
LANGUAGE 'plpgsql'
AS $BODY$
DECLARE

BEGIN
    INSERT INTO "${Schema}".vmax_decline_reason
    (correlation_id, logical_recordId, application_status, application_policy_id, decline_reason, af_decline_reason, crc_decline_reason, cif_number, isapproved, producttype, vmaxtransactionid, vmaxtransactionname, transactiontime)
    VALUES
    (correlation_id, logical_recordId, application_status, application_policy_id, decline_reason, af_decline_reason, crc_decline_reason, cif_number, isapproved, producttype, vmaxtransactionid, vmaxtransactionname, transactiontime);
END;

$BODY$;
ALTER PROCEDURE "${Schema}".sp_insert_vmax_decline_reason(text, bigint, text, bigint, text, text, text, text, boolean, int, bigint, text, text)
    OWNER TO "${db_user}";