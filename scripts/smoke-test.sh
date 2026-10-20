#!/usr/bin/env bash
# Local smoke test: real tokens from the local Keycloak, real calls to the running API.
# Usage: scripts/smoke-test.sh   (API on :5080, Keycloak on :8080)
set -u
KC=${KC:-http://localhost:8080}
API=${API:-http://localhost:5080}

token() {
  curl -s -X POST "$KC/realms/accessdesk/protocol/openid-connect/token" \
    -d grant_type=password -d client_id=accessdesk-dev-cli \
    -d "username=$1" -d "password=$1-dev-only" |
    sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p'
}

status() { # method path token [json]
  if [ -n "${4:-}" ]; then
    curl -s -o /tmp/smoke-body.json -w "%{http_code}" -X "$1" "$API$2" \
      -H "Authorization: Bearer $3" -H "Content-Type: application/json" -d "$4"
  else
    curl -s -o /tmp/smoke-body.json -w "%{http_code}" -X "$1" "$API$2" \
      -H "Authorization: Bearer $3"
  fi
}

check() { # label expected actual
  if [ "$2" = "$3" ]; then echo "PASS  $1 -> $3"; else echo "FAIL  $1 -> expected $2, got $3"; FAILED=1; fi
}

FAILED=0
ALICE=$(token alice); MIA=$(token mia); ANN=$(token ann)
[ -n "$ALICE" ] && [ -n "$MIA" ] && [ -n "$ANN" ] || { echo "Could not get tokens from Keycloak"; exit 1; }

check "no token on GET /api/requests" 401 "$(curl -s -o /dev/null -w '%{http_code}' "$API/api/requests")"

check "alice creates a request" 201 "$(status POST /api/requests "$ALICE" '{"system":"GitHub","justification":"Need repo access for the platform team"}')"
ID=$(sed -n 's/.*"id":\([0-9]*\).*/\1/p' /tmp/smoke-body.json)
echo "      created request id $ID"

check "alice lists her requests" 200 "$(status GET /api/requests "$ALICE")"
check "alice cannot approve (employee)" 403 "$(status POST "/api/requests/$ID/approve" "$ALICE" '{}')"
check "mia approves" 200 "$(status POST "/api/requests/$ID/approve" "$MIA" '{"comment":"approved for the project"}')"
check "mia cannot decide it twice" 409 "$(status POST "/api/requests/$ID/reject" "$MIA" '{}')"
check "unknown request id" 404 "$(status POST /api/requests/987654/approve "$MIA" '{}')"
check "alice cannot read audit" 403 "$(status GET /api/audit "$ALICE")"
check "mia cannot read audit" 403 "$(status GET /api/audit "$MIA")"
check "ann reads audit" 200 "$(status GET /api/audit "$ANN")"
echo "      audit entries: $(grep -o '"action"' /tmp/smoke-body.json | wc -l)"

[ "$FAILED" = "0" ] && echo "ALL CHECKS PASSED" || { echo "SOME CHECKS FAILED"; exit 1; }
