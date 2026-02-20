-- Create HeatconERP database if it does not exist
-- Run: psql -U postgres -f scripts/create_db.sql

SELECT 'CREATE DATABASE heatconerp OWNER postgres'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'heatconerp');
\gexec
