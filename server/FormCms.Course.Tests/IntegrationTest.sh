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
  remove_container cms-postgres
  docker run -d --name cms-postgres -e POSTGRES_USER=cmsuser -e POSTGRES_PASSWORD=Admin12345678! -e POSTGRES_DB=cms_test -p 5432:5432 postgres:latest
  
  export DatabaseProvider=Postgres
  export ConnectionStrings__Postgres="Host=localhost;Database=cms_test;Username=cmsuser;Password=Admin12345678!"
  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft_AspNetCore=None\
  dotnet test 
}

test_sqlserver_container(){
  remove_container cms-sqlserver
  docker run --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e "MSSQL_SA_PASSWORD=Admin12345678!" -p 1433:1433 --name cms-sqlserver -d sqlserver-fts
  sleep 10
  export DatabaseProvider=SqlServer
  export ConnectionStrings__SqlServer="Server=localhost;Database=cms_test;User Id=sa;Password=Admin12345678!;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft__AspNetCore=None\
  Logging__LogLevel__Microsoft__AspNetCore__Diagnostics__ExceptionHandlerMiddleware=None\
  dotnet test  
}

test_mysql_container(){
  remove_container cms-mysql
  docker run -d -p 3306:3306 --name cms-mysql -e MYSQL_DATABASE=cms -e MYSQL_USER=cmsuser -e MYSQL_PASSWORD=Admin12345678!  -e MYSQL_ROOT_PASSWORD=secret mysql:8.0 --log-bin-trust-function-creators=1
  sleep 10

  export DatabaseProvider=Mysql
  export ConnectionStrings__Mysql="Server=localhost;Port=3306;Database=cms;User=cmsuser;Password=Admin12345678!;"

  Logging__LogLevel__Default=None\
  Logging__LogLevel__Microsoft__AspNetCore=None\
  Logging__LogLevel__Microsoft__AspNetCore__Diagnostics__ExceptionHandlerMiddleware=None\
  dotnet test
}

# Exit immediately if a command exits with a non-zero status
set -e

db_path=$(pwd)/_cms_test.db && rm -f "$db_path" && test_sqlite "$db_path"

#test_postgres_container 

#test_sqlserver_container

#test_mysql_container
