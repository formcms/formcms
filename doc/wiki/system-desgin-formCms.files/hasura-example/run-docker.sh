docker run -d \
--name hasura \
-p 8080:8080 \
-e HASURA_GRAPHQL_DATABASE_URL='postgres://cmsuser:Admin12345678!@host.docker.internal:5432/formcms' \
-e HASURA_GRAPHQL_ENABLE_CONSOLE=true \
-e HASURA_GRAPHQL_ADMIN_SECRET=myadminsecretkey \
-e HASURA_GRAPHQL_UNAUTHORIZED_ROLE=anonymous \
hasura/graphql-engine:latest  