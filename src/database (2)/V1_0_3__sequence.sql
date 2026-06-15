
/**================================**===============================================================**/
--  dlq_kafka_id_seq - this is the auto increment sequence for the dlq_kafka and  decline reasons table
/**================================**===============================================================**/

CREATE SEQUENCE IF NOT EXISTS "${Schema}".dlq_kafka_id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 2147483647
    CACHE 1;

ALTER SEQUENCE "${Schema}".dlq_kafka_id_seq
    OWNER TO "${db_user}";


CREATE SEQUENCE IF NOT EXISTS "${Schema}".vmax_decline_reason_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

ALTER SEQUENCE "${Schema}".vmax_decline_reason_seq
    OWNER TO "${db_user}";