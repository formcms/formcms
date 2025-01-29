



---

## Publish Content
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

### Publication Worker
The **Publication Worker** automates the process of updating scheduled items in batches, transitioning them to the **`published`** status at the appropriate time.

</details>  
