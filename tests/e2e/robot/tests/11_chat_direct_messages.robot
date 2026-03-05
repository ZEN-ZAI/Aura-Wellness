*** Settings ***
Documentation       Direct Messages — two users exchange private messages via DM.
...
...                 This suite validates the Direct Message workflow:
...
...                 Setup  — owner creates UserA and UserB, grants both chat access.
...                          UserA creates a DM with UserB, then both contexts reload
...                          /chat so their WebSocket connections subscribe to the new
...                          DM conversation channel before any messaging tests run.
...                 Test 1 — DM conversation appears in both users' sidebars with correct
...                          header and hidden Members panel.
...                 Test 2 — UserA sends a DM to UserB; UserB receives it in real-time.
...                 Test 3 — UserB replies in the DM; UserA receives it in real-time.
...                 Test 4 — Both users see the full DM conversation history.
...                 Test 5 — DM messages do NOT appear in the group conversation.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open DM Chat Session
Suite Teardown      Close Browser


*** Variables ***
# Staff accounts created during suite setup
${DM_A_FIRST}           DmUserA
${DM_A_LAST}            Dm${TIMESTAMP}A
${DM_A_EMAIL}           dm-a-${TIMESTAMP}@test.com

${DM_B_FIRST}           DmUserB
${DM_B_LAST}            Dm${TIMESTAMP}B
${DM_B_EMAIL}           dm-b-${TIMESTAMP}@test.com

# Unique message texts
${DM_MSG_A}             DmMsgA ${TIMESTAMP}
${DM_MSG_B}             DmMsgB ${TIMESTAMP}

# Browser context id variables — populated by suite setup
${DM_CTX_A}             ${EMPTY}
${DM_CTX_B}             ${EMPTY}

# Selectors
${CHAT_TEXTAREA}        css=textarea[placeholder*="Type a message"]
${SEND_BUTTON}          css=.ant-btn-primary


