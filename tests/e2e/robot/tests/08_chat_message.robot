*** Settings ***
Documentation       Chat message flow — sequential end-to-end scenario:
...                 1. Owner adds a new staff member
...                 2. Owner grants that member chat access
...                 3. Staff member logs in and navigates to /chat
...                 4. Staff member sends messages and verifies they appear
...
...                 Tests run in order; browser session and cookies carry over between cases.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open Browser Session
Suite Teardown      Close Browser Session


*** Variables ***
${STAFF_FIRST}               ChatStaff
${STAFF_LAST}                Chat${TIMESTAMP}
${STAFF_EMAIL}               chat-staff-${TIMESTAMP}@test.com
${STAFF_DEFAULT_PASSWORD}    P@ssw0rd
${CHAT_TEXTAREA}             css=textarea[placeholder*="Type a message"]
${SEND_BUTTON}               css=.ant-btn-primary


*** Test Cases ***
Owner Adds New Staff Member
    [Documentation]    Login as owner, open the Add Staff modal, create a new staff member,
    ...    and confirm the member appears in the staff table.
    [Tags]    chat    setup
    Login As Owner
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s
    Click And Wait For Modal    button:has-text("Add Staff Member")
    Fill Text    ${ANT_MODAL} >> input#firstName    ${STAFF_FIRST}
    Fill Text    ${ANT_MODAL} >> input#lastName     ${STAFF_LAST}
    Fill Text    ${ANT_MODAL} >> input#email        ${STAFF_EMAIL}
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_TABLE} >> text=${STAFF_LAST} >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}

Owner Grants Chat Access To Staff Member
    [Documentation]    Still logged in as owner — navigate to /chat-access, select the first BU,
    ...    find the new member's row by last name, and click Grant Access.
    [Tags]    chat    setup
    Navigate To Protected Page    /chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    css=tr:has-text("${STAFF_LAST}") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    css=tr:has-text("${STAFF_LAST}") >> nth=0 >> button:has-text("Grant Access")
    Wait For Elements State
    ...    css=tr:has-text("${STAFF_LAST}") >> nth=0 >> button:has-text("Revoke Access")
    ...    visible    timeout=${NAVIGATION_TIMEOUT}

Staff Member Logs In And Opens Chat
    [Documentation]    Owner logs out; new staff member logs in with the default password
    ...    and navigates to /chat.
    [Tags]    chat    setup
    Logout
    Login As    ${STAFF_EMAIL}    ${STAFF_DEFAULT_PASSWORD}
    Navigate To Protected Page    /chat

Chat Page Loads For Staff Member
    [Documentation]    Staff are scoped to their own BU — no BU selector is shown; the
    ...    member sidebar header ("Members") and the message textarea are immediately visible.
    [Tags]    smoke    chat
    Wait For Elements State    css=.ant-layout-content    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=Members    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}

Send Button Is Disabled When Input Is Empty
    [Documentation]    The send button must be disabled while the textarea is empty.
    [Tags]    chat
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    Fill Text    ${CHAT_TEXTAREA}    ${EMPTY}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s

Send A Chat Message Via Button
    [Documentation]    Type a message, click the send button, verify the message appears
    ...    in the list and the textarea is cleared (button disabled again).
    [Tags]    chat
    ${msg}=    Set Variable    Hello from E2E ${TIMESTAMP}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    Fill Text    ${CHAT_TEXTAREA}    ${msg}
    Wait For Elements State    ${SEND_BUTTON}    enabled    timeout=5s
    Click    ${SEND_BUTTON}
    Wait For Elements State    text=${msg}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s

Send A Chat Message Via Enter Key
    [Documentation]    Pressing Enter (without Shift) in the focused textarea sends the message.
    [Tags]    chat
    ${msg}=    Set Variable    Enter-key test ${TIMESTAMP}
    Wait For Elements State    ${CHAT_TEXTAREA}    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    ${CHAT_TEXTAREA}
    Fill Text    ${CHAT_TEXTAREA}    ${msg}
    Keyboard Key    press    Enter
    Wait For Elements State    text=${msg}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${SEND_BUTTON}    disabled    timeout=5s
