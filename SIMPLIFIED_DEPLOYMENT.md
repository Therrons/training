# How to Deploy Your System - Explained Simply

This explains how to get your fraud detection system running in different ways.

---

## 🎯 The Three Ways to Run It

```
┌─────────────────────┬──────────────────┬──────────────────┐
│   JUST YOU          │   YOUR TEAM      │   THE WORLD      │
│   (Simple)          │   (Medium)       │   Big Business)  │
├─────────────────────┼──────────────────┼──────────────────┤
│ Docker              │ Docker Compose   │ Kubernetes       │
│ 1 Folder            │ Full Setup       │ Cloud            │
│ 5 Minutes           │ 10 Minutes       │ 20 Minutes       │
└─────────────────────┴──────────────────┴──────────────────┘
```

---

## 🌟 Way 1: Simple Docker (Just for Testing)

**Like:** Running the app just on your laptop

### What You Get:
- Just the app
- Your own database on your computer
- No backups
- Just for you

### How to Run It:

```bash
# Step 1: Build the app
./install-docker.sh build

# Step 2: Run it
./install-docker.sh run

# Step 3: Visit website
# http://localhost:8080/swagger
```

### What It Does:
```
YOUR LAPTOP
├── App (Fraud Checker)
└── Database (Notebook) ← On your computer
```

### When to Use:
- Testing new features
- Learning the system
- Running locally
- It's just you

---

## 🏪 Way 2: Docker Compose (Full Local Setup)

**Like:** Running a restaurant with all the equipment

### What You Get:
- The app (worker)
- Database (notebook)
- Web server (nginx - displays results)
- All connected together
- Works perfectly locally

### How to Run It:

```bash
# Step 1: Start everything
./install-docker-compose.sh up

# Step 2: Wait for startup (1-2 minutes)
# You'll see: "Services started!"

# Step 3: Visit website
# http://localhost:8085/swagger
```

### What It Does:
```
YOUR LAPTOP
├── App (Port 8080) ─┐
├── Database (Port 5432) ├── Connected together
└── Web Server (Port 8085) ┘
    (Nginx - shows results)
```

### When to Use:
- Your whole team
- Testing everything together
- More realistic setup
- Local development

---

## ☁️ Way 3: Kubernetes in the Cloud (Big Business)

**Like:** Having restaurants all over the world that automatically scale

### What You Get:
- Auto-scaling (add workers when busy)
- High availability (if one breaks, others survive)
- Multiple environments (dev, staging, production)
- Cloud backup
- Professional grade

### How to Run It:

```bash
# Step 1: Deploy to development
./install-k8s-helm.sh deploy dev

# Step 2: Deploy to staging
./install-k8s-helm.sh deploy staging

# Step 3: Deploy to production
./install-k8s-helm.sh deploy prod

# Step 4: Check status
./install-k8s-helm.sh status prod
```

### What It Does:
```
CLOUD (AWS/Azure/Google Cloud)
├── Development Cluster
│   ├── 1 App copy
│   ├── Database
│   └── Backup
├── Staging Cluster
│   ├── 2 App copies
│   ├── Database
│   └── Backup
└── Production Cluster
    ├── 3+ App copies (auto-scales)
    ├── Database with redundancy
    └── Automatic backups
```

### When to Use:
- Real business
- Millions of transactions
- Need uptime guarantee (99.99%)
- Multiple teams

---

## 📊 Comparison Table

| Question | Docker | Docker Compose | Kubernetes |
|----------|--------|----------------|-----------|
| **How many computers needed?** | Just yours | Your computer | Cloud |
| **How many copies of the app?** | 1 | 1 | Many (auto) |
| **Auto-scaling?** | No | No | YES |
| **If one breaks?** | Everything stops | Everything stops | Others keep running |
| **Backup?** | Manual | Manual | Automatic |
| **Cost?** | Free | Free | Costs money |
| **Setup time?** | 5 min | 10 min | 20 min |
| **Best for?** | Testing | Team work | Real business |

---

## 🚀 Step-by-Step: Docker Compose (Most Common)

### Preparation (5 minutes)

**Step 1:** Download the files
```bash
cd C:\Development\Therron\training
```

**Step 2:** Check you have Docker
```bash
docker --version
docker-compose --version
```

If you don't, download Docker Desktop from docker.com

**Step 3:** Check the settings file
```
Look at: .env file
It should say:
POSTGRES_PASSWORD=therrons
POSTGRES_USER=therrons
POSTGRES_DB=fraud_db
```

### Running (1 minute)

