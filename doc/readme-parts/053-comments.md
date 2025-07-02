


## Comments Plugin
<details>
<summary>
FormCMS's Comments Plugin enables adding a comments feature to any entity, enhancing user interaction.
</summary>

### Adding the Comments Component
1. In the Page Designer, drag the `Comments` component from the `Blocks` toolbox onto your page. Customize its layout as needed.
2. From the `Layout Manager` toolbox, select the `Comment-form` component and set its `Entity Name` trait.

After configuring, click `Save and Publish` to enable the comments feature. The Comments Plugin is designed for `Detail Pages`, where comments are associated with an `Entity Name` and `RecordId` (automatically retrieved from the page URL parameters).

### Comment Interactions
Authenticated users can add, edit, delete, like, and reply to comments. The Comments Plugin sends events for these actions, which are handled by other plugins. For example:
- The Notification Plugin processes these events to send notice to the comment's creator.
- The Engage Activity Plugin uses these events to update the record's engagement score.

### Integrating Comments with GraphQL
Each `Detail Page` is linked to a FormCMS GraphQL query. To include comments:
- Add the `Comments` field to your GraphQL query.
- The Comments Plugin automatically attaches comment data to the query results.

</details>

