from datetime import datetime

# Application URLs
BASE_URL = "http://localhost:3000"
BACKEND_URL = "http://localhost:5001"

# Browser config
BROWSER = "chromium"
HEADLESS = True
TIMEOUT = "15s"
NAVIGATION_TIMEOUT = "20s"

# Fixed test owner — seeded by e2e-test.sh before suite runs
E2E_OWNER_EMAIL = "e2e-owner@aura-test.com"
E2E_OWNER_PASSWORD = "E2eTest@123"
E2E_COMPANY_NAME = "E2E Test Company"

# Unique suffix to avoid conflicts when tests create new resources
TIMESTAMP = datetime.now().strftime("%Y%m%d%H%M%S")
