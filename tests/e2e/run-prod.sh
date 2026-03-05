#!/usr/bin/env bash
# =============================================================================
# Run Robot Framework e2e tests against the production environment.
#
# Usage:
#   ./run-prod.sh                  # all tests, headless
#   ./run-prod.sh --headed         # all tests, with visible browser
#   ./run-prod.sh 08_chat_message  # specific suite, headless
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.test.prod"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "ERROR: $ENV_FILE not found."
  exit 1
fi

# Load production env vars
set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

# Fetch STAFF_DEFAULT_PASSWORD from GCP Secret Manager
echo "==> Fetching DEFAULT_STAFF_PASSWORD from GCP Secret Manager..."
STAFF_DEFAULT_PASSWORD=$(gcloud secrets versions access latest \
  --secret="DEFAULT_STAFF_PASSWORD" \
  --project="zenzai-dev-project" 2>&1)
export STAFF_DEFAULT_PASSWORD

# Parse args
HEADLESS_FLAG=""
SUITE_FILTER=""
HEADED=false

for arg in "$@"; do
  case "$arg" in
    --headed) HEADED=true ;;
    *)        SUITE_FILTER="$arg" ;;
  esac
done

if [[ "$HEADED" == true ]]; then
  export HEADLESS=false
else
  export HEADLESS=true
fi

# Build robot command
ROBOT_ARGS=(
  --outputdir "$SCRIPT_DIR/results"
  --pythonpath "$SCRIPT_DIR/robot"
  # Skip DB-reset suites — never run against production
  --exclude reset
  --exclude local
  --loglevel INFO
)

if [[ -n "$SUITE_FILTER" ]]; then
  ROBOT_ARGS+=(--suite "$SUITE_FILTER")
fi

echo "==> Running e2e tests against production"
echo "    BASE_URL        : $BASE_URL"
echo "    BACKEND_URL     : $BACKEND_URL"
echo "    CHAT_WS_URL     : $CHAT_WS_URL"
echo ""

cd "$SCRIPT_DIR"
python3 -m robot "${ROBOT_ARGS[@]}" robot/tests

EXIT_CODE=$?

# Open the HTML report
REPORT="$SCRIPT_DIR/results/report.html"
if [[ -f "$REPORT" ]]; then
  echo ""
  echo "==> Report: $REPORT"
  open "$REPORT" 2>/dev/null || true
fi

exit $EXIT_CODE
