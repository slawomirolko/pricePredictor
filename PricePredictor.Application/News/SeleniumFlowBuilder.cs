using ErrorOr;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace PricePredictor.Application.News;

public interface ISeleniumFlowBuilderFactory
{
    ISeleniumFlowBuilder Create();
}

public interface ISeleniumFlowBuilder : IDisposable
{
    string CurrentUrl { get; }
    string Title { get; }
    Task<ErrorOr<bool>> OpenAsync(string url, CancellationToken cancellationToken);
    Task<ErrorOr<bool>> NavigateToReadyPageAsync(string url, CancellationToken cancellationToken);
    Task<ErrorOr<bool>> RefreshAsync(CancellationToken cancellationToken);
    Task<ErrorOr<bool>> WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
    ErrorOr<bool> IsBlockedPage();
    Task<ErrorOr<bool>> AcceptConsentAsync(CancellationToken cancellationToken);
    ErrorOr<string> GetHtml();
    ErrorOr<string?> GetBodyText();
    ErrorOr<IReadOnlyList<IWebElement>> GetElements(By by);
}

internal sealed class SeleniumFlowBuilderFactory : ISeleniumFlowBuilderFactory
{
    public ISeleniumFlowBuilder Create()
    {
        var options = CreateOptions();
        return new SeleniumFlowBuilder(options);
    }

    private static ChromeOptions CreateOptions()
    {
        var options = new ChromeOptions();
        // Chromium must stay visible during runs.

        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--lang=en-US");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalChromeOption("useAutomationExtension", false);
        return options;
    }
}

internal sealed class SeleniumFlowBuilder : ISeleniumFlowBuilder
{
    private readonly IWebDriver _driver;

    public SeleniumFlowBuilder(ChromeOptions options)
    {
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
    }

    public string CurrentUrl => _driver.Url;
    public string Title => _driver.Title;

