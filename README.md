# Functional C#

A minimal set of functional programming classes for C#. Copy any class you need directly into your project — no NuGet package will be provided, and no external depencies are required. For more robust and feature-rich libraries, see [LanguageExt](https://github.com/louthy/language-ext) or [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions).

> **Note:** Null checks are intentionally omitted; we rely on the compiler’s nullable reference type analysis for safety.

## API Documentation

| Type | Description |
|------|-------------|
| [Option&lt;T&gt;](#optiont) | Represents an optional value that may or may not exist |
| [Result&lt;T&gt;](#resultt) | Represents the result of an operation that can either succeed with a value or fail with an error |
| [Either&lt;TLeft, TRight&gt;](#eithertleft-tright) | Represents a value that can be one of two types, either left or right |
| [Error](#error) | Represents error information with multiple messages or exceptions |
| [Unit](#unit) | Represents the absence of a meaningful value (functional equivalent of void) |
| [Enumerable Extensions](#enumerable-extensions) | Provides extension methods for working with IEnumerable&lt;T&gt; in a functional style |
| [AsyncEnumerable Extensions](#asyncenumerable-extensions) | Provides extension methods for working with IAsyncEnumerable&lt;T&gt; in a functional style |
| [Dictionary Extensions](#dictionary-extensions) | Provides extension methods for safe dictionary operations |

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

#### `T2 Match<T2>(Func<T, T2> some, Func<T2> none)`
Pattern matches on the option state.

```csharp
Option<string> userEmail = GetUserEmail(userId);
string displayText = userEmail.Match(
    email => $"Contact: {email}",
    () => "No email provided"
);
```

#### `void Match(Action<T> some, Action none)`
Pattern matches on the option state for side effects.

```csharp
Option<User> currentUser = GetCurrentUser();
currentUser.Match(
    user => Console.WriteLine($"Welcome, {user.Name}!"),
    () => Console.WriteLine("Please log in")
);
```

#### `Option<T2> Map<T2>(Func<T, T2> mapper)`
Transforms the option value using a function.

```csharp
Option<string> userInput = GetUserInput();
Option<int> inputLength = userInput.Map(input => input.Length);

// Chain transformations
Option<decimal> price = Option.Some("29.99");
Option<decimal> tax = price.Map(p => decimal.Parse(p))
                           .Map(p => p * 0.08m); // Some(2.3992m)
```

#### `ValueTask<Option<T2>> MapTask<T2>(Func<T, ValueTask<T2>> asyncMapper)`
Asynchronously transforms the option value using a function that returns a ValueTask.

```csharp
Option<string> filePath = Option.Some("config.json");
Option<string> fileContent = await filePath.MapTask(async path =>
    await File.ReadAllTextAsync(path));

// Chain async transformations
Option<User> user = Option.Some("user123");
Option<UserProfile> profile = await user.MapTask(async userId =>
    await LoadUserProfileAsync(userId));
```

#### `Option<T2> Bind<T2>(Func<T, Option<T2>> binder)`
Chains option operations together (monadic bind).

```csharp
Option<string> userId = Option.Some("123");
Option<User> user = userId.Bind(id => FindUserById(id));
Option<string> userEmail = user.Bind(u => GetUserEmail(u.Id));

static Option<User> FindUserById(string id) =>
    id == "123"
        ? Option.Some(new User("John"))
        : Option.None;
```

#### `ValueTask<Option<T2>> BindTask<T2>(Func<T, ValueTask<Option<T2>>> asyncBinder)`
Asynchronously chains option operations together (monadic bind with async function).

```csharp
Option<string> userId = Option.Some("123");
Option<User> user = await userId.BindTask(async id => await FindUserByIdAsync(id));
Option<string> userEmail = await user.BindTask(async u => await GetUserEmailAsync(u.Id));

static async ValueTask<Option<User>> FindUserByIdAsync(string id)
{
    var user = await DatabaseContext.Users.FindAsync(id);
    return user != null ? Option.Some(user) : Option.None;
}
```

#### `Option<T> Where(Func<T, bool> predicate)`
Filters an option using a predicate.

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

#### `T IfNone(Func<T> defaultProvider)`
Provides a fallback value for empty options.

```csharp
Option<string> userName = GetUserName();
string displayName = userName.IfNone(() => "Anonymous User");
```

#### `Option<T> IfNone(Func<Option<T>> fallbackProvider)`
Provides a fallback option for empty options.

```csharp
Option<Config> primaryConfig = LoadPrimaryConfig();
Option<Config> configWithFallback = primaryConfig.IfNone(() => LoadDefaultConfig());

// Chain multiple fallback strategies
Option<User> user = GetUserFromCache(userId)
    .IfNone(() => GetUserFromDatabase(userId))
    .IfNone(() => GetGuestUser());

user.Match(
    u => Console.WriteLine($"Found user: {u.Name}"),
    () => Console.WriteLine("No user available")
);
```

#### `T IfNoneThrow(Func<Exception> getException)`
Extracts the option value or throws an exception.

```csharp
Option<DatabaseConnection> connection = EstablishConnection();
DatabaseConnection activeConnection = connection.IfNoneThrow(
    () => new InvalidOperationException("Failed to establish database connection")
);
```

#### `void Iter(Action<T> action)`
Executes an action if the option contains a value.

```csharp
Option<LogEntry> latestEntry = GetLatestLogEntry();
latestEntry.Iter(entry => Console.WriteLine($"Latest: {entry.Message}"));
```

#### `ValueTask IterTask(Func<T, ValueTask> asyncAction)`
Executes an async action if the option contains a value.

```csharp
Option<string> filePath = GetConfigFilePath();
await filePath.IterTask(async path => await SaveConfigAsync(path));
```

#### `T? IfNoneNull()`
Converts the option to a nullable reference type.

```csharp
Option<string> userName = GetUserName();
string? nullableUserName = userName.IfNoneNull(); // Returns null if None, otherwise the string value

#### `T? IfNoneNullable()`
Converts the option to a nullable value type.

```csharp
Option<int> userId = GetUserId();
int? nullableUserId = userId.IfNoneNullable(); // Returns null if None, otherwise the int value
```

---

### Result&lt;T&gt;

Represents the result of an operation that can either succeed with a value or fail with an error.

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

#### `TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)`
Pattern matches on the result state.

```csharp
Result<User> userResult = AuthenticateUser(credentials);
string message = userResult.Match(
    user => $"Welcome back, {user.Name}!",
    error => $"Login failed: {error}"
);
```

#### `void Match(Action<T> onSuccess, Action<Error> onError)`
Pattern matches on the result state for side effects.

```csharp
Result<Order> orderResult = ProcessOrder(orderRequest);
orderResult.Match(
    order => Console.WriteLine($"Order {order.Id} processed successfully"),
    error => Console.WriteLine($"Order processing failed: {error}")
);
```

#### `Result<T2> Map<T2>(Func<T, T2> mapper)`
Transforms the success value using a function.

```csharp
Result<string> userInput = ValidateInput(request);
Result<UserCommand> command = userInput.Map(input => ParseCommand(input));

// Transform errors are preserved
Result<string> invalidInput = Result.Error<string>(Error.From("Invalid format"));
Result<UserCommand> failedCommand = invalidInput.Map(input => ParseCommand(input)); // Error("Invalid format")
```

#### `ValueTask<Result<T2>> MapTask<T2>(Func<T, ValueTask<T2>> asyncMapper)`
Asynchronously transforms the success value using a function that returns a ValueTask.

```csharp
Result<string> configPath = Result.Success("appsettings.json");
Result<Configuration> config = await configPath.MapTask(async path =>
    await LoadConfigurationAsync(path));

// Errors are preserved through async transformations
Result<string> invalidPath = Result.Error<string>(Error.From("File not found"));
Result<Configuration> errorResult = await invalidPath.MapTask(async path =>
    await LoadConfigurationAsync(path)); // Error("File not found")
```

#### `Result<T> MapError(Func<Error, Error> mapper)`
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
#### `Result<T2> Bind<T2>(Func<T, Result<T2>> binder)`
Chains result operations together (monadic bind).

```csharp
// Chain validation and processing steps
Result<Payment> paymentResult =
    Result.Success(orderRequest)
          .Bind(req => PaymentGateway.Validate(req))
          .Bind(valid => PaymentGateway.Charge(valid));
```

#### `ValueTask<Result<T2>> BindTask<T2>(Func<T, ValueTask<Result<T2>>> asyncBinder)`
Asynchronously chains result operations together (monadic bind with async function).

```csharp
Result<Order> orderResult = Result.Success(orderRequest);
Result<ValidatedOrder> validatedResult = await orderResult
    .BindTask(async req => await PaymentGateway.ValidateAsync(req));
Result<PaymentResult> paymentResult = await validatedResult
    .BindTask(async valid => await PaymentGateway.ChargeAsync(valid));
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

#### `T IfError(Func<Error, T> errorHandler)`
Provides a fallback value for error results.

```csharp
Result<User> userResult = GetUser(userId);
User user = userResult.IfError(error => new User("Guest"));
```

#### `Result<T> IfError(Func<Error, Result<T>> errorHandler)`
Provides a fallback result for error results. This allows for chaining error recovery operations that might themselves fail.

```csharp
Result<User> userResult = GetUser(userId);
Result<User> recoveredResult = userResult.IfError(error => 
    GetUserFromCache(userId)); // Recovery operation that might also fail

// Chain multiple fallback strategies
Result<Config> configResult = LoadPrimaryConfig()
    .IfError(_ => LoadBackupConfig())
    .IfError(_ => LoadDefaultConfig());
```

#### `T IfErrorThrow()`
Extracts the success value or throws the error as an exception.

```csharp
Result<DatabaseConnection> connectionResult = ConnectToDatabase();
DatabaseConnection connection = connectionResult.IfErrorThrow(); // Throws if connection failed
```

#### `void Iter(Action<T> action)`
Executes an action if the result is successful.

```csharp
Result<Report> reportResult = GenerateReport(parameters);
reportResult.Iter(report => SaveReportToFile(report));
```

#### `ValueTask IterTask(Func<T, ValueTask> asyncAction)`
Executes an async action if the result is successful.

```csharp
Result<EmailMessage> emailResult = ComposeEmail(recipient, subject, body);
await emailResult.IterTask(async email => await SendEmailAsync(email));
```

#### `Option<T> ToOption()`
Converts a Result to an Option. Success values become Some, errors become None.

```csharp
Result<User> userResult = GetUser(userId);
Option<User> userOption = userResult.ToOption();

userOption.Match(
    user => Console.WriteLine($"Found user: {user.Name}"),
    () => Console.WriteLine("User not found")
);
```

#### `T? IfErrorNull()`
Converts the result to a nullable reference type. Returns the value if successful, otherwise returns null.

```csharp
Result<string> apiResponse = CallApi();
string? nullableResponse = apiResponse.IfErrorNull(); // Returns null if error, otherwise the string value
```

#### `T? IfErrorNullable()`
Converts the result to a nullable value type. Returns the value if successful, otherwise returns null.

```csharp
Result<int> calculationResult = PerformCalculation();
int? nullableResult = calculationResult.IfErrorNullable(); // Returns null if error, otherwise the int value
```

---

### Either&lt;TLeft, TRight&gt;

Represents a value that can be one of two types, either left or right.

```csharp
// Creating Left values
Either<LocalFile, RemoteFile> localSource = Either.Left<LocalFile, RemoteFile>(new LocalFile("./data.json"));

// Creating Right values
Either<LocalFile, RemoteFile> remoteSource = Either.Right<LocalFile, RemoteFile>(new RemoteFile("https://api.example.com/data"));

// Using static methods
Either<CachedData, FreshData> cachedResult = Either.Left<CachedData, FreshData>(new CachedData(timestamp, data));
Either<CachedData, FreshData> freshResult = Either.Right<CachedData, FreshData>(new FreshData(apiResponse));

// Implicit conversion
Either<string, int> leftValue = "error message";         // Left("error message")
Either<string, int> rightValue = 42;                     // Right(42)
```

#### `T Match<T>(Func<TLeft, T> onLeft, Func<TRight, T> onRight)`
Pattern matches on the either state.

```csharp
Either<DatabaseResult, CacheResult> dataSource = GetData(useCache: true);
string sourceInfo = dataSource.Match(
    dbResult => $"Loaded from database: {dbResult.RecordCount} records",
    cacheResult => $"Loaded from cache: {cacheResult.Age} seconds old"
);
```

#### `void Match(Action<TLeft> onLeft, Action<TRight> onRight)`
Pattern matches on the either state for side effects.

```csharp
Either<EmailNotification, SmsNotification> notification = ChooseNotificationMethod(user);
notification.Match(
    email => SendEmail(email.Address, email.Subject, email.Body),
    sms => SendSms(sms.PhoneNumber, sms.Message)
);
```

#### `Either<TLeft, TRight2> Map<TRight2>(Func<TRight, TRight2> mapper)`
Transforms the right value using a function.

```csharp
Either<ErrorMessage, UserData> userData = LoadUserData(userId);
Either<ErrorMessage, string> displayName = userData.Map(data => data.FullName);

// Left values are preserved unchanged
Either<ErrorMessage, UserData> errorCase = Either.Left<ErrorMessage, UserData>(new ErrorMessage("Not found"));
Either<ErrorMessage, string> errorResult = errorCase.Map(data => data.FullName); // Left(ErrorMessage("Not found"))
```

#### `Either<TLeft, TRight2> Bind<TRight2>(Func<TRight, Either<TLeft, TRight2>> binder)`
Chains either operations together (monadic bind).

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

#### `TRight IfLeft(Func<TLeft, TRight> leftHandler)`
Extracts the right value or converts the left value.

```csharp
Either<DefaultConfig, CustomConfig> configChoice = LoadUserConfig();
CustomConfig finalConfig = configChoice.IfLeft(defaultCfg => ConvertToCustomConfig(defaultCfg));
```

#### `TLeft IfRight(Func<TRight, TLeft> rightHandler)`
Extracts the left value or converts the right value.

```csharp
Either<BasicPlan, PremiumPlan> userPlan = Either.Right<BasicPlan, PremiumPlan>(premiumFeatures);
BasicPlan planForLogging = userPlan.IfRight(premium => CreateBasicSummary(premium));
```

#### `TRight IfLeftThrow(Exception exception)`
Extracts the right value or throws an exception.

```csharp
Either<TrialVersion, FullVersion> softwareVersion = CheckLicense();
FullVersion licensed = softwareVersion.IfLeftThrow(new InvalidOperationException("Full license required"));
```

#### `TLeft IfRightThrow(Exception exception)`
Extracts the left value or throws an exception.

```csharp
Either<BasicPlan, PremiumPlan> userPlan = Either.Left<BasicPlan, PremiumPlan>(basicFeatures);
BasicPlan plan = userPlan.IfRightThrow(new InvalidOperationException("Expected basic plan but got premium"));
```

#### `void Iter(Action<TRight> action)`
Executes an action if the either contains a right value.

```csharp
Either<PreviewMode, PublishMode> mode = DeterminePublishingMode(document);
mode.Iter(publish => ExecutePublishWorkflow(publish));
```

#### `ValueTask IterTask(Func<TRight, ValueTask> asyncAction)`
Executes an async action if the either contains a right value.

```csharp
Either<CacheResult, DatabaseResult> dataSource = GetDataSource();
await dataSource.IterTask(async dbResult => await ProcessDatabaseResultAsync(dbResult));
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

#### `Error operator +(Error left, Error right)`
Combines multiple errors into a single error.

```csharp
Error emailError = Error.From("Invalid email format");
Error passwordError = Error.From("Password too weak");
Error combinedError = emailError + passwordError; // Error with both messages: "Invalid email format", "Password too weak"
```

#### `ImmutableHashSet<string> Messages`
Gets all error messages as an immutable set.

```csharp
Error error = Error.From("Error 1", "Error 2");
error.Messages.Iter(message => Console.WriteLine($"Error: {message}"));
```

#### `Exception ToException()`
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

Provides extension methods for working with `IEnumerable<T>` in a functional style.

#### `Option<T> Head<T>()`
Gets the first element of the sequence as an option.

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

#### `Option<T> SingleOrNone<T>()`
Returns the single element of the sequence as an option.

```csharp
// Safe alternative to .Single()
List<User> admins = GetAdminUsers();
Option<User> singleAdmin = admins.SingleOrNone();

singleAdmin.Match(
    admin => Console.WriteLine($"Admin user: {admin.Name}"),
    () => Console.WriteLine("Expected exactly one admin, but found zero or multiple")
);

// Works with any collection size
int[] oneElement = { 42 };
Option<int> single = oneElement.SingleOrNone(); // Some(42)

int[] empty = { };
Option<int> none = empty.SingleOrNone(); // None

int[] multiple = { 1, 2, 3 };
Option<int> alsoNone = multiple.SingleOrNone(); // None (multiple elements)
```

#### `IEnumerable<T2> Choose<T, T2>(Func<T, Option<T2>> selector)`
Filters and transforms elements using an option-returning selector.

```csharp
// Parse valid integers from mixed input
string[] inputs = { "42", "invalid", "100", "", "7" };
var validNumbers = inputs.Choose(input =>
    int.TryParse(input, out var number) ? Option.Some(number) : Option.None);

validNumbers.Iter(number => Console.WriteLine($"Valid number: {number}")); // Output: 42, 100, 7
```

#### `IAsyncEnumerable<T2> Choose<T, T2>(Func<T, ValueTask<Option<T2>>> selector)`
Filters and transforms elements using an async option-returning selector.

```csharp
// Process files asynchronously, keeping only successful operations
string[] filePaths = { "file1.txt", "file2.txt", "file3.txt" };
IAsyncEnumerable<string> fileContents = filePaths.Choose(async path =>
{
    try
    {
        string content = await File.ReadAllTextAsync(path);
        return Option.Some(content);
    }
    catch
    {
        return Option.None; // Skip files that can't be read
    }
});

await fileContents.IterTask(async content =>
    await ProcessFileContentAsync(content));
```

#### `Option<T2> Pick<T, T2>(Func<T, Option<T2>> selector)`
Finds the first element that produces a Some value when transformed.

```csharp
// Find the first valid configuration file
string[] configPaths = { "/etc/app.conf", "/usr/local/etc/app.conf", "./app.conf" };
Option<ConfigFile> config = configPaths.Pick(path =>
    File.Exists(path) ? Option.Some(LoadConfig(path)) : Option.None);

config.Match(
    cfg => Console.WriteLine($"Loaded config from {cfg.Path}"),
    () => Console.WriteLine("No configuration file found")
);

// Find the first even number
int[] numbers = { 1, 3, 5, 8, 9, 12 };
Option<int> firstEven = numbers.Pick(n =>
    n % 2 == 0 ? Option.Some(n) : Option.None); // Some(8)
```

#### `Result<ImmutableArray<T2>> Traverse<T, T2>(Func<T, Result<T2>> selector, CancellationToken cancellationToken)`
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

#### `Option<ImmutableArray<T2>> Traverse<T, T2>(Func<T, Option<T2>> selector, CancellationToken cancellationToken)`
Applies an operation returning an `Option<T2>` to each element, returning `Some` if all succeed or `None` if any fail.

```csharp
// Parse all numbers - succeeds only if all are valid
string[] numberStrings = { "42", "123", "999" };
Option<ImmutableArray<int>> parsedNumbers = numberStrings.Traverse(
    input => int.TryParse(input, out var result) ? Option.Some(result) : Option.None,
    CancellationToken.None
); // Some([42, 123, 999])

// If any parsing fails, the entire operation returns None
string[] mixedInputs = { "42", "invalid", "123" };
Option<ImmutableArray<int>> failedParsing = mixedInputs.Traverse(
    input => int.TryParse(input, out var result) ? Option.Some(result) : Option.None,
    CancellationToken.None
); // None
```

#### `(ImmutableArray<T1>, ImmutableArray<T2>) Unzip<T1, T2>()`
Separates an enumerable of tuples into a tuple of immutable arrays. This is the inverse operation of `Zip`.

```csharp
// Split coordinate pairs into separate x and y collections
var coordinates = new[] { (1, 10), (2, 20), (3, 30), (4, 40) };
var (xValues, yValues) = coordinates.Unzip();

// xValues: [1, 2, 3, 4]
// yValues: [10, 20, 30, 40]

// Split names and ages from user data
var users = new[] { ("Alice", 25), ("Bob", 30), ("Charlie", 35) };
var (names, ages) = users.Unzip();

Console.WriteLine($"Names: {string.Join(", ", names)}");     // Names: Alice, Bob, Charlie
Console.WriteLine($"Ages: {string.Join(", ", ages)}");       // Ages: 25, 30, 35

// Works with any tuple types
var dataPoints = new[] { ("A", 1.5), ("B", 2.0), ("C", 3.5) };
var (labels, values) = dataPoints.Unzip();
// labels: ["A", "B", "C"], values: [1.5, 2.0, 3.5]
```

#### `void Iter<T>(Action<T> action, CancellationToken cancellationToken = default)`
Executes an action on each element sequentially.

```csharp
List<ImageFile> images = GetImagesToProcess();

// Process sequentially
images.Iter(image => ProcessImage(image));

// Process with cancellation support
images.Iter(
    image => ProcessImage(image),
    cancellationToken: GetCancellationToken()
);
```

#### `void IterParallel<T>(Action<T> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken = default)`
Executes an action on each element in parallel.

```csharp
List<ImageFile> images = GetImagesToProcess();

// Process with unlimited parallelism
images.IterParallel(
    image => ProcessImage(image),
    maxDegreeOfParallelism: Option.None
);

// Process with limited parallelism
images.IterParallel(
    image => ProcessImage(image),
    maxDegreeOfParallelism: 4 // Max 4 parallel operations, uses implicit conversion of int -> Option<int>
);
```

#### `ValueTask IterTask<T>(Func<T, ValueTask> action, CancellationToken cancellationToken = default)`
Executes an async action on each element sequentially.

```csharp
List<EmailAddress> recipients = GetEmailRecipients();

// Process sequentially
await recipients.IterTask(async email => await SendEmailAsync(email));

// Process with cancellation support
await recipients.IterTask(
    async email => await SendEmailAsync(email),
    cancellationToken: GetCancellationToken()
);
```

#### `ValueTask IterTaskParallel<T>(Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken = default)`
Executes an async action on each element in parallel.

```csharp
List<EmailAddress> recipients = GetEmailRecipients();

// Process with unlimited parallelism
await recipients.IterTaskParallel(
    async email => await SendEmailAsync(email),
    maxDegreeOfParallelism: Option.None
);

// Process with limited parallelism
await recipients.IterTaskParallel(
    async email => await SendEmailAsync(email),
    maxDegreeOfParallelism: 10 // Max 10 concurrent email sends, uses implicit conversion of int -> Option<int>
);
```

#### `IEnumerable<T> Tap<T>(Action<T> action)`
Executes a side effect action on each element as it's enumerated, returning the original enumerable unchanged. Useful for debugging, logging, or other side effects in LINQ chains without affecting the data flow.

```csharp
// Debugging and logging
List<User> users = GetUsers()
    .Where(user => user.IsActive)
    .Tap(user => Console.WriteLine($"Processing user: {user.Name}")) // Log processing
    .Where(user => user.HasValidEmail)
    .Tap(user => logger.LogInfo($"Valid user: {user.Id}")) // Additional logging
    .ToList();

// Chain multiple taps for different concerns
var processedOrders = orders
    .Tap(order => auditLogger.Log($"Processing order: {order.Id}"))
    .Where(order => order.IsValid)
    .Tap(order => metrics.IncrementCounter("valid_orders"))
    .Select(order => EnrichOrder(order));
```

---

### AsyncEnumerable Extensions

Provides extension methods for working with `IAsyncEnumerable<T>` in a functional style.

#### `ValueTask<Option<T>> Head<T>(CancellationToken cancellationToken)`
Gets the first element of the async sequence as an option.

```csharp
IAsyncEnumerable<LogEntry> logStream = GetLogStreamAsync();
Option<LogEntry> firstEntry = await logStream.Head(cancellationToken);

firstEntry.Match(
    entry => Console.WriteLine($"Latest log: {entry.Message}"),
    () => Console.WriteLine("No log entries available")
);
```

#### `IAsyncEnumerable<T2> Choose<T, T2>(Func<T, Option<T2>> selector)`
Filters and transforms async elements using an option-returning selector.

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
}); // Only valid log entries are processed, invalid lines are skipped
```

#### `IAsyncEnumerable<T2> Choose<T, T2>(Func<T, ValueTask<Option<T2>>> selector)`
Filters and transforms async elements using an async option-returning selector.

```csharp
// Process streaming data with async validation
IAsyncEnumerable<string> dataStream = GetDataStreamAsync();
IAsyncEnumerable<ProcessedData> validData = dataStream.Choose(async item =>
{
    try
    {
        var validated = await ValidateDataAsync(item);
        var processed = await ProcessDataAsync(validated);
        return Option.Some(processed);
    }
    catch
    {
        return Option.None; // Skip invalid or unprocessable items
    }
});

await validData.IterTask(async data =>
    await SaveToDatabase(data));
```

#### `ValueTask<Option<T2>> Pick<T, T2>(Func<T, Option<T2>> selector, CancellationToken cancellationToken)`
Finds the first async element that produces a Some value when transformed.

```csharp
// Find the first valid data record in a stream
IAsyncEnumerable<string> dataStream = ReadDataStreamAsync();
Option<DataRecord> firstValid = await dataStream.Pick(line =>
    TryParseDataRecord(line, out var record)
        ? Option.Some(record)
        : Option.None,
    cancellationToken); // Returns as soon as a valid record is found
```

#### `ValueTask<Result<ImmutableArray<T2>>> Traverse<T, T2>(Func<T, ValueTask<Result<T2>>> selector, CancellationToken cancellationToken)`
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

#### `ValueTask<Option<ImmutableArray<T2>>> Traverse<T, T2>(Func<T, ValueTask<Option<T2>>> selector, CancellationToken cancellationToken)`
Applies an async operation returning `ValueTask<Option<T2>>` to each element, returning `Some` if all succeed or `None` if any fail.

```csharp
// Process file downloads asynchronously - succeeds only if all downloads complete
IAsyncEnumerable<string> urls = GetDownloadUrls();
Option<ImmutableArray<DownloadedFile>> downloadResult =
    await urls.Traverse(
        async url => await TryDownloadFileAsync(url), // Returns Option<DownloadedFile>
        CancellationToken.None
    );

downloadResult.Match(
    files => Console.WriteLine($"Successfully downloaded {files.Length} files"),
    () => Console.WriteLine("One or more downloads failed")
);
```

#### `ValueTask<(ImmutableArray<T1>, ImmutableArray<T2>)> Unzip<T1, T2>(CancellationToken cancellationToken)`
Asynchronously separates an async enumerable of tuples into a tuple of immutable arrays. This is the async version of the synchronous Unzip operation.

```csharp
// Process streaming coordinate data
IAsyncEnumerable<(int X, int Y)> coordinateStream = GetCoordinateStreamAsync();
var (xValues, yValues) = await coordinateStream.Unzip(cancellationToken);

// Process async user data stream
IAsyncEnumerable<(string Name, int Age)> userStream = GetUserStreamAsync();
var (names, ages) = await userStream.Unzip(cancellationToken);

Console.WriteLine($"Names: {string.Join(", ", names)}");
Console.WriteLine($"Ages: {string.Join(", ", ages)}");

// Works with any async tuple stream
IAsyncEnumerable<(string Label, double Value)> dataStream = GetDataPointStreamAsync();
var (labels, values) = await dataStream.Unzip(cancellationToken);
// labels: ImmutableArray<string>, values: ImmutableArray<double>
```

#### `ValueTask IterTask<T>(Func<T, ValueTask> action, CancellationToken cancellationToken = default)`
Executes an async action on each element sequentially.

```csharp
IAsyncEnumerable<DatabaseRecord> records = GetRecordsAsync();

// Process sequentially
await records.IterTask(async record => await ProcessRecordAsync(record));

// Process with cancellation support
await records.IterTask(
    async record => await ProcessRecordAsync(record),
    cancellationToken: GetCancellationToken()
);
```

#### `ValueTask IterTaskParallel<T>(Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken = default)`
Executes an async action on each element in parallel.

```csharp
IAsyncEnumerable<DatabaseRecord> records = GetRecordsAsync();

// Process with unlimited parallelism
await records.IterTaskParallel(
    async record => await ProcessRecordAsync(record),
    maxDegreeOfParallelism: Option.None
);

// Process with limited parallelism
await records.IterTaskParallel(
    async record => await ProcessRecordAsync(record),
    maxDegreeOfParallelism: 5 // Process 5 records concurrently, uses implicit conversion of int -> Option<int>
);
```

#### `IAsyncEnumerable<T> Tap<T>(Action<T> action)`
Executes a side effect action on each element as it's enumerated, returning the original async enumerable unchanged. Useful for debugging, logging, or other side effects in async LINQ chains without affecting the data flow.

```csharp
// Sync logging in async streams
IAsyncEnumerable<Order> processedOrders = orderStream
    .Where(order => order.RequiresValidation)
    .Tap(order => Console.WriteLine($"Validating order: {order.Id}")) // Sync logging
    .Where(order => order.IsValid)
    .Tap(order => metrics.IncrementCounter("valid_orders")) // Sync metrics
    .Select(order => EnrichOrder(order));

await foreach (var order in processedOrders)
{
    await ProcessOrderAsync(order);
}
```

#### `IAsyncEnumerable<T> TapTask<T>(Func<T, ValueTask> action)`
Executes an async side effect action on each element as it's enumerated, returning the original async enumerable unchanged. Useful for debugging, logging, or other async side effects in async LINQ chains without affecting the data flow.

```csharp
// Async logging and monitoring
IAsyncEnumerable<Order> processedOrders = orderStream
    .Where(order => order.RequiresValidation)
    .TapTask(async order => await auditLogger.LogAsync($"Validating order: {order.Id}")) // Async logging
    .Where(order => order.IsValid)
    .TapTask(async order => await notificationService.NotifyAsync(order.CustomerId)) // Async notifications
    .Select(order => EnrichOrder(order));

// Chain sync and async taps
await foreach (var order in processedOrders)
{
    await ProcessOrderAsync(order);
}
```
---

### Dictionary Extensions

Provides extension methods for safe dictionary operations.

#### `Option<TValue> Find<TKey, TValue>(TKey key)`
Safely retrieves a value from a dictionary.

```csharp
// Safe configuration lookup with fallback
Dictionary<string, string> config = LoadConfiguration();
int timeout = config.Find("RequestTimeoutSeconds")
                    .Bind(value => int.TryParse(value, out var parsed)
                                    ? Option.Some(parsed)
                                    : Option.None)
                    .IfNone(() => 30);
```
---

### Unit

Represents the absence of a meaningful value. Unit is used in functional programming to indicate that a function performs side effects but doesn't return a meaningful value. It's the functional equivalent of void, but as a proper type that can be used in generic contexts.

```csharp
// Using Unit as a return type for side-effect functions
Func<string, Unit> logMessage = message => {
    Console.WriteLine($"Log: {message}");
    return Unit.Instance;
};

// Unit in generic contexts where void cannot be used
Option<Unit> maybeLog = shouldLog 
    ? Option.Some(logMessage("Hello world"))
    : Option.None;

// String representation
Unit unit = Unit.Instance;
Console.WriteLine(unit); // Output: ()
```

#### `static Unit Instance`
A shared instance of Unit. Since all Unit values are logically equivalent, this instance can be reused to avoid unnecessary allocations.

```csharp
// Preferred way to return Unit
public Unit ProcessData(string data)
{
    // ... perform side effects ...
    return Unit.Instance;
}
```