/**================================**==============================================**/
-- Table: cn.dlq_kafka - capture records that cannot be put on the dead letter topics
/**================================**==============================================**/
CREATE TABLE IF NOT EXISTS ${Schema}.dlq_kafka (
    id integer NOT NULL DEFAULT nextval('${Schema}.dlq_kafka_id_seq'::regclass),
    topic_data TEXT NOT NULL,
    topic_schema VARCHAR(250) NOT NULL,
    topic_name VARCHAR(250) NOT NULL,
    topic_dlq_name VARCHAR(250) NOT NULL,
    message_data TEXT NOT NULL,
    error VARCHAR(2000),
    time_logged TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT dlq_kafka_pkey PRIMARY KEY (id)
)
    TABLESPACE pg_default;

ALTER TABLE IF EXISTS ${Schema}.dlq_kafka
    OWNER to "${db_user}";


/**================================**================================================================**/
-- Table: cn.vmax_decline_reason - capture VMAX decline reasons records supplied by VMAX Proxy service
/**================================**================================================================**/

CREATE TABLE IF NOT EXISTS ${Schema}.vmax_decline_reason
(
    id integer NOT NULL DEFAULT nextval('${Schema}.vmax_decline_reason_seq'::regclass),
    correlation_id text COLLATE pg_catalog."default",
    logical_recordId bigint,
    application_status text COLLATE pg_catalog."default",
    application_policy_id bigint,
    decline_reason text COLLATE pg_catalog."default",
    af_decline_reason text COLLATE pg_catalog."default",
    crc_decline_reason text COLLATE pg_catalog."default",
    cif_number text COLLATE pg_catalog."default",
    isapproved boolean,
    producttype int,
    vmaxtransactionid bigint,
    vmaxtransactionname text COLLATE pg_catalog."default",
    transactiontime text COLLATE pg_catalog."default",
    time_logged timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT vmax_decline_reason_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS ${Schema}.vmax_decline_reason
    OWNER TO "${db_user}";