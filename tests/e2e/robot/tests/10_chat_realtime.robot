*** Settings ***
Documentation       Real-time bidirectional chat — two concurrent browser contexts.
...
...                 Both users are logged in simultaneously (separate browser contexts,
...                 each with their own session cookies). This validates that WebSocket
...                 pub/sub delivers messages live without requiring a page refresh:
...
...                 Setup  — owner creates UserA and UserB, grants both chat access,
...                          then opens a dedicated /chat page for each user.
...                 Test 1 — UserA sends a message; UserB's already-open /chat must show
...                          it in real-time via the WebSocket subscription.
...                 Test 2 — UserB sends a reply; UserA's already-open /chat must show
...                          it in real-time.
...                 Test 3 — cross-check: both messages are visible in both contexts.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open Realtime Chat Session
Suite Teardown      Close Browser


*** Variables ***
# Staff accounts created during suite setup
${RT_A_FIRST}           RtUserA
${RT_A_LAST}            Rt${TIMESTAMP}A
${RT_A_EMAIL}           rt-a-${TIMESTAMP}@test.com

${RT_B_FIRST}           RtUserB
${RT_B_LAST}            Rt${TIMESTAMP}B
${RT_B_EMAIL}           rt-b-${TIMESTAMP}@test.com

# Unique message texts (no brackets — Playwright text= locators treat [ as regex/attr)
${MSG_RT_A}             RtMsgA ${TIMESTAMP}
${MSG_RT_B}             RtMsgB ${TIMESTAMP}

# Browser context id variables — populated by suite setup
${CTX_A}                ${EMPTY}
${CTX_B}                ${EMPTY}

# Selectors
${CHAT_TEXTAREA}        css=textarea[placeholder*="Type a message"]
${SEND_BUTTON}          css=.ant-btn-primary


*** Keywords ***
Open Realtime Chat Session
    [Documentation]    One browser — three contexts:
    ...      • setup ctx  : owner creates users and grants access, then closes
    ...      • CTX_A      : logged in as UserA, /chat open and ready
    ...      • CTX_B      : logged in as UserB, /chat open and ready
    New Browser    browser=${BROWSER}    headless=${HEADLESS}
    Set Browser Timeout    ${NAVIGATION_TIMEOUT}

    # ── Setup context: owner work ────────────────────────────────────────────
    New Context    viewport={'width': 1280, 'height': 800}
    New Page    ${BASE_URL}
    Set Browser Timeout    ${TIMEOUT}
    Login As Owner

    # Create UserA
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${RT_A_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${RT_A_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${RT_A_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${RT_A_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Navigate back to /staff so the DOM is clean before opening the next Add Staff modal.
    # (The "Staff Member Created" success modal leaves a lingering .ant-modal-container in
    # the DOM; a fresh navigation clears it and avoids a strict-mode violation.)
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Create UserB
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${RT_B_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${RT_B_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${RT_B_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${RT_B_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Grant chat access to both
    Navigate To Protected Page    /chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}

    Wait For Elements State    css=tr:has-text("${RT_A_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${RT_A_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${RT_A_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

    Wait For Elements State    css=tr:has-text("${RT_B_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${RT_B_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${RT_B_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

    # Close setup context — it is no longer needed
    Close Context

    # ── Context A: UserA logged in with /chat open ───────────────────────────
    ${ctx_a}=    New Context    viewport={'width': 1280, 'height': 800}
    Set Suite Variable    ${CTX_A}    ${ctx_a}
    New Page    ${BASE_URL}
    Login As    ${RT_A_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    # ── Context B: UserB logged in with /chat open ───────────────────────────
    ${ctx_b}=    New Context    viewport={'width': 1280, 'height': 800}
    Set Suite Variable    ${CTX_B}    ${ctx_b}
    New Page    ${BASE_URL}
    Login As    ${RT_B_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    # Allow both WebSocket connections to fully complete their Redis SUBSCRIBE handshake
    # before any message is sent.  The UI elements above confirm the component mounted
    # and the HTTP requests finished, but the WS upgrade + pub/sub setup is async.
    Sleep    3s

Send Message In Context
    [Documentation]    Switch to the given context, type msg, click Send, confirm textarea clears.
    [Arguments]    ${ctx}    ${msg}
    Switch Context    ${ctx}
    ${url}=    Get Url
    Log    [Send] context=${ctx} url=${url}    level=INFO
    Fill Text    ${CHAT_TEXTAREA}    ${msg}
    Wait For Elements State    ${SEND_BUTTON}    enabled    timeout=5s
    Click    ${SEND_BUTTON}
    Wait For Elements State    text=${msg} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s

Message Visible In Context
    [Documentation]    Switch to the given context and assert msg is visible without reloading.
    ...    Uses a 30 s timeout — WebSocket delivery should be sub-second, but generous
    ...    headroom is kept to avoid flakiness on slow CI environments.
    [Arguments]    ${ctx}    ${msg}
    Switch Context    ${ctx}
    ${url}=    Get Url
    Log    [Receive] context=${ctx} url=${url}    level=INFO
    Wait For Elements State    text=${msg} >> nth=0    visible    timeout=30s

Sender Label Visible In Content Area
    [Documentation]    Assert sender name appears inside the message content column (not sidebar).
    [Arguments]    ${ctx}    ${sender_name}
    Switch Context    ${ctx}
    Wait For Elements State
    ...    css=.ant-layout-content >> text=${sender_name} >> nth=0
    ...    visible    timeout=${NAVIGATION_TIMEOUT}


*** Test Cases ***
UserA Sends And UserB Receives In Real Time
    [Documentation]    UserA sends a message while UserB is already on /chat.
    ...    UserB's page must display the message via WebSocket — no refresh allowed.
    [Tags]    chat-realtime
    # Send from UserA's context
    Send Message In Context    ${CTX_A}    ${MSG_RT_A}

    # Immediately switch to UserB — message must arrive through the live WS subscription
    Message Visible In Context    ${CTX_B}    ${MSG_RT_A}

    # UserB's chat shows UserA's sender label in the message column
    Sender Label Visible In Content Area    ${CTX_B}    ${RT_A_FIRST} ${RT_A_LAST}

UserB Replies And UserA Receives In Real Time
    [Documentation]    UserB sends a reply while UserA is already on /chat.
    ...    UserA's page must display the reply via WebSocket — no refresh allowed.
    [Tags]    chat-realtime
    # Send from UserB's context
    Send Message In Context    ${CTX_B}    ${MSG_RT_B}

    # Immediately switch to UserA — reply must arrive through the live WS subscription
    Message Visible In Context    ${CTX_A}    ${MSG_RT_B}

    # UserA's chat shows UserB's sender label in the message column
    Sender Label Visible In Content Area    ${CTX_A}    ${RT_B_FIRST} ${RT_B_LAST}

Both Messages Visible In Both Contexts
    [Documentation]    After the exchange, both contexts must show the full conversation:
    ...    UserA sees their own message and UserB's reply; UserB sees UserA's message and
    ...    their own reply. Confirms message history is consistent for all participants.
    [Tags]    chat-realtime
    # UserA context: own message + B's reply
    Switch Context    ${CTX_A}
    Wait For Elements State    text=${MSG_RT_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=${MSG_RT_B} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # UserB context: A's message + own reply
    Switch Context    ${CTX_B}
    Wait For Elements State    text=${MSG_RT_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=${MSG_RT_B} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
