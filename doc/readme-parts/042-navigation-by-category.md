


---

## Navigation by Category  
<details>  
<summary>Category trees and breadcrumbs provide structure, context, and clarity, enabling users to find and navigate data more efficiently.</summary>  

### **Demo of Category Tree and Breadcrumb Navigation**  
- **Category Tree Navigation**:  
  [View Demo](https://fluent-cms-admin.azurewebsites.net/course)  
- **Breadcrumb Navigation**:  
  [View Demo](https://fluent-cms-admin.azurewebsites.net/course/27)  

---

### **Creating a Category Entity**
To create a category entity in the Schema Builder, include `parent` and `children` attributes.
- **Example Configuration**:  
  [Edit Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=103)

---

### **Configuration Options for Navigation**
- **DataType: `lookup`** & **DisplayType: `TreeSelect`**  
  Use this configuration to display a category as a property.  
  [Edit Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=96)

- **DataType: `junction`** & **DisplayType: `Tree`**  
  Use this configuration to enable category-based navigation.  
  [Edit Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/edit.html?schema=entity&id=27)

---

### **Using Navigation Components in Page Designer**
- **Tree Layer Menu**:  
  Use the `Tree Layer Menu` component for hierarchical navigation.  
  [Edit Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/page.html?schema=page&id=42)

- **Breadcrumbs**:  
  Use the `Breadcrumbs` component to display navigation paths.  
  [Edit Example](https://fluent-cms-admin.azurewebsites.net/_content/FormCMS/schema-ui/page.html?schema=page&id=33)

</details>  