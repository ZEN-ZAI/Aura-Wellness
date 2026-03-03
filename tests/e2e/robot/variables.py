from datetime import datetime

# Application URLs
BASE_URL = "http://localhost:3000"
BACKEND_URL = "http://localhost:5001"

# Browser config
BROWSER = "chromium"
HEADLESS = True
TIMEOUT = "15s"
NAVIGATION_TIMEOUT = "20s"

# Fixed test owner — seeded by DatabaseResetter
E2E_OWNER_EMAIL = "Welcome@example.com"
E2E_OWNER_PASSWORD = "P@ssw0rd"
E2E_COMPANY_NAME = "Aura Wellness Demo"

# Unique suffix to avoid conflicts when tests create new resources
TIMESTAMP = datetime.now().strftime("%Y%m%d%H%M%S")
