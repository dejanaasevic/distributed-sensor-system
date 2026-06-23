#!/bin/bash

# 1. Ensure the k8s target directory exists
mkdir -p k8s

# 2. Surgically extract ONLY the needed variables from .env
# Using cut to grab everything after the first '=' sign
DB_USER=$(grep '^POSTGRES_USER=' .env | cut -d= -f2-)
DB_PASS=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)
DB_NAME=$(grep '^POSTGRES_DB=' .env | cut -d= -f2-)

# 3. Construct the connection string dynamically
CONN_STR="Host=db;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS}"

# 4. Base64 encode the values 
# (echo -n prevents trailing newlines, tr -d '\n' handles cross-platform base64 line wrapping)
B64_USER=$(echo -n "$DB_USER" | base64 | tr -d '\n')
B64_PASS=$(echo -n "$DB_PASS" | base64 | tr -d '\n')
B64_NAME=$(echo -n "$DB_NAME" | base64 | tr -d '\n')
B64_CONN=$(echo -n "$CONN_STR" | base64 | tr -d '\n')

# 5. Output directly to the k8s directory using a heredoc
cat <<EOF > k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: postgres-secret
type: Opaque
data:
  postgres-user: ${B64_USER}
  postgres-password: ${B64_PASS}
  postgres-db: ${B64_NAME}
  connection-string: ${B64_CONN}
EOF

echo "✅ Successfully generated k8s/secrets.yaml!"