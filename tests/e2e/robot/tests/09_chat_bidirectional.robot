*** Settings ***
Documentation       Bidirectional chat flow — two staff members exchange messages in the
...                 shared BU workspace channel:
...
...                 1. Owner creates two staff members and grants both chat access.
...                 2. User A logs in, opens /chat, and sends a message.
...                 3. User B logs in, opens /chat, verifies User A's message is visible,
...                    and sends a reply.
...                 4. User A logs back in, opens /chat, and verifies User B's reply
...                    appears with the correct sender attribution.
...
...                 Tests run in order; each case builds on state left by the previous one.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open Browser Session
Suite Teardown      Close Browser Session
Test Teardown       Run Keyword If Test Failed    Delete All Cookies


*** Variables ***
# User A
${CHAT_A_FIRST}         ChatUserA
${CHAT_A_LAST}          BidirA${TIMESTAMP}
${CHAT_A_EMAIL}         chat-a-${TIMESTAMP}@test.com

# User B
${CHAT_B_FIRST}         ChatUserB
${CHAT_B_LAST}          BidirB${TIMESTAMP}
${CHAT_B_EMAIL}         chat-b-${TIMESTAMP}@test.com

# Message texts (TIMESTAMP keeps them unique across runs)
${MSG_FROM_A}           Hello from User A — ${TIMESTAMP}
${MSG_FROM_B}           Hello from User B — ${TIMESTAMP}

# Selectors
${CHAT_TEXTAREA}        css=textarea[placeholder*="Type a message"]
${SEND_BUTTON}          css=.ant-btn-primary


*** Test Cases ***
Owner Creates User A And Grants Chat Access
    [Documentation]    Login as owner, create the first staff member (User A), grant chat access.
    [Tags]    chat-bidir    setup
    Login As Owner
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Create User A
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${CHAT_A_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${CHAT_A_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${CHAT_A_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${CHAT_A_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Grant User A chat access
    Navigate To Protected Page    /chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    css=tr:has-text("${CHAT_A_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${CHAT_A_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${CHAT_A_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

Owner Creates User B And Grants Chat Access
    [Documentation]    Still logged in as owner — create User B and grant chat access.
    [Tags]    chat-bidir    setup
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Create User B
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${CHAT_B_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${CHAT_B_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${CHAT_B_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${CHAT_B_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Grant User B chat access
    Navigate To Protected Page    /chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    css=tr:has-text("${CHAT_B_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${CHAT_B_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${CHAT_B_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}
    Logout

User A Logs In And Sends A Message
    [Documentation]    User A logs in, navigates to /chat, and posts a message to the
    ...    shared workspace channel.
    [Tags]    chat-bidir
    Login As    ${CHAT_A_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    Fill Text    ${CHAT_TEXTAREA}    ${MSG_FROM_A}
    Wait For Elements State    ${SEND_BUTTON}    enabled    timeout=5s
    Click    ${SEND_BUTTON}
    Wait For Elements State    text=${MSG_FROM_A}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s
    Logout

User B Logs In And Sees User A's Message
    [Documentation]    User B logs in, opens /chat, and verifies that User A's message
    ...    is visible in the channel with User A's name as sender label.
    [Tags]    chat-bidir
    Login As    ${CHAT_B_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    # Message content from A must be visible
    Wait For Elements State    text=${MSG_FROM_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Sender attribution label shown above non-own messages — scoped to the message
    # content column to avoid a strict-mode collision with the member sidebar entry.
    Wait For Elements State    css=.ant-layout-content >> text=${CHAT_A_FIRST} ${CHAT_A_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

User B Sends A Reply To User A
    [Documentation]    While logged in as User B, send a reply message in the shared channel.
    [Tags]    chat-bidir
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    Fill Text    ${CHAT_TEXTAREA}    ${MSG_FROM_B}
    Wait For Elements State    ${SEND_BUTTON}    enabled    timeout=5s
    Click    ${SEND_BUTTON}
    Wait For Elements State    text=${MSG_FROM_B}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s
    Logout

User A Logs Back In And Sees User B's Reply
    [Documentation]    User A logs back in, opens /chat, and verifies that User B's reply
    ...    is visible in the channel with User B's name as sender label.
    [Tags]    chat-bidir
    Login As    ${CHAT_A_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

    # Own message from earlier should still be visible (right-aligned, no sender label)
    Wait For Elements State    text=${MSG_FROM_A} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # User B's reply must be visible
    Wait For Elements State    text=${MSG_FROM_B} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

    # Sender attribution label — scoped to content column to avoid member sidebar collision.
    Wait For Elements State    css=.ant-layout-content >> text=${CHAT_B_FIRST} ${CHAT_B_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Logout
