

---
## New CMS? — Data Modeling

### Data Modeling in Current CMS Solutions

Most CMS solutions support entity customization and adding custom properties, but they implement these changes in three distinct ways:

1. **Denormalized Key-Value Storage**: Custom properties are stored in a table with columns like ContentItemId, Key, and Value.
2. **JSON Data Storage**: Some CMS platforms store custom properties as JSON data in a document database, while others use relational databases.
3. **Manually Created C# Classes**: Writing code adds custom properties to create classes that the system uses with Entity Framework.

#### The Pros and Cons:
- **Key-Value Storage**: This approach offers flexibility but suffers from performance inefficiencies and lacks relational integrity.
- **Document Database**: Storing data as documents lacks a structured format and makes data integrity harder to enforce.
- **C# Classes**: While my preferred method, it lacks flexibility. Any minor changes require rebuilding and redeploying the system.

### Data Modeling with FormCMS

In contrast, FormCMS adopts a normalized, structured data approach, where each property is mapped to a corresponding table field:

1. **Maximized Relational Database Functionality**: By leveraging indexing and constraints, FormCMS enhances performance and ensures data integrity.
2. **Data Accessibility**: This model allows for easy data integration with other applications, Entity Framework, or even non-C# languages.
3. **Support for Relationships**: FormCMS enables complex relationships (many-to-one, one-to-many, many-to-many), making it easy to provide GraphQL Query out of the box and provide more advanced querying capabilities.