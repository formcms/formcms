



---

## User Portal

<details>  
<summary>  
Users can access their view history, liked posts, and bookmarked posts in a personalized portal.  
</summary> 

The **User Portal** in FormCMS provides a centralized interface for users to manage their social activity, including viewing their interaction history, liked posts, and bookmarked content. This enhances user engagement by offering a tailored experience to revisit and organize content.

### History
Users can view a list of all items they have previously accessed, such as pages, posts, or other content. Each item in the history is displayed with a clickable link, allowing users to easily revisit the content.

### Liked Items
The Liked Items section displays all posts or content that the user has liked. Users can browse their liked items, with options to unlike content or click through to view the full item, fostering seamless interaction with preferred content.

### Bookmarked Items
Users can organize and view their saved content in the Bookmarked Items section. Bookmarks can be grouped into custom folders for easy categorization, enabling users to efficiently manage and access their saved items by folder or as a complete list.

### Configuration
The User Portal displays items with the following metadata:
- **Image**: A thumbnail or visual representation of the item.
- **Title**: The primary name or heading of the item.
- **Subtitle**: A brief description or secondary text for the item.
- **URL**: The link to access the full item.
- **PublishedAt**: The publication date and time of the item.

Metadata mappings are configured on the **Entity Settings** page, where administrators can define how data fields map to the portal's display. The following settings are available:

- **PageUrl**: Specifies the base URL for item links (e.g., "/content/").
- **BookmarkQuery**: Defines the query used to fetch bookmarked items.
- **BookmarkQueryParamName**: Sets the parameter name for the query (e.g., "id").
- **BookmarkTitleField**: Maps the field containing the item's title.
- **BookmarkSubtitleField**: Maps the field for the item's subtitle.
- **BookmarkImageField**: Maps the field for the item's image URL.
- **BookmarkPublishTimeField**: Maps the field for the item's publication timestamp.

These settings allow for flexible customization, ensuring the User Portal displays content accurately and consistently across history, liked items, and bookmarked items.

</details>