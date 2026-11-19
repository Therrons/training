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
    kafka_topic         VARCHAR(250) NOT NULL,
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
    transaction_time    TIMESTAMP    NOT NULL,  -- Fraud evaluation outcome
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


/**================================**=================================================================**/
-- Table: fraud_rule_result - individual rule evaluations for each fraud_event
/**================================**=================================================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".fraud_rule_result (
    id              bigint       NOT NULL DEFAULT nextval('"${Schema}".fraud_rule_result_id_seq'::regclass),
    fraud_event_id  bigint       NOT NULL,
    rule_code       VARCHAR(100) NOT NULL,
    rule_description VARCHAR(500),
    is_triggered    BOOLEAN      NOT NULL DEFAULT FALSE,
    score_contribution NUMERIC(5,2) NOT NULL DEFAULT 0,
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


/**================================**=================================================================**/
-- Table: role - application roles, referenced by login
/**================================**=================================================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".role (
    id          BIGINT       NOT NULL DEFAULT nextval('"${Schema}".role_id_seq'::regclass),
    RoleId      INTEGER      NOT NULL,
    RoleName    VARCHAR(50)  NOT NULL,
    CONSTRAINT role_pkey PRIMARY KEY (id),
    CONSTRAINT role_roleid_uq UNIQUE (RoleId),
    CONSTRAINT role_rolename_uq UNIQUE (RoleName)
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS "${Schema}".role
    OWNER TO "${db_user}";

INSERT INTO "${Schema}".role (RoleId, RoleName)
VALUES (1, 'Admin'),
       (2, 'Analyst'),
       (3, 'Tester')
ON CONFLICT DO NOTHING;


/**================================**=================================================================**/
-- Table: login - application user credentials and assigned role
/**================================**=================================================================**/
CREATE TABLE IF NOT EXISTS "${Schema}".login (
    id          BIGINT       NOT NULL DEFAULT nextval('"${Schema}".login_id_seq'::regclass),
    username    VARCHAR(100) NOT NULL,
    password    VARCHAR(255) NOT NULL,   -- store a salted hash, never plain text
    RoleId      INTEGER      NOT NULL,
    CONSTRAINT login_pkey PRIMARY KEY (id),
    CONSTRAINT login_username_uq UNIQUE (username),
    CONSTRAINT fk_login_role
        FOREIGN KEY (RoleId) REFERENCES "${Schema}".role (RoleId)
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS "${Schema}".login
    OWNER TO "${db_user}";

CREATE INDEX IF NOT EXISTS idx_login_roleid
    ON "${Schema}".login (RoleId);

INSERT INTO "${Schema}".login (username, password, RoleId)
VALUES ('fraud-admin', 'PBKDF2-SHA256$310000$qfRQOdFN5EPU1gonIfWsNg==$eBWsnggQTcSzTFFSCzGMHWtKSn+Au3yYnPT8SWOQQKI=', 1),
       ('fraud-analyst', 'PBKDF2-SHA256$310000$pmhQKTTnLPO9/DvGVO6ujw==$u1xKLn4jtSCRb2iuxTmRqNSr+5NS9vobZ2tdmiBhpCI=', 2),
       ('fraud-tester', 'PBKDF2-SHA256$310000$sNaQu++5HKGBcGtKHbbD5w==$zE9OdjvsYEDqh7yl6atpkMIr2sb9zthUFUDq16zDhwE=', 3)
ON CONFLICT DO NOTHING;
