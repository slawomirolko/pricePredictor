using ErrorOr;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using Network = OpenQA.Selenium.DevTools.V131.Network;
using Page = OpenQA.Selenium.DevTools.V131.Page;

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

        // options.AddArgument("--headless=new");
        options.AddArgument("--start-minimized");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--lang=en-US");
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");

        // Use Eager strategy so navigation completes as soon as the DOM is interactive,
        // without waiting for all async scripts/trackers to finish loading.
        // This prevents WebDriverTimeoutException on heavy news sites that never reach readyState=complete.
        options.PageLoadStrategy = PageLoadStrategy.Eager;

        return options;
    }
}

internal sealed class SeleniumFlowBuilder : ISeleniumFlowBuilder
{
    private const int PageLoadTimeoutSeconds = 60;

    private readonly ChromeDriver _driver;

    public SeleniumFlowBuilder(ChromeOptions options)
    {
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(PageLoadTimeoutSeconds);
    }

    public string CurrentUrl => _driver.Url;
    public string Title => _driver.Title;

    public async Task<ErrorOr<bool>> OpenAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            await ApplyStealthAndHeadersAsync();
            // Task.Run's cancellationToken only prevents starting; it does not interrupt a running GoToUrl.
            // GoToUrl is synchronous and will block until PageLoad timeout or renderer timeout.
            // WebDriverTimeoutException is treated as a partial-load (page may still be usable), not a hard failure.
            await Task.Run(() => _driver.Navigate().GoToUrl(url), cancellationToken);
            return true;
        }
        catch (WebDriverTimeoutException ex)
        {
            // The page did not reach readyState within the timeout but may be partially loaded.
            // Return a soft error so callers can decide whether to continue scraping.
            return Error.Unexpected(
                code: "Selenium.PageLoadTimeout",
                description: $"Page load timed out for URL '{url}' after {PageLoadTimeoutSeconds}s: {ex.Message}");
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
                || _driver.PageSource.Contains("Automated (bot) activity", StringComparison.OrdinalIgnoreCase)
                || _driver.PageSource.Contains("Rapid taps or clicks", StringComparison.OrdinalIgnoreCase)
                || _driver.PageSource.Contains("JavaScript disabled", StringComparison.OrdinalIgnoreCase)
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

            // Human-like delay before clicking
            var randomDelayMs = new Random().Next(500, 2000);
            await Task.Delay(randomDelayMs, cancellationToken);

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

    private async Task ApplyStealthAndHeadersAsync()
    {
        try
        {
            IDevTools devTools = _driver;
            var session = devTools.GetDevToolsSession();

            // Set extra headers
            var headers = new Network.Headers();
            headers.Add("X-Requested-With", "XMLHttpRequest");
            headers.Add("Accept-Language", "en-US,en;q=0.9");
            headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"8\", \"Chromium\";v=\"131\", \"Google Chrome\";v=\"131\"");
            headers.Add("sec-ch-ua-mobile", "?0");
            headers.Add("sec-ch-ua-platform", "\"Windows\"");

            await session.SendCommand(new Network.SetExtraHTTPHeadersCommandSettings
            {
                Headers = headers
            });

            // Inject script to be evaluated on new document
            await session.SendCommand(new Page.AddScriptToEvaluateOnNewDocumentCommandSettings
            {
                Source = @"
                    // Hide navigator.webdriver
                    Object.defineProperty(navigator, 'webdriver', {get: () => undefined});

                    // Spoof WebGL parameters
                    const getParameter = WebGLRenderingContext.prototype.getParameter;
                    WebGLRenderingContext.prototype.getParameter = function(parameter) {
                        if (parameter === 37445) return 'Intel Open Source Technology Center';
                        if (parameter === 37446) return 'Mesa DRI Intel(R) HD Graphics 520 (Skylake GT2)';
                        return getParameter(parameter);
                    };

                    // Spoof languages
                    Object.defineProperty(navigator, 'languages', {get: () => ['en-US', 'en']});

                    // Spoof plugins
                    Object.defineProperty(navigator, 'plugins', {get: () => [1, 2, 3, 4, 5]});

                    // Spoof permissions
                    const originalQuery = window.navigator.permissions.query;
                    window.navigator.permissions.query = (parameters) => (
                        parameters.name === 'notifications' ?
                            Promise.resolve({ state: Notification.permission }) :
                            originalQuery(parameters)
                    );

                    // Spoof chrome object
                    window.chrome = {
                        runtime: {},
                        loadTimes: function() {},
                        csi: function() {},
                        app: {}
                    };
                "
            });
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
        // PageLoadTimeout means the page didn't fully load but may be partially usable — continue.
        // Any other error (e.g. net::ERR_CONNECTION_REFUSED) is a hard failure.
        if (openResult.IsError && openResult.FirstError.Code != "Selenium.PageLoadTimeout")
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
