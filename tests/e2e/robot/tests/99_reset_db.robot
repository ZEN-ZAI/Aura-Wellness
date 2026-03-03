*** Settings ***
Documentation       Reset the E2E test database to a known, seeded state.
...                 This suite must run first (00_) so every subsequent suite
...                 starts from a clean baseline.
Resource            ../resources/common.resource
Variables           ../variables.py

Suite Setup         Open Browser Session
Suite Teardown      Close Browser Session


*** Test Cases ***
Reset Database Via Dashboard Button
    [Documentation]    Log in as the seeded owner, click the "Reset DB to Init" button on
    ...    the dashboard, confirm the Modal.confirm dialog, and verify the success message.
    [Tags]    smoke    admin    reset

    # Log in and land on the dashboard
    Login As Owner

    # Wait for the danger "Reset DB to Init" button (visible to Owner only)
    Wait For Elements State    button:has-text("Reset DB to Init")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Reset DB to Init")

    # Ant Design Modal.confirm — click the danger "Yes, reset" confirmation button
    Wait For Elements State    button:has-text("Yes, reset")    visible    timeout=${NAVIGATION_TIMEOUT}
    Click    button:has-text("Yes, reset")

    # Verify the success toast message
    Wait For Elements State    css=.ant-message-success    visible    timeout=${NAVIGATION_TIMEOUT}
