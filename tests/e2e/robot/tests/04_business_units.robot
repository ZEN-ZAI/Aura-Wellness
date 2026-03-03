*** Settings ***
Documentation       Business Units page — list and create business units.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Run Keywords
...                 Open Browser Session    AND
...                 Login As Owner          AND
...                 Navigate To Protected Page    /business-units
Suite Teardown      Close Browser Session


*** Test Cases ***
Business Units Table Loads
    [Documentation]    The BU table should render on page load.
    [Tags]    smoke    business-units
    Wait For Elements State    ${ANT_TABLE}    visible

Business Units Table Has Name Column Header
    [Documentation]    The table should have at minimum a "Name" column header.
    [Tags]    business-units
    Wait For Elements State    text=Name    visible

Create New Business Unit
    [Documentation]    Open the modal, enter a BU name, submit, and verify it appears in the table.
    [Tags]    smoke    business-units
    ${bu_name}=    Set Variable    QA Team ${TIMESTAMP}

    # Navigate fresh to guarantee clean state and full React hydration
    Navigate To Protected Page    /business-units
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Retry-click handles the case where React hydration is not yet complete
    Click And Wait For Modal    button:has-text("New Business Unit")

    Fill Text    ${ANT_MODAL} >> input    ${bu_name}
    Click    ${ANT_MODAL} >> button:has-text("Create")

    # Wait for both modal wrap AND content to be fully hidden (Ant Design animation)
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    text=${bu_name}    visible    timeout=${NAVIGATION_TIMEOUT}

Create Business Unit With Empty Name Is Disabled
    [Documentation]    The Create button should be disabled when the name input is empty.
    [Tags]    business-units    validation
    # Navigate fresh to guarantee clean state
    Navigate To Protected Page    /business-units
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    Click And Wait For Modal    button:has-text("New Business Unit")

    Fill Text    ${ANT_MODAL} >> input    ${EMPTY}
    ${states}=    Get Element States    ${ANT_MODAL} >> button:has-text("Create")
    Should Contain    ${states}    disabled

    Click    ${ANT_MODAL} >> button:has-text("Cancel")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden
