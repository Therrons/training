/**================================**===================================================**/
-- sp_insert_fraud_event - persist a processed fraud event and its rule results
-- Modified with UPSERT logic to handle duplicate transaction_ids gracefully
/**================================**===================================================**/
CREATE OR REPLACE PROCEDURE "${Schema}".sp_insert_fraud_event(
    IN p_kafka_topic        varchar(250),
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
    IN p_transaction_time   timestamp with time zone,
    IN p_is_flagged         boolean,
    IN p_fraud_score        numeric(5,2),
    IN p_flagged_reason     text,
    OUT p_fraud_event_id    bigint
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    -- Use INSERT...ON CONFLICT to upsert: insert if new, update fraud evaluation if duplicate exists
    INSERT INTO "${Schema}".fraud_event (
        kafka_topic, transaction_id, customer_id, account_id,
        amount, currency, merchant_name, merchant_category,
        transaction_type, channel, country_code, transaction_time,
        is_flagged, fraud_score, flagged_reason
    )
    VALUES (
        p_kafka_topic, p_transaction_id, p_customer_id, p_account_id,
        p_amount, p_currency, p_merchant_name, p_merchant_category,
        p_transaction_type, p_channel, p_country_code, p_transaction_time,
        p_is_flagged, p_fraud_score, p_flagged_reason
    )
    -- If transaction_id already exists (conflict on unique constraint), update only the fraud evaluation fields
    ON CONFLICT (transaction_id) DO UPDATE SET
        is_flagged = EXCLUDED.is_flagged,
        fraud_score = EXCLUDED.fraud_score,
        flagged_reason = EXCLUDED.flagged_reason
    RETURNING id INTO p_fraud_event_id;
END;
$BODY$;

ALTER PROCEDURE "${Schema}".sp_insert_fraud_event(
    varchar, uuid, varchar, varchar, numeric, varchar,
    varchar, varchar, varchar, varchar, varchar, timestamp with time zone, boolean, numeric, text)
    OWNER TO "${db_user}";
