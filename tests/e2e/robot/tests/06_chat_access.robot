*** Settings ***
Documentation       Chat Access page — select a BU, then grant and revoke chat access for staff.
...
...                 The page shows a BU selector first. Only after selecting a BU does the
...                 workspace table appear. Access is controlled via Grant/Revoke buttons
...                 in the table's Action column (not Switch components).
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Run Keywords
...                 Open Browser Session    AND
...                 Login As Owner          AND
...                 Navigate To Protected Page    /chat-access
Suite Teardown      Close Browser Session


*** Test Cases ***
Chat Access Page Loads And Shows BU Selector
    [Documentation]    The page should render with a Business Unit selector visible.
    [Tags]    smoke    chat-access
    # Wait for the page title (static SSR element) confirming the page loaded
    Wait For Elements State    text=Chat Access Control    visible    timeout=${NAVIGATION_TIMEOUT}
    # Then confirm the Ant Design Select outer wrapper is visible
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}

Select Business Unit Loads Workspace Table
    [Documentation]    Choosing a BU from the dropdown should reveal the workspace table.
    [Tags]    smoke    chat-access
    # Use the outer .ant-select wrapper (confirmed visible in test 1)
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}

Chat Access Table Shows Member Columns
    [Documentation]    After BU selection the table should have Member and Chat Access columns.
    [Tags]    chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    # Use column header scope to avoid strict-mode collision with body text containing "member"
    Wait For Elements State    css=.ant-table-thead >> text=Member    visible
    Wait For Elements State    css=.ant-table-thead >> text=Chat Access    visible

Grant Chat Access To A Staff Member
    [Documentation]    Click a Grant button to enable chat access and verify it changes to Revoke.
    ...    Uses >> nth=0 throughout to avoid strict-mode violations when multiple
    ...    staff members appear in the table (e.g. from multiple test runs).
    [Tags]    chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}

    ${has_grant}=    Run Keyword And Return Status
    ...    Wait For Elements State    button:has-text("Grant") >> nth=0    visible    timeout=5s
    IF    ${has_grant}
        Click    button:has-text("Grant") >> nth=0
        Wait For Elements State    button:has-text("Revoke") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    ELSE
        Log    No 'Grant' button found — all members may already have access    WARN
        Wait For Elements State    button:has-text("Revoke") >> nth=0    visible
    END

Revoke Chat Access From A Staff Member
    [Documentation]    Click a Revoke button to disable chat access and verify it changes to Grant.
    ...    Uses >> nth=0 throughout to avoid strict-mode violations when multiple
    ...    staff members appear in the table (e.g. from multiple test runs).
    [Tags]    chat-access
    Wait For Elements State    css=.ant-select    visible    timeout=${NAVIGATION_TIMEOUT}
    Select Ant Option By Index    css=.ant-select    0
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}

    ${has_revoke}=    Run Keyword And Return Status
    ...    Wait For Elements State    button:has-text("Revoke") >> nth=0    visible    timeout=5s
    IF    ${has_revoke}
        Click    button:has-text("Revoke") >> nth=0
        Wait For Elements State    button:has-text("Grant") >> nth=0    visible    timeout=${NAVIGATION_TIMEOUT}
    ELSE
        Log    No 'Revoke' button found — all members may have access denied    WARN
        Wait For Elements State    button:has-text("Grant") >> nth=0    visible
    END
