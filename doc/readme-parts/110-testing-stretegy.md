

---
## Testing Strategy
<details>
<summary>
This chapter describes the systems' automated testing strategy
</summary>

Favors integration testing over unit testing because integration tests can catch more real-world issues.
For example, when inserting a record into the database, multiple modules are involved:
- `EntitiesHandler`
- `EntitiesService`
- `Entity` (in core)
- `Query executors` (e.g., `SqlLite`, `Postgres`, `SqlServer`)

Writing unit tests for each function and mocking its upstream and downstream services can be tedious.
Instead, FormCMS focuses on checking the input and output of RESTful API endpoints in its integration tests.

### Integration Testing for FormCMS.Course `/formcms/server/FormCMS.Course.Tests`
This project focuses on verifying the functionalities of the FormCMS.Course example project.

### New Feature Testing `/formcms/server/FormCMS.App.Tests`
This project is dedicated to testing experimental functionalities, like MongoDB and Kafka plugins.

</details>