*** Settings ***
Documentation       Change Password page — update password and revert, wrong-password error.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Run Keywords
...                 Open Browser Session    AND
...                 Login As Owner

Suite Teardown      Close Browser Session

*** Variables ***
${NEW_PASSWORD}    NewPass@456E2E


*** Test Cases ***
Change Password Page Loads
    [Documentation]    The change-password page should render with all three form inputs.
    [Tags]    smoke    change-password
    Navigate To Protected Page    /change-password
    Wait For Elements State    input#currentPassword    visible
    Wait For Elements State    input#newPassword        visible
    Wait For Elements State    input#confirmPassword    visible

Wrong Current Password Shows Error Alert
    [Documentation]    Submitting a wrong current password should display an error alert.
    [Tags]    change-password    negative
    Navigate To Protected Page    /change-password
    Wait For Elements State    input#currentPassword    visible

    Fill Text    input#currentPassword     WrongCurrent@99
    Fill Text    input#newPassword         ${NEW_PASSWORD}
    Fill Text    input#confirmPassword     ${NEW_PASSWORD}
    Click    button:has-text("Change Password")

    Wait For Elements State    ${ANT_ALERT_ERROR}    visible    timeout=${NAVIGATION_TIMEOUT}

Change Password With Valid Data Succeeds
    [Documentation]    Submit a valid new password, verify success alert, then revert.
    [Tags]    smoke    change-password
    Navigate To Protected Page    /change-password
    Wait For Elements State    input#currentPassword    visible

    Fill Text    input#currentPassword     ${E2E_OWNER_PASSWORD}
    Fill Text    input#newPassword         ${NEW_PASSWORD}
    Fill Text    input#confirmPassword     ${NEW_PASSWORD}
    Click    button:has-text("Change Password")

    # Success shows an inline Alert — allow up to 30s for the API round-trip
    Wait For Elements State    ${ANT_ALERT_SUCCESS}    visible    timeout=30s

    # Verify new password works by re-logging in with it
    Logout
    Login As    ${E2E_OWNER_EMAIL}    ${NEW_PASSWORD}
    Get Url    contains    /dashboard

    # Revert to original password so future test runs can log in
    Navigate To Protected Page    /change-password
    Wait For Elements State    input#currentPassword    visible
    Fill Text    input#currentPassword     ${NEW_PASSWORD}
    Fill Text    input#newPassword         ${E2E_OWNER_PASSWORD}
    Fill Text    input#confirmPassword     ${E2E_OWNER_PASSWORD}
    Click    button:has-text("Change Password")
    Wait For Elements State    ${ANT_ALERT_SUCCESS}    visible    timeout=30s

Change Password Validates Mismatched Passwords
    [Documentation]    Mismatched new/confirm passwords trigger a client-side error alert.
    [Tags]    change-password    validation
    Navigate To Protected Page    /change-password
    Wait For Elements State    input#currentPassword    visible

    Fill Text    input#currentPassword     ${E2E_OWNER_PASSWORD}
    Fill Text    input#newPassword         ${NEW_PASSWORD}
    Fill Text    input#confirmPassword     DifferentPassword@1
    Click    button:has-text("Change Password")

    # Mismatch is caught in onFinish and shown as an Alert (not a form field error)
    Wait For Elements State    ${ANT_ALERT_ERROR}    visible
