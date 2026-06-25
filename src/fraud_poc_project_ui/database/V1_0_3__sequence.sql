/**================================**===============================================================**/
-- dlt_kafka_id_seq - auto increment sequence for the dead letter queue table
/**================================**===============================================================**/

CREATE SEQUENCE IF NOT EXISTS "${Schema}".dlt_kafka_id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

ALTER SEQUENCE "${Schema}".dlt_kafka_id_seq
    OWNER TO "${db_user}";


/**================================**===============================================================**/
-- fraud_event_id_seq - auto increment sequence for the fraud_event table
/**================================**===============================================================**/

CREATE SEQUENCE IF NOT EXISTS "${Schema}".fraud_event_id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

ALTER SEQUENCE "${Schema}".fraud_event_id_seq
    OWNER TO "${db_user}";


/**================================**===============================================================**/
-- fraud_rule_result_id_seq - auto increment sequence for the fraud_rule_result table
/**================================**===============================================================**/

CREATE SEQUENCE IF NOT EXISTS "${Schema}".fraud_rule_result_id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

ALTER SEQUENCE "${Schema}".fraud_rule_result_id_seq
    OWNER TO "${db_user}";
