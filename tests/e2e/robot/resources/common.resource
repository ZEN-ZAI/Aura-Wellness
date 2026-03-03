*** Settings ***
Library     Browser
Variables   ../variables.py


*** Keywords ***
Open Browser Session
    [Documentation]    Launch a new browser, context, and page with configured defaults.
    New Browser    browser=${BROWSER}    headless=${HEADLESS}
    New Context    viewport={'width': 1280, 'height': 800}
    New Page       ${BASE_URL}
    Set Browser Timeout    ${TIMEOUT}

Close Browser Session
    [Documentation]    Close the browser and all contexts.
    Close Browser

Login As Owner
    [Documentation]    Log in using the fixed E2E test owner credentials.
    Login As    ${E2E_OWNER_EMAIL}    ${E2E_OWNER_PASSWORD}

Login As
    [Documentation]    Log in with given email and password, then wait for dashboard.
    ...    Handles the optional BU-selection step: if a BU dropdown appears after the
    ...    first submit the keyword selects the first available option and submits again.
    [Arguments]    ${email}    ${password}
    Go To    ${BASE_URL}/login
    Wait For Elements State    input#email    visible
    Clear Text    input#email
    Fill Text    input#email    ${email}
    Clear Text    input#password
    Fill Text    input#password    ${password}
    Click    button:has-text("Sign In")
    # If the owner belongs to multiple BUs, the form shows a BU picker — submit once more
    ${needs_bu}=    Run Keyword And Return Status
    ...    Wait For Elements State    button:has-text("Continue")    visible    timeout=3s
    IF    ${needs_bu}
        Click    button:has-text("Continue")
    END
    Wait For URL    /dashboard

Logout
    [Documentation]    Click the Sign Out button in the sidebar and wait for redirect.
    Click    button:has-text("Sign Out")
    Wait For URL    /login

Navigate To Protected Page
    [Documentation]    Navigate to a page and wait for network idle (ensures React hydration
    ...    and any immediate page-level API calls complete before tests interact).
    [Arguments]    ${path}
    Go To    ${BASE_URL}${path}
    Wait For Load State    networkidle    timeout=30s

Wait For URL
    [Documentation]    Poll the current page URL until it contains the given path fragment.
    ...    Strips leading ** glob prefix so both "/dashboard" and "**/dashboard" work.
    [Arguments]    ${url_pattern}    ${timeout}=${NAVIGATION_TIMEOUT}
    ${fragment}=    Evaluate    '${url_pattern}'.replace('**', '')
    Wait Until Keyword Succeeds    ${timeout}    500ms    _URL Contains Fragment    ${fragment}

_URL Contains Fragment
    [Documentation]    Assert the current URL contains fragment (used internally by Wait For URL).
    [Arguments]    ${fragment}
    ${url}=    Get Url
    Should Contain    ${url}    ${fragment}

Select Ant Option
    [Documentation]    Open an Ant Design Select dropdown and click an option by visible text.
    [Arguments]    ${trigger_selector}    ${option_text}
    Wait For Elements State    ${trigger_selector}    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    ${trigger_selector}
    Wait For Elements State    css=.ant-select-dropdown    visible    timeout=${NAVIGATION_TIMEOUT}
    # Use >> nth=0 to avoid strict-mode violation when multiple options are present
    Wait For Elements State    css=.ant-select-item-option >> nth=0    visible    timeout=20s
    Click    css=.ant-select-item-option-content >> text="${option_text}"
    Wait For Elements State    css=.ant-select-dropdown    hidden    timeout=${NAVIGATION_TIMEOUT}

Select Ant Option By Index
    [Documentation]    Open an Ant Design Select dropdown and pick the option at 0-based index.
    ...    Waits for options to load before selecting (handles async option lists).
    [Arguments]    ${trigger_selector}    ${index}=0
    Wait For Elements State    ${trigger_selector}    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    ${trigger_selector}
    Wait For Elements State    css=.ant-select-dropdown    visible    timeout=${NAVIGATION_TIMEOUT}
    # Use >> nth=0 to avoid strict-mode violation when multiple options are present
    Wait For Elements State    css=.ant-select-item-option >> nth=0    visible    timeout=20s
    Click    css=.ant-select-item-option >> nth=${index}
    Wait For Elements State    css=.ant-select-dropdown    hidden    timeout=${NAVIGATION_TIMEOUT}

Click And Wait For Modal
    [Documentation]    Click a button and retry until the expected modal appears.
    ...    Handles pre-hydration clicks and avoids double-clicks when the modal is
    ...    already open from a previous attempt.
    [Arguments]    ${button_selector}    ${modal_selector}=${ANT_MODAL}
    Wait Until Keyword Succeeds    30s    2s
    ...    _Click Then Check Modal    ${button_selector}    ${modal_selector}

_Click Then Check Modal
    [Documentation]    If the modal is already visible return immediately; otherwise click
    ...    the button and wait for the modal to appear.
    ...    React 18 batches the setShowModal state update to a microtask that runs
    ...    after the click event handler returns, so the modal-wrap never covers the
    ...    button during Playwright's click sequence — no interception, no retry.
    ...    The [data-ready] attribute is set by useEffect once React has hydrated the
    ...    component and all onClick handlers are attached.
    [Arguments]    ${button_selector}    ${modal_selector}
    # If the modal is already open (from a previous attempt), succeed without re-clicking
    ${already_open}=    Run Keyword And Return Status
    ...    Wait For Elements State    ${modal_selector}    visible    timeout=1s
    Return From Keyword If    ${already_open}
    # Wait for React hydration (useEffect sets [data-ready] once mounted and interactive)
    Wait For Elements State    css=[data-ready]    visible    timeout=30s
    Click    ${button_selector}
    Wait For Elements State    ${modal_selector}    visible    timeout=8s

Clear Auth Cookies
    [Documentation]    Delete all cookies so subsequent navigation starts unauthenticated.
    Delete All Cookies

Element Count Should Be Greater Than
    [Documentation]    Assert that the count of elements matching selector is > 0.
    [Arguments]    ${selector}    ${min_count}=1
    ${count}=    Get Element Count    ${selector}
    Should Be True    ${count} >= ${min_count}
    ...    msg=Expected at least ${min_count} element(s) matching '${selector}', got ${count}
