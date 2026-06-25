/**================================**===================================================**/
-- sp_insert_dlt_error - capture Kafka messages that could not be processed
/**================================**===================================================**/
CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_dlt_error(
    IN topic_data      text,
    IN topic_schema    varchar(250),
    IN topic_name      varchar(250),
    IN topic_dlt_name  varchar(250),
    IN message_data    text,
    IN error           varchar(2000)
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO "${Schema}".dlt_kafka
        (topic_data, topic_schema, topic_name, topic_dlt_name, message_data, error)
    VALUES
        (topic_data, topic_schema, topic_name, topic_dlt_name, message_data, error);
END;
$BODY$;

ALTER PROCEDURE "${Schema}".sp_insert_dlt_error(text, varchar, varchar, varchar, text, varchar)
    OWNER TO "${db_user}";


/**================================**===================================================**/
-- sp_insert_fraud_event - persist a processed fraud event and its rule results
/**================================**===================================================**/
CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_fraud_event(
    IN p_kafka_topic        varchar(250),
    IN p_kafka_partition    integer,
    IN p_kafka_offset       bigint,
    IN p_transaction_id     uuid,
    IN p_customer_id        varchar(100),
    IN p_account_id         varchar(100),
    IN p_amount             numeric(18,2),
    IN p_currency           varchar(10),
    IN p_merchant_name      varchar(250),
    IN p_merchant_category  varchar(100),
    IN p_transaction_type   varchar(50),
    IN p_channel            varchar(50),
    IN p_country_code       varchar(10),
    IN p_transaction_time   timestamp,
    IN p_is_flagged         boolean,
    IN p_fraud_score        numeric(5,2),
    IN p_flagged_reason     text,
    OUT p_fraud_event_id    bigint
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO "${Schema}".fraud_event (
        kafka_topic, kafka_partition, kafka_offset,
        transaction_id, customer_id, account_id,
        amount, currency, merchant_name, merchant_category,
        transaction_type, channel, country_code, transaction_time,
        is_flagged, fraud_score, flagged_reason
    )
    VALUES (
        p_kafka_topic, p_kafka_partition, p_kafka_offset,
        p_transaction_id, p_customer_id, p_account_id,
        p_amount, p_currency, p_merchant_name, p_merchant_category,
        p_transaction_type, p_channel, p_country_code, p_transaction_time,
        p_is_flagged, p_fraud_score, p_flagged_reason
    )
    RETURNING id INTO p_fraud_event_id;
END;
$BODY$;

ALTER PROCEDURE "${Schema}".sp_insert_fraud_event(
    varchar, integer, bigint, uuid, varchar, varchar, numeric, varchar,
    varchar, varchar, varchar, varchar, varchar, timestamp, boolean, numeric, text)
    OWNER TO "${db_user}";


/**================================**===================================================**/
-- sp_insert_fraud_rule_result - persist a single rule evaluation for a fraud event
/**================================**===================================================**/
CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_fraud_rule_result(
    IN p_fraud_event_id     bigint,
    IN p_rule_code          varchar(100),
    IN p_rule_description   varchar(500),
    IN p_is_triggered       boolean,
    IN p_score_contribution numeric(5,2)
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO "${Schema}".fraud_rule_result (
        fraud_event_id, rule_code, rule_description, is_triggered, score_contribution
    )
    VALUES (
        p_fraud_event_id, p_rule_code, p_rule_description, p_is_triggered, p_score_contribution
    );
END;
$BODY$;

ALTER PROCEDURE "${Schema}".sp_insert_fraud_rule_result(bigint, varchar, varchar, boolean, numeric)
    OWNER TO "${db_user}";
