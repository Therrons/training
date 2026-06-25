/**================================**==============================================**/
-- Table: dlt_kafka - capture Kafka messages that could not be processed
/**================================**==============================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".dlt_kafka (
    id          integer      NOT NULL DEFAULT nextval('"${Schema}".dlt_kafka_id_seq'::regclass),
    topic_data  TEXT         NOT NULL,
    topic_schema VARCHAR(250) NOT NULL,
    topic_name  VARCHAR(250) NOT NULL,
    topic_dlt_name VARCHAR(250) NOT NULL,
    message_data TEXT        NOT NULL,
    error       VARCHAR(2000),
    time_logged TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT dlt_kafka_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS "${Schema}".dlt_kafka
    OWNER TO "${db_user}";


/**================================**=================================================================**/
-- Table: fraud_event - the processed transaction event as consumed from Kafka, with overall fraud flag
/**================================**=================================================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".fraud_event (
    id                  bigint       NOT NULL DEFAULT nextval('"${Schema}".fraud_event_id_seq'::regclass),
    -- Kafka envelope fields
    kafka_topic         VARCHAR(250) NOT NULL,
    kafka_partition     INTEGER      NOT NULL DEFAULT 0,
    kafka_offset        BIGINT       NOT NULL DEFAULT 0,
    consumed_at         TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Transaction event payload
    transaction_id      UUID         NOT NULL,
    customer_id         VARCHAR(100) NOT NULL,
    account_id          VARCHAR(100) NOT NULL,
    amount              NUMERIC(18,2) NOT NULL,
    currency            VARCHAR(10)  NOT NULL DEFAULT 'ZAR',
    merchant_name       VARCHAR(250),
    merchant_category   VARCHAR(100),
    transaction_type    VARCHAR(50)  NOT NULL,   -- e.g. POS, ATM, EFT, CNP
    channel             VARCHAR(50),              -- e.g. Online, InStore, ATM
    country_code        VARCHAR(10),
    transaction_time    TIMESTAMP    NOT NULL,
    -- Fraud evaluation outcome
    is_flagged          BOOLEAN      NOT NULL DEFAULT FALSE,
    fraud_score         NUMERIC(5,2) NOT NULL DEFAULT 0,   -- 0-100 composite score
    flagged_reason      TEXT,
    time_logged         TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fraud_event_pkey PRIMARY KEY (id),
    CONSTRAINT fraud_event_transaction_id_uq UNIQUE (transaction_id)
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS "${Schema}".fraud_event
    OWNER TO "${db_user}";

CREATE INDEX IF NOT EXISTS idx_fraud_event_customer_id
    ON "${Schema}".fraud_event (customer_id);

CREATE INDEX IF NOT EXISTS idx_fraud_event_transaction_time
    ON "${Schema}".fraud_event (transaction_time);

CREATE INDEX IF NOT EXISTS idx_fraud_event_is_flagged
    ON "${Schema}".fraud_event (is_flagged);

CREATE INDEX IF NOT EXISTS idx_fraud_event_consumed_at
    ON "${Schema}".fraud_event (consumed_at);


/**================================**=================================================================**/
-- Table: fraud_rule_result - individual rule evaluations for each fraud_event
/**================================**=================================================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".fraud_rule_result (
    id              bigint       NOT NULL DEFAULT nextval('"${Schema}".fraud_rule_result_id_seq'::regclass),
    fraud_event_id  bigint       NOT NULL,
    rule_code       VARCHAR(100) NOT NULL,   -- e.g. HIGH_AMOUNT, VELOCITY, GEO_ANOMALY
    rule_description VARCHAR(500),
    is_triggered    BOOLEAN      NOT NULL DEFAULT FALSE,
    score_contribution NUMERIC(5,2) NOT NULL DEFAULT 0,
    evaluated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fraud_rule_result_pkey PRIMARY KEY (id),
    CONSTRAINT fk_fraud_rule_result_event
        FOREIGN KEY (fraud_event_id) REFERENCES "${Schema}".fraud_event (id)
        ON DELETE CASCADE
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS "${Schema}".fraud_rule_result
    OWNER TO "${db_user}";

CREATE INDEX IF NOT EXISTS idx_fraud_rule_result_event_id
    ON "${Schema}".fraud_rule_result (fraud_event_id);

CREATE INDEX IF NOT EXISTS idx_fraud_rule_result_rule_code
    ON "${Schema}".fraud_rule_result (rule_code);
