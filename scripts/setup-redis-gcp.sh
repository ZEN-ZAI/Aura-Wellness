#!/usr/bin/env bash
# =============================================================================
# Create (or re-create) a GCP Memorystore for Redis instance.
#
# This script:
#   1. Deletes the existing Redis instance if it exists
#   2. Creates a new Memorystore for Redis instance
#   3. Creates a Serverless VPC Access connector (if missing)
#   4. Prints the connection details for Cloud Run services
#
# Prerequisites:
#   - gcloud CLI authenticated with sufficient permissions
#   - APIs enabled: redis.googleapis.com, vpcaccess.googleapis.com
#
# Usage:
#   ./scripts/setup-redis-gcp.sh                        # uses defaults
#   REDIS_TIER=STANDARD_HA ./scripts/setup-redis-gcp.sh # HA tier
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Configuration (override via environment variables)
# ---------------------------------------------------------------------------
PROJECT="${GCP_PROJECT:-zenzai-dev-project}"
REGION="${GCP_REGION:-asia-southeast1}"
INSTANCE_NAME="${REDIS_INSTANCE:-aura-redis}"
REDIS_VERSION="${REDIS_VERSION:-REDIS_7_2}"
TIER="${REDIS_TIER:-BASIC}"            # BASIC (dev) or STANDARD_HA (prod)
MEMORY_SIZE_GB="${REDIS_MEMORY_GB:-1}" # 1 GB is minimum
NETWORK="${REDIS_NETWORK:-default}"
VPC_CONNECTOR="${VPC_CONNECTOR:-aura-vpc-connector}"
CONNECTOR_RANGE="${CONNECTOR_RANGE:-10.8.0.0/28}"
CONNECTOR_MACHINE="${CONNECTOR_MACHINE:-e2-micro}"

echo "============================================="
echo "  GCP Memorystore for Redis Setup"
echo "============================================="
echo "  Project:       $PROJECT"
echo "  Region:        $REGION"
echo "  Instance:      $INSTANCE_NAME"
echo "  Version:       $REDIS_VERSION"
echo "  Tier:          $TIER"
echo "  Memory:        ${MEMORY_SIZE_GB} GB"
echo "  Network:       $NETWORK"
echo "  VPC Connector: $VPC_CONNECTOR"
echo "============================================="
echo ""

# ---------------------------------------------------------------------------
# 1. Enable required APIs
# ---------------------------------------------------------------------------
echo "==> Enabling required APIs..."
gcloud services enable redis.googleapis.com \
  --project="$PROJECT" --quiet
gcloud services enable vpcaccess.googleapis.com \
  --project="$PROJECT" --quiet
echo "    APIs enabled."

# ---------------------------------------------------------------------------
# 2. Delete existing instance (if any)
# ---------------------------------------------------------------------------
if gcloud redis instances describe "$INSTANCE_NAME" \
    --region="$REGION" \
    --project="$PROJECT" &>/dev/null; then
  echo "==> Existing Redis instance '$INSTANCE_NAME' found. Deleting..."
  gcloud redis instances delete "$INSTANCE_NAME" \
    --region="$REGION" \
    --project="$PROJECT" \
    --quiet
  echo "    Deleted."
else
  echo "==> No existing Redis instance '$INSTANCE_NAME' found. Creating fresh."
fi

# ---------------------------------------------------------------------------
# 3. Create new Memorystore Redis instance
# ---------------------------------------------------------------------------
echo "==> Creating Memorystore Redis instance '$INSTANCE_NAME'..."
gcloud redis instances create "$INSTANCE_NAME" \
  --region="$REGION" \
  --project="$PROJECT" \
  --tier="$TIER" \
  --size="$MEMORY_SIZE_GB" \
  --redis-version="$REDIS_VERSION" \
  --network="$NETWORK" \
  --quiet

echo "    Instance created."

# ---------------------------------------------------------------------------
# 4. Retrieve connection details
# ---------------------------------------------------------------------------
REDIS_HOST=$(gcloud redis instances describe "$INSTANCE_NAME" \
  --region="$REGION" \
  --project="$PROJECT" \
  --format="value(host)")

REDIS_PORT=$(gcloud redis instances describe "$INSTANCE_NAME" \
  --region="$REGION" \
  --project="$PROJECT" \
  --format="value(port)")

echo ""
echo "==> Redis connection details:"
echo "    Host: $REDIS_HOST"
echo "    Port: $REDIS_PORT"
echo "    Addr: ${REDIS_HOST}:${REDIS_PORT}"

# ---------------------------------------------------------------------------
# 5. Ensure Serverless VPC Access connector exists
# ---------------------------------------------------------------------------
if gcloud compute networks vpc-access connectors describe "$VPC_CONNECTOR" \
    --region="$REGION" \
    --project="$PROJECT" &>/dev/null; then
  echo ""
  echo "==> VPC connector '$VPC_CONNECTOR' already exists."
else
  echo ""
  echo "==> Creating Serverless VPC Access connector '$VPC_CONNECTOR'..."
  gcloud compute networks vpc-access connectors create "$VPC_CONNECTOR" \
    --region="$REGION" \
    --project="$PROJECT" \
    --network="$NETWORK" \
    --range="$CONNECTOR_RANGE" \
    --machine-type="$CONNECTOR_MACHINE" \
    --quiet
  echo "    Connector created."
fi

# ---------------------------------------------------------------------------
# 6. Print Cloud Run deployment commands
# ---------------------------------------------------------------------------
echo ""
echo "============================================="
echo "  Setup Complete"
echo "============================================="
echo ""
echo "Redis Address: ${REDIS_HOST}:${REDIS_PORT}"
echo ""
echo "Update your Cloud Run services with:"
echo ""
echo "  # Chat Service (gRPC)"
echo "  gcloud run services update chat-service \\"
echo "    --region=$REGION \\"
echo "    --project=$PROJECT \\"
echo "    --set-env-vars=REDIS_ADDR=${REDIS_HOST}:${REDIS_PORT} \\"
echo "    --vpc-connector=$VPC_CONNECTOR"
echo ""
echo "  # Chat WS (WebSocket)"
echo "  gcloud run services update chat-ws \\"
echo "    --region=$REGION \\"
echo "    --project=$PROJECT \\"
echo "    --set-env-vars=REDIS_ADDR=${REDIS_HOST}:${REDIS_PORT} \\"
echo "    --vpc-connector=$VPC_CONNECTOR"
echo ""
echo "  # Backend (.NET)"
echo "  gcloud run services update backend \\"
echo "    --region=$REGION \\"
echo "    --project=$PROJECT \\"
echo "    --set-env-vars=ConnectionStrings__Redis=${REDIS_HOST}:${REDIS_PORT},allowAdmin=true \\"
echo "    --vpc-connector=$VPC_CONNECTOR"
echo ""
