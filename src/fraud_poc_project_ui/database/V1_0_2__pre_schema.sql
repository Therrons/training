/**================
--SCHEMA CREATION
================**/
CREATE SCHEMA IF NOT EXISTS "${Schema}";
COMMENT ON SCHEMA "${Schema}" IS 'schema for fraud detection processing';
GRANT ALL PRIVILEGES ON SCHEMA "${Schema}" TO "${db_user}";
