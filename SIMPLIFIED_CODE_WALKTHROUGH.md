# How the Code Works - Explained Simply

Let's walk through the actual code and see how a transaction moves through the system.

---

## 🎬 Scene 1: A Transaction Arrives

**File:** `FraudKafkaProducer.cs` (The Transaction Creator)

```csharp
// SIMPLE VERSION:
var transaction = new TransactionEvent
{
    TransactionId = "ABC123",
    CustomerId = "CUST-001",
    Amount = 5000,           // Big amount!
    Country = "Nigeria",     // Foreign!
    MerchantCategory = "money_transfer",  // Risky!
    TransactionTime = DateTime.Now   // Whenever it happened
};

// Send it to Kafka (the mailbox)
await fraudProducer.ProduceAsync(transaction);
```

**What happens:**
1. Someone buys something
2. Creates a transaction object (package)
3. Sends it to Kafka (mailbox)
4. ✅ Done!

---

## 📬 Scene 2: Transaction Goes in the Mailbox

**File:** Docker-compose (The Setup)

```yaml
# This is like saying:
# "When transactions arrive, put them in Kafka"
# "Kafka holds up to 5 at a time"
# "Check every 100 milliseconds"
```

**What happens:**
```
Transaction arrives → Goes in mailbox → Waits
Transaction arrives → Goes in mailbox → Waits
Transaction arrives → Goes in mailbox → Waits
Transaction arrives → Goes in mailbox → Waits
Transaction arrives → Goes in mailbox → FULL! Or 5 seconds passed?
                                         PROCESS NOW!
```

---

## 🔍 Scene 3: Fraud Checker Wakes Up

**File:** `FraudConsumerWorker.cs` (The Worker That Checks Transactions)

```csharp
// This runs forever and forever and forever...
while (true)
{
    // Step 1: Listen to mailbox
    var transaction = mailbox.GetMessage();  // Get a message
    
    if (transaction == null)
        continue;  // No messages yet, keep listening
    
    // Step 2: Add to batch (collect up to 5)
    batch.Add(transaction);
    
    // Step 3: Time to check?
    // Yes if: 5 messages collected OR 5 seconds passed
    if (batch.Count >= 5 OR timeExpired)
    {
        // GO TO SCENE 4
        CheckTheseTransactions(batch);
    }
}
```

**What happens:**
1. Listen constantly (like a doorbell)
2. Collect messages (5 or 5 seconds)
3. When ready, pass to fraud checker

---

## 🎯 Scene 4: The Fraud Checker Evaluates

**File:** `FraudEvaluationService.cs` (The Brain That Does the Checking)

### SIMPLE CODE:
```csharp
public FraudScore CheckTransaction(Transaction tx)
{
    int score = 0;
    
    // RULE 1: Is the amount big?
    if (tx.Amount > 10000)
        score += 25;  // YES - Add points
    
    // RULE 2: Is it from abroad?
    if (tx.Country != "ZA")
        score += 20;  // YES - Add points
    
    // RULE 3: Is it a risky store?
    if (tx.MerchantCategory == "gambling" OR "crypto")
        score += 20;  // YES - Add points
    
    // RULE 4: Is it weird time (3 AM)?
    if (tx.TransactionTime.Hour == 3)
        score += 15;  // YES - Add points
    
    // RULE 5: Is it a round number?
    if (tx.Amount % 100 == 0)  // Ends in .00
        score += 20;  // YES - Add points
    
    // FINAL DECISION
    if (score >= 40)
        return "FRAUD";  // Danger!
    else
        return "OK";     // Safe
}
```

**What this does:**
1. Look at each rule
2. If it matches, add points
3. Total the points
4. If >= 40, it's fraud

**Example:**
```
Transaction: 50,000 from Nigeria at 3 AM
Rule 1: Big? YES → +25
Rule 2: Foreign? YES → +20
Rule 3: Risky? NO → +0
Rule 4: Weird time? YES → +15
Rule 5: Round number? YES → +20
                            ____
Total: 80 points → FRAUD! ❌
```

---

## 💾 Scene 5: Save to Database

**File:** `FraudRepository.cs` (The Notebook Writer)

