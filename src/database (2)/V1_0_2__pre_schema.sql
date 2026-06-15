/**================
--SCHEMA CREATION   
================**/
CREATE SCHEMA IF NOT EXISTS "${Schema}";
COMMENT ON SCHEMA "${Schema}" IS 'schema for Infinity and Beyond data solutions';
GRANT ALL PRIVILEGES ON SCHEMA "${Schema}" TO "${db_user}";
