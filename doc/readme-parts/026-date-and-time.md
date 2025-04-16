



---
## Date and Time
<details>
<summary>
The Date and Time system in FormCMS manages how dates and times are displayed and stored, supporting three distinct formats: `localDatetime`, `datetime`, and `date`. It ensures accurate representation across time zones and consistent storage in the database.
</summary>
### Overview   

FormCMS provides three display formats for handling date and time data, each serving a specific purpose:    
    `localDatetime`: Displays dates and times adjusted to the user's browser time zone (e.g., a start time that varies by location).    
    `datetime`: Zone-agnostic, showing the same date and time universally (e.g., a fixed event time).    
    `date`: Zone-agnostic, displaying only the date without time (e.g., a birthday).    

---

### Display Formats

#### `localDatetime`
- **Purpose**: Represents a date and time that adjusts to the user's local time zone, ideal for events like start times where the local context matters.
- **Behavior**: The system converts the stored UTC `datetime` to the browser's time zone for display. For example, an event starting at `2025-03-19 14:00 UTC` would appear as `2025-03-19 09:00 EST` for a user in New York (UTC-5) or `2025-03-19 23:00 JST` for a user in Tokyo (UTC+9).
- **Storage**: When entered, the system converts the user’s local input to UTC before saving. For instance, `2025-03-19 09:00 EST` is stored as `2025-03-19 14:00 UTC`.
- **Use Case**: Event schedules, deadlines, or anything requiring local time awareness.

#### `datetime`
- **Purpose**: Displays a fixed date and time that remains consistent regardless of the user’s time zone, suitable for universal reference points.
- **Behavior**: No conversion occurs; the stored value is shown as-is. For example, `2025-03-19 14:00` is displayed as `2025-03-19 14:00` everywhere.
- **Storage**: Saved exactly as input, without time zone adjustments, assuming it’s a universal time.
- **Use Case**: Logs, publication timestamps, or system events where a single point in time applies globally.

#### `date`
- **Purpose**: Represents only a date without time, zone-agnostic, and consistent across all users.
- **Behavior**: Displayed as a date only (e.g., `2025-03-19`), with no time component or zone consideration.
- **Storage**: Stored as a `datetime` with the time set to `00:00:00` (midnight), typically in UTC for consistency, but the time portion is ignored in display.
- **Use Case**: Birthdays, anniversaries, or any date-specific data where time is irrelevant.

---

### Storage in Database

- **System-Generated Timestamps**: All automatically generated times (e.g., `CreatedAt`, `UpdatedAt`) are stored as UTC `datetime` values (e.g., `2025-03-19 14:00:00Z`). This ensures a universal reference point for auditing and synchronization.
- **`localDatetime` Handling**:     
  Input: Converted from the user’s local time (based on browser settings) to UTC before storage.    
  Output: Converted from UTC back to the user’s local time zone for display.
- **`datetime` Handling**: Stored and retrieved as entered, with no conversion, assuming it’s a fixed point in time.
- **`date` Handling**: Stored as a `datetime` with the time component set to `00:00:00` (e.g., `2025-03-19 00:00:00Z`), though only the date part is used in display.

---

### Examples

1. **Event Start (`localDatetime`)**:
    - User in New York enters: `2025-03-19 09:00 EST`.
    - Stored as: `2025-03-19 14:00:00Z` (UTC).
    - Displayed in Tokyo: `2025-03-19 23:00 JST`.

2. **Log Entry (`datetime`)**:
    - Entered and stored as: `2025-03-19 14:00`.
    - Displayed everywhere as: `2025-03-19 14:00`.

3. **Birthday (`date`)**:
    - Entered as: `2025-03-19`.
    - Stored as: `2025-03-19 00:00:00Z`.
    - Displayed as: `2025-03-19`.

---

### Benefits

- **Consistency**: UTC storage for system times ensures reliable auditing and cross-time-zone integrity.
- **Flexibility**: `localDatetime` adapts to user locations, while `datetime` and `date` provide universal clarity.
- **Simplicity**: Clear separation of use cases reduces confusion in data entry and display.
- **Scalability**: Standardized UTC storage supports global applications without time zone conflicts.

</details>