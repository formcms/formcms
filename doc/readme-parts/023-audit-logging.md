



---
## Audit Logging
<details>
<summary>
Audit logging in FormCMS helps maintain accountability and provides a historical record of modifications made to entities within the system. 
</summary>

###  Audit Log Entity
An audit log entry captures essential information about each modification. The entity structure includes the following fields:

- **UserId** (*string*): The unique identifier of the user performing the action.
- **UserName** (*string*): The name of the user.
- **Action** (*ActionType*): The type of action (Create, update, Delete) performed. 
- **EntityName** (*string*): The name of the entity affected.
- **RecordId** (*string*): The unique identifier of the record modified.
- **RecordLabel** (*string*): A human-readable label for the record.
- **Payload** (*Record*): The data associated with the action.
- **CreatedAt** (*DateTime*): The timestamp when the action occurred.

### When Is Audit Log Added
An audit log entry is created when a user performs any of the following actions:

- **Creating** a new record.
- **Updating** an existing record.
- **Deleting** a record.
### How to view Audit Log
Audit logs can be accessed and searched by users with appropriate permissions. The following roles have access:

- **Admin**
- **Super Admin**

These users can:
- View a list of audit logs.
- Search logs by user, entity, or action type.
- Filter logs by date range.

### Benefits of Audit Logging
- Ensures transparency and accountability.
- Helps with troubleshooting and debugging.
- Provides insights into system usage and modifications.
- Supports compliance with regulatory requirements.

By maintaining a detailed audit trail, the System enhances security and operational efficiency, ensuring that all modifications are tracked and can be reviewed when necessary.
</details>  