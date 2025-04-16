



---
## Concurrent Update Protection
<details>
<summary>
Protect user from dirty write(concurrent update)
</summary>

###  How does it work?

Return the updated_at timestamp when fetching the item. When updating, compare the stored updated_at with the one in the request. If they differ, reject the update

### When Is updatedAt Checked
- During updates
- During Deletions

If a concurrent modification is detected, the system will throw the following exception:  
"Error: Concurrent Write Detected. Someone else has modified this item since you last accessed it. Please refresh the data and try again."

</details>  
