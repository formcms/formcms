



---

## Schema Version Control
<details>  
<summary>  
FormCMS saves each version of schemas, allowing users to roll back to earlier versions. Admins can freely test draft versions, while only published versions take effect.  
</summary>  

### Requirements
To illustrate this feature, let's take a `Page` as an example. Once a page is published, it becomes publicly accessible. You may need version control for two main reasons:

- You want to make changes but ensure they do not take effect until thoroughly tested.
- If issues arise in the latest version, you need the ability to roll back to a previous version.

### Choosing a Version
After making changes, the latest version's status changes to `draft` in the [Page List Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/list.html?type=page).  
To manage versions, click the `View History` button to navigate to the [History Version List Page](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/history.html?schemaId=01JKKB85KWA651945N5W0T6PJR).  
Here, you can select any version and set it to `published` status.

### Testing a `Draft` Version
To preview a draft version, append `sandbox=1` as a query parameter in the URL: [Preview Draft Version Page](https://fluent-cms-admin.azurewebsites.net/story/?sandbox=1).  
Alternatively, click the `View Page` button on the `Page Design` page.

### Compare schema Changes
You can compare the difference between different versions, use the [Schema Diff Tool](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/diff.html?schemaId=01JKKA93AJG2HNY648H9PC16PN&type=query&oldId=126&newId=138).

### Duplicate
You can duplicate any schema version and save it as a new schema.

</details>  