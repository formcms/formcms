

---
## **GraphQL Query**

<details>
<summary>
FormCMS simplifies frontend development by offering robust GraphQL support.
</summary>

### Getting Started
#### Accessing the GraphQL IDE
To get started, launch the web application and navigate to `/graph`. You can also try our [online demo](https://fluent-cms-admin.azurewebsites.net/graph).

---
#### Singular vs. List Response
For each entity in FormCMS, two GraphQL fields are automatically generated:  
- `<entityName>`: Returns a record.
- `<entityNameList>`: Returns a list of records.  

**Single Course **
```graphql
{
  course {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20course%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**List of Courses **
```graphql
{
  courseList {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---
#### Field Selection
You can query specific fields for both the current entity and its related entities.
**Example Query:**
```graphql
{
  courseList{
    id
    name
    teacher{
      id
      firstname
      lastname
      skills{
        id
        name
      }
    }
    materials{
      id,
      name
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20firstname%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%20%20skills%7B%0A%20%20%20%20%20%20%20%20id%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%20%20%20%20materials%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D%0A)

---
#### Filtering with `Value Match` in FormCMS

FormCMS provides flexible filtering capabilities using the `idSet` field (or any other field), enabling precise data queries by matching either a single value or a list of values.

**Filter by a Single Value Example:**
```graphql
{
  courseList(idSet: 5) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A5)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**Filter by Multiple Values Example:**
```graphql
{
  courseList(idSet: [5, 7]) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A%5B5%2C7%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---
#### Advanced Filtering with `Operator Match` in FormCMS

FormCMS supports advanced filtering options with `Operator Match`, allowing users to combine various conditions for precise queries.

##### `matchAll` Example:
Filters where all specified conditions must be true.  
In this example: `id > 5 and id < 15`.

```graphql
{
  courseList(id: {matchType: matchAll, gt: 5, lt: 15}) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(id%3A%7BmatchType%3AmatchAll%2Cgt%3A5%2Clt%3A15%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

##### `matchAny` Example:
Filters where at least one of the conditions must be true.  
In this example: `name starts with "A"` or `name starts with "I"`.

```graphql
{
  courseList(name: [{matchType: matchAny}, {startsWith: "A"}, {startsWith: "I"}]) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(name%3A%5B%7BmatchType%3AmatchAny%7D%2C%20%7BstartsWith%3A%22A%22%7D%2C%7BstartsWith%3A%22I%22%7D%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)


---

#### `Filter Expressions` in FormCMS

Filter Expressions allow precise filtering by specifying a field, including nested fields using JSON path syntax. This enables filtering on subfields for complex data structures.

***Example: Filter by Teacher's Last Name***
This query returns courses taught by a teacher whose last name is "Circuit."

```graphql
{
  courseList(filterExpr: {field: "teacher.lastname", clause: {equals: "Circuit"}}) {
    id
    name
    teacher {
      id
      lastname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(filterExpr%3A%20%7Bfield%3A%20%22teacher.lastname%22%2C%20clause%3A%20%7Bequals%3A%20%22Circuit%22%7D%7D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%20%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D)

---

#### Sorting  
Sorting by a single field
```graphql
{
  courseList(sort:nameDesc){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3AnameDesc)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

Sorting by multiple fields
```graphql
{
  courseList(sort:[level,id]){
    id,
    level
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3A%5Blevel%2Cid%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20level%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---

#### Sort Expressions in FormCMS


Sort Expressions allow sorting by nested fields using JSON path syntax. 

***Example: Sort by Teacher's Last Name***

```graphql
{
  courseList(sortExpr:{field:"teacher.lastname", order:Desc}) {
    id
    name
    teacher {
      id
      lastname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sortExpr%3A%7Bfield%3A%22teacher.lastname%22%2C%20order%3ADesc%7D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%20%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D)

---

#### Pagination
Pagination on root field
```graphql
{
  courseList(offset:2, limit:3){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%20%20%7B%0A%20%20%20%20courseList(offset%3A2%2C%20limit%3A3)%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A)   
Pagination on sub field
```graphql
{
  courseList{
    id,
    name
    materials(limit:2){
      id,
      name
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%20%20%7B%0A%20%20%20%20courseList%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20materials(limit%3A2)%7B%0A%20%20%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%20%20%7D%0A)

---



#### Variable

Variables are used to make queries more dynamic, reusable, and secure.
##### Variable in `Value filter`
```
query ($id: Int!) {
  teacher(idSet: [$id]) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacher(idSet%3A%20%5B%24id%5D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22id%22%3A3%0A%7D)

##### Variable in `Operator Match` filter
```
query ($id: Int!) {
  teacherList(id:{equals:$id}){
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacherList(id%3A%7Bequals%3A%24id%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22id%22%3A%203%0A%7D)

##### Variable in `Filter Expression`
```
query ($years: String) {
  teacherList(filterExpr:{field:"skills.years",clause:{gt:$years}}){
    id
    firstname
    lastname
    skills{
      id
      name
      years
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24years%3A%20String)%20%7B%0A%20%20teacherList(filterExpr%3A%7Bfield%3A%22skills.years%22%2Cclause%3A%7Bgt%3A%24years%7D%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%20%20skills%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20years%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22years%22%3A%20%229%22%0A%7D)

##### Variable in Sort 
```
query ($sort_field:TeacherSortEnum) {
  teacherList(sort:[$sort_field]) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24sort_field%3ATeacherSortEnum)%20%7B%0A%20%20teacherList(sort%3A%5B%24sort_field%5D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%22sort_field%22%3A%20%22idDesc%22%7D)
##### Variable in Sort Expression
```
query ($sort_order:  SortOrderEnum) {
  courseList(sortExpr:{field:"teacher.id", order:$sort_order}){
    id,
    name,
    teacher{
      id,
      firstname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24sort_order%3A%20%20SortOrderEnum)%20%7B%0A%20%20courseList(sortExpr%3A%7Bfield%3A%22teacher.id%22%2C%20order%3A%24sort_order%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%2C%0A%20%20%20%20teacher%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20firstname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22sort_order%22%3A%20%22Asc%22%0A%7D)

##### Variable in Pagination
```
query ($offset:Int) {
  teacherList(limit:2, offset:$offset) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24offset%3AInt)%20%7B%0A%20%20teacherList(limit%3A2%2C%20offset%3A%24offset)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%09%22offset%22%3A%202%0A%7D)

---

#### Required vs Optional
If you want a variable to be mandatory, you can add a  `!` to the end of the type
```
query ($id: Int!) {
  teacherList(id:{equals:$id}){
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacherList(id%3A%7Bequals%3A%24id%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D)

Explore the power of FormCMS GraphQL and streamline your development workflow!
***
***
### Saved Query

**Realtime queries** may expose excessive technical details, potentially leading to security vulnerabilities.

**Saved Queries** address this issue by abstracting the GraphQL query details. They allow clients to provide only variables, enhancing security while retaining full functionality.

---

#### Transitioning from **Real-Time Queries** to **Saved Queries**

##### Using `OperationName` as the Saved Query Identifier
In FormCMS, the **Operation Name** in a GraphQL query serves as a unique identifier for saved queries. For instance, executing the following query automatically saves it as `TeacherQuery`:

```graphql
query TeacherQuery($id: Int) {
  teacherList(idSet: [$id]) {
    id
    firstname
    lastname
    skills {
      id
      name
    }
  }
}
```

[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20TeacherQuery(%24id%3A%20Int)%20%7B%0A%20%20teacherList(idSet%3A%5B%24id%5D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%20%20skills%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&operationName=TeacherQuery)

---

##### Saved Query Endpoints
FormCMS generates two API endpoints for each saved query:

1. **List Records:**  
   [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery)

2. **Single Record:**  
   [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/single/](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/single)

---

##### Using REST API Query Strings as Variables
The Saved Query API allows passing variables via query strings:

- **Single Value:**  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/?id=3](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/?id=3)

- **Array of Values:**  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?id=3&id=4](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?id=3&id=4)  
  This passes `[3, 4]` to the `idSet` argument.

---

#### Additional Features of `Saved Query`

Beyond performance and security improvements, `Saved Query` introduces enhanced functionalities to simplify development workflows.

---

##### Pagination by `offset`
Built-in variables `offset` and `limit` enable efficient pagination. For example:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=2&offset=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=2&offset=2)

---

##### `offset` Pagination for Subfields
To display a limited number of subfield items (e.g., the first two skills of a teacher), use the JSON path variable, such as `skills.limit`:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2)

---

##### Pagination by `cursor`
For large datasets, `offset` pagination can strain the database. For example, querying with `offset=1000&limit=10` forces the database to retrieve 1010 records and discard the first 1000.

To address this, `Saved Query` supports **cursor-based pagination**, which reduces database overhead.  
Example response for [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3):

```json
[
  {
    "hasPreviousPage": false,
    "cursor": "eyJpZCI6M30"
  },
  {
  },
  {
    "hasNextPage": true,
    "cursor": "eyJpZCI6NX0"
  }
]
```

- If `hasNextPage` of the last record is `true`, use the cursor to retrieve the next page:  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&last=eyJpZCI6NX0](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&last=eyJpZCI6NX0)

- Similarly, if `hasPreviousPage` of the first record is `true`, use the cursor to retrieve the previous page:  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&first=eyJpZCI6Nn0](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&first=eyJpZCI6Nn0)

---

##### Cursor-Based Pagination for Subfields
Subfields also support cursor-based pagination. For instance, querying [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2) returns a response like this:

```json
[
  {
    "id": 3,
    "firstname": "Jane",
    "lastname": "Debuggins",
    "hasPreviousPage": false,
    "skills": [
      {
        "hasPreviousPage": false,
        "cursor": "eyJpZCI6MSwic291cmNlSWQiOjN9"
      },
      {
        "hasNextPage": true,
        "cursor": "eyJpZCI6Miwic291cmNlSWQiOjN9"
      }
    ],
    "cursor": "eyJpZCI6M30"
  }
]
```

To fetch the next two skills, use the cursor:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/part/skills?limit=2&last=eyJpZCI6Miwic291cmNlSWQiOjN9](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/part/skills?limit=2&last=eyJpZCI6Miwic291cmNlSWQiOjN9)

Below is a rewritten version of the **Asset Type** and **Distinct** chapters from your GraphQL Query documentation. The rewrite aims to improve clarity, structure, and readability while preserving the technical details.

---

### Asset Type

In FormCMS, attributes with display types such as `image`, `file`, or `gallery` are represented as **Asset Objects** in GraphQL query results. These objects correspond to assets stored in the system's centralized Asset Library (see the **Asset Library** section for details). When querying entities with these attributes, the response includes structured asset data, such as the asset’s `Path`, `Url`, `Name`, `Title`, and other metadata.

#### Example Query
Consider a `course` entity with an `image` field:
```graphql
{
  courseList {
    id
    name
    image {
      id
      path
      url
      name
      title
      size
      type
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%20%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20image%20%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20path%0A%20%20%20%20%20%20url%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20title%0A%20%20%20%20%20%20size%0A%20%20%20%20%20%20type%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D)

#### Response Example
```json
{
  "data": {
    "courseList": [
      {
        "id": 1,
        "name": "Introduction to GraphQL",
        "image": {
          "id": "abc123",
          "path": "2025-03-abc123",
          "url": "/files/2025-03-abc123",
          "name": "graphql_intro.jpg",
          "title": "GraphQL Course Banner",
          "size": 102400,
          "type": "image/jpeg"
        }
      }
    ]
  }
}
```

#### Benefits
- **Consistency**: The `url` field provides a fixed access point to the asset, ensuring reliable retrieval across the application.
- **Metadata**: Fields like `title` can be used as captions or `alt` text for images, enhancing accessibility and SEO.
- **Flexibility**: The Asset Object structure supports various file types (`image`, `file`, `gallery`) with a uniform response format.

---

### Distinct

When querying related entities in FormCMS, joining tables can result in duplicate records due to one-to-many relationships. The `DISTINCT` keyword helps eliminate these duplicates, but it has limitations that require careful query design.

#### Why Use `DISTINCT`?
Consider the following data structure:
- **Posts**: `[{id: 1, title: "p1"}]`
- **Tags**: `[{id: 1, name: "t1"}, {id: 2, name: "t2"}]`
- **Post_Tag**: `[{post_id: 1, tag_id: 1}, {post_id: 1, tag_id: 2}]`

A query joining these tables might look like this in SQL:
```sql
SELECT posts.id, posts.title
FROM posts
LEFT JOIN post_tag ON posts.id = post_tag.post_id
LEFT JOIN tags ON post_tag.tag_id = tags.id
WHERE tags.id > 0;
```

Without `DISTINCT`, the result duplicates the post for each tag:
```json
[
  {"id": 1, "title": "p1"},  // For tag_id: 1
  {"id": 1, "title": "p1"}   // For tag_id: 2
]
```

Using `DISTINCT` ensures each post appears only once:
```sql
SELECT DISTINCT posts.id, posts.title
FROM posts
LEFT JOIN post_tag ON posts.id = post_tag.post_id
LEFT JOIN tags ON post_tag.tag_id = tags.id
WHERE tags.id > 0;
```
Result:
```json
[
  {"id": 1, "title": "p1"}
]
```

#### Limitation of `DISTINCT`
In some databases, such as SQL Server, `DISTINCT` cannot be applied to fields of type `TEXT` (or large data types like `NTEXT` or `VARCHAR(MAX)`). Including such fields in a query with `DISTINCT` causes errors.

#### Solution/Workaround
To address this limitation, split the entity’s queries into two parts: 

1. **List Query**: Retrieves a lightweight list of records without `TEXT` fields, using `DISTINCT` to avoid duplicates. 
```
{
    postList {
       id
       title
   }
}
```
2. **Detail Query**: Retrieves full details, including `TEXT` fields, by querying a single record using its ID. 
```
{
    post(idSet: 1) {
       id
       title
       description  # TEXT field
   }
}
```


#### Example with GraphQL
List query to avoid duplicates:
```
{
  postList {
    id
    title
    tags {
      id
      name
    }
  }
}
```
Detail query for a specific post:
```graphql
{
  post(idSet: 1) {
    id
    title
    description  # TEXT field, only queried here
    tags {
      id
      name
    }
  }
}
```

#### Benefits
- **Efficiency**: The list query remains lightweight and deduplicated.
- **Compatibility**: Avoids database-specific limitations on `DISTINCT`.
- **Flexibility**: Developers can fetch detailed data only when needed.

---
</details>