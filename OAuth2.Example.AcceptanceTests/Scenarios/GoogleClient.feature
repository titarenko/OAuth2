Feature: Google Client

Scenario Outline: Authentication
	Given I have opened example application
	When I have clicked "Log In (Google)" link
	And I have entered "<Email>" into "id:Email" textbox
	And I have entered "<Password>" into "id:Passwd" textbox
	And I have clicked "id:signIn" button
	And I have clicked "id:save" button in case if I saw it
	And I have clicked "id:submit_approve_access" button in case if I saw it
	And I waited for "OAuth2.Example" text
	Then I should be on "/Auth" page
	And I should see "<First Name> <Last Name>" text
	And I should see "<Email>" text

Examples: 
| Email | Password | First Name | Last Name |
| 1     | 2        | 3          | 4         |