**Step 4:** Start everything
```bash
./install-docker-compose.sh up
```

**Step 5:** Wait for this message:
```
✓ Services started!
API (Swagger):     http://localhost:8085/swagger
PostgreSQL:        localhost:5432
```

### Using (Instantly)

**Step 6:** Open in web browser:
```
http://localhost:8085/swagger
```

**Step 7:** You can now:
- Send test transactions
- See them being processed
- Check if they're fraud or not

### Stopping (1 minute)

**Step 8:** When done:
```bash
./install-docker-compose.sh stop
```

---

## 📊 What Happens Inside (Simple Version)

### When You Start Docker Compose:

```
1. Downloads base images (like getting ingredients)
   - Gets Linux OS
   - Gets PostgreSQL
   - Gets Nginx

2. Builds your app (like cooking)
   - Takes your code
   - Compiles it
   - Packages it up

3. Starts all three services (like opening a restaurant)
   - Database: "I'm ready to store data!"
   - App: "I'm ready to check fraud!"
   - Nginx: "I'm ready to show results!"

4. Connects them (like building hallways)
   - App can talk to database
   - Nginx can talk to app
   - Everything works together!

5. Opens ports (like unlocking doors)
   - Port 8085: You can access the website
   - Port 5432: Database is accessible
   - Port 8080: App is running
```

---

## 🎓 Common Operations

### Starting Everything:
```bash
./install-docker-compose.sh up
```

### Stopping Everything:
```bash
./install-docker-compose.sh stop
```

### Removing Everything (Careful!):
```bash
./install-docker-compose.sh clean
```

### Checking Status:
```bash
./install-docker-compose.sh ps
```

### Viewing Logs:
```bash
./install-docker-compose.sh logs
```

### Opening Shell in Database:
```bash
./install-docker-compose.sh shell-db
```

---

## ❓ Troubleshooting (Simple Fixes)

### "Docker is not running"
**Fix:** Start Docker Desktop application

### "Port 8085 is already in use"
**Fix:** 
```bash
# Stop whatever is using it, OR
# Edit install-docker-compose.sh and change:
HOST_PORT="9090"  # Use 9090 instead
```

### "Services won't start"
**Fix:**
```bash
# Clean and restart
./install-docker-compose.sh clean
./install-docker-compose.sh up
```

### "Database connection refused"
**Fix:** Wait 30 seconds - database takes time to start

### "Transactions not being processed"
**Fix:** Check logs
```bash
./install-docker-compose.sh logs
# Look for error messages
```

---

## 📈 From Testing to Production

### Phase 1: Testing (Docker)
```
You develop → Test on your laptop → Works!
```

### Phase 2: Team Testing (Docker Compose)
```
Your code → Your team tests locally → Works!
```

### Phase 3: Staging (Kubernetes)
```
Your code → Deploy to staging → Real testing → Works!
```

### Phase 4: Production (Kubernetes)
```
Your code → Deploy to production → Millions of people using it!
```

---

## 🎯 Quick Decision Tree

**Q: Am I alone?**
→ Use **Docker**

**Q: Is my team testing locally?**
→ Use **Docker Compose**

**Q: Is this for real customers?**
→ Use **Kubernetes**

**Q: Do I need auto-scaling?**
→ Use **Kubernetes**

**Q: Do I need 24/7 uptime?**
→ Use **Kubernetes**

---

## 💡 Key Concepts

### Docker
- Package everything (like Tupperware)
- Seal it up
- Works the same anywhere

### Docker Compose
- Multiple packages working together
- Like a meal with multiple dishes
- Orchestrates them

### Kubernetes
- Manages many packages
- Auto-scales them
- Handles failures
- Professional management

---

## 📞 When Something Goes Wrong

### Quick Checklist:
1. Is Docker running? → Start it
2. Are ports available? → Change port or kill process
3. Do logs show errors? → Read error messages
4. Is database ready? → Wait 30 seconds
5. Did you rebuild? → Run `docker build` again

### Getting Help:
- Check logs: `./install-docker-compose.sh logs`
- Check status: `./install-docker-compose.sh ps`
- Restart: `./install-docker-compose.sh stop` then `up`
- Nuclear option: `./install-docker-compose.sh clean` then `up`

---

## ✅ You're Ready!

Pick a deployment method and run it:

```bash
# Option 1: Simple
./install-docker.sh build
./install-docker.sh run

# Option 2: Full Setup (Recommended for learning)
./install-docker-compose.sh up

# Option 3: Professional
./install-k8s-helm.sh deploy dev
```

Then visit the website and watch your fraud detection system work! 🎉