    public async Task<ErrorOr<bool>> OpenAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => _driver.Navigate().GoToUrl(url), cancellationToken);
            ApplyStealthScripts();
            return true;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.OpenFailed",
                description: $"Open failed for URL '{url}': {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> NavigateToReadyPageAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var targetUri))
        {
            return Error.Validation(
                code: "Selenium.InvalidUrl",
                description: $"Invalid navigation URL '{url}'.");
        }

        var firstAttemptResult = await NavigateCoreAsync(targetUri, cancellationToken);
        if (!firstAttemptResult.IsError)
        {
            return true;
        }

        var retryAttemptResult = await NavigateCoreAsync(targetUri, cancellationToken);
        if (retryAttemptResult.IsError)
        {
            return retryAttemptResult.FirstError;
        }

        return true;
    }

    public async Task<ErrorOr<bool>> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => _driver.Navigate().Refresh(), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.RefreshFailed",
                description: $"Refresh failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.WaitFailed",
                description: $"Wait failed: {ex.Message}");
        }
    }

    public ErrorOr<bool> IsBlockedPage()
    {
        try
        {
            var isBlocked = _driver.PageSource.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                || _driver.PageSource.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase)
                || _driver.PageSource.Contains("Please verify you are a human", StringComparison.OrdinalIgnoreCase)
                || _driver.Title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                || _driver.Title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase);
            return isBlocked;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.BlockCheckFailed",
                description: $"Blocked page check failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> AcceptConsentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var buttons = _driver.FindElements(By.TagName("button"));
            var acceptButton = buttons.FirstOrDefault(ShouldAcceptButton);
            if (acceptButton?.Displayed != true)
            {
                return false;
            }

            acceptButton.Click();
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.ConsentFailed",
                description: $"Consent handling failed: {ex.Message}");
        }
    }

    public ErrorOr<string> GetHtml()
    {
        try
        {
            var html = ((IJavaScriptExecutor)_driver).ExecuteScript("return document.documentElement.outerHTML;") as string
                       ?? _driver.PageSource;
            return html;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.GetHtmlFailed",
                description: $"GetHtml failed: {ex.Message}");
        }
    }

    public ErrorOr<string?> GetBodyText()
    {
        try
        {
            return _driver.FindElement(By.TagName("body")).Text;
        }
        catch
        {
            return (string?)null;
        }
    }

    public ErrorOr<IReadOnlyList<IWebElement>> GetElements(By by)
    {
        try
        {
            return _driver.FindElements(by).ToList();
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Selenium.GetElementsFailed",
                description: $"GetElements failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _driver.Quit();
        }
        catch
        {
            // Ignore shutdown issues.
        }

        _driver.Dispose();
    }

    private void ApplyStealthScripts()
    {
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) return 'Intel Open Source Technology Center';
                    if (parameter === 37446) return 'Mesa DRI Intel(R) HD Graphics 520 (Skylake GT2)';
                    return getParameter(parameter);
                };
            ");
        }
        catch
        {
            // Best effort.
        }
    }

    private static bool ShouldAcceptButton(IWebElement button)
    {
        try
        {
            var text = button.Text;
            return text.Contains("Accept", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Agree", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Allow All", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Zaakceptuj", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Akceptuję", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Approve", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Zgadzam się", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Got it", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<ErrorOr<bool>> NavigateCoreAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        var openResult = await OpenAsync(targetUri.AbsoluteUri, cancellationToken);
        if (openResult.IsError)
        {
            return openResult.FirstError;
        }

        var waitAfterOpenResult = await WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        if (waitAfterOpenResult.IsError)
        {
            return waitAfterOpenResult.FirstError;
        }

        var blockedResult = IsBlockedPage();
        if (!blockedResult.IsError && blockedResult.Value)
        {
            var refreshResult = await RefreshAsync(cancellationToken);
            if (refreshResult.IsError)
            {
                return refreshResult.FirstError;
            }

            var waitAfterRefreshResult = await WaitAsync(TimeSpan.FromSeconds(8), cancellationToken);
            if (waitAfterRefreshResult.IsError)
            {
                return waitAfterRefreshResult.FirstError;
            }
        }

        var consentResult = await AcceptConsentAsync(cancellationToken);
        if (consentResult.IsError)
        {
            return consentResult.FirstError;
        }

        var waitAfterConsentResult = await WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        if (waitAfterConsentResult.IsError)
        {
            return waitAfterConsentResult.FirstError;
        }

        // Cookie banners can redirect to the home page, so verify article URL right after consent.
        var locationCheckResult = EnsureExpectedLocation(targetUri);
        if (locationCheckResult.IsError)
        {
            return locationCheckResult.FirstError;
        }

        return true;
    }

    private ErrorOr<bool> EnsureExpectedLocation(Uri targetUri)
    {
        if (!Uri.TryCreate(CurrentUrl, UriKind.Absolute, out var currentUri))
        {
            return Error.Unexpected(
                code: "Selenium.NavigationMismatch",
                description: $"Navigation failed. Expected '{targetUri.AbsoluteUri}', got '{CurrentUrl}'.");
        }

        if (!string.Equals(currentUri.Host, targetUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Unexpected(
                code: "Selenium.NavigationMismatch",
                description: $"Navigation host mismatch. Expected '{targetUri.Host}', got '{currentUri.Host}'. CurrentUrl='{currentUri.AbsoluteUri}'.");
        }

        if (!string.Equals(targetUri.AbsolutePath, "/", StringComparison.Ordinal)
            && string.Equals(currentUri.AbsolutePath, "/", StringComparison.Ordinal))
        {
            return Error.Unexpected(
                code: "Selenium.NavigationMismatch",
                description: $"Navigation ended on homepage instead of article path. ExpectedPath='{targetUri.AbsolutePath}', CurrentUrl='{currentUri.AbsoluteUri}'.");
        }

        return true;
    }
}
