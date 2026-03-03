*** Settings ***
Documentation       Reset the E2E test database to a known, seeded state via API.
...                 This suite runs first (00_) so every subsequent suite
...                 starts from a clean baseline.
Library             RequestsLibrary
Variables           ../variables.py


*** Test Cases ***
Reset Database Via API
    [Documentation]    Obtain a JWT for the seeded owner, then call POST /api/admin/reset-db.
    ...    Uses the REST API directly — no browser required — so this step is fast
    ...    and independent of frontend availability.
    [Tags]    smoke    admin    reset

    # Step 1 — authenticate and retrieve the access token
    ${login_body}=    Create Dictionary
    ...    email=${E2E_OWNER_EMAIL}
    ...    password=${E2E_OWNER_PASSWORD}
    ${login_response}=    POST
    ...    url=${BACKEND_URL}/api/auth/login
    ...    json=${login_body}
    ...    expected_status=200
    ${access_token}=    Set Variable    ${login_response.json()["token"]}

    # Step 2 — call the reset-db endpoint with the bearer token
    ${auth_headers}=    Create Dictionary    Authorization=Bearer ${access_token}
    POST
    ...    url=${BACKEND_URL}/api/admin/reset-db
    ...    headers=${auth_headers}
    ...    expected_status=204
