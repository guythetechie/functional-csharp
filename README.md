# Functional C#

A minimal set of functional programming classes for C#. Copy any class you need directly into your project. No NuGet package will be provided, and no external dependencies are required. For more robust and feature-rich libraries, see [LanguageExt](https://github.com/louthy/language-ext) or [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions).

> **Note:** Null checks are intentionally omitted; we rely on the compiler’s nullable reference type analysis for safety.

## API Documentation

| Type | Description |
|------|-------------|
| [Option&lt;T&gt;](#optiont) | Represents an optional value that may or may not exist |
| [Result&lt;T&gt;](#resultt) | Represents the result of an operation that can either succeed with a value or fail with an error |
| [Error](#error) | Represents error information with multiple messages or exceptions |
| [Enumerable Extensions](#enumerable-extensions) | Provides extension methods for working with IEnumerable&lt;T&gt; in a functional style |
| [AsyncEnumerable Extensions](#asyncenumerable-extensions) | Provides extension methods for working with IAsyncEnumerable&lt;T&gt; in a functional style |
| [Dictionary Extensions](#dictionary-extensions) | Provides extension methods for safe dictionary operations |
| [Unit](#unit) | Represents the absence of a meaningful value (functional equivalent of void) |

### Option&lt;T&gt;

Represents an optional value that may or may not exist. Useful for avoiding null reference exceptions and making the absence of values explicit.

```csharp
// Creating Some values
Option<string> userName = Option.Some("alice");
Option<int> userAge = Option.Some(25);

// Creating None values
Option<string> missingValue = Option.None;
Option<int> notFound = Option.None;
```

#### `bool IsSome`
Returns `true` if the option contains a value.

```csharp
Option<string> name = Option.Some("alice");
name.IsSome; // true
```

#### `bool IsNone`
Returns `true` if the option is empty.

```csharp
Option<string> missing = Option.None;
missing.IsNone; // true
```

#### `T2 Match<T2>(Func<T, T2> onSome, Func<T2> onNone)`
Returns the result of one of two functions based on the option's state.

```csharp
Option<string> userEmail = GetUserEmail(userId);
string displayText = userEmail.Match(
    email => $"Contact: {email}",
    () => "No email provided"
);
```

#### `void Match(Action<T> onSome, Action onNone)`
Executes one of two actions based on the option's state.

```csharp
Option<User> currentUser = GetCurrentUser();
currentUser.Match(
    user => Console.WriteLine($"Welcome, {user.Name}!"),
    () => Console.WriteLine("Please log in")
);
```

#### `Option<T2> Map<T2>(Func<T, T2> f)`
Applies `f` to the wrapped value.

```csharp
Option<string> userInput = GetUserInput();
Option<int> inputLength = userInput.Map(input => input.Length);

// Chain transformations
Option<string> price = Option.Some("29.99");
Option<decimal> priceWithTax = price.Map(p => decimal.Parse(p))
                                    .Map(p => p * 1.08m); // Some(32.3892m)
```

#### `ValueTask<Option<T2>> MapTask<T2>(Func<T, ValueTask<T2>> f)`
Asynchronously applies `f` to the wrapped value.

```csharp
Option<string> filePath = Option.Some("config.json");
Option<string> fileContent = await filePath.MapTask(async path =>
    await File.ReadAllTextAsync(path));

// Chain async transformations
Option<string> userId = Option.Some("user123");
Option<UserProfile> profile = await userId.MapTask(async id =>
    await LoadUserProfileAsync(id));
```

#### `Option<T2> Bind<T2>(Func<T, Option<T2>> f)`
Applies `f` and flattens the result.

```csharp
Option<string> userId = Option.Some("123");
Option<User> user = userId.Bind(id => FindUserById(id));
Option<string> userEmail = user.Bind(u => GetUserEmail(u.Id));

// FindUserById returns Option<User> (might not find the user)
static Option<User> FindUserById(string id) =>
    id == "123"
        ? Option.Some(new User("John"))
        : Option.None;
```

#### `ValueTask<Option<T2>> BindTask<T2>(Func<T, ValueTask<Option<T2>>> f)`
Applies `f` and flattens the result.

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
Returns the option if the predicate succeeds, otherwise None.

```csharp
Option<int> userAge = Option.Some(25);
Option<int> adultAge = userAge.Where(age => age >= 18); // Some(25)

Option<int> childAge = Option.Some(17);
Option<int> adultAge2 = childAge.Where(age => age >= 18); // None
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

#### `T IfNone(Func<T> f)`
Returns the wrapped value if Some, otherwise the result of `f`.

```csharp
Option<string> userName = GetUserName();
string displayName = userName.IfNone(() => "Anonymous User");
```

#### `Option<T> IfNone(Func<Option<T>> f)`
Returns this option if Some, otherwise the result of `f`.

```csharp
Option<Config> primaryConfig = LoadPrimaryConfig();
Option<Config> configWithFallback = primaryConfig.IfNone(() => LoadDefaultConfig());

// Chain multiple fallback strategies
Option<User> user = GetUserFromCache(userId)
    .IfNone(() => GetUserFromDatabase(userId))
    .IfNone(() => GetGuestUser());
```

#### `void IfNone(Action f)`
Executes `f` when the option is None.

```csharp
Option<Config> config = LoadConfig();
config.IfNone(() => Console.WriteLine("Warning: no config loaded, using defaults"));
```

#### `ValueTask<T> IfNoneTask(Func<ValueTask<T>> f)`
Asynchronously returns the wrapped value if Some, otherwise the result of `f`.

```csharp
Option<string> cachedValue = GetFromCache(key);
string value = await cachedValue.IfNoneTask(async () => await FetchFromDatabaseAsync(key));
```

#### `ValueTask<Option<T>> IfNoneTask(Func<ValueTask<Option<T>>> f)`
Asynchronously returns this option if Some, otherwise the result of `f`.

```csharp
Option<User> localUser = GetLocalUser(userId);
Option<User> user = await localUser.IfNoneTask(async () => await FetchRemoteUserAsync(userId));
```

#### `ValueTask IfNoneTask(Func<ValueTask> f)`
Asynchronously executes `f` when the option is None.

```csharp
Option<Config> config = LoadConfig();
await config.IfNoneTask(async () => await LogWarningAsync("No config loaded"));
```

#### `void Iter(Action<T> f)`
Executes `f` when the option is Some.

```csharp
Option<LogEntry> latestEntry = GetLatestLogEntry();
latestEntry.Iter(entry => Console.WriteLine($"Latest: {entry.Message}"));
```

#### `ValueTask IterTask(Func<T, ValueTask> f)`
Asynchronously executes `f` when the option is Some.

```csharp
Option<string> filePath = GetConfigFilePath();
await filePath.IterTask(async path => await SaveConfigAsync(path));
```

#### `T? IfNoneNull()`
Converts the option to a nullable reference type.

```csharp
Option<string> userName = GetUserName();
string? nullableUserName = userName.IfNoneNull(); // null if None, otherwise the value
```

#### `T? IfNoneNullable()`
Converts the option to a nullable value type.

```csharp
Option<int> userId = GetUserId();
int? nullableUserId = userId.IfNoneNullable(); // null if None, otherwise the value
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
Result<int> negativeResult = Result.Error<int>(Error.From("Value cannot be negative."));
Result<Request> parseResult = Result.Error<Request>(Error.From(new JsonException("Could not parse request.")));

// Implicit conversion from Error
Result<string> fromError = Error.From("failure");       // Error
```

#### `bool IsSuccess`
Returns `true` if the result contains a success value.

```csharp
Result<int> success = Result.Success(42);
success.IsSuccess; // true
```

#### `bool IsError`
Returns `true` if the result contains an error.

```csharp
Result<int> failure = Result.Error<int>(Error.From("bad input"));
failure.IsError; // true
```

#### `TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)`
Returns the result of one of two functions based on the result's state.

```csharp
Result<User> userResult = AuthenticateUser(credentials);
string message = userResult.Match(
    user => $"Welcome back, {user.Name}!",
    error => $"Login failed: {error}"
);
```

#### `void Match(Action<T> onSuccess, Action<Error> onError)`
Executes one of two actions based on the result's state.

```csharp
Result<Order> orderResult = ProcessOrder(orderRequest);
orderResult.Match(
    order => Console.WriteLine($"Order {order.Id} processed successfully"),
    error => Console.WriteLine($"Order processing failed: {error}")
);
```

#### `Result<T2> Map<T2>(Func<T, T2> f)`
Applies `f` to the success value.

```csharp
Result<string> userInput = ValidateInput(request);
Result<UserCommand> command = userInput.Map(input => ParseCommand(input));

// Errors are preserved
Result<string> invalidInput = Result.Error<string>(Error.From("Invalid format"));
Result<UserCommand> failedCommand = invalidInput.Map(input => ParseCommand(input)); // Error("Invalid format")
```

#### `ValueTask<Result<T2>> MapTask<T2>(Func<T, ValueTask<T2>> f)`
Asynchronously applies `f` to the success value.

```csharp
Result<string> configPath = Result.Success("appsettings.json");
Result<Configuration> config = await configPath.MapTask(async path =>
    await LoadConfigurationAsync(path));

// Errors are preserved through async transformations
Result<string> invalidPath = Result.Error<string>(Error.From("File not found"));
Result<Configuration> errorResult = await invalidPath.MapTask(async path =>
    await LoadConfigurationAsync(path)); // Error("File not found")
```

#### `Result<T> MapError(Func<Error, Error> f)`
Applies `f` to the error, preserving any success value.

```csharp
// Success values are preserved
Result<User> successResult = Result.Success(new User("Alice"));
Result<User> stillSuccess = successResult.MapError(error => 
    Error.From("Won't be called")); // Success(User("Alice"))

// Add context to errors
Result<ConfigFile> configResult = LoadConfig("settings.json");
Result<ConfigFile> contextualError = configResult.MapError(error =>
    error + Error.From($"Failed to load configuration from settings.json"));
```

#### `Result<T2> Bind<T2>(Func<T, Result<T2>> f)`
Applies `f` and flattens the result.

```csharp
// Chain validation and processing steps
Result<Payment> paymentResult =
    ValidateOrderRequest(orderRequest)
          .Bind(req => ValidatePaymentInfo(req))
          .Bind(validated => ChargePayment(validated));
```

#### `ValueTask<Result<T2>> BindTask<T2>(Func<T, ValueTask<Result<T2>>> f)`
Applies `f` and flattens the result.

```csharp
Result<Order> orderResult = ValidateOrder(orderRequest);
Result<ValidatedOrder> validatedResult = await orderResult
    .BindTask(async order => await ValidatePaymentAsync(order));
Result<Payment> paymentResult = await validatedResult
    .BindTask(async validated => await ChargePaymentAsync(validated));
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

#### `T IfError(Func<Error, T> f)`
Returns the success value if successful, otherwise the result of `f`.

```csharp
Result<User> userResult = GetUser(userId);
User user = userResult.IfError(error => new User("Guest"));
```

#### `Result<T> IfError(Func<Error, Result<T>> f)`
Returns this result if successful, otherwise the result of `f`.

```csharp
Result<User> userResult = GetUser(userId);
Result<User> recoveredResult = userResult.IfError(error => 
    GetUserFromCache(userId)); // Recovery operation that might also fail

// Chain multiple fallback strategies
Result<Config> configResult = LoadPrimaryConfig()
    .IfError(_ => LoadBackupConfig())
    .IfError(_ => LoadDefaultConfig());
```

#### `void IfError(Action<Error> f)`
Executes `f` when the result is an error.

```csharp
Result<Report> reportResult = GenerateReport(parameters);
reportResult.IfError(error => Console.WriteLine($"Report generation failed: {error}"));
```

#### `ValueTask<T> IfErrorTask(Func<Error, ValueTask<T>> f)`
Asynchronously returns the success value if successful, otherwise the result of `f`.

```csharp
Result<Config> configResult = LoadConfig();
Config config = await configResult.IfErrorTask(async error =>
    await FetchDefaultConfigAsync());
```

#### `ValueTask<Result<T>> IfErrorTask(Func<Error, ValueTask<Result<T>>> f)`
Asynchronously returns this result if successful, otherwise the result of `f`.

```csharp
Result<User> userResult = GetLocalUser(userId);
Result<User> user = await userResult.IfErrorTask(async error =>
    await FetchRemoteUserAsync(userId));
```

#### `ValueTask IfErrorTask(Func<Error, ValueTask> f)`
Asynchronously executes `f` when the result is an error.

```csharp
Result<Report> reportResult = GenerateReport(parameters);
await reportResult.IfErrorTask(async error =>
    await LogErrorAsync($"Report generation failed: {error}"));
```

#### `T IfErrorThrow()`
Returns the success value or throws the error as an exception.

```csharp
Result<DatabaseConnection> connectionResult = ConnectToDatabase();
DatabaseConnection connection = connectionResult.IfErrorThrow(); // Throws if connection failed
```

#### `void Iter(Action<T> f)`
Executes `f` when the result is successful.

```csharp
Result<Report> reportResult = GenerateReport(parameters);
reportResult.Iter(report => SaveReportToFile(report));
```

#### `ValueTask IterTask(Func<T, ValueTask> f)`
Asynchronously executes `f` when the result is successful.

```csharp
Result<EmailMessage> emailResult = ComposeEmail(recipient, subject, body);
await emailResult.IterTask(async email => await SendEmailAsync(email));
```

#### `Option<T> ToOption()`
Converts the result to an option, discarding error information.

```csharp
Result<User> userResult = GetUser(userId);
Option<User> userOption = userResult.ToOption();

userOption.Match(
    user => Console.WriteLine($"Found user: {user.Name}"),
    () => Console.WriteLine("User not found")
);
```

#### `T? IfErrorNull()`
Converts the result to a nullable reference type.

```csharp
Result<string> apiResponse = CallApi();
string? nullableResponse = apiResponse.IfErrorNull(); // null if error, otherwise the value
```

#### `T? IfErrorNullable()`
Converts the result to a nullable value type.

```csharp
Result<int> calculationResult = PerformCalculation();
int? nullableResult = calculationResult.IfErrorNullable(); // null if error, otherwise the value
```

---

### Error

Represents error information containing messages and/or exceptions. Messages are case-insensitive and automatically deduplicated.

```csharp
// Creating from messages
Error singleMessage = Error.From("User not found");
Error multipleMessages = Error.From("Invalid email", "Password too short", "Username taken");

// Creating from exceptions
Error singleException = Error.From(new FileNotFoundException("Config file missing"));
Error multipleExceptions = Error.From(
    new FileNotFoundException("settings.json"),
    new TimeoutException("Remote service did not respond"));

// Implicit conversion from string
Error fromString = "Something went wrong";

// Implicit conversion from exception
Error fromException = new InvalidOperationException("Operation not allowed");
```

#### Properties

##### `ImmutableHashSet<string> Messages`
Set of all error messages. Duplicates are collapsed using case-insensitive comparison.

```csharp
Error error1 = Error.From("Error A", "error a", "Error B");
Error error2 = Error.From("Error C");
Error combined = error1 + error2;

combined.Messages.Iter(m => Console.WriteLine(m));
// Output (order not guaranteed):
// Error A
// Error B
// Error C
```

##### `ImmutableHashSet<Exception> Exceptions`
Set of all exceptions contained in this error.

```csharp
Error error = Error.From(
    new FileNotFoundException("missing.txt"),
    new TimeoutException("Operation timed out"));

error.Exceptions.Iter(ex => Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"));
// FileNotFoundException: missing.txt
// TimeoutException: Operation timed out
```

#### `Error From(params string[] messages)`
Creates an error from one or more messages.

```csharp
// Single message
Error single = Error.From("Validation failed");

// Multiple messages
Error multiple = Error.From(
    "Email is required",
    "Password must be at least 8 characters",
    "Username is already taken");
```

#### `Error From(params Exception[] exceptions)`
Creates an error from one or more exceptions.

```csharp
// Single exception
Error single = Error.From(new InvalidOperationException("Cannot process"));

// Multiple exceptions
Error multiple = Error.From(
    new FileNotFoundException("config.json"),
    new UnauthorizedAccessException("Access denied"),
    new TimeoutException("Request timed out"));
```

#### `Exception ToException()`
Converts the error to an exception.

```csharp
// Single exception is preserved exactly
Error singleEx = Error.From(new FileNotFoundException("settings.json"));
singleEx.ToException(); // FileNotFoundException (original instance)

// Multiple exceptions are wrapped in AggregateException
Error multipleEx = Error.From(
    new InvalidOperationException("Invalid state"),
    new TimeoutException("Timed out"));
multipleEx.ToException(); // AggregateException containing InvalidOperationException and TimeoutException

// Single message becomes InvalidOperationException
Error singleMsg = Error.From("Something went wrong");
singleMsg.ToException(); // InvalidOperationException("Something went wrong")

// Multiple messages become InvalidOperationException instances in AggregateException
Error multipleMsg = Error.From("Error 1", "Error 2");
multipleMsg.ToException(); // AggregateException(InvalidOperationException("Error 1"), InvalidOperationException("Error 2"))

// Mixed messages and exceptions
Error mixed = Error.From("Custom error") 
            + Error.From(new TimeoutException("Timeout"));
mixed.ToException(); // AggregateException(InvalidOperationException("Custom error"), TimeoutException("Timeout"))
```

#### Combining Errors with `+`
Merges two errors, combining their messages and exceptions. Duplicate messages are collapsed.

```csharp
Error validation = Error.From("Invalid email", "Password too short");
Error network = Error.From(new TimeoutException("Connection timeout"));
Error authorization = Error.From("Unauthorized");

Error combined = validation + network + authorization;

combined.Messages.Iter(m => Console.WriteLine(m));
// Invalid email
// Password too short
// Unauthorized

combined.Exceptions.Iter(ex => Console.WriteLine(ex.GetType().Name));
// TimeoutException

// Duplicates are collapsed (case-insensitive)
Error a = Error.From("Error message");
Error b = Error.From("ERROR MESSAGE", "Different error");
Error merged = a + b;

merged.Messages.Count; // 2 (not 3)
// "Error message"
// "Different error"
```

#### Equality
Two errors are equal if they have the same set of messages (case-insensitive) and the same set of exceptions.

```csharp
Error e1 = Error.From("Error A", "Error B");
Error e2 = Error.From("error b", "error a"); // Case-insensitive, order doesn't matter
e1.Equals(e2); // true

Error e5 = Error.From("Message");
Error e6 = Error.From(new InvalidOperationException("Message"));
e5.Equals(e6); // false (different structure - one has message, one has exception)
```

#### `string ToString()`
Returns a formatted string with all messages and exception descriptions, sorted alphabetically.

```csharp
Error error = Error.From("Validation failed", "Invalid format")
            + Error.From(new TimeoutException("Request timeout"));

Console.WriteLine(error.ToString());
// Invalid format
// Validation failed
// TimeoutException: Request timeout
```

---

### Enumerable Extensions

Provides extension methods for working with `IEnumerable<T>` in a functional style.

#### `Option<T> Head<T>()`
Returns the first element as an option.

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

#### `Option<T> Head<T>(Func<T, bool> predicate)`
Returns the first element matching `predicate` as an option.

```csharp
// Safe alternative to .First(x => predicate(x)) / .Where(predicate).First()
int[] values = { 3, 5, 8, 8, 10 };
Option<int> firstEven = values.Head(x => x % 2 == 0); // Some(8)

// When no element matches, returns None instead of throwing
int[] odds = { 1, 3, 5 };
Option<int> noEven = odds.Head(x => x % 2 == 0); // None

// Works with (potentially) infinite sequences – short-circuits on first match
var naturals = Enumerable.Range(1, int.MaxValue);
Option<int> greaterThan100 = naturals.Head(x => x > 100); // Some(101)
```

Category theory perspective: this is the total (Option-returning) form of the partial `find` function. It factorizes through the `Option` (Maybe) monad to handle absence safely.

Law (derivable from `Pick`):

```
source.Head(p) == source.Pick(x => p(x) ? Option.Some(x) : Option.None)
```

This makes `Head(predicate)` a convenience wrapper over a common `Pick` pattern while preserving laziness and short-circuiting behavior.

#### `Option<T> SingleOrNone<T>()`
Returns the single element as an option.

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
Filters and transforms elements using `selector`.

```csharp
// Parse valid integers from mixed input
string[] inputs = { "42", "invalid", "100", "", "7" };
var validNumbers = inputs.Choose(input =>
    int.TryParse(input, out var number) ? Option.Some(number) : Option.None);

validNumbers.Iter(number => Console.WriteLine($"Valid number: {number}")); // Output: 42, 100, 7
```

#### `IAsyncEnumerable<T2> Choose<T, T2>(Func<T, ValueTask<Option<T2>>> selector)`
Asynchronously filters and transforms elements using `selector`.

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
Returns the first element that produces Some when transformed by `selector`.

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
Applies `selector` to each element, collecting successes or aggregating errors.

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
Applies `selector` to each element, succeeding only if all elements succeed.

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
Separates tuples into two immutable arrays. Inverse of `Zip`.

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
```

#### `void Iter<T>(Action<T> action, CancellationToken cancellationToken = default)`
Executes `action` on each element sequentially.

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
Executes `action` on each element in parallel.

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
    maxDegreeOfParallelism: Option.Some(4)
);
```

#### `ValueTask IterTask<T>(Func<T, ValueTask> action, CancellationToken cancellationToken = default)`
Asynchronously executes `action` on each element sequentially.

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
Asynchronously executes `action` on each element in parallel.

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
    maxDegreeOfParallelism: Option.Some(10)
);
```

#### `IEnumerable<T> Tap<T>(Action<T> action)`
Executes `action` on each element as it's enumerated, returning the original sequence unchanged.

```csharp
// Debugging and logging in LINQ chains
List<User> users = GetUsers()
    .Where(user => user.IsActive)
    .Tap(user => Console.WriteLine($"Processing user: {user.Name}"))
    .Where(user => user.HasValidEmail)
    .Tap(user => Console.WriteLine($"Valid user: {user.Id}"))
    .ToList();

// Multiple taps for different concerns
var processedOrders = orders
    .Tap(order => Console.WriteLine($"Processing order: {order.Id}"))
    .Where(order => order.IsValid)
    .Tap(order => metrics.IncrementCounter("valid_orders"))
    .Select(order => EnrichOrder(order));
```

---

### AsyncEnumerable Extensions

Provides extension methods for working with `IAsyncEnumerable<T>` in a functional style.

#### `ValueTask<Option<T>> Head<T>(CancellationToken cancellationToken)`
Returns the first element of the async sequence as an option.

```csharp
IAsyncEnumerable<LogEntry> logStream = GetLogStreamAsync();
Option<LogEntry> firstEntry = await logStream.Head(cancellationToken);

firstEntry.Match(
    entry => Console.WriteLine($"Latest log: {entry.Message}"),
    () => Console.WriteLine("No log entries available")
);
```

#### `IAsyncEnumerable<T2> Choose<T, T2>(Func<T, Option<T2>> selector)`
Filters and transforms async elements using `selector`.

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
Asynchronously filters and transforms async elements using `selector`.

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
Returns the first async element that produces Some when transformed by `selector`.

```csharp
// Find the first valid data record in a stream
IAsyncEnumerable<string> dataStream = ReadDataStreamAsync();
Option<DataRecord> firstValid = await dataStream.Pick(line =>
    TryParseDataRecord(line, out var record)
        ? Option.Some(record)
        : Option.None,
    cancellationToken); // Returns as soon as a valid record is found
```

#### `ValueTask<Option<T2>> Pick<T, T2>(Func<T, ValueTask<Option<T2>>> selector, CancellationToken cancellationToken)`
Asynchronously returns the first async element that produces Some when transformed by `selector`.

```csharp
// Find the first available service endpoint
IAsyncEnumerable<string> endpoints = GetServiceEndpointsAsync();
Option<HealthCheckResult> firstHealthy = await endpoints.Pick(async endpoint =>
{
    var result = await CheckHealthAsync(endpoint);
    return result.IsHealthy ? Option.Some(result) : Option.None;
}, cancellationToken);
```

#### `ValueTask<Result<ImmutableArray<T2>>> Traverse<T, T2>(Func<T, ValueTask<Result<T2>>> selector, CancellationToken cancellationToken)`
Asynchronously applies `selector` to each element, collecting successes or aggregating errors.

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
Asynchronously applies `selector` to each element, succeeding only if all elements succeed.

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
Asynchronously separates tuples into two immutable arrays. Async inverse of `Zip`.

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
Asynchronously executes `action` on each element sequentially.

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
Asynchronously executes `action` on each element in parallel.

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
    maxDegreeOfParallelism: Option.Some(5)
);
```

#### `IAsyncEnumerable<T> Tap<T>(Action<T> action)`
Executes `action` on each async element as it's enumerated, returning the original sequence unchanged.

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
Asynchronously executes `action` on each async element as it's enumerated, returning the original sequence unchanged.

```csharp
// Async logging and side effects
IAsyncEnumerable<Order> processedOrders = orderStream
    .Where(order => order.RequiresValidation)
    .TapTask(async order => await LogAsync($"Validating order: {order.Id}"))
    .Where(order => order.IsValid)
    .TapTask(async order => await NotifyCustomerAsync(order.CustomerId))
    .Select(order => EnrichOrder(order));

await foreach (var order in processedOrders)
{
    await ProcessOrderAsync(order);
}
```
---

### Dictionary Extensions

Provides extension methods for safe dictionary operations.

#### `Option<TValue> Find<TKey, TValue>(TKey key)`
Returns the value for `key` as an option.

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
---