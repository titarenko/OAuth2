using System.Configuration;
using TechTalk.SpecFlow;
using WatiN.Core;
using FluentAssertions;

namespace OAuth2.Example.AcceptanceTests.Steps
{
    [Binding]
    public class CommonSteps
    {
        [BeforeScenario]
        public void BeforeScenario()
        {
            Browser.ClearCookies();
        }

        [Given(@"I have opened example application")]
        public void GivenIHaveOpenedExampleApplication()
        {
            Browser.GoTo(BaseUri);
        }

        [When(@"I have clicked ""(.*)"" link")]
        public void WhenIHaveClickedLink(string text)
        {
            Browser.Link(Find.ByText(text)).ClickNoWait();
        }

        [When(@"I have entered ""(.*)"" into ""(.*)"" textbox")]
        public void WhenIHaveEnteredIntoTextbox(string text, string caption)
        {
            var button = caption.StartsWith("id:")
                ? Browser.Element(caption.Substring(3))
                : Browser.Element(Find.ByLabelText(caption));

            button.SetAttributeValue("value", text);
        }

        [When(@"I have clicked ""(.*)"" button")]
        public void WhenIHaveClickedButton(string text)
        {
            var button = text.StartsWith("id:")
                ? Browser.Button(text.Substring(3))
                : Browser.Button(Find.ByText(text));
            
            button.ClickNoWait();
        }

        [When(@"I have clicked ""(.*)"" button in case if I saw it")]
        public void WhenIHaveClickedButtonInCaseIfISawIt(string text)
        {
            var button = text.StartsWith("id:")
                ? Browser.Button(text.Substring(3))
                : Browser.Button(Find.ByText(text));

            if (button.Exists)
            {
                button.ClickNoWait();
            }
        }

        [When(@"I waited for ""(.*)"" text")]
        public void WhenIWaitedForText(string text)
        {
            Browser.WaitUntilContainsText(text);
        }

        [Then(@"I should be on ""(.*)"" page")]
        public void ThenIShouldBeOnPage(string address)
        {
            Browser.Url.Should().Contain(address);
        }

        [Then(@"I should see ""(.*)"" text")]
        public void ThenIShouldSeeText(string text)
        {
            Browser.WaitUntilContainsText(text);
            Browser.ContainsText(text).Should().BeTrue();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            Browser.Dispose();
        }

        private IE Browser
        {
            get
            {
                const string browserKey = "Browser";
                if (!ScenarioContext.Current.ContainsKey(browserKey))
                {
                    ScenarioContext.Current[browserKey] = new IE();
                }
                return (IE) ScenarioContext.Current[browserKey];
            }
        }

        private string BaseUri
        {
            get { return ConfigurationManager.AppSettings["BaseUri"]; }
        }
    }
}