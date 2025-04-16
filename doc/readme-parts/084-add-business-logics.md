

---
## Adding Business Logic

<details>
<summary>
Learn how to customize your application by adding validation logic, hook functions, and producing events to Kafka.
</summary>

### Adding Validation Logic with Simple C# Expressions

#### Simple C# Validation
You can define simple C# expressions in the `Validation Rule` of attributes using [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso). For example, a rule like `name != null` ensures the `name` attribute is not null.

Additionally, you can specify a `Validation Error Message` to provide users with feedback when validation fails.

#### Using Regular Expressions
`Dynamic Expresso` supports regular expressions, allowing you to write rules like `Regex.IsMatch(email, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")`.

> Note: Since `Dynamic Expresso` doesn't support [verbatim strings](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim), you must escape backslashes (`\`).

---

### Extending Functionality with Hook Functions

To implement custom business logic, such as verifying that a `teacher` entity has valid email and phone details, you can register hook functions to run before adding or updating records:

```csharp
var registry = app.GetHookRegistry();

// Hook function for pre-add validation
registry.EntityPreAdd.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});

// Hook function for pre-update validation
registry.EntityPreUpdate.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});
```

---

### Producing Events to an Event Broker (e.g., Kafka)

To enable asynchronous business logic through an event broker like Kafka, you can produce events using hook functions. This feature requires just a few additional setup steps:

1. Add the Kafka producer configuration:
   ```csharp
   builder.AddKafkaMessageProducer("localhost:9092");
   ```

2. Register the message producer hook:
   ```csharp
   app.RegisterMessageProducerHook();
   ```

Here’s a complete example:

```csharp
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```

With this setup, events are produced to Kafka, allowing consumers to process business logic asynchronously.

</details>