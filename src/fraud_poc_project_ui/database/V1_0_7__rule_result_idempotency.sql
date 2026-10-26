/**================================**=================================================**/
-- V1_0_7: make fraud rule results idempotent on Kafka redelivery.
-- All scripts in this folder run on every application startup, so this file must be
-- safe to run repeatedly. The constraint and de-duplication only run when the constraint
-- does not yet exist.
/**================================**=================================================**/

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE c.conname = 'fraud_rule_result_event_rule_uq'
          AND n.nspname = '${Schema}'
    ) THEN
        -- Remove duplicate rule results left behind by earlier redeliveries, keeping the lowest id.
        DELETE FROM "${Schema}".fraud_rule_result a
        USING "${Schema}".fraud_rule_result b
        WHERE a.fraud_event_id = b.fraud_event_id
          AND a.rule_code = b.rule_code
          AND a.id > b.id;

        ALTER TABLE "${Schema}".fraud_rule_result
            ADD CONSTRAINT fraud_rule_result_event_rule_uq UNIQUE (fraud_event_id, rule_code);
    END IF;
END
$$;


/**================================**=================================================**/
-- sp_insert_fraud_rule_result - one rule result per (event, rule). Re-saving overwrites.
/**================================**=================================================**/
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
    )
    ON CONFLICT (fraud_event_id, rule_code) DO UPDATE SET
        rule_description   = EXCLUDED.rule_description,
        is_triggered       = EXCLUDED.is_triggered,
        score_contribution = EXCLUDED.score_contribution;
END;
$BODY$;
