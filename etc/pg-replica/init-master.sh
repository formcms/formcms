#!/bin/bash
set -e

# Configure postgresql.conf for replication
echo "wal_level = replica" >> /var/lib/postgresql/data/postgresql.conf
echo "max_wal_senders = 10" >> /var/lib/postgresql/data/postgresql.conf
echo "wal_keep_size = 64" >> /var/lib/postgresql/data/postgresql.conf
echo "hot_standby = on" >> /var/lib/postgresql/data/postgresql.conf

# Configure pg_hba.conf to allow replication connections
echo "hostnossl replication replicator 172.21.0.0/16 md5" >> /var/lib/postgresql/data/pg_hba.conf

# Create replication role
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE ROLE replicator WITH REPLICATION PASSWORD 'Admin12345678!' LOGIN;
EOSQL