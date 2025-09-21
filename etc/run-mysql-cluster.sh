#!/bin/bash
set -e

docker run -d -p 3306:3306 --name cms-mysql -e MYSQL_DATABASE=cms -e MYSQL_USER=cmsuser -e MYSQL_PASSWORD=Admin12345678!  -e MYSQL_ROOT_PASSWORD=secret mysql:8.0 --log-bin-trust-function-creators=1

# Configuration
MYSQL_ROOT_PASSWORD=root
REPL_USER=repl
REPL_PASS=replpass
NETWORK_NAME=mysql-net

# Create a Docker network
docker network create $NETWORK_NAME || true

# Start 4 MySQL containers
docker run -d --name mysql1 --network $NETWORK_NAME \
  -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD \
  -e MYSQL_DATABASE=cms \
  -p 3307:3306 \
  mysql:8.0 \
  --server-id=1 --log-bin=mysql-bin --binlog-do-db=cms

docker run -d --name mysql2 --network $NETWORK_NAME \
  -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD \
  -e MYSQL_DATABASE=cms \
  -p 3308:3306 \
  mysql:8.0 \
  --server-id=2 --log-bin=mysql-bin --binlog-do-db=cms

docker run -d --name mysql3 --network $NETWORK_NAME \
  -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD \
  -e MYSQL_DATABASE=cms \
  -p 3309:3306 \
  mysql:8.0 \
  --server-id=3 --relay-log=relay-bin --binlog-do-db=cms

docker run -d --name mysql4 --network $NETWORK_NAME \
  -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD \
  -e MYSQL_DATABASE=cms \
  -p 3310:3306 \
  mysql:8.0 \
  --server-id=4 --relay-log=relay-bin --binlog-do-db=cms
echo "⏳ Waiting for MySQL containers to start..."
sleep 20

# Create replication user on master1
docker exec -i mysql1 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "
CREATE USER IF NOT EXISTS '$REPL_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$REPL_PASS';
GRANT REPLICATION SLAVE ON *.* TO '$REPL_USER'@'%';
FLUSH PRIVILEGES;
FLUSH TABLES WITH READ LOCK;
"

# Get master1 binlog info
MASTER1_FILE=$(docker exec -i mysql1 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "SHOW MASTER STATUS\G" | grep File: | awk '{print $2}')
MASTER1_POS=$(docker exec -i mysql1 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "SHOW MASTER STATUS\G" | grep Position: | awk '{print $2}')

# Create replication user on master2
docker exec -i mysql2 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "
CREATE USER IF NOT EXISTS '$REPL_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$REPL_PASS';
GRANT REPLICATION SLAVE ON *.* TO '$REPL_USER'@'%';
FLUSH PRIVILEGES;
FLUSH TABLES WITH READ LOCK;
"

# Get master2 binlog info
MASTER2_FILE=$(docker exec -i mysql2 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "SHOW MASTER STATUS\G" | grep File: | awk '{print $2}')
MASTER2_POS=$(docker exec -i mysql2 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "SHOW MASTER STATUS\G" | grep Position: | awk '{print $2}')

# Configure mysql3 to replicate from mysql1
docker exec -i mysql3 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "
CHANGE REPLICATION SOURCE TO
  SOURCE_HOST='mysql1',
  SOURCE_USER='$REPL_USER',
  SOURCE_PASSWORD='$REPL_PASS',
  SOURCE_LOG_FILE='$MASTER1_FILE',
  SOURCE_LOG_POS=$MASTER1_POS;
START REPLICA;
"

# Configure mysql4 to replicate from mysql2
docker exec -i mysql4 mysql -uroot -p$MYSQL_ROOT_PASSWORD -e "
CHANGE REPLICATION SOURCE TO
  SOURCE_HOST='mysql2',
  SOURCE_USER='$REPL_USER',
  SOURCE_PASSWORD='$REPL_PASS',
  SOURCE_LOG_FILE='$MASTER2_FILE',
  SOURCE_LOG_POS=$MASTER2_POS;
START REPLICA;
"

echo "✅ MySQL replication setup completed"
echo "mysql1 (master) -> mysql3 (replica) | ports: 3307 -> 3309"
echo "mysql2 (master) -> mysql4 (replica) | ports: 3308 -> 3310"
