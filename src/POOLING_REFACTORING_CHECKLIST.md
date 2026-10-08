# Connection Pooling Refactoring Checklist

## ❌ Current Implementation Problems

### 1. **DBConnection.cs - Line 30**
```csharp
// WRONG: Single persistent connection
_dbConnector = SetupDatabaseConnection().Build().OpenConnection();
```

**Issue:** Opens one connection in constructor, holds forever, reused by all requests

---

### 2. **FraudRepository.cs - Lines 35, 108, 136, 172, 207**
```csharp
// WRONG: State checking anti-pattern
if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();
```

**Issue:** Indicates design flaw; proper pooling doesn't require state checks

---

### 3. **Transaction Concurrency Risk (FraudRepository.cs Line 37)**
```csharp
// RISKY: Multiple requests share same connection
var dbConnection = _dbConnection.DB_Connector;
await using var tx = await dbConnection.BeginTransactionAsync();
```

**Issue:** Two concurrent requests = two transactions on same connection = FAIL

---

## ✅ Refactoring Steps

### Step 1: Update IDBConnection Interface
**File:** `fraud_poc_project_repo/Connection/IDBConnection.cs`

```csharp
// REMOVE: Single connection property
// public NpgsqlConnection DB_Connector { get; }

// ADD: Connection string property
public interface IDBConnection
{
    string ConnectionString { get; }  // NEW
    string DB_Schema { get; }
}
```

---

### Step 2: Update DBConnection Implementation
**File:** `fraud_poc_project_repo/Connection/DBConnection.cs`

```csharp
public class DBConnection : DbContext, IDBConnection
{
    private readonly string _connectionString;  // NEW
    private readonly Database _databaseOptions;
    private ILogger<DBConnection> _logger;

    // REMOVE: private readonly NpgsqlConnection _dbConnector;
    // REMOVE: public NpgsqlConnection DB_Connector { get { return _dbConnector; } }

    // ADD: Connection string property
    public string ConnectionString { get { return _connectionString; } }
    public string DB_Schema { get { return _databaseOptions.DBSchema; } }

    public DBConnection(
        ILogger<DBConnection> logger,
        DbContextOptions<DBConnection> options,
        IOptions<Database> dbOptions,
        IConfiguration config)
        : base(options)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("PostgreSQL");  // STORE, don't open
        _databaseOptions = dbOptions.Value;
        // REMOVE: _dbConnector = SetupDatabaseConnection().Build().OpenConnection();
    }

    private NpgsqlDataSourceBuilder SetupDatabaseConnection()
    {
        try
        {
            return new NpgsqlDataSourceBuilder(_connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Setup Data Base Connection");
            throw;
        }
    }
}
```

---

### Step 3: Refactor FraudRepository.cs

#### Change Constructor
```csharp
// OLD:
public FraudRepository(IConfiguration configuration,
    ILogger<FraudRepository> logger,
    IDBConnection dbConnection)
{
    _connectionString = configuration.GetConnectionString("PostgreSQL") ?? throw ...;
    _dbConnection = dbConnection;  // REMOVE
}

// NEW:
public FraudRepository(IConfiguration configuration,
    ILogger<FraudRepository> logger,
    IDBConnection dbConnection)
{
    _connectionString = dbConnection.ConnectionString;  // Use from IDBConnection
    _schema = dbConnection.DB_Schema;
    _logger = logger;
    // REMOVE: _dbConnection field - not needed
}
```

#### Remove Unused Field
```csharp
// REMOVE THIS LINE:
private readonly IDBConnection _dbConnection;
```

#### Update SaveFraudEvaluationAsync
```csharp
public async Task<long> SaveFraudEvaluationAsync(FraudEventRecord result)
{
    // CHANGE: Create new connection each call (from pool)
    await using var dbConnection = new NpgsqlConnection(_connectionString);
    await dbConnection.OpenAsync().ConfigureAwait(false);

    await using var tx = await dbConnection.BeginTransactionAsync().ConfigureAwait(false);

    try
    {
        // ... rest of code stays the same ...
        // REMOVE: the state check "if (dbConnection.State == ConnectionState.Closed)"
    }
    catch
    {
        // ...
    }
    // Connection automatically returns to pool when disposed
}
```

