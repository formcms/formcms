test_sqlite(){
  local db_name=$1
  export DatabaseProvider=Sqlite
  export ConnectionStrings__Sqlite="Data Source=${db_name}"
  sleep 1
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
  local container_name="FormCMS-db-postgres"
  
  remove_container $container_name
  local docker_run_command="docker run -d --name $container_name -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_DB=cms_integration_tests -p 5432:5432"

  docker_run_command+=" postgres:latest"
  eval "$docker_run_command"
  
  export DatabaseProvider=Postgres
  export ConnectionStrings__Postgres="Host=localhost;Database=cms_integration_tests;Username=postgres;Password=mysecretpassword"
  dotnet test
}

test_sqlserver_container(){
  local container_name="FormCMS-db-sql-edge"
  local password=Admin12345678!
  remove_container $container_name

  docker run --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e "MSSQL_SA_PASSWORD=$password" -p 1433:1433 --name $container_name -d mcr.microsoft.com/mssql/server:2022-latest 
  sleep 10
  
  export DatabaseProvider=SqlServer
  export ConnectionStrings__SqlServer="Server=localhost;Database=cms_integration_tests;User Id=sa;Password=Admin12345678!;TrustServerCertificate=True"
  dotnet test  
}

# Exit immediately if a command exits with a non-zero status
set -e
export Logging__LogLevel__Default=Warning
export Logging__LogLevel__Microsoft_AspNetCore=Warning

#Sqlite With Data 
db_path=$(pwd)/_cms_integration_tests.db && rm -f "$db_path" && cp ../FormCMS.Course/cms.db "$db_path" && test_sqlite "$db_path"

#Sqlite With Empty Data 
db_path=$(pwd)/temp.db && rm -f "$db_path" && test_sqlite "$db_path"

test_postgres_container 

test_sqlserver_container

