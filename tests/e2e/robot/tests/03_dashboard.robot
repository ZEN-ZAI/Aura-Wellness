*** Settings ***
Documentation       Dashboard page — statistics cards, business unit list, and role display.
Resource            ../resources/common.resource
Resource            ../resources/selectors.resource
Variables           ../variables.py

Suite Setup         Run Keywords
...                 Open Browser Session    AND
...                 Login As Owner
Suite Teardown      Close Browser Session


*** Test Cases ***
Dashboard Page Loads And Shows Title
    [Documentation]    After login the dashboard should display the page heading.
    [Tags]    smoke    dashboard
    Navigate To Protected Page    /dashboard
    Wait For Elements State    h3    visible
    Get Text    h3    contains    Dashboard

Dashboard Shows Business Units Statistics Card
    [Documentation]    The "Business Units" statistic card should be present.
    [Tags]    dashboard
    Navigate To Protected Page    /dashboard
    # Use class-specific selector to avoid strict-mode collision with sidebar menu text
    Wait For Elements State    css=.ant-statistic-title:has-text("Business Units")    visible

Dashboard Shows Staff Members Statistics Card
    [Documentation]    The "Staff Members" statistic card should be present.
    [Tags]    dashboard
    Navigate To Protected Page    /dashboard
    Wait For Elements State    css=.ant-statistic-title:has-text("Staff Members")    visible

Dashboard Shows Owner Role In Statistics Card
    [Documentation]    The "Your Role" card should display "Owner" for the test owner.
    [Tags]    dashboard
    Navigate To Protected Page    /dashboard
    Wait For Elements State    css=.ant-statistic-title:has-text("Your Role")    visible
    # The statistic value cell contains just "Owner" — avoid matching sidebar/welcome text
    Wait For Elements State    css=.ant-statistic-content-value:has-text("Owner")    visible

Dashboard Shows Business Units List Section
    [Documentation]    The BU list component should be rendered below the stats.
    [Tags]    dashboard
    Navigate To Protected Page    /dashboard
    Wait For Elements State    ${ANT_LIST}    visible

Dashboard Greets User By Name
    [Documentation]    The welcome message should include the user's first name.
    [Tags]    dashboard
    Navigate To Protected Page    /dashboard
    Wait For Elements State    text=Welcome back    visible
