*** Settings ***
Documentation       Staff Management page — list staff, add new staff member.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Run Keywords
...                 Open Browser Session    AND
...                 Login As Owner          AND
...                 Navigate To Protected Page    /staff
Suite Teardown      Close Browser Session


*** Test Cases ***
Staff Table Loads
    [Documentation]    The staff table should render on page load.
    [Tags]    smoke    staff
    Wait For Elements State    ${ANT_TABLE}    visible

Staff Table Shows Column Headers
    [Documentation]    The table should have Name, Email, and Role column headers.
    [Tags]    staff
    Wait For Elements State    ${ANT_TABLE}    visible
    Wait For Elements State    css=.ant-table-thead    visible
    Wait For Elements State    text=Name    visible
    Wait For Elements State    text=Email    visible
    Wait For Elements State    text=Role    visible

Add Staff Member Action Buttons Are Visible For Owner
    [Documentation]    The Owner should see Add Staff Member and Enroll in Another BU buttons.
    [Tags]    staff
    # Navigate fresh ensures React hydration is complete and buttons are interactive
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    button:has-text("Add Staff Member")    visible
    # Use exact button label to avoid strict-mode collision with EnrollExistingForm's "Enroll" OK button
    Wait For Elements State    button:has-text("Enroll in Another BU")    visible    timeout=${NAVIGATION_TIMEOUT}

Add New Staff Member Successfully
    [Documentation]    Fill the Add Staff modal and verify the new member appears in the table.
    [Tags]    smoke    staff
    ${staff_last}=    Set Variable    Robot${TIMESTAMP}
    ${staff_email}=    Set Variable    alice-${TIMESTAMP}@test.com

    # Navigate fresh for a clean page state and confirmed hydration
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    # Retry-click handles the case where React hydration is not yet complete
    Click And Wait For Modal    button:has-text("Add Staff Member")

    Fill Text    ${ANT_MODAL} >> input#firstName    Alice
    Fill Text    ${ANT_MODAL} >> input#lastName     ${staff_last}
    Fill Text    ${ANT_MODAL} >> input#email        ${staff_email}

    # Open BU Select (first select in modal) and pick first available option
    Select Ant Option By Index    ${ANT_MODAL} >> css=.ant-select-content >> nth=0    0

    # Role defaults to Staff — leave unchanged
    Click    ${ANT_MODAL} >> button:has-text("Add Staff")

    # A "Staff Member Created" password-info modal opens after success — dismiss it first
    Wait For Elements State    button:has-text("Got it")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Got it")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=${NAVIGATION_TIMEOUT}
    # Use last name (unique, not in email) to avoid matching the email cell case-insensitively
    Wait For Elements State    ${ANT_TABLE} >> text=${staff_last}    visible    timeout=${NAVIGATION_TIMEOUT}

Add Staff Modal Validates Required Fields
    [Documentation]    Submitting an empty Add Staff modal should show validation errors.
    [Tags]    staff    validation
    Navigate To Protected Page    /staff
    Wait For Elements State    ${ANT_TABLE}    visible    timeout=${NAVIGATION_TIMEOUT}
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden    timeout=5s

    Click And Wait For Modal    button:has-text("Add Staff Member")

    Click    ${ANT_MODAL} >> button:has-text("Add Staff")

    Wait For Elements State    ${ANT_FORM_ERROR} >> nth=0    visible
    Element Count Should Be Greater Than    ${ANT_FORM_ERROR}    1

    Click    ${ANT_MODAL} >> button:has-text("Cancel")
    Wait For Elements State    ${ANT_MODAL_WRAP}    hidden
