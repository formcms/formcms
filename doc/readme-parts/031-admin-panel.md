



---

## **Admin Panel**
<details>  
<summary>  
The last chapter introduced how to model entities, this chapter introduction how to use Admin-Panel to manage data of those entities.
</summary>  

### **Display Types**
The Admin Panel supports various UI controls to display attributes:

- `"text"`: Single-line text input.
- `"textarea"`: Multi-line text input.
- `"editor"`: Rich text input.

- `"number"`: Single-line text input for numeric values only.
- `"datetime"`: Datetime picker for date and time inputs.
- `"date"`: Date picker for date-only inputs.

- `"image"`: Upload a single image, storing the image URL.
- `"gallery"`: Upload multiple images, storing their URLs.
- `"file"`: Upload a file, storing the file URL.

- `"dropdown"`: Select an item from a predefined list.
- `"multiselect"`: Select multiple items from a predefined list.

- `"lookup"`: Select an item from another entity with a many-to-one relationship (requires `Lookup` data type).
- `"treeSelect"`: Select an item from another entity with a many-to-one relationship (requires `Lookup` data type), items are organized as tree.

- `"picklist"`: Select multiple items from another entity with a many-to-many relationship (requires `Junction` data type).
- `"tree"`: Select multiple items from another entity with a many-to-many relationship (requires `Junction` data type), items are organized as a tree.
- `"edittable"`: Manage items of a one-to-many sub-entity (requires `Collection` data type).  


---
[See this example how to configure entity `category`, so it's item can be organized as tree.](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=103)
### **DataType to DisplayType Mapping Table**
Below is a mapping of valid `DataType` and `DisplayType` combinations:

| **DataType**  | **DisplayType** | **Description**                               |
|---------------|-----------------|-----------------------------------------------|
| Int           | Number          | Input for integers.                           |
| Datetime      | Datetime        | Datetime picker for date and time inputs.     |
| Datetime      | Date            | Date picker for date-only inputs.             |
| String        | Number          | Input for numeric values.                     |
| String        | Datetime        | Datetime picker for date and time inputs.     |
| String        | Date            | Date picker for date-only inputs.             |
| String        | Text            | Single-line text input.                       |
| String        | Textarea        | Multi-line text input.                        |
| String        | Image           | Single image upload.                          |
| String        | Gallery         | Multiple image uploads.                       |
| String        | File            | File upload.                                  |
| String        | Dropdown        | Select an item from a predefined list.        |
| String        | Multiselect     | Select multiple items from a predefined list. |
| Text          | Multiselect     | Select multiple items from a predefined list. |
| Text          | Gallery         | Multiple image uploads.                       |
| Text          | Textarea        | Multi-line text input.                        |
| Text          | Editor          | Rich text editor.                             |
| Lookup        | Lookup          | Select an item from another entity.           |
| Lookup        | TreeSelect      | Select an item from another entity.           |
| Junction      | Picklist        | Select multiple items from another entity.    |
| Lookup        | Tree            | Select multiple items from another entity.    |
| Collection    | EditTable       | Manage items of a sub-entity.                 |  

---

### **List Page**
[Example Course List Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course?offset=0&limit=20)

The **List Page** displays entities in a tabular format, supporting sorting, searching, and pagination for efficient browsing or locating of specific records.


#### **Sorting**
Sort records by clicking the `↑` or `↓` icon in the table header.
- [Order by Created At Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course?offset=0&limit=20&sort[created_at]=-1)
- [Order by Name Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course?offset=0&limit=20&sort[name]=1)

#### **Filtering**
Apply filters by clicking the Funnel icon in the table header.

- [Filter by Created At (2024-09-07)](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course?offset=0&limit=20&created_at[dateIs]=2024-09-07&sort[created_at]=1)
- [Filter by Course Name (Starts with A or C)](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course?offset=0&limit=20&name[operator]=or&name[startsWith]=A&name[startsWith]=C&sort[created_at]=1)

#### **Duplicate**
Clicking the duplicate button opens the "Add New Data" page with prefilled values from the selected record for quick entry.
 
---

### **Detail Page**  
Detail page provides an interface to manage single record.  

#### Example of display types `date`,`image`, `gallery`, `muliselect`, `dropdown`,
[Lesson Detail Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/lesson/6?ref=https%3A%2F%2Ffluent-cms-admin.azurewebsites.net%2F_content%FormCMS%2Fadmin%2Fentities%2Fcourse%2F27%3Fref%3Dhttps%253A%252F%252Ffluent-cms-admin.azurewebsites.net%252F_content%FormCMS%252Fadmin%252Fentities%252Fcourse%253Foffset%253D0%2526limit%253D20).

#### Example of `lookup`,`picklist`,`edittable`
[Course Detail Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course/22)

</details>  

