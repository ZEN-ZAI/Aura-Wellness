import os
from datetime import datetime

# Application URLs
BASE_URL = os.environ.get("BASE_URL", "http://localhost:3000")
BACKEND_URL = os.environ.get("BACKEND_URL", "http://localhost:5001")

# Browser config
BROWSER = "chromium"
HEADLESS = os.environ.get("HEADLESS", "true").lower() != "false"
TIMEOUT = "15s"
NAVIGATION_TIMEOUT = "20s"

# Fixed test owner — seeded by DatabaseResetter
E2E_OWNER_EMAIL = os.environ.get("E2E_OWNER_EMAIL", "Welcome@example.com")
E2E_OWNER_PASSWORD = os.environ.get("E2E_OWNER_PASSWORD", "P@ssw0rd")
E2E_COMPANY_NAME = os.environ.get("E2E_COMPANY_NAME", "Aura Wellness Demo")

# Default password assigned to newly created staff members
STAFF_DEFAULT_PASSWORD = os.environ.get("STAFF_DEFAULT_PASSWORD", "P@ssw0rd")

# Unique suffix to avoid conflicts when tests create new resources
TIMESTAMP = datetime.now().strftime("%Y%m%d%H%M%S")
