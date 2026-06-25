/**================
--USER
================**/
DO
$do$
BEGIN
   IF EXISTS (
      SELECT FROM pg_catalog.pg_roles
      WHERE  rolname = '${db_user}') THEN
      RAISE NOTICE 'Role "${db_user}" already exists. Skipping.';
   ELSE
      CREATE ROLE "${db_user}" WITH
     LOGIN;
      GRANT pg_read_all_data, pg_write_all_data to "${db_user}";
   END IF;
END
$do$;
