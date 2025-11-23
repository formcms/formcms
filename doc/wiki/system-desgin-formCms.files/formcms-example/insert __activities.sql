INSERT INTO public.__activities
("entityName", "recordId", "activityType", "userId", "isActive", "publishedAt")
SELECT
    'article' AS "entityName",
    a.id AS "recordId",
    'view' AS "activityType",
    u."Id" AS "userId",
    true AS "isActive",
    now() AS "publishedAt"
FROM (
         SELECT id
         FROM public.article
         ORDER BY random()
             LIMIT 1000           -- batch size
     ) a
         CROSS JOIN (
    SELECT "Id"
    FROM public."AspNetUsers"
    ORDER BY random()
        LIMIT 1000          -- batch size
) u
    ON CONFLICT ("entityName", "recordId", "activityType", "userId") DO NOTHING;