FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root user to install packages
USER root

# Install dependencies for adding Microsoft repo
RUN apt-get update && apt-get install -yq curl apt-transport-https gnupg

# Add Microsoft package signing key and repository for SQL Server 2022
RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
RUN curl https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list

# Update and install Full-Text Search
RUN apt-get update
RUN apt-get install -y mssql-server-fts

# Clean up to reduce image size
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

# Switch back to mssql user
USER mssql

# Expose the default SQL Server port
EXPOSE 1433

# Run SQL Server
ENTRYPOINT ["/opt/mssql/bin/sqlservr"]