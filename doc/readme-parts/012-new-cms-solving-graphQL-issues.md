

---

## New CMS? — GraphQL Issues

### Key Challenges

1. **Security & Over-Fetching** – Complex or poorly optimized queries can overload the backend, exposing vulnerabilities and impacting performance.
2. **Caching Limitations** – GraphQL lacks built-in CDN caching, making performance optimization harder.
3. **N+1 Query Problem** – Individual resolver calls can lead to inefficient database queries.

---

### Solution: Persisted Queries with GET Requests

Many GraphQL frameworks support persisted queries with GET requests, enabling caching and improved performance.

---

### How FormCMS Solves These Issues

FormCMS automatically saves GraphQL queries and converts them into RESTful GET requests. For example:

```graphql
query TeacherQuery($id: Int) {
  teacherList(idSet: [$id]) {
    id firstname lastname
    skills { id name }
  }
}
```

becomes `GET /api/queries/TeacherQuery`.

- **Security & Efficiency** – Only Admins can define GraphQL queries, preventing abuse. Backend and frontend teams optimize queries to avoid excessive data requests.
- **Caching** – GET requests enable efficient CDN caching, while ASP.NET Core’s hybrid cache further boosts performance.
- **Performance** – Related entities are retrieved in a single optimized query, avoiding the N+1 problem.

By transforming GraphQL into optimized REST-like queries, FormCMS ensures a secure, efficient, and scalable API experience.