Quick Start guide

Prerequisites
- .Net 9 SDK installed
- sqlite3 
- jq for prettier curls


Quick run (dev)
1. cd /MyProject/backend
2. run dotnet restore, run doetnet build
3. Create and apply migrations to data base
    dotnet ef migrations add AddUsers
    dotnet ef database upate
4. run in dev
    ASPNETCORE_ENVIORNMENT=Development dotnet run
5. checkout the swagger UI!!
    http://localhost:5241/swagger

If you use default seed
- username: josh
- password: password123!

Endpoint curl commands

Register (create account)
curl -s -X POST http://localhost:5241/api/auth/register \
-H "Content-Type: application/json" \
- d  '{"username":"newuser", "password":"mypassword"}' | jq .

Login to get JWT bearer token
TOKEN=$(curl -s -X POST http://localhost5241/api/auth/login \
-H "Content-Type: application/json" \
-d '{"username":"newuser","password":"mypassword"}' | jq -r '.token // . Token')
echo $TOKEN

# Create band (POST)
curl -s -X POST http://localhost:5241/api/bands \
-H "Content-Type: application/json" \
-H "Authorization : Bearer $TOKEN" \
-d '{name: "New Band", "genre", "EDM", "dateTime" :"2026-07-01T20:00:00", "stage":"Main"}' | jq .

# Update band (PUT) - replace <ID>
curl -s -X PUT http://localhost:5241/api/bands/<ID> \
-H "Content-Type: application/json" \
-H "Authorization : Bearer $TOKEN" \
-d '{name: "New Band", "genre", "EDM", "dateTime" :"2026-07-01T20:00:00", "stage":"Main"}' | jq .

# Delete band (DELETE) - replace <ID>
curl -i -X DELETE http://localhost:5241/api/bands/<ID> \
-H "Authorization: Bearer $TOKEN"


DB inspection (copying safely)
cp musicfestival.db /tmp/musicfestival.db

Example SQL commands
sqlite3 /tmp/musicfestival.db "SELECT id, username, role FROM Users;"
sqlite3 /tmp/musicfestival.db "SELECT id, name, DateTime FROM Bands;"

Docker Notes
- Containerize with a Dockerfile
- Use a contianer host (Probably Render as I have used that before)
- Dockerfile -> build image -> push to registry -> Deploy on host

Notes
- Make sure to update readme with any new information
