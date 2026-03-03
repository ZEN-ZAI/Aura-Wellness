*** Settings ***
Documentation       Company onboarding (registration) flow.
...                 Uses a timestamp-unique email so the test can run repeatedly
...                 without conflicting with the seeded E2E owner account.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open Browser Session
Suite Teardown      Close Browser Session


*** Test Cases ***
Register New Company Successfully
    [Documentation]    Fill the onboard form with valid data and verify success.
    [Tags]    smoke    onboarding
    Go To    ${BASE_URL}/onboard
    Wait For Elements State    input#companyName    visible

    Fill Text    input#companyName             Robot Test Co ${TIMESTAMP}
    Fill Text    input#ownerFirstName          Robot
    Fill Text    input#ownerEmail              robot-${TIMESTAMP}@test.com
    Fill Text    input#ownerPassword           Test@12345
    Fill Text    input#ownerConfirmPassword    Test@12345

    Click    button:has-text("Register Company")

    Wait For Elements State    ${ANT_RESULT_SUCCESS}    visible    timeout=${NAVIGATION_TIMEOUT}
    # The result card title says "Company Registered!" — check the key phrase
    Get Text    ${ANT_RESULT_SUCCESS}    contains    Company Registered

    Click    text=Go to Login
    Wait For URL    /login

Onboard With Empty Form Shows Validation Errors
    [Documentation]    Submitting an empty form should display required-field errors for all fields.
    [Tags]    onboarding    validation
    Go To    ${BASE_URL}/onboard
    Wait For Elements State    button:has-text("Register Company")    visible

    Click    button:has-text("Register Company")

    # Use nth=0 to avoid strict-mode error when multiple error elements appear
    Wait For Elements State    ${ANT_FORM_ERROR} >> nth=0    visible
    # Confirm multiple required-field errors are shown
    Element Count Should Be Greater Than    ${ANT_FORM_ERROR}    1

Onboard With Mismatched Passwords Shows Error
    [Documentation]    Confirm password that doesn't match should show a validation error.
    [Tags]    onboarding    validation
    Go To    ${BASE_URL}/onboard
    Wait For Elements State    input#companyName    visible

    Fill Text    input#companyName             Mismatch Co ${TIMESTAMP}
    Fill Text    input#ownerFirstName          Test
    Fill Text    input#ownerEmail              mismatch-${TIMESTAMP}@test.com
    Fill Text    input#ownerPassword           Password@1
    Fill Text    input#ownerConfirmPassword    DifferentPassword@2

    Click    button:has-text("Register Company")

    Wait For Elements State    ${ANT_FORM_ERROR} >> nth=0    visible
