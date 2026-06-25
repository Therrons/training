/**================================**==========================================================================**/
-- fn_select_fraud_events - retrieve fraud events filtered by date range, customer, and/or flagged status
/**================================**==========================================================================**/
CREATE OR REPLACE FUNCTION "${Schema}".fn_select_fraud_events(
    IN p_date_from          TIMESTAMP,
    IN p_date_to            TIMESTAMP,
    IN p_customer_id        TEXT        DEFAULT NULL,
    IN p_is_flagged_only    BOOLEAN     DEFAULT NULL,
    IN p_transaction_type   TEXT        DEFAULT NULL,
    IN p_min_fraud_score    NUMERIC     DEFAULT NULL
)
RETURNS TABLE (
    id                  bigint,
    kafka_topic         varchar(250),
    kafka_partition     integer,
    kafka_offset        bigint,
    consumed_at         timestamp,
    transaction_id      uuid,
    customer_id         varchar(100),
    account_id          varchar(100),
    amount              numeric(18,2),
    currency            varchar(10),
    merchant_name       varchar(250),
    merchant_category   varchar(100),
    transaction_type    varchar(50),
    channel             varchar(50),
    country_code        varchar(10),
    transaction_time    timestamp,
    is_flagged          boolean,
    fraud_score         numeric(5,2),
    flagged_reason      text,
    time_logged         timestamp
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        fe.id,
        fe.kafka_topic,
        fe.kafka_partition,
        fe.kafka_offset,
        fe.consumed_at,
        fe.transaction_id,
        fe.customer_id,
        fe.account_id,
        fe.amount,
        fe.currency,
        fe.merchant_name,
        fe.merchant_category,
        fe.transaction_type,
        fe.channel,
        fe.country_code,
        fe.transaction_time,
        fe.is_flagged,
        fe.fraud_score,
        fe.flagged_reason,
        fe.time_logged
    FROM "${Schema}".fraud_event fe
    WHERE fe.transaction_time BETWEEN p_date_from AND p_date_to
      AND (p_customer_id       IS NULL OR fe.customer_id = p_customer_id)
      AND (p_is_flagged_only   IS NULL OR fe.is_flagged  = p_is_flagged_only)
      AND (p_transaction_type  IS NULL OR fe.transaction_type = p_transaction_type)
      AND (p_min_fraud_score   IS NULL OR fe.fraud_score >= p_min_fraud_score)
    ORDER BY fe.transaction_time DESC;
END;
$BODY$;

ALTER FUNCTION "${Schema}".fn_select_fraud_events(TIMESTAMP, TIMESTAMP, TEXT, BOOLEAN, TEXT, NUMERIC)
    OWNER TO "${db_user}";


/**================================**==========================================================================**/
-- fn_select_fraud_rule_results - retrieve all rule results for a given fraud event
/**================================**==========================================================================**/
CREATE OR REPLACE FUNCTION "${Schema}".fn_select_fraud_rule_results(
    IN p_fraud_event_id BIGINT
)
RETURNS TABLE (
    id                  bigint,
    fraud_event_id      bigint,
    rule_code           varchar(100),
    rule_description    varchar(500),
    is_triggered        boolean,
    score_contribution  numeric(5,2),
    evaluated_at        timestamp
)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        frr.id,
        frr.fraud_event_id,
        frr.rule_code,
        frr.rule_description,
        frr.is_triggered,
        frr.score_contribution,
        frr.evaluated_at
    FROM "${Schema}".fraud_rule_result frr
    WHERE frr.fraud_event_id = p_fraud_event_id
    ORDER BY frr.score_contribution DESC;
END;
$BODY$;

ALTER FUNCTION "${Schema}".fn_select_fraud_rule_results(BIGINT)
    OWNER TO "${db_user}";