*** Keywords ***
Open DM Chat Session
    [Documentation]    One browser — three contexts:
    ...      • setup ctx  : owner creates users and grants access, then closes
    ...      • DM_CTX_A   : logged in as UserA, /chat open, DM created and subscribed
    ...      • DM_CTX_B   : logged in as UserB, /chat open, DM subscribed
    ...
    ...      The DM conversation is created by UserA during setup. UserA then reloads
    ...      /chat so the WebSocket reconnects with the new DM subscription. UserB's
    ...      context is opened AFTER the DM exists, so it subscribes automatically.
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
    Fill Text    ${ANT_MODAL} >> input#firstName    ${DM_A_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${DM_A_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${DM_A_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${DM_A_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Fresh navigation to clear lingering modal DOM
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Create UserB
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${DM_B_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${DM_B_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${DM_B_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${DM_B_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Grant chat access to both
    Navigate To Protected Page    /chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}

    Wait For Elements State    css=tr:has-text("${DM_A_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${DM_A_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${DM_A_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

    Wait For Elements State    css=tr:has-text("${DM_B_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${DM_B_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${DM_B_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

    # Close setup context — it is no longer needed
    Close Context

    # ── Context A: UserA logs in, creates the DM, then reloads ───────────────
    ${ctx_a}=    New Context    viewport={'width': 1280, 'height': 800}
    Set Suite Variable    ${DM_CTX_A}    ${ctx_a}
    New Page    ${BASE_URL}
    Login As    ${DM_A_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    # Create the DM conversation by clicking UserB under "Start a conversation"
    Wait For Elements State    text=Start a conversation    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State
    ...    css=.ant-layout-sider >> text=${DM_B_FIRST} ${DM_B_LAST} >> nth=0
    ...    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=.ant-layout-sider >> text=${DM_B_FIRST} ${DM_B_LAST} >> nth=0
    Wait For Elements State    text=Direct Messages    visible    timeout=${NAVIGATION_TIMEOUT}

    # Reload /chat so the WebSocket reconnects with the new DM subscription.
    # Without this, the WS write-pump has no goroutine for the DM conversation
    # and message echoes never arrive.
    Navigate To Protected Page    /chat
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    # ── Context B: UserB logs in AFTER the DM exists ─────────────────────────
    # The WebSocket will subscribe to all conversations including the DM.
    ${ctx_b}=    New Context    viewport={'width': 1280, 'height': 800}
    Set Suite Variable    ${DM_CTX_B}    ${ctx_b}
    New Page    ${BASE_URL}
    Login As    ${DM_B_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    # Allow both WebSocket connections to complete their Redis SUBSCRIBE handshake
    Sleep    3s

Send DM In Context
    [Documentation]    Switch to the given context, type msg, click Send, confirm it appears.
    [Arguments]    ${ctx}    ${msg}
    Switch Context    ${ctx}
    Fill Text    ${CHAT_TEXTAREA}    ${msg}
    Wait For Elements State    ${SEND_BUTTON}    enabled    timeout=5s
    Click    ${SEND_BUTTON}
    Wait For Elements State    text=${msg} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s

DM Visible In Context
    [Documentation]    Switch to the given context and assert msg is visible without reloading.
    [Arguments]    ${ctx}    ${msg}
    Switch Context    ${ctx}
    Wait For Elements State    text=${msg} >> nth=0    visible    timeout=30s

Click Conversation In Sidebar
    [Documentation]    Click a conversation entry in the left sidebar by its label text.
    [Arguments]    ${label}
    Wait For Elements State    css=.ant-layout-sider >> text=${label} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=.ant-layout-sider >> text=${label} >> nth=0

Header Shows DM With User
    [Documentation]    Assert the chat header shows the DM partner's name (not the group name).
    [Arguments]    ${name}
    Wait For Elements State    css=.ant-layout-content >> css=h5 >> text=${name} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

Members Panel Is Hidden
    [Documentation]    The right-side member list should NOT be visible in a DM conversation.
    Wait For Elements State    text=Members    hidden    timeout=5s


*** Test Cases ***
DM Conversation Appears For Both Users
    [Documentation]    Both users see the DM conversation in their sidebar.
    ...    Clicking it shows the correct header and hides the Members panel.
    [Tags]    chat-dm
    # UserA sees the DM in their sidebar
    Switch Context    ${DM_CTX_A}
    Wait For Elements State    text=Direct Messages    visible    timeout=${NAVIGATION_TIMEOUT}
    Click Conversation In Sidebar    ${DM_B_FIRST} ${DM_B_LAST}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    Header Shows DM With User    ${DM_B_FIRST} ${DM_B_LAST}
    Members Panel Is Hidden

    # UserB sees the DM in their sidebar
    Switch Context    ${DM_CTX_B}
    Wait For Elements State    text=Direct Messages    visible    timeout=${NAVIGATION_TIMEOUT}
    Click Conversation In Sidebar    ${DM_A_FIRST} ${DM_A_LAST}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    Header Shows DM With User    ${DM_A_FIRST} ${DM_A_LAST}
    Members Panel Is Hidden

UserA Sends A DM And UserB Receives It In Real Time
    [Documentation]    UserA sends a message in the DM. UserB should receive it in
    ...    real-time via the WebSocket subscription — no page refresh.
    [Tags]    chat-dm
    # Send from UserA's DM context
    Send DM In Context    ${DM_CTX_A}    ${DM_MSG_A}

    # UserB should receive the message in real-time (already on the DM conversation)
    DM Visible In Context    ${DM_CTX_B}    ${DM_MSG_A}

UserB Replies In The DM And UserA Receives It In Real Time
    [Documentation]    UserB sends a reply in the DM. UserA should receive it in
    ...    real-time without refreshing.
    [Tags]    chat-dm
    # Send from UserB's DM context
    Send DM In Context    ${DM_CTX_B}    ${DM_MSG_B}

    # UserA should receive UserB's reply in real-time
    DM Visible In Context    ${DM_CTX_A}    ${DM_MSG_B}

    # Sender label should show UserB's name in UserA's chat
    Switch Context    ${DM_CTX_A}
    Wait For Elements State
    ...    css=.ant-layout-content >> text=${DM_B_FIRST} ${DM_B_LAST} >> nth=0
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

Both Users See Full DM Conversation History
    [Documentation]    Both users should see the complete DM conversation:
    ...    UserA's original message and UserB's reply.
    [Tags]    chat-dm
    # UserA sees both messages
    Switch Context    ${DM_CTX_A}
    Wait For Elements State    text=${DM_MSG_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=${DM_MSG_B} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # UserB sees both messages
    Switch Context    ${DM_CTX_B}
    Wait For Elements State    text=${DM_MSG_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=${DM_MSG_B} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

DM Messages Do Not Appear In Group Conversation
    [Documentation]    Switch both users back to the group conversation and verify
    ...    that the DM messages are NOT visible there. This confirms message isolation.
    [Tags]    chat-dm
    # UserA navigates to /chat which auto-selects the group conversation
    Switch Context    ${DM_CTX_A}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    # DM messages must NOT be visible in the group conversation
    Wait For Elements State    text=${DM_MSG_A}    hidden    timeout=5s
    Wait For Elements State    text=${DM_MSG_B}    hidden    timeout=5s

    # UserB navigates to /chat which auto-selects the group conversation
    Switch Context    ${DM_CTX_B}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    # DM messages must NOT be visible in the group conversation
    Wait For Elements State    text=${DM_MSG_A}    hidden    timeout=5s
    Wait For Elements State    text=${DM_MSG_B}    hidden    timeout=5s
