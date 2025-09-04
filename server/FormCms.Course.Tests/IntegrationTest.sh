test_sqlite(){
  local db_name=$1
  export DatabaseProvider=Sqlite
  export ConnectionStrings__Sqlite="Data Source=${db_name}"
  sleep 1
  
  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft__AspNetCore=None\
  Logging__LogLevel__Microsoft__AspNetCore__Diagnostics__ExceptionHandlerMiddleware=None\
  dotnet test 
}

remove_container(){
  local container_name=$1
   # Remove existing container if it exists
   if docker ps -a --format '{{.Names}}' | grep -q "^${container_name}$"; then
     echo "Removing existing container: ${container_name}"
     docker rm -f "${container_name}"
   fi 
}

test_postgres_container() {
  local container_name="integration-test-postgres"
  
  remove_container $container_name
  local docker_run_command="docker run -d --name $container_name -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_DB=cms_integration_tests -p 5432:5432 postgres:latest"
  eval "$docker_run_command"
  
  export DatabaseProvider=Postgres
  export ConnectionStrings__Postgres="Host=localhost;Database=cms_integration_tests;Username=postgres;Password=mysecretpassword"
  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft_AspNetCore=None\
  dotnet test 
}

test_sqlserver_container(){
  local container_name="integration-test-sql-edge"
  local password=Admin12345678!
  remove_container $container_name
  
  docker run --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e "MSSQL_SA_PASSWORD=$password" -p 1433:1433 --name $container_name -d mcr.microsoft.com/mssql/server:2022-latest 
  sleep 10
  
  export DatabaseProvider=SqlServer
  export ConnectionStrings__SqlServer="Server=localhost;Database=cms_integration_tests;User Id=sa;Password=Admin12345678!;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  
  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft__AspNetCore=None\
  Logging__LogLevel__Microsoft__AspNetCore__Diagnostics__ExceptionHandlerMiddleware=None\
  dotnet test  
}

test_mysql_container(){
  local container_name="integration-test-mysql"
  local password="mysecretpassword"
  local db_name="cms_integration_tests"

  remove_container $container_name

  docker run -d --name $container_name \
    -e MYSQL_ROOT_PASSWORD=$password \
    -e MYSQL_DATABASE=$db_name \
    -e MYSQL_USER=myuser \
    -e MYSQL_PASSWORD=$password \
    -p 3306:3306 \
    mysql:8.0 \
    --default-authentication-plugin=mysql_native_password \
    --log-bin-trust-function-creators=1
  # Wait for MySQL to be ready
  echo "Waiting for MySQL to be ready..."
  until docker exec $container_name mysql -h localhost -u myuser -p$password -e "SELECT 1;" $db_name >/dev/null 2>&1; do
    sleep 2
  done

  export DatabaseProvider=Mysql
  export ConnectionStrings__Mysql="Server=localhost;Database=$db_name;User=myuser;Password=$password;"

  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft__AspNetCore=None\
  Logging__LogLevel__Microsoft__AspNetCore__Diagnostics__ExceptionHandlerMiddleware=None\
  dotnet test
}

# Exit immediately if a command exits with a non-zero status
set -e

#db_path=$(pwd)/_cms_temp_integration_tests.db.db && rm -f "$db_path" && test_sqlite "$db_path"

#test_postgres_container 

#test_sqlserver_container

test_mysql_container
