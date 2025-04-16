

---
## Online Course System Backend

<details> 
<summary> 
This section provides detailed guidance on developing a foundational online course system, encompassing key entities: `teacher`, `course`, `lesson`,`skill`, and `material`.
</summary>

### Database Schema

#### 1. **Teachers Table**
The `Teachers` table maintains information about instructors, including their personal and professional details.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `firstname`      | First Name       | String        |
| `lastname`       | Last Name        | String        |
| `email`          | Email            | String        |
| `phone_number`   | Phone Number     | String        |
| `image`          | Image            | String        |
| `bio`            | Bio              | Text          |

#### 2. **Courses Table**
The `Courses` table captures the details of educational offerings, including their scope, duration, and prerequisites.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Course Name      | String        |
| `status`         | Status           | String        |
| `level`          | Level            | String        |
| `summary`        | Summary          | String        |
| `image`          | Image            | String        |
| `desc`           | Description      | Text          |
| `duration`       | Duration         | String        |
| `start_date`     | Start Date       | Datetime      |

#### 3. **Lessons Table**
The `Lessons` table contains detailed information about the lessons within a course, including their title, content, and associated teacher.

| **Field**        | **Header**        | **Data Type** |
|-------------------|-------------------|---------------|
| `id`             | ID                | Int           |
| `name`           | Lesson Name       | String        |
| `description`    | Description       | Text          |
| `teacher`     | Teacher           | Int (Foreign Key) |
| `course`      | Course            | Int (Foreign Key) |
| `created_at`     | Created At        | Datetime      |
| `updated_at`     | Updated At        | Datetime      | 


#### 4. **Skills Table**
The `Skills` table records competencies attributed to teachers.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Skill Name       | String        |
| `years`          | Years of Experience | Int      |
| `created_by`     | Created By       | String        |
| `created_at`     | Created At       | Datetime      |
| `updated_at`     | Updated At       | Datetime      |

#### 5. **Materials Table**
The `Materials` table inventories resources linked to courses.

| **Field**        | **Header**  | **Data Type** |
|-------------------|-------------|---------------|
| `id`             | ID          | Int           |
| `name`           | Name        | String        |
| `type`           | Type        | String        |
| `image`          | Image       | String        |
| `link`           | Link        | String        |
| `file`           | File        | String        |

---

### Relationships
- **Courses to Teachers**: Man-to-One(Each teacher can teach multiple courses; each course is assigned to one teacher. A teacher can exist independently of a course).
- **Teachers to Skills**: Many-to-Many (Multiple teachers can share skills, and one teacher may have multiple skills).
- **Courses to Materials**: Many-to-Many (A course may include multiple materials, and the same material can be used in different courses).
- **Courses to Lessons**: One-to-Many (Each course can have multiple lessons, but each lesson belongs to one course. A lesson cannot exist without a course, as it has no meaning on its own).

---

### Schema Creation via FormCMS Schema Builder

#### Accessing Schema Builder
After launching the web application, locate the **Schema Builder** menu on the homepage to start defining your schema.

#### Adding Entities
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/list.html?schema=entity)  
1. Navigate to the **Entities** section of the Schema Builder.
2. Create entities such as "Teacher" and "Course."
3. For the `Course` entity, add attributes such as `name`, `status`, `level`, and `description`.
---
### Defining Relationships
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=27)  

#### 1. **Course and Teacher (Many-to-One Relationship)**
To establish a many-to-one relationship between the `Course` and `Teacher` entities, you can include a `Lookup` attribute in the `Course` entity. This allows selecting a single `Teacher` record when adding or updating a `Course`.

| **Attribute**   | **Value**    |
|-----------------|--------------|
| **Field**       | `teacher`    |
| **DataType**    | Lookup       |
| **DisplayType** | Lookup       |
| **Options**     | Teacher      |

**Description:** When a course is created or modified, a teacher record can be looked up and linked to the course.

#### 2 **Course and Lesson(One-to-Many Relationship)**
To establish a one-to-many relationship between the `Course` and `Lesson` entities, use a `Collection` attribute in the `Course` entity. This enables associating multiple lessons with a single course.

| **Attribute**   | **Value**  |
|-----------------|------------|
| **Field**       | `lessons`  |
| **DataType**    | Collection |
| **DisplayType** | EditTable  |
| **Options**     | Lesson     |

**Description:** When managing a course , you can manage lessons of this course.

#### 3. **Course and Materials (Many-to-Many Relationship)**
To establish a many-to-many relationship between the `Course` and `Material` entities, use a `Junction` attribute in the `Course` entity. This enables associating multiple materials with a single course.

| **Attribute** | **Value**   |
|---------------|-------------|
| **Field**     | `materials` |
| **DataType**  | Junction    |
| **DisplayType** | Picklist    |
| **Options**   | Material    |

**Description:** When managing a course, you can select multiple material records from the `Material` table to associate with the course.


</details>