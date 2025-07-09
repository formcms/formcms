



---
## Subscription

<details>
<summary>
A website can integrate a subscription feature to generate revenue.
</summary>

### Overview
FormCms integrates Stripe to ensure secure payments. FormCms does not store any credit card information; it only uses the Stripe subscription ID to query subscription status.
Admins and users can visit the Stripe website to view transactions and logs.

### Register Stripe Developer Account and Obtain Keys
Follow the Stripe documentation to obtain the Stripe Publishable Key and Secret Key.
Add these keys to the `appSettings.json` file as follows:
```json
"Stripe": {
"SecretKey": "sk_***",
"PublishableKey": "pk_***"
}
```

### Set the Access Level
For the online course demo system at https://fluent-cms-admin.azurewebsites.net/, each course may include multiple lessons.
The course video serves as an introduction, and the first lesson of a course is free. When users attempt to access further lessons, they are restricted and prompted by FormCms to subscribe.

To implement this, add an `accessLevel` field to the `lesson` entity.
Then, include a condition `accessLevel:{lte: $access_level}` in the query to provide data for the Lesson Page:
```graphql
query lesson($lesson_id:Int, $access_level:Int){
lesson(idSet:[$lesson_id],
accessLevel:{lte: $access_level}
){
id, name, description, introduction, accessLevel
}
```

### The Subscription Page
When an unpaid user attempts to access restricted content (requiring a subscription), FormCms redirects them to the Stripe website for payment.
After payment, users can view their subscription status in the user portal.
</details>