```csharp
public async Task SaveTransaction(FraudEventRecord fraud)
{
    // Open notebook (database connection)
    var db = GetDatabase();
    
    // Write in the transaction
    db.Insert(new {
        transaction_id = fraud.TransactionId,
        amount = fraud.Amount,
        fraud_score = fraud.Score,
        is_flagged = fraud.IsFlagged,
        saved_at = DateTime.Now
    });
    
    // Close notebook
    db.Close();
}
```

**What this does:**
1. Opens database connection (like opening a notebook)
2. Writes transaction info (writes down what happened)
3. Saves the fraud score (0-100)
4. Saves whether it's flagged (yes/no)
5. Closes connection

**Example entry in notebook:**
```
Transaction ID: ABC123
Amount: $5,000
Score: 80
Status: FLAGGED ⚠️
Time: 2026-08-31 08:17:45
```

---

## 🖥️ Scene 6: Website Shows Results

**File:** `FraudController.cs` (The Web Interface)

```csharp
[ApiController]
[Route("api/fraud")]
public class FraudController
{
    // ENDPOINT 1: Show all transactions
    [HttpGet("events")]
    public async Task<List<Transaction>> GetAllTransactions()
    {
        var db = GetDatabase();
        return db.GetAll();  // Get everything from notebook
    }
    
    // ENDPOINT 2: Show only fraud
    [HttpGet("events/flagged")]
    public async Task<List<Transaction>> GetFraudOnly()
    {
        var db = GetDatabase();
        return db.GetWhere(x => x.IsFlagged == true);  // Get only fraud
    }
    
    // ENDPOINT 3: Show details of one transaction
    [HttpGet("events/{id}/rules")]
    public async Task<TransactionDetails> GetTransactionDetails(string id)
    {
        var db = GetDatabase();
        return db.GetById(id);  // Get specific transaction
    }
}
```

**What this does:**
1. Creates three "pages" on the website
2. Each page gets different information from the notebook
3. Shows it on the screen

---

## 📊 Complete Flow Diagram with Real Code

```
┌─────────────────────────────────────────────┐
│ 1. TRANSACTION CREATED                      │
│ FraudKafkaProducer.ProduceAsync()          │
│ Creates: TransactionEvent                   │
└────────────────┬────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────┐
│ 2. GOES TO MESSAGE QUEUE                    │
│ Kafka Message Queue                         │
│ Waits for: 5 messages OR 5 seconds         │
└────────────────┬────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────┐
│ 3. WORKER PICKS IT UP                       │
│ FraudConsumerWorker.Consume()              │
│ Gets batch of up to 5 messages             │
└────────────────┬────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────┐
│ 4. FRAUD CHECKER EVALUATES                 │
│ FraudEvaluationService.Evaluate()          │
│ Checks 5 rules                             │
│ Gives score 0-100                          │
└────────────────┬────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────┐
│ 5. SAVES TO DATABASE                        │
│ FraudRepository.Save()                     │
│ Writes to notebook                         │
│ Records score and fraud status             │
└────────────────┬────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────┐
│ 6. WEBSITE SHOWS RESULTS                    │
│ FraudController.GetEvents()                │
│ Display: "This was fraud" or "This is OK"  │
└─────────────────────────────────────────────┘
```

---

## 🚀 Simple Config File Explained

**File:** `appsettings.LOC.json` (The Settings)

```json
{
  "KafkaSettings": {
    "ConsumerSettings": {
      "TransactionTopic": "poc-fraud-dev",      // Listen to this mailbox
      "BatchSize": 5,                            // Collect 5 at a time
      "BatchPolling": 100,                       // Check every 100 milliseconds
      "BatchProcessTimeout": 5                   // Or wait max 5 seconds
    }
  },
  "Database": {
    "ConnectionStringReadWrite": "Server=localhost;..."  // Where is our notebook?
  }
}
```

**What this means:**
- Listen to mailbox called "poc-fraud-dev"
- Collect 5 messages before processing
- Check for new messages every 100ms (very fast!)
- Wait max 5 seconds before processing
- Database is on this computer at port 5432

---

## 🐳 Docker Explained

**File:** `Dockerfile` (The Recipe)

