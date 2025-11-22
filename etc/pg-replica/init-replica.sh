#!/bin/bash
set -e

# Wait for master to be ready
until pg_isready -h postgres-master -p 5432 -U cmsuser; do
  echo "Waiting for master to be ready..."
  sleep 2
done

# Clear data directory and replicate from master
rm -rf /var/lib/postgresql/data/*
pg_basebackup -h postgres-master -p 5432 -U replicator -D /var/lib/postgresql/data --wal-method=stream

# Create standby.signal file to indicate replica mode
touch /var/lib/postgresql/data/standby.signal

# Configure postgresql.conf for replica
echo "primary_conninfo = 'host=postgres-master port=5432 user=replicator password=Admin12345678!'" >> /var/lib/postgresql/data/postgresql.conf
echo "hot_standby = on" >> /var/lib/postgresql/data/postgresql.conf