#### Update SavedltErrorAsync
```csharp
public async Task SavedltErrorAsync(string topic, string messageData, string error)
{
    try
    {
        // CHANGE: New connection each call
        await using var dbConnection = new NpgsqlConnection(_connectionString);
        await dbConnection.OpenAsync().ConfigureAwait(false);
        
        // REMOVE: State check

        await using var cmd = new NpgsqlCommand(..., dbConnection);
        // ... rest stays the same ...
    }
    catch
    {
        // ...
    }
}
```

#### Update QueryFraudEventsAsync
```csharp
public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query)
{
    try
    {
        var results = new List<FraudEventRecord>();

        // CHANGE: New connection each call
        await using var dbConnection = new NpgsqlConnection(_connectionString);
        await dbConnection.OpenAsync().ConfigureAwait(false);
        
        // REMOVE: State check

        await using var cmd = new NpgsqlCommand(..., dbConnection);
        // ... rest stays the same ...
    }
    catch
    {
        // ...
    }
}
```

#### Update QueryFlaggedOnlyFraudEventsAsync
Same pattern as QueryFraudEventsAsync

#### Update GetRuleResultsForEventAsync
Same pattern as QueryFraudEventsAsync

---

### Step 4: Update Connection String Configuration

**File:** `appsettings.json` or `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Server=localhost;Port=5432;Database=fraud_poc;Username=postgres;Password=YourPassword;Maximum Pool Size=20;Minimum Pool Size=5;Connection Idle Lifetime=60;Connection Pruning Interval=60"
  }
}
```

**Pool Configuration Explanation:**
- `Maximum Pool Size=20` - Never have more than 20 connections open
- `Minimum Pool Size=5` - Always keep 5 connections ready
- `Connection Idle Lifetime=60` - Recycle connections unused for 60 seconds
- `Connection Pruning Interval=60` - Check for idle connections every 60 seconds

Adjust based on your load:
- **Low traffic:** `Max Pool Size=5`, `Min Pool Size=2`
- **Medium traffic:** `Max Pool Size=10`, `Min Pool Size=3`
- **High traffic:** `Max Pool Size=20-30`, `Min Pool Size=5-10`

---

## Verification Steps

After refactoring, verify:

```csharp
// Test 1: Concurrent requests work
var task1 = repository.SaveFraudEvaluationAsync(fraudEvent1);
var task2 = repository.SaveFraudEvaluationAsync(fraudEvent2);
await Task.WhenAll(task1, task2);
// ✅ Should complete without errors or deadlocks

// Test 2: No connection state issues
for (int i = 0; i < 100; i++)
{
    await repository.QueryFraudEventsAsync(query);
}
// ✅ Should handle rapid-fire requests

// Test 3: Transaction isolation
var t1 = repository.SaveFraudEvaluationAsync(fraudEvent1);
var t2 = repository.SaveFraudEvaluationAsync(fraudEvent2);
// ✅ Transactions should not interfere with each other
```

---

## Summary of Changes

| Component | Change | Benefit |
|-----------|--------|---------|
| **DBConnection** | Store connection string, don't open persistent connection | Allows proper pooling |
| **IDBConnection** | Add ConnectionString property | Repositories can create new connections |
| **FraudRepository** | Create new connection per call | Thread-safe, scalable, properly pooled |
| **State Checks** | Remove all `if (dbConnection.State == ...)` | Not needed with proper pooling |
| **Connection String** | Add pool configuration parameters | Tuned pool behavior |

---

## Estimated Effort

- **DBConnection.cs:** ~10 minutes
- **IDBConnection.cs:** ~5 minutes
- **FraudRepository.cs:** ~30 minutes
- **Testing:** ~30 minutes
- **Configuration:** ~5 minutes

**Total: ~1.5 hours**

---

## Expected Improvements

✅ **Before:** Single connection, sequential processing, fails under concurrency
✅ **After:** Pooled connections, concurrent requests, scales to load

**Performance Impact:**
- Low concurrency: ~2-3% overhead (new connection per call vs. reuse)
- High concurrency: ~50-200% throughput increase (proper pooling vs. bottleneck)
