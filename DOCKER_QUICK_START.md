# Docker Deployment - Quick Summary

## Files Created

1. **Dockerfile.api** - Builds the .NET API service
2. **Dockerfile.web** - Builds the Blazor Web application
3. **docker-compose.yml** - Orchestrates all services (API, Web, PostgreSQL)
4. **.dockerignore** - Optimizes docker builds by excluding unnecessary files
5. **DOCKER_DEPLOYMENT.md** - Comprehensive deployment documentation
6. **deploy.sh** - Automated deployment script (Linux/Mac)
7. **deploy.ps1** - Automated deployment script (Windows PowerShell)

---

## Exact 7-Step Deployment Process

### **For Linux/Mac Servers:**

```bash
# 1. Clone repository
git clone <your-repo-url> && cd heatconERP

# 2. Configure environment
nano .env  # Edit with production values

# 3. Make deploy script executable
chmod +x deploy.sh

# 4. Run deployment
./deploy.sh

# 5. Access application
# Web: http://your_server_ip:5118
# API: http://your_server_ip:5212/swagger
```

### **For Windows Servers:**

```powershell
# 1. Clone repository
git clone <your-repo-url>; cd heatconERP

# 2. Configure environment
notepad .env  # Edit with production values

# 3. Run deployment
.\deploy.ps1

# 4. Access application
# Web: http://your_server_ip:5118
# API: http://your_server_ip:5212/swagger
```

### **Manual Steps (If Script Fails):**

```bash
# 1. Clone and navigate
git clone <your-repo-url> && cd heatconERP

# 2. Edit .env with production database password
# Update: DB_PASSWORD, API_PORT, WEB_PORT

# 3. Build images
docker-compose build

# 4. Start services
docker-compose up -d

# 5. Wait for PostgreSQL (healthy status)
docker-compose ps

# 6. Apply database migrations
docker-compose exec api dotnet ef database update --no-build

# 7. Verify all containers running
docker-compose ps
```

---

## Key Ports

| Service | Port | URL |
|---------|------|-----|
| Web Application | 5118 | http://server:5118 |
| API | 5212 | http://server:5212 |
| API Swagger | 5212 | http://server:5212/swagger |
| PostgreSQL | 5432 | server:5432 |

---

## Configuration (.env file)

```dotenv
# SECURITY: Change these values!
DB_USER=postgres
DB_PASSWORD=YOUR_SECURE_PASSWORD_HERE   # Use: openssl rand -base64 32
DB_NAME=heatconerp
DB_PORT=5432

# Ports (change if needed on server)
API_PORT=5212
WEB_PORT=5118
```

---

## Default Credentials

**Change these immediately after first login!**

```
Username: admin
Password: admin123
```

---

## Essential Commands

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose stop

# View logs (real-time)
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f web

# Check service status
docker-compose ps

# Restart specific service
docker-compose restart api

# SSH into API container
docker-compose exec api bash

# Backup database
docker-compose exec postgres pg_dump -U postgres heatconerp > backup.sql

# Full cleanup
docker-compose down -v
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Port already in use | Change port in .env and restart: `docker-compose down && docker-compose up -d` |
| Database connection error | Check: `docker-compose logs postgres` |
| Migration failed | Check: `docker-compose logs api` |
| Out of memory | Reduce processes or increase server RAM |
| API not responding | Restart: `docker-compose restart api` |

---

## Important Notes

✅ **PostgreSQL data persists** in Docker volume (safe to restart containers)  
✅ **All services auto-restart** if server reboots (restart: unless-stopped policy)  
✅ **Logs are preserved** even after container restart  
✅ **Use strong passwords** - database is exposed to the network  
✅ **Back up regularly** - automated scheduled backups recommended  
✅ **HTTPS recommended** - Set up reverse proxy with Let's Encrypt (see DOCKER_DEPLOYMENT.md)

---

## Next Steps

1. Review [DOCKER_DEPLOYMENT.md](DOCKER_DEPLOYMENT.md) for production setup
2. Set up HTTPS with Nginx reverse proxy
3. Configure automated backups
4. Set up monitoring and alerting
5. Create firewall rules to restrict access

---

For detailed information, see `DOCKER_DEPLOYMENT.md`
