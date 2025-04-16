



---

## Publish / Preview Content
<details>
<summary>
This feature allows content creators to plan and organize their work, saving drafts for later completion.
</summary>

### Content Publication Status
Content can have one of the following publication statuses:
- **`draft`**
- **`scheduled`**
- **`published`**
- **`unpublished`**

Only content with the status **`published`** can be retrieved through GraphQL queries.

---

### Setting Default Publication Status in the Schema Builder
When defining an entity in the Schema Builder, you can configure its default publication status as either **`draft`** or **`published`**.

---

### Managing Publication Status in the Admin Panel
On the content edit page, you can:
- **Publish**: Make content immediately available.
- **Unpublish**: Remove content from public view.
- **Schedule**: Set a specific date and time for the content to be published.

---
### Preview Draft/Scheduled/Unpublished Content

By default, only published content appears in query results. 
If you want to preview how the content looks on a page before publishing, you can add the query parameter `preview=1` to the page URL.

For a more convenient approach, you can set the **Preview URL** in the **Entity Settings** page. 
[Example Entity Settings Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=27)

Once set, you can navigate to the **Entity Management** page and simply click the **Preview** button to view the content in preview mode.
[Example Content Manage Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/admin/entities/course/3?ref=https%3A%2F%2Ffluent-cms-admin.azurewebsites.net%2F_content%2FFormCMS%2Fadmin%2Fentities%2Fcourse%3Foffset%3D0%26limit%3D20#)
---

### Publication Worker
The **Publication Worker** automates the process of updating scheduled items in batches, transitioning them to the **`published`** status at the appropriate time.

</details>  
