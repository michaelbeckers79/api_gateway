#!/bin/bash

# API Gateway Test Script
# This script demonstrates the OAuth authentication flow

BASE_URL="http://localhost:5261"

echo "=== API Gateway Test Script ==="
echo ""

# 1. Check health
echo "1. Checking health..."
curl -s "${BASE_URL}/health"
echo -e "\n"

# 2. Check if logged in (should be false)
echo "2. Checking if logged in (should be false)..."
curl -s "${BASE_URL}/oauth/isloggedin" | jq .
echo ""

# 3. Start login flow
echo "3. Starting login flow..."
RESPONSE=$(curl -s -X POST "${BASE_URL}/oauth/login/start" \
  -H "Content-Type: application/json" \
  -d '{"redirectUri":"http://localhost:5261/oauth/callback"}')

echo "$RESPONSE" | jq .
echo ""

# Extract authorization URL
AUTH_URL=$(echo "$RESPONSE" | jq -r '.authorizationUrl')
echo "Authorization URL: $AUTH_URL"
echo ""

# 4. Instructions for next steps
echo "=== Next Steps ==="
echo "To complete the authentication flow:"
echo "1. Configure your OAuth provider settings in appsettings.json"
echo "2. Navigate to the authorization URL in a browser"
echo "3. Complete the OAuth flow with your identity provider"
echo "4. You will be redirected to /oauth/callback with a code"
echo "5. The gateway will exchange the code for tokens"
echo "6. A session cookie will be set automatically"
echo ""

# 5. Test logout (will fail without session)
echo "4. Testing logout (will succeed even without session)..."
curl -s -X POST "${BASE_URL}/oauth/logout" | jq .
echo ""

echo "=== Test Complete ==="
