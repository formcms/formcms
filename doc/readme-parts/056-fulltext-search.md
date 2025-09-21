



## Fulltext search
<details>
<summary>
FormCMS's Full-Text Search feature allows users to search for keywords in titles, content, and other fields.
</summary>

### Adding the Comments Component
1. In the Page Designer, drag the `Search Bar` component from the `Blocks` toolbox onto your page.
2. Create a new `Search` page, drag a component from the `Data List` catalog, and bind its query to the `search` query.

### How Full-Text Search Calculates Scores
FormCMS searches for the query keyword in the `title`, `subtitle`, and `content` fields, with keywords in the title receiving the highest score.

</details>