```dockerfile
# START WITH BLANK KITCHEN
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# INSTALL COOKING EQUIPMENT (.NET)
RUN apt-get install dotnet-8

# COPY INGREDIENTS (source code)
COPY . .

# MIX IT TOGETHER (compile)
RUN dotnet build

# PUT IN OVEN (run the app)
ENTRYPOINT ["dotnet", "fraud_poc_project.dll"]
```

**What this does:**
1. Gets a blank computer
2. Installs .NET (the cooking equipment)
3. Copies your code (ingredients)
4. Compiles it (mixes)
5. Runs the app (cooks it)

---

## 🐳 Docker Compose Explained

**File:** `docker-compose.yml` (Opening a Restaurant)

```yaml
services:
  api:
    # Build from Dockerfile
    build: .
    # Run this app
    ports:
      - "8080:8080"  # People visit on port 8080
    
  postgres:
    # Use pre-made database
    image: postgres:16-alpine
    # Where to store data
    volumes:
      - postgres_data:/var/lib/postgresql/data
    # Open port for connections
    ports:
      - "5432:5432"
    
  nginx:
    # Use pre-made web server
    image: nginx:alpine
    # Show results on port 8085
    ports:
      - "8085:8085"

networks:
  edge:
    # Connect all three together
```

**What this does:**
1. Start the app (api)
2. Start the database (postgres)
3. Start the web server (nginx)
4. Connect them together
5. Open ports so people can access

---

## ☁️ Kubernetes Explained (The Franchise)

**File:** `values.yaml` (The Template)

```yaml
# How many copies?
replicaCount: 1

# What's the app called?
image:
  name: fraud-poc-api
  tag: latest

# What resources do we give it?
resources:
  requests:
    memory: "128Mi"    # Like renting a small room
    cpu: "100m"        # Like having a part-time worker

# Environment-specific settings
# values-dev.yaml:   replicaCount: 1  (small)
# values-staging.yaml: replicaCount: 2 (medium)
# values-prod.yaml:  replicaCount: 3  (large)
```

**What this does:**
1. Template for how to set up the app
2. Can have different sizes (dev, staging, prod)
3. Auto-scales up/down based on load

---

## 📈 Terraform Explained (Building in the Cloud)

**File:** `main.tf` (The Blueprint)

```hcl
# Create a computer in the cloud
resource "aws_instance" "fraud-checker" {
  ami           = "ami-12345"     # What OS?
  instance_type = "t2.medium"     # How powerful?
  
  # Give it memory
  root_block_device {
    volume_size = 50  # 50 GB of storage
  }
  
  # Open port 8080
  security_groups = ["allow-8080"]
}

# Create a database in the cloud
resource "aws_rds_instance" "fraud-db" {
  engine         = "postgres"     # Use PostgreSQL
  instance_class = "db.t3.micro"  # Size
  storage        = 100            # 100 GB storage
}
```

**What this does:**
1. Blueprint for what computers to create
2. How big should they be?
3. Where should the database be?
4. What ports should be open?
5. When you run this, it builds everything in the cloud automatically!

---

## 🎯 The Whole Picture

```
USER BUYS SOMETHING
        ↓
TRANSACTION CREATED (ProduceAsync)
        ↓
GOES TO KAFKA (Message Queue)
        ↓
WORKER PICKS IT UP (FraudConsumerWorker)
        ↓
FRAUD CHECKER LOOKS AT IT (FraudEvaluationService)
        ↓
CHECKS 5 RULES (Big? Foreign? Risky? Weird? Round?)
        ↓
CALCULATES SCORE (0-100)
        ↓
SAVES TO DATABASE (FraudRepository)
        ↓
WEBSITE SHOWS RESULTS (FraudController)
        ↓
YOU SEE: "This transaction is OK" or "This is FRAUD!"
```

---

## 💡 The Key Ideas

1. **Transactions are like messages** - they move through a system
2. **Kafka is like a mailbox** - holds messages until we're ready
3. **Fraud checker is like a security guard** - checks each transaction
4. **Database is like a notebook** - writes everything down
5. **Website is like a report** - shows what we found
6. **Docker is like packaging** - works the same everywhere
7. **Kubernetes is like franchising** - manage many copies
8. **Terraform is like a blueprint** - build in the cloud automatically

That's everything! From transaction to fraud detection to displaying results! 🎉
