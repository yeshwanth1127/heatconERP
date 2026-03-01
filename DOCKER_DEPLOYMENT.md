# HeatconERP - Docker Deployment Guide

## Prerequisites

- **Docker** installed on server (version 20.10+)
- **Docker Compose** installed (version 1.29+)
- Minimum **2GB RAM** available
- Ports **5212** (API), **5118** (Web), **5432** (Database) available
- Git installed to clone repository

---

## Deployment Steps

### **Step 1: Prepare Server & Clone Repository**

```bash
# SSH into your server
ssh user@your_server_ip

# Clone the repository
git clone https://your_repo_url/heatconERP.git
cd heatconERP
```

---

### **Step 2: Configure Environment Variables**

Edit the `.env` file with production values:

```bash
nano .env
```

Update with your production settings:

```dotenv
# Database Configuration
DB_USER=postgres
DB_PASSWORD=SecurePasswordHere123!@#
DB_NAME=heatconerp
DB_PORT=5432

# Port Configuration (change if needed)
API_PORT=5212
WEB_PORT=5118
```

**Security Note:** Use a strong password for `DB_PASSWORD`. Generate one with:
```bash
openssl rand -base64 32
```

---

### **Step 3: Build Docker Images**

```bash
# Build both API and Web images
docker-compose build

# This will take 5-10 minutes on first build
```

---

### **Step 4: Start Services**

```bash
# Start all services (postgres, api, web)
docker-compose up -d

# Verify all containers are running
docker-compose ps
```

Expected output:
```
NAME                    STATUS
heatconerp-db          Up (healthy)
heatconerp-api         Up
heatconerp-web         Up
```

---

### **Step 5: Apply Database Migrations**

```bash
# Run migrations inside the API container
docker-compose exec api dotnet ef database update \
  --project /app/HeatconERP.Infrastructure.dll \
  --no-build

# Or alternatively, if the above doesn't work:
docker-compose exec -w /app api dotnet ef database update --no-build
```

Wait for the migrations to complete (30-60 seconds).

---

### **Step 6: Verify Deployment**

Check logs for any errors:

```bash
# View API logs
docker-compose logs api

# View Web logs
docker-compose logs web

# View Database logs
docker-compose logs postgres
```

---

### **Step 7: Access the Application**

- **Web Application**: `http://your_server_ip:5118`
- **API Swagger**: `http://your_server_ip:5212/swagger`
- **Database**: Connect with `postgres` user on port `5432`

Login with default credentials:
- **Username**: admin
- **Password**: admin123

---

## Common Operations

### **View Logs**
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f web
docker-compose logs -f postgres
```

### **Stop Services**
```bash
docker-compose stop
```

### **Start Services**
```bash
docker-compose start
```

### **Restart Services**
```bash
docker-compose restart
```

### **Full Cleanup (Remove Everything)**
```bash
# Stop and remove containers, but keep volumes
docker-compose down

# Remove everything including database data
docker-compose down -v
```

### **Rebuild After Code Changes**
```bash
# Pull latest code
git pull

# Rebuild images
docker-compose build

# Restart services
docker-compose up -d
```

---

## Production Configuration

### **Using a Reverse Proxy (Nginx)**

Create `nginx.conf`:

```nginx
upstream api {
    server heatconerp-api:5212;
}

upstream web {
    server heatconerp-web:5118;
}

server {
    listen 80;
    server_name your_domain.com;

    # API routes
    location /api/ {
        proxy_pass http://api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # Swagger UI
    location /swagger/ {
        proxy_pass http://api;
        proxy_set_header Host $host;
    }

    # Web app (Blazor)
    location / {
        proxy_pass http://web;
        proxy_set_header Host $host;
        proxy_set_header Connection "upgrade";
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_http_version 1.1;
    }
}
```

### **Enable HTTPS with Let's Encrypt**

```bash
# Install Certbot
sudo apt-get install certbot python3-certbot-nginx

# Generate certificate
sudo certbot certonly --nginx -d your_domain.com

# Update nginx.conf to use HTTPS
```

---

## Troubleshooting

### **Database Connection Error**

```bash
# Check if postgres is ready
docker-compose logs postgres

# Verify from inside API container
docker-compose exec api ping postgres
```

### **API Not Responding**

```bash
# Check API logs
docker-compose logs api

# Restart API container
docker-compose restart api
```

### **Port Already in Use**

```bash
# Change port in .env file
# API_PORT=5213  # Change from 5212
# WEB_PORT=5119  # Change from 5118

# Restart
docker-compose down
docker-compose up -d
```

### **Database Migration Fails**

```bash
# Check what migrations have been applied
docker-compose exec api dotnet ef migrations list

# Rollback if needed
docker-compose exec api dotnet ef database update <previous_migration_name>
```

### **Out of Disk Space**

```bash
# Check Docker disk usage
docker system df

# Clean up unused images/volumes
docker system prune -a
```

---

## Backup & Recovery

### **Backup Database**

```bash
# Backup PostgreSQL data
docker-compose exec postgres pg_dump -U postgres heatconerp > backup_$(date +%Y%m%d_%H%M%S).sql

# Archive backup
tar -czf heatconerp_backup_$(date +%Y%m%d).tar.gz backup_*.sql
```

### **Restore Database**

```bash
# Restore from backup
docker-compose exec -T postgres psql -U postgres heatconerp < backup_20260301_120000.sql
```

---

## Performance Tuning

### **Increase Database Connection Pool**

Edit `docker-compose.yml` and add to postgres service:

```yaml
environment:
  POSTGRES_INITDB_ARGS: -c max_connections=200
```

### **Enable Database Query Logging**

```yaml
environment:
  POSTGRES_INITDB_ARGS: -c log_statement=all -c log_duration=on
```

View logs:
```bash
docker-compose logs postgres | grep LOG
```

---

## Monitoring

### **Container Health**

```bash
docker-compose ps
docker stats heatconerp-api
docker stats heatconerp-web
docker stats heatconerp-db
```

### **Application Logs**

```bash
# Real-time logs
docker-compose logs -f

# Last 100 lines
docker-compose logs --tail=100 api
```

---

## Update Deployment

When you have new code changes:

```bash
# Pull latest code
git pull

# Rebuild images
docker-compose build

# Restart services
docker-compose up -d

# Apply any new migrations
docker-compose exec api dotnet ef database update --no-build
```

---

## Security Recommendations

1. **Change default credentials** immediately after deployment
2. **Use strong database password** (minimum 16 characters, mixed case + numbers + special chars)
3. **Enable HTTPS** using Let's Encrypt (see Production Configuration section)
4. **Restrict network access** - only allow required ports
5. **Regular backups** - automate with cron jobs
6. **Update base images** regularly:
   ```bash
   docker pull mcr.microsoft.com/dotnet/sdk:10.0
   docker pull mcr.microsoft.com/dotnet/aspnet:10.0
   docker pull postgres:16-alpine
   docker-compose build --no-cache
   ```
7. **Monitor logs** for unusual activity

---

## Support & Quick Reference

| Command | Purpose |
|---------|---------|
| `docker-compose up -d` | Start all services |
| `docker-compose down` | Stop all services |
| `docker-compose logs -f` | View live logs |
| `docker-compose ps` | List running containers |
| `docker-compose build` | Rebuild images |
| `docker-compose exec api bash` | Shell into API container |
| `docker system prune -a` | Clean up unused resources |

