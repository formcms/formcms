

---
## Drag and Drop Page Designer
<details> 
<summary> 
The page designer utilizes the open-source GrapesJS and Handlebars, enabling seamless binding of `GrapesJS Components` with `FormCMS Queries` for dynamic content rendering. 
</summary>

---
### Page Types: Landing Page, Detail Page, and Home Page

#### **Landing Page**
A landing page is typically the first page a visitor sees.  
- **URL format**: `/page/<pagename>`  
- **Structure**: Comprised of multiple sections, each section retrieves data via a `query`.  

**Example**:    
[Landing Page](https://fluent-cms-admin.azurewebsites.net/)    
This page fetches data from:  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured)  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced)  

---

#### **Detail Page**
A detail page provides specific information about an item.  
- **URL format**: `/page/<pagename>/<router parameter>`  
- **Data Retrieval**: FormCMS fetches data by passing the router parameter to a `query`.  

**Example**:  
[Course Detail Page](https://fluent-cms-admin.azurewebsites.net/pages/course/22)  
This page fetches data from:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22](https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22)

---

#### **Home Page**
The homepage is a special type of landing page named `home`.  
- **URL format**: `/pages/home`   
- **Special Behavior**: If no other route matches the path `/`, FormCMS renders `/pages/home` by default.  

**Example**:    
The URL `/` will be resolved to `/pages/home` unless explicitly overridden.  

---
### Introduction to GrapesJS Panels

Understanding the panels in GrapesJS is crucial for leveraging FormCMS's customization capabilities in the Page Designer UI. This section explains the purpose of each panel and highlights how FormCMS enhances specific areas to streamline content management and page design. 

![GrapesJS Toolbox](https://raw.githubusercontent.com/formcms/formcms/doc/doc/screenshots/grapes-toolbox.png)

1. **Style Manager**:
    - Used to customize CSS properties of elements selected on the canvas.
    - *FormCMS Integration*: This panel is left unchanged by FormCMS, as it already provides powerful styling options.

2. **Traits Panel**:
    - Allows modification of attributes for selected elements.
    - *FormCMS Integration*: Custom traits are added to this panel, enabling users to bind data to components dynamically.

3. **Layers Panel**:
    - Displays a hierarchical view of elements on the page, resembling a DOM tree.
    - *FormCMS Integration*: While FormCMS does not alter this panel, it’s helpful for locating and managing FormCMS blocks within complex page designs.

4. **Blocks Panel**:
    - Contains pre-made components that can be dragged and dropped onto the page.
    - *FormCMS Integration*: FormCMS enhances this panel by adding custom-designed blocks tailored for its CMS functionality.

By familiarizing users with these panels and their integration points, this chapter ensures a smoother workflow and better utilization of FormCMS's advanced page-building tools.

---
### Data Binding: Singleton or List

FormCMS leverages [Handlebars expressions](https://github.com/Handlebars-Net/Handlebars.Net) for dynamic data binding in pages and components.

---

#### **Singleton**

Singleton fields are enclosed within `{{ }}` to dynamically bind individual values.

- **Example Page Settings:** [Page Schema Settings](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/page.html?schema=page&id=33)
- **Example Query:** [Retrieve Course Data](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?course_id=22)
- **Example Rendered Page:** [Rendered Course Page](https://fluent-cms-admin.azurewebsites.net/pages/course/22)

---

#### **List**

`Handlebars` supports iterating over arrays using the `{{#each}}` block for repeating data structures.

```handlebars
{{#each course}}
    <li>{{title}}</li>
{{/each}}
```

In FormCMS, you won’t explicitly see the `{{#each}}` statement in the Page Designer. If a block's data source is set to `data-list`, the system automatically generates the loop.

- **Example Page Settings:** [Page Schema Settings](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/page.html?schema=page&id=32)
- **Example Rendered Page:** [Rendered List Page](https://fluent-cms-admin.azurewebsites.net/)
- **Example Queries:**
   - [Featured Courses](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured)
   - [Advanced Level Courses](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced)

---

#### **Steps to Bind a Data Source**

To bind a `Data List` to a component, follow these steps:

1. Drag a block from the **Data List** category in the Page Designer.
2. Open the **Layers Panel** and select the `Data List` component.
3. In the **Traits Panel**, configure the following fields:

| **Field**     | **Description**                                                                                                                                                        |
|---------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Query**     | The query to retrieve data.                                                                                                                                           |
| **Qs**        | Query string parameters to pass (e.g., `?status=featured`, `?level=Advanced`).                                                                                        |
| **Offset**    | Number of records to skip.                                                                                                                                            |
| **Limit**     | Number of records to retrieve.                                                                                                                                        |
| **Pagination**| Options for displaying content:                                                                                                                                       |
|               | - **Button**: Divides content into multiple pages with navigation buttons (e.g., "Next," "Previous," or numbered buttons).                                            |
|               | - **Infinite Scroll**: Automatically loads more content as users scroll. Ideal for a single component at the bottom of the page.                                      |
|               | - **None**: Displays all available content at once without requiring additional user actions.                                                                          |

</details>