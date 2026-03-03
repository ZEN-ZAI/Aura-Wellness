*** Settings ***
Documentation       Authentication flow: login, logout, and redirect guard.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Open Browser Session
Suite Teardown      Close Browser Session
# Clear auth cookies before each test so every test starts unauthenticated
Test Setup          Clear Auth Cookies


*** Test Cases ***
Login With Valid Credentials Redirects To Dashboard
    [Documentation]    Successful login should redirect to /dashboard.
    [Tags]    smoke    auth
    Login As Owner
    Wait For Elements State    h3    visible
    Get Text    h3    contains    Dashboard

Login With Wrong Password Shows Error Alert
    [Documentation]    Invalid password should display an error alert, not redirect.
    [Tags]    auth    negative
    Go To    ${BASE_URL}/login
    Wait For Elements State    input#email    visible

    Fill Text    input#email      ${E2E_OWNER_EMAIL}
    Fill Text    input#password    WrongPassword@99
    Click    button:has-text("Sign In")

    Wait For Elements State    ${ANT_ALERT_ERROR}    visible
    Get Text    ${ANT_ALERT_ERROR}    contains    Invalid

Login With Unknown Email Shows Error Alert
    [Documentation]    Non-existent email should display an error alert.
    [Tags]    auth    negative
    Go To    ${BASE_URL}/login
    Wait For Elements State    input#email    visible

    Fill Text    input#email      nobody-${TIMESTAMP}@unknown.com
    Fill Text    input#password    SomePassword@1
    Click    button:has-text("Sign In")

    Wait For Elements State    ${ANT_ALERT_ERROR}    visible

Logout Clears Session And Redirects To Login
    [Documentation]    Clicking Logout should clear the session and go to /login.
    [Tags]    smoke    auth
    # Log in first, then log out
    Login As Owner
    Logout
    Get Url    contains    /login

Unauthenticated Access To Protected Page Redirects To Login
    [Documentation]    Accessing /dashboard without a session should redirect to /login.
    [Tags]    auth    security
    # Navigate directly — cookies are already cleared by Test Setup
    Go To    ${BASE_URL}/dashboard
    Wait For URL    /login
