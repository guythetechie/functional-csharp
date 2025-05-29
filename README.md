# Functional C#

A minimal set of functional programming classes for C#. Copy any class you need directly into your project — no NuGet package will be provided, and no external depencies are required. For more robust and feature-rich libraries, see [LanguageExt](https://github.com/louthy/language-ext) or [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions).

> **Note:** Null checks are intentionally omitted; we rely on the compiler’s nullable reference type analysis for safety.

## API Documentation

| Type | Description |
|------|-------------|
| [Option&lt;T&gt;](#optiont) | Represents an optional value that may or may not exist |
| [Result&lt;T&gt;](#resultt) | Represents the result of an operation that can succeed or fail |
| [Either&lt;TLeft, TRight&gt;](#eithertleft-tright) | Represents a value that can be one of two types |
| [Error](#error) | Represents error information with multiple messages or exceptions |
| [Enumerable Extensions](#enumerable-extensions) | Extensions for working with `IEnumerable<T>` |
| [AsyncEnumerable Extensions](#asyncenumerable-extensions) | Extensions for working with `IAsyncEnumerable<T>` |
| [Dictionary Extensions](#dictionary-extensions) | Extensions for safe dictionary operations |

### Option&lt;T&gt;

Represents an optional value that may or may not exist. Useful for avoiding null reference exceptions and making the absence of values explicit.

```csharp
// Creating Some values
Option<string> userName = Option.Some("alice");
Option<int> userAge = Option.Some(25);

// Creating None values
Option<string> missingValue = Option.None;
var notFound = Option<int>.None();

// Implicit conversion
Option<string> fromValue = "hello";      // Some("hello")
Option<string> fromNone = Option.None;   // None
```

#### `Match<T2>(Func<T, T2> some, Func<T2> none)`
Pattern matches on the option, executing the appropriate function.

```csharp
Option<string> userEmail = GetUserEmail(userId);
string displayText = userEmail.Match(
    email => $"Contact: {email}",
    () => "No email provided"
);
```

#### `Match(Action<T> some, Action none)`
Pattern matches for side effects without returning a value.

```csharp
Option<User> currentUser = GetCurrentUser();
currentUser.Match(
    user => Console.WriteLine($"Welcome, {user.Name}!"),
    () => Console.WriteLine("Please log in")
);
```

#### `Map<T2>(Func<T, T2> mapper)`
Transforms the value if present, otherwise returns None.

```csharp
Option<string> userInput = GetUserInput();
Option<int> inputLength = userInput.Map(input => input.Length);

// Chain transformations
Option<decimal> price = Option.Some("29.99");
Option<decimal> tax = price.Map(p => decimal.Parse(p))
                           .Map(p => p * 0.08m); // Some(2.3992m)
```

#### `Bind<T2>(Func<T, Option<T2>> binder)`
Applies an Option-returning function to the value. If the value is not present, returns None.

```csharp
Option<string> userId = Option.Some("123");
Option<User> user = userId.Bind(id => FindUserById(id));
Option<string> userEmail = user.Bind(u => GetUserEmail(u.Id));

static Option<User> FindUserById(string id) =>
    id == "123"
        ? Option.Some(new User("John"))
        : Option.None;
```

#### `Where(Func<T, bool> predicate)`
Filters the option based on a predicate. Returns None if there is no value, or if the predicate is false.

```csharp
Option<int> userAge = Option.Some(17);
Option<int> adultAge = userAge.Where(age => age >= 18) // None (17 is less than 18)

Option<int> validAge = Option.Some(25);
Option<int> filteredAge = validAge.Where(age => age >= 18); // Some(25)
```

#### LINQ Support

```csharp
// Combine multiple optional values
var orderSummary =
    from userId in GetCurrentUserId()
    from user in FindUser(userId)
    from cart in GetUserCart(user.Id)
    select $"Order for {user.Name}: {cart.ItemCount} items";

orderSummary.Match(
    summary => Console.WriteLine(summary),
    () => Console.WriteLine("Unable to create order summary")
);
```

#### `IfNone<T>(Func<T> defaultProvider)`
Provides a default value when the option is None.

```csharp
Option<string> userName = GetUserName();
string displayName = userName.IfNone(() => "Anonymous User");
```

#### `IfNoneThrow<T>(Func<Exception> getException)`
Throws an exception when the option is None. The exception is only created if needed.

```csharp
Option<DatabaseConnection> connection = EstablishConnection();
DatabaseConnection activeConnection = connection.IfNoneThrow(
    () => new InvalidOperationException("Failed to establish database connection")
);
```

#### `Iter(Action<T> action)`
Executes an action if the option contains a value.

```csharp
Option<LogEntry> latestEntry = GetLatestLogEntry();
latestEntry.Iter(entry => Console.WriteLine($"Latest: {entry.Message}"));
```

#### `IterTask(Func<T, ValueTask> asyncAction)`
Executes an async action if the option contains a value.

```csharp
Option<string> filePath = GetConfigFilePath();
await filePath.IterTask(async path => await SaveConfigAsync(path));
```

---

### Result&lt;T&gt;

Represents the result of an operation that can either succeed with a value or fail with an error. Eliminates exception-based error handling for expected failure scenarios.

```csharp
// Creating success results
Result<User> userResult = Result.Success(new User("Alice"));
Result<int> ageResult = Result.Success(25);

// Creating error results
Result<User> errorResult = Result.Error<User>(Error.From("User not found"));
Result<int> negativeResult = Result.Error("Value cannot be negative."); // Uses implicit conversion of string -> Error
Result<Request> parseResult = Result.Error(new JsonException("Could not parse request.")) // Uses implicit conversion of Exception -> Error

// Implicit conversion
Result<string> fromValue = "success";                    // Success
Result<string> fromError = Error.From("failure");       // Error
```

#### `Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)`
Pattern matches on the result, executing the appropriate function.

```csharp
Result<User> userResult = AuthenticateUser(credentials);
string message = userResult.Match(
    user => $"Welcome back, {user.Name}!",
    error => $"Login failed: {error}"
);
```

#### `Match(Action<T> onSuccess, Action<Error> onError)`
Pattern matches for side effects without returning a value.

```csharp
Result<Order> orderResult = ProcessOrder(orderRequest);
orderResult.Match(
    order => Console.WriteLine($"Order {order.Id} processed successfully"),
    error => Console.WriteLine($"Order processing failed: {error}")
);
```

#### `Map<T2>(Func<T, T2> mapper)`
Transforms the success value, otherwise preserves the error.

```csharp
Result<string> userInput = ValidateInput(request);
Result<UserCommand> command = userInput.Map(input => ParseCommand(input));

// Transform errors are preserved
Result<string> invalidInput = Result.Error<string>(Error.From("Invalid format"));
Result<UserCommand> failedCommand = invalidInput.Map(input => ParseCommand(input)); // Error("Invalid format")
```

#### `MapError<T>(Func<Error, Error> mapper)`
Transforms the error if the result is in error; otherwise preserves the success value.

```csharp
// Success values are preserved unchanged
Result<User> successResult = Result.Success(new User("Alice"));
Result<User> unchangedSuccess = successResult.MapError(error => Error.From("Won't be called")); // Success(User("Alice"))

// Error transformation with context
Result<ConfigFile> configResult = LoadConfig("settings.json");
Result<ConfigFile> contextualError = configResult.MapError(error =>
    error + Error.From($"Failed to load configuration from settings.json"));
```

#### `Bind<T2>(Func<T, Result<T2>> binder)`
Chains operations that return Results, allowing for sequential error handling.

```csharp
// Chain validation and processing steps
Result<Payment> paymentResult =
    Result.Success(orderRequest)
          .Bind(req => PaymentGateway.Validate(req))
          .Bind(valid => PaymentGateway.Charge(valid));
```

#### LINQ Support

```csharp
// Chain operations with automatic error handling
var result = from request in ValidateRequest(httpRequest)
             from user in AuthenticateUser(request.Token)
             from data in FetchUserData(user.Id)
             select new ApiResponse(data);

result.Match(
    response => SendResponse(response),
    error => SendErrorResponse(error)
);
```

#### `IfError<T>(Func<Error, T> errorHandler)`
Provides a fallback value when the result is an error.

```csharp
Result<User> userResult = GetUser(userId);
User user = userResult.IfError(error => new User("Guest"));
```

#### `IfError<T>(Func<Error, Result<T>> errorHandler)`
Provides a fallback Result when the current result is an error. This allows for chaining error recovery operations that might themselves fail.

```csharp
Result<User> userResult = GetUser(userId);
Result<User> recoveredResult = userResult.IfError(error => 
    GetUserFromCache(userId)); // Recovery operation that might also fail

// Chain multiple fallback strategies
Result<Config> configResult = LoadPrimaryConfig()
    .IfError(_ => LoadBackupConfig())
    .IfError(_ => LoadDefaultConfig());
```

#### `IfErrorThrow<T>()`
Throws an exception when the result is an error.

```csharp
Result<DatabaseConnection> connectionResult = ConnectToDatabase();
DatabaseConnection connection = connectionResult.IfErrorThrow(); // Throws if connection failed
```

#### `Iter(Action<T> action)`
Executes an action if the result is successful.

```csharp
Result<Report> reportResult = GenerateReport(parameters);
reportResult.Iter(report => SaveReportToFile(report));
```

#### `IterTask(Func<T, ValueTask> asyncAction)`
Executes an async action if the result is successful.

```csharp
Result<EmailMessage> emailResult = ComposeEmail(recipient, subject, body);
await emailResult.IterTask(async email => await SendEmailAsync(email));
```

#### `ToOption<T>()`
Converts a Result to an Option. Success values become Some, errors become None.

```csharp
Result<User> userResult = GetUser(userId);
Option<User> userOption = userResult.ToOption();

userOption.Match(
    user => Console.WriteLine($"Found user: {user.Name}"),
    () => Console.WriteLine("User not found")
);
```

---

### Either&lt;TLeft, TRight&gt;

Represents a value that can be one of two types. Useful for representing alternatives where both possibilities are valid outcomes.

```csharp
// Creating Left values
Either<LocalFile, RemoteFile> localSource = Either.Left<LocalFile, RemoteFile>(new LocalFile("./data.json"));

// Creating Right values
Either<LocalFile, RemoteFile> remoteSource = Either.Right<LocalFile, RemoteFile>(new RemoteFile("https://api.example.com/data"));

// Using static methods
Either<CachedData, FreshData> cachedResult = Either.Left<CachedData, FreshData>(new CachedData(timestamp, data));
Either<CachedData, FreshData> freshResult = Either.Right<CachedData, FreshData>(new FreshData(apiResponse));
```

#### `Match<T>(Func<TLeft, T> onLeft, Func<TRight, T> onRight)`
Pattern matches on the either, executing the appropriate function.

```csharp
Either<DatabaseResult, CacheResult> dataSource = GetData(useCache: true);
string sourceInfo = dataSource.Match(
    dbResult => $"Loaded from database: {dbResult.RecordCount} records",
    cacheResult => $"Loaded from cache: {cacheResult.Age} seconds old"
);
```

#### `Match(Action<TLeft> onLeft, Action<TRight> onRight)`
Pattern matches for side effects without returning a value.

```csharp
Either<EmailNotification, SmsNotification> notification = ChooseNotificationMethod(user);
notification.Match(
    email => SendEmail(email.Address, email.Subject, email.Body),
    sms => SendSms(sms.PhoneNumber, sms.Message)
);
```

#### `Map<TRight2>(Func<TRight, TRight2> mapper)`
Transforms the right value, otherwise preserves the left value.

```csharp
Either<ErrorMessage, UserData> userData = LoadUserData(userId);
Either<ErrorMessage, string> displayName = userData.Map(data => data.FullName);

// Left values are preserved unchanged
Either<ErrorMessage, UserData> errorCase = Either.Left<ErrorMessage, UserData>(new ErrorMessage("Not found"));
Either<ErrorMessage, string> errorResult = errorCase.Map(data => data.FullName); // Left(ErrorMessage("Not found"))
```

#### `Bind<TRight2>(Func<TRight, Either<TLeft, TRight2>> binder)`
Chains operations that return Either values.

```csharp
// Try local cache then fallback to remote
Either<CacheMiss, Product> productSrc =
    Cache.TryGet(productId)
         .MapRight<Product>(p => p)
         .IfLeft(_ => RemoteApi.FetchProduct(productId));

productSrc.Match(
    _       => Console.WriteLine("Loaded from cache"),
    product => Console.WriteLine($"Fetched {product.Name} from API")
);
```

#### LINQ Support

```csharp
// Process data from different sources
var result = from source in DetermineDataSource(config)
             from data in LoadFromSource(source)
             from processed in ProcessData(data)
             select new ProcessedResult(processed);
```

#### `IfLeft<TRight>(Func<TLeft, TRight> leftHandler)`
Provides a right value when the either contains a left value.

```csharp
Either<DefaultConfig, CustomConfig> configChoice = LoadUserConfig();
CustomConfig finalConfig = configChoice.IfLeft(defaultCfg => ConvertToCustomConfig(defaultCfg));
```

#### `IfRight<TLeft>(Func<TRight, TLeft> rightHandler)`
Provides a left value when the either contains a right value.

```csharp
Either<BasicPlan, PremiumPlan> userPlan = Either.Right<BasicPlan, PremiumPlan>(premiumFeatures);
BasicPlan planForLogging = userPlan.IfRight(premium => CreateBasicSummary(premium));
```

#### `IfLeftThrow<TRight>(Exception exception)`
Throws an exception when the either contains a left value.

```csharp
Either<TrialVersion, FullVersion> softwareVersion = CheckLicense();
FullVersion licensed = softwareVersion.IfLeftThrow(new InvalidOperationException("Full license required"));
```

#### `Iter(Action<TRight> action)`
Executes an action if the either contains a right value.

```csharp
Either<PreviewMode, PublishMode> mode = DeterminePublishingMode(document);
mode.Iter(publish => ExecutePublishWorkflow(publish));
```

---

### Error

Represents error information that can contain multiple error messages or wrap exceptions.

```csharp
// Single error message
Error singleError = Error.From("User not found");

// Multiple error messages
Error multipleErrors = Error.From("Invalid email", "Password too short", "Username taken");

// From exception
Error exceptionError = Error.From(new FileNotFoundException("Config file missing"));

// Implicit conversion from string
Error implicitError = "Something went wrong";

// Implicit conversion from exception
Error implicitException = new InvalidOperationException("Can't do that.");
```

#### `operator +(Error left, Error right)`
Combines multiple errors into a single error.

```csharp
Error emailError = Error.From("Invalid email format");
Error passwordError = Error.From("Password too weak");
Error combinedError = emailError + passwordError; // Error with both messages: "Invalid email format", "Password too weak"
```

#### `Messages`
Gets all error messages as an immutable set.

```csharp
Error error = Error.From("Error 1", "Error 2");
error.Messages.Iter(message => Console.WriteLine($"Error: {message}"));
```

#### `ToException()`
Converts the error to an appropriate exception.

```csharp
Error singleError = Error.From("Database connection failed");
Exception singleException = singleError.ToException(); // InvalidOperationException with message "Database connection failed"

Error multipleErrors = Error.From("Error 1", "Error 2");
Exception aggregateException = multipleErrors.ToException(); // AggregateException containing multiple InvalidOperationExceptions
```

#### Exceptional Errors

```csharp
var fileError = Error.From(new FileNotFoundException("settings.json"));
Exception originalException = fileError.ToException(); // The original FileNotFoundException, not a wrapped exception
```

---

### Enumerable Extensions

Extensions for working with `IEnumerable<T>` in a functional style.

#### `Head<T>()`
Returns the first element as an Option, or None if the sequence is empty.

```csharp
List<Customer> customers = GetActiveCustomers();
Option<Customer> firstCustomer = customers.Head();

firstCustomer.Match(
    customer => Console.WriteLine($"First customer: {customer.Name}"),
    () => Console.WriteLine("No customers found")
);

// Safe alternative to .First()
int[] numbers = { };
Option<int> firstNumber = numbers.Head(); // None (instead of throwing an exception)
```

#### `Choose<T, T2>(Func<T, Option<T2>> selector)`
Applies a function to each element and returns only the successful transformations.

```csharp
// Parse valid integers from mixed input
string[] inputs = { "42", "invalid", "100", "", "7" };
var validNumbers = inputs.Choose(input =>
    int.TryParse(input, out var number) ? Option.Some(number) : Option.None);

validNumbers.Iter(number => Console.WriteLine($"Valid number: {number}")); // Output: 42, 100, 7
```

#### `Traverse<T, T2>(Func<T, Result<T2>> selector, CancellationToken cancellationToken)`
Applies an operation returning a `Result<T2>` to each element, aggregates successes or combines errors.

```csharp
// Validate all user inputs - succeeds only if all are valid
string[] userInputs = { "john@email.com", "jane@email.com", "bob@email.com" };
Result<ImmutableArray<Email>> validatedEmails = userInputs.Traverse(
    input => ValidateEmail(input),
    CancellationToken.None
); // Success([john@email.com, jane@email.com, bob@email.com])

// If any validation fails, collect all errors
string[] mixedInputs = { "john@email.com", "invalid-email", "jane@email.com", "bad-format" };
Result<ImmutableArray<Email>> failedValidation = mixedInputs.Traverse(
    input => ValidateEmail(input),
    CancellationToken.None
); // Error("Invalid email: invalid-email", "Invalid email: bad-format")
```

#### `Iter<T>(Action<T> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)`
Executes an action on each element in parallel.

```csharp
List<ImageFile> images = GetImagesToProcess();
CancellationToken cancellationToken = GetCancellationToken();

// Process with default parallelism
images.Iter(
    image => ProcessImage(image),
    maxDegreeOfParallelism: Option.None,
    cancellationToken
);

// Process with limited parallelism
images.Iter(
    image => ProcessImage(image),
    maxDegreeOfParallelism: 4, // Max 4 parallel operations, uses implicit conversion of int -> Option<int>
    cancellationToken
);
```

#### `IterTask<T>(Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)`
Executes an async action on each element in parallel.

```csharp
List<EmailAddress> recipients = GetEmailRecipients();
CancellationToken cancellationToken = GetCancellationToken();

await recipients.IterTask(
    async email => await SendEmailAsync(email),
    maxDegreeOfParallelism: 10, // Max 10 concurrent email sends, uses implicit conversion of int -> Option<int>
    cancellationToken
);
```

---

### AsyncEnumerable Extensions

Extensions for working with `IAsyncEnumerable<T>`.

#### `Head<T>(CancellationToken cancellationToken)`
Returns the first element as an Option asynchronously.

```csharp
IAsyncEnumerable<LogEntry> logStream = GetLogStreamAsync();
Option<LogEntry> firstEntry = await logStream.Head(cancellationToken);

firstEntry.Match(
    entry => Console.WriteLine($"Latest log: {entry.Message}"),
    () => Console.WriteLine("No log entries available")
);
```

#### `Choose<T, T2>(Func<T, Option<T2>> selector)`
Applies a function to each element and returns only the successful transformations.

```csharp
// Filter and transform streaming data
IAsyncEnumerable<string> logLines = ReadLogFileAsync("app.log");
IAsyncEnumerable<LogEntry> validEntries = logLines.Choose(line =>
    TryParseLogEntry(line, out var entry)
        ? Option.Some(entry)
        : Option.None
);

await validEntries.IterTask(async entry => {
    if (entry.Level == LogLevel.Error)
    {
        await NotifyAdmins(entry);
    }
}, Option.None, CancellationToken.None); // Only valid log entries are processed, invalid lines are skipped
```

#### `Traverse<T, T2>(Func<T, ValueTask<Result<T2>>> selector, CancellationToken cancellationToken)`
Applies an async operation returning `ValueTask<Result<T2>>` to each element, aggregates successes or combines errors.

```csharp
// Validate file uploads asynchronously
IAsyncEnumerable<UploadedFile> fileStream = GetUploadedFiles();
Result<ImmutableArray<ValidatedFile>> validationResult =
    await fileStream.Traverse(
        async file => await ValidateFileAsync(file),
        CancellationToken.None
    );

validationResult.Match(
    validFiles => Console.WriteLine($"All {validFiles.Length} files are valid"),
    errors => Console.WriteLine($"Validation failed: {errors}")
);
```

#### `IterTask<T>(Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)`
Executes an async action on each element in parallel.

```csharp
IAsyncEnumerable<DatabaseRecord> records = GetRecordsAsync();

await records.IterTask(
    async record => await ProcessRecordAsync(record),
    maxDegreeOfParallelism: 5, // Process 5 records concurrently, uses implicit conversion of int -> Option<int>
    cancellationToken
);
```

---

### Dictionary Extensions

#### `Find<TKey, TValue>(TKey key)`
Safely retrieves a value from a dictionary, returning an Option.

```csharp
// Safe configuration lookup with fallback
Dictionary<string, string> config = LoadConfiguration();
int timeout = config.Find("RequestTimeoutSeconds")
                    .Bind(value => int.TryParse(value, out var parsed)
                                    ? Option.Some(parsed)
                                    : Option.None)
                    .IfNone(() => 30);
```