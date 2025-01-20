




---

## Development Guide

<details>
<summary>
The backend is written in ASP.NET Core, the Admin Panel uses React, and the Schema Builder is developed with jQuery.
</summary>

### Overview  
The system comprises three main components:  
1. **Backend** - Developed in ASP.NET Core.  
2. **Admin Panel** - Built using React.  
3. **Schema Builder** - Created with jQuery.  

#### System Diagram  
![System Overview](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/overview.png)

### Repository Links  
- [**Backend Server**](https://github.com/formcms/formcms/tree/main/server/FormCMS)  
- [**Admin Panel UI**](https://github.com/formcms/formcms/tree/main/admin-panel)  
- [**Schema Builder**](https://github.com/formcms/formcms/tree/main/server/FormCMS/wwwroot/schema-ui)  

---

### Backend Server
#### Tools
- **ASP.NET Core**
- **SqlKata** ([SqlKata Documentation](https://sqlkata.com/))

#### Architecture
The backend is influenced by Domain-Driven Design (DDD).  
![DDD Architecture](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/ddd-architecture.png)

Code organization follows this diagram:  
![Backend Code Structure](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/C4_Elements-Backend.png)

##### Core (Domain Layer)
The **Core layer** encapsulates:
- **Descriptors**: Includes `Entity`, `Filter`, `Sort`, and similar components for building queries.
- **HookFactory**: Maintains a global `Hook Registry`, enabling developers to integrate custom plugins.

> **Note**: The Core layer is independent of both the Application and Infrastructure layers.

##### Application Layer
The **Application layer** provides the following functionalities:
1. **CMS**: Entity CRUD, GraphQL Queries, and Page Designer.
2. **Auth**: Manages permissions and roles.
3. **DataLink**: Integrates DocumentDB and Event Streams for scalability.

> Includes `Builders` to configure Dependency Injection and manage Infrastructure components.

##### Infrastructure Layer
The **Infrastructure layer** defines reusable system infrastructural components.
- Application services depend on interfaces instead of implementations.
- Components are designed for portability and can be reused across other projects.

##### Util Layer
A separate **Util component** contains static classes with pure functions.
- Accessible across all layers.

---

### Admin Panel UI
#### Tools
- **React**
- **PrimeReact** ([PrimeReact UI Library](https://primereact.org/))
- **SWR** ([Data Fetching/State Management](https://swr.vercel.app/))

#### Admin Panel Sequence
![Admin Panel Sequence](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/admin-panel-sequence.png)

---

### Schema Builder UI
#### Tools
- **jsoneditor** ([JSON Editor Documentation](https://github.com/json-editor/json-editor))

</details>  