

---
## Online Course System Frontend
<details> 
<summary> 
Having established our understanding of FormCMS essentials like Entity, Query, and Page, we're ready to build a frontend for an online course website.
</summary>

---
### Key Pages

- **Home Page (`home`)**: The main entry point, featuring sections like *Featured Courses* and *Advanced Courses*. Each course links to its respective **Course Details** page.
- **Course Details (`course/{course_id}`)**: Offers detailed information about a specific course and includes links to the **Teacher Details** page.
- **Teacher Details (`teacher/{teacher_id}`)**: Highlights the instructor’s profile and includes a section displaying their latest courses, which link back to the **Course Details** page.


```plaintext
             Home Page
                 |
                 |
       +-------------------+
       |                   |
       v                   v
 Latest Courses       Course Details 
       |                   |        
       |                   |       
       v                   v            
Course Details <-------> Teacher Details 
```

---

### Designing the Home Page

1. **Drag and Drop Components**: Use the  FormCMS page designer to drag a `Content-B` component.
2. **Set Data Source**: Assign the component's data source to the `course` query.
3. **Link Course Items**: Configure the link for each course to `/pages/course/{{id}}`. The Handlebars expression `{{id}}` is dynamically replaced with the actual course ID during rendering.

![Link Example](https://raw.githubusercontent.com/formcms/formcms/doc/doc/screenshots/designer-link.png)

---

### Creating the Course Details Page

1. **Page Setup**: Name the page `course/{course_id}` to capture the `course_id` parameter from the URL (e.g., `/pages/course/20`).
2. **Query Configuration**: The variable `{course_id:20}` is passed to the `course` query, generating a `WHERE id IN (20)` clause to fetch the relevant course data.
3. **Linking to Teacher Details**: Configure the link for each teacher item on this page to `/pages/teacher/{{teacher.id}}`. Handlebars dynamically replaces `{{teacher.id}}` with the teacher’s ID. For example, if a teacher object has an ID of 3, the link renders as `/pages/teacher/3`.

---

### Creating the Teacher Details Page

1. **Page Setup**: Define the page as `teacher/{teacher_id}` to capture the `teacher_id` parameter from the URL.
2. **Set Data Source**: Assign the `teacher` query as the page’s data source.

#### Adding a Teacher’s Courses Section

- Drag a `ECommerce A` component onto the page.
- Set its data source to the `course` query, filtered by the teacher’s ID (`WHERE teacher IN (3)`).

![Teacher Page Designer](https://raw.githubusercontent.com/formcms/formcms/doc/doc/screenshots/designer-teacher.png)

When rendering the page, the `PageService` automatically passes the `teacher_id` (e.g., `{teacher_id: 3}`) to the query.
</details>  
