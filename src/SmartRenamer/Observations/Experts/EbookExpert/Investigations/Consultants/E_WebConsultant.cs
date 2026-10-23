using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// Bounded web-discovery consultant for missing ebook descriptions.
    /// Returns sourced candidates only; it never approves repairs or edits EPUBs.
    /// </summary>
    internal sealed class E_WebConsultant
    {
        private const int MaxSearchResults = 5;
        private const int MaxPageCharacters = 1_500_000;
        private const int MaxRedirects = 5;
        private static readonly HttpClient PageClient = CreateClient(allowRedirects: true);
        // Do not let HttpClient hide search redirects: log each hop so DNS failures identify the actual host.
        private static readonly HttpClient SearchClient = CreateClient(allowRedirects: false);

        private static readonly Regex DuckDuckGoResultLinkRegex = new(
            "<a\\b(?=[^>]*\\bclass\\s*=\\s*[\"'][^\"']*\\bresult__a\\b[^\"']*[\"'])[^>]*\\bhref\\s*=\\s*[\"'](?<url>[^\"']+)[\"'][^>]*>(?<title>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex BingResultLinkRegex = new(
            "<h2\\b[^>]*>\\s*<a\\b[^>]*\\bhref\\s*=\\s*[\"'](?<url>[^\"']+)[\"'][^>]*>(?<title>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex MetaRegex = new(
            "<meta\\b(?=[^>]*(?:name|property)=['\\\"](?<key>description|og:description|twitter:description)['\\\"])[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ContentRegex = new(
            "content=['\\\"](?<value>.*?)[ '\\\"](?:\\s|/?>)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // Backward-compatible entry point for callers that only have title and author.
        public List<MetadataResearchCandidate> ResearchDescription(
            string title, string author, CancellationToken cancellationToken = default) =>
            ResearchDescription(title, author, string.Empty, string.Empty, string.Empty, cancellationToken);

        // Evidence-rich entry point used by E_MetadataResearchResource. Keeping this
        // overload here fixes the caller/consultant contract without discarding publisher,
        // ISBN, or opening-content evidence.
        public List<MetadataResearchCandidate> ResearchDescription(
            string title, string author, string publisher, string isbn, string openingContent,
            CancellationToken cancellationToken = default)
        {
            List<MetadataResearchCandidate> candidates = new();
            if (string.IsNullOrWhiteSpace(title)) return candidates;

            string cleanTitle = NormalizeResearchTitle(CleanText(title));
            string cleanAuthor = CleanText(author ?? string.Empty);
            string firstAuthor = cleanAuthor.Split(new[] { ';', '|', '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => CleanText(part))
                .FirstOrDefault(part => part.Length > 0) ?? cleanAuthor;
            string cleanPublisher = CleanText(publisher ?? string.Empty);
            string cleanIsbn = Regex.Replace(isbn ?? string.Empty, "[^0-9Xx]", string.Empty);
            (string StoryTitle, string StoryAuthors) story = ExtractStoryClue(openingContent ?? string.Empty);

            // Every general inquiry preserves the core identity pair: normalized book title
            // AND author. ISBN, publisher, official-domain filters, and opening-content clues
            // refine that pair; none replaces it. This prevents common-title collisions such
            // as Chrome Circle -> Google Chrome and keeps each of the four inquiries useful.
            string goal = "(synopsis OR blurb OR description)";
            List<string> primaryOne = new() { $"\"{cleanTitle}\"" };
            if (!string.IsNullOrWhiteSpace(firstAuthor)) primaryOne.Add($"\"{firstAuthor}\"");
            if (!string.IsNullOrWhiteSpace(cleanPublisher)) primaryOne.Add($"\"{cleanPublisher}\"");
            if (cleanPublisher.Contains("Baen", StringComparison.OrdinalIgnoreCase))
                primaryOne.Add("site:baen.com");
            else if (cleanAuthor.Contains("Mercedes Lackey", StringComparison.OrdinalIgnoreCase))
                primaryOne.Add("site:mercedeslackey.com");
            primaryOne.Add(goal);
            string primaryQueryOne = string.Join(" ", primaryOne);

            // Keep author paired with title even when an ISBN is available; the ISBN is
            // additional edition evidence, not a replacement for work identity.
            List<string> primaryTwo = new() { $"\"{cleanTitle}\"" };
            if (!string.IsNullOrWhiteSpace(firstAuthor)) primaryTwo.Add($"\"{firstAuthor}\"");
            if (!string.IsNullOrWhiteSpace(cleanIsbn)) primaryTwo.Add($"\"{cleanIsbn}\"");
            else if (!string.IsNullOrWhiteSpace(cleanPublisher)) primaryTwo.Add($"\"{cleanPublisher}\"");
            primaryTwo.Add("book plot summary");
            string primaryQueryTwo = string.Join(" ", primaryTwo);

            // Story clues are additive evidence only. They never replace the book identity.
            bool usableStoryClue = IsUsableStoryClue(story.StoryTitle, story.StoryAuthors);
            List<string> backupOne = new() { $"\"{cleanTitle}\"" };
            if (!string.IsNullOrWhiteSpace(firstAuthor)) backupOne.Add($"\"{firstAuthor}\"");
            if (!string.IsNullOrWhiteSpace(cleanIsbn)) backupOne.Add($"\"{cleanIsbn}\"");
            else if (!string.IsNullOrWhiteSpace(cleanPublisher)) backupOne.Add($"\"{cleanPublisher}\"");
            if (usableStoryClue) backupOne.Add($"\"{story.StoryTitle}\"");
            backupOne.Add("book synopsis");
            string backupQueryOne = string.Join(" ", backupOne);

            // Final inquiry changes the source angle but still includes title + author.
            List<string> backupTwo = new() { $"\"{cleanTitle}\"" };
            if (!string.IsNullOrWhiteSpace(firstAuthor)) backupTwo.Add($"\"{firstAuthor}\"");
            if (!string.IsNullOrWhiteSpace(cleanPublisher)) backupTwo.Add($"\"{cleanPublisher}\"");
            if (usableStoryClue && !string.IsNullOrWhiteSpace(story.StoryAuthors))
                backupTwo.Add($"\"{story.StoryAuthors}\"");
            backupTwo.Add("official book description plot");
            string backupQueryTwo = string.Join(" ", backupTwo);

            var inquiries = new (string Purpose, string Engine, string Query, Func<string, string> BuildUrl, Regex Parser)[]
            {
                ("primary-1", "DuckDuckGo HTML", primaryQueryOne,
                    q => "https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString(q), DuckDuckGoResultLinkRegex),
                ("primary-2", "DuckDuckGo HTML", primaryQueryTwo,
                    q => "https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString(q), DuckDuckGoResultLinkRegex),
                ("backup-1", "Bing", backupQueryOne,
                    q => "https://www.bing.com/search?q=" + Uri.EscapeDataString(q), BingResultLinkRegex),
                ("backup-2", "Bing", backupQueryTwo,
                    q => "https://www.bing.com/search?q=" + Uri.EscapeDataString(q), BingResultLinkRegex)
            };

            Debug.WriteLine($"[WEB CONSULTANT TRACE] Research goal=Description | Title='{cleanTitle}' | Author='{cleanAuthor}' | Publisher='{cleanPublisher}' | ISBNPresent={!string.IsNullOrWhiteSpace(cleanIsbn)} | StoryTitle='{(IsUsableStoryClue(story.StoryTitle, story.StoryAuthors) ? story.StoryTitle : string.Empty)}' | StoryAuthors='{(IsUsableStoryClue(story.StoryTitle, story.StoryAuthors) ? story.StoryAuthors : string.Empty)}' | InquiryCount={inquiries.Length} | MaxInquiryCount=4");

            // Direct official-source lookup is deliberately attempted before general search.
            // It is bounded to at most two predictable URLs for publishers/authors with known
            // catalog URL conventions, and each page must pass identity checks before use.
            // This route does not evade a challenge: it contacts the official page directly.
            foreach (Uri directUri in BuildDirectOfficialSourceUris(cleanTitle, cleanAuthor, cleanPublisher))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using HttpResponseMessage response = PageClient.GetAsync(directUri, cancellationToken).GetAwaiter().GetResult();
                    string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? directUri.ToString();
                    if (!response.IsSuccessStatusCode ||
                        (response.Content.Headers.ContentType?.MediaType is string mediaType &&
                         !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)))
                    {
                        string directFailureState = (int)response.StatusCode == 429 ? "RateLimited" : "PageUnavailable";
                        Debug.WriteLine($"[WEB CONSULTANT STATUS] State={directFailureState} | Route=DirectOfficialSource | URL={directUri} | Status={(int)response.StatusCode}");
                        continue;
                    }

                    // Keep direct-source discovery on the expected official domain after redirects.
                    if (!Uri.TryCreate(finalUrl, UriKind.Absolute, out Uri? finalUri) ||
                        !IsSameOfficialHostFamily(finalUri.Host, directUri.Host))
                    {
                        Debug.WriteLine($"[WEB CONSULTANT DIRECT] Rejected unexpected redirect | From={directUri} | To={finalUrl}");
                        continue;
                    }

                    string html = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                    if (html.Length > MaxPageCharacters) html = html.Substring(0, MaxPageCharacters);
                    string visibleText = CleanHtml(html);
                    bool titleMatch = ContainsPhrase(visibleText, cleanTitle);
                    bool authorMatch = !string.IsNullOrWhiteSpace(cleanAuthor) && ContainsPhrase(visibleText, cleanAuthor);
                    bool publisherMatch = !string.IsNullOrWhiteSpace(cleanPublisher) && ContainsPhrase(visibleText, cleanPublisher);
                    bool isbnMatch = !string.IsNullOrWhiteSpace(cleanIsbn) &&
                        ContainsPhrase(Regex.Replace(visibleText, "[^0-9Xx]", string.Empty), cleanIsbn);
                    if (!titleMatch || !(authorMatch || publisherMatch || isbnMatch))
                    {
                        Debug.WriteLine($"[WEB CONSULTANT DIRECT] Rejected identity mismatch | TitleMatch={titleMatch} | AuthorMatch={authorMatch} | PublisherMatch={publisherMatch} | ISBNMatch={isbnMatch} | URL={finalUrl}");
                        continue;
                    }

                    string description = ExtractDescription(html);
                    if (!IsSubstantiveDescription(description))
                    {
                        Debug.WriteLine($"[WEB CONSULTANT STATUS] State=DescriptionTooShortOrMissing | Route=DirectOfficialSource | URL={finalUrl}");
                        continue;
                    }

                    MetadataResearchCandidate directCandidate = new()
                    {
                        Field = "Description",
                        Value = description,
                        Source = "Official Direct Source",
                        Evidence = $"Official publisher/author page directly fetched and identity-checked: {finalUrl}.",
                        // Keep description replacements in the review band: the user explicitly reviews proposed prose.
                        Confidence = isbnMatch ? 0.89 : (authorMatch && publisherMatch ? 0.88 : (authorMatch || publisherMatch ? 0.86 : 0.82))
                    };
                    directCandidate.Details.Add("Discovery route: direct official-source lookup; no search-engine result was required.");
                    directCandidate.Details.Add($"Source URL: {finalUrl}");
                    directCandidate.Details.Add($"Identity checks: title={titleMatch}, author={authorMatch}, publisher={publisherMatch}, ISBN={isbnMatch}.");
                    directCandidate.Details.Add("The candidate remains subject to EbookExpert's existing decision and authorization rules; no repair was applied by the consultant.");
                    candidates.Add(directCandidate);
                    Debug.WriteLine($"[WEB CONSULTANT DIRECT] Candidate accepted | Title='{cleanTitle}' | URL='{finalUrl}' | Characters={description.Length} | Confidence={directCandidate.Confidence:P0}");
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException)
                {
                    Debug.WriteLine($"[WEB CONSULTANT STATUS] State=PageUnavailable | Route=DirectOfficialSource | URL={directUri} | {ex.Message}");
                }
            }

            // A verified official description satisfies the research goal. Do not spend the
            // four general-search inquiries after a substantive direct-source result exists.
            if (candidates.Count > 0) return candidates;

            foreach (var inquiry in inquiries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Debug.WriteLine($"[WEB CONSULTANT TRACE] Inquiry start | Purpose={inquiry.Purpose} | Engine={inquiry.Engine} | Query='{inquiry.Query}'");
                string searchHtml;
                try
                {
                    searchHtml = GetSearchHtml(new Uri(inquiry.BuildUrl(inquiry.Query)), inquiry.Engine, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException)
                {
                    Debug.WriteLine($"[WEB CONSULTANT TRACE] Inquiry failed | Purpose={inquiry.Purpose} | Engine={inquiry.Engine} | {ex.Message}");
                    continue;
                }

                bool challenge = IsChallengeResponse(searchHtml);
                MatchCollection resultMatches = inquiry.Parser.Matches(searchHtml);
                Debug.WriteLine($"[WEB CONSULTANT TRACE] Inquiry response | Purpose={inquiry.Purpose} | Engine={inquiry.Engine} | HtmlCharacters={searchHtml.Length} | ParsedLinks={resultMatches.Count} | Challenge={challenge}");
                if (challenge || resultMatches.Count == 0)
                {
                    string state = challenge ? "Challenge" : "NoMatch";
                    Debug.WriteLine($"[WEB CONSULTANT STATUS] State={state} | Purpose={inquiry.Purpose} | Engine={inquiry.Engine} | ParsedLinks={resultMatches.Count}");
                    Debug.WriteLine($"[WEB CONSULTANT TRACE] Inquiry unusable | Purpose={inquiry.Purpose} | Challenge={challenge} | ParsedLinks={resultMatches.Count}");
                    continue;
                }

                int inspected = 0;
                foreach (Match match in resultMatches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (inspected >= MaxSearchResults) break;

                    string resultTitle = CleanText(WebUtility.HtmlDecode(TagRegex.Replace(match.Groups["title"].Value, " ")));
                    if (!IsRelevantSearchResultTitle(resultTitle, cleanTitle))
                    {
                        Debug.WriteLine($"[WEB CONSULTANT TRACE] Search result skipped | Engine={inquiry.Engine} | Reason=TitleNotRelevant | ResultTitle='{resultTitle}' | ExpectedTitle='{cleanTitle}'");
                        continue;
                    }

                    string rawUrl = WebUtility.HtmlDecode(match.Groups["url"].Value).Replace("&amp;", "&");
                    try { rawUrl = UnwrapSearchResultUrl(rawUrl); }
                    catch (FormatException ex)
                    {
                        Debug.WriteLine($"[WEB CONSULTANT TRACE] Search result skipped | Engine={inquiry.Engine} | Reason=MalformedWrappedUrl | {ex.Message}");
                        continue;
                    }
                    if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? pageUri) ||
                        (pageUri.Scheme != Uri.UriSchemeHttps && pageUri.Scheme != Uri.UriSchemeHttp))
                    {
                        Debug.WriteLine($"[WEB CONSULTANT TRACE] Search result skipped | Engine={inquiry.Engine} | Reason=InvalidDestination | URL='{rawUrl}'");
                        continue;
                    }
                    if (IsSearchOrSocialHost(pageUri.Host)) continue;

                    inspected++;
                    try
                    {
                        using HttpResponseMessage response = PageClient.GetAsync(pageUri, cancellationToken).GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode ||
                            (response.Content.Headers.ContentType?.MediaType is string mediaType &&
                             !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)))
                        {
                            string pageFailureState = (int)response.StatusCode == 429 ? "RateLimited" : "PageUnavailable";
                        Debug.WriteLine($"[WEB CONSULTANT STATUS] State={pageFailureState} | Route=SearchResultPage | URL={pageUri} | Status={(int)response.StatusCode} | ContentType={response.Content.Headers.ContentType?.MediaType ?? "(none)"}");
                            continue;
                        }

                        string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? pageUri.ToString();
                        string html = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                        if (html.Length > MaxPageCharacters) html = html.Substring(0, MaxPageCharacters);
                        string visibleText = CleanHtml(html);
                        bool titleMatch = ContainsPhrase(visibleText, cleanTitle);
                        bool authorMatch = !string.IsNullOrWhiteSpace(cleanAuthor) && ContainsPhrase(visibleText, cleanAuthor);
                        bool publisherMatch = !string.IsNullOrWhiteSpace(cleanPublisher) && ContainsPhrase(visibleText, cleanPublisher);
                        bool isbnMatch = !string.IsNullOrWhiteSpace(cleanIsbn) &&
                            ContainsPhrase(Regex.Replace(visibleText, "[^0-9Xx]", string.Empty), cleanIsbn);

                        // The requested title must appear on the candidate page. The primary
                        // inquiry may use author evidence, but a questionable author is never
                        // mandatory corroboration. ISBN/publisher evidence can independently
                        // support the candidate and the normal decision engine still authorizes it.
                        // A backup query is not identity evidence. In particular, a short/common
                        // title such as "Goodbye" can appear inside an unrelated title such as
                        // "Gone Before Goodbye". Require independent identity evidence on the page.
                        // The query route itself must never raise a page to candidate status.
                        bool corroboratedIdentity = titleMatch &&
                            (isbnMatch || publisherMatch || authorMatch);
                        if (!corroboratedIdentity)
                        {
                            Debug.WriteLine($"[WEB CONSULTANT] Rejected page identity mismatch | TitleMatch={titleMatch} | AuthorMatch={authorMatch} | PublisherMatch={publisherMatch} | ISBNMatch={isbnMatch} | Purpose={inquiry.Purpose} | Query='{inquiry.Query}' | URL={finalUrl}");
                            continue;
                        }

                        string description = ExtractDescription(html);
                        if (!IsSubstantiveDescription(description))
                        {
                            Debug.WriteLine($"[WEB CONSULTANT STATUS] State=DescriptionTooShortOrMissing | Route=SearchResultPage | URL={finalUrl}");
                            continue;
                        }

                        MetadataResearchCandidate candidate = new()
                        {
                            Field = "Description",
                            Value = description,
                            Source = "Web Consultant",
                            Evidence = $"Web Consultant {inquiry.Purpose} inquiry ({inquiry.Engine}) found a page matching the requested title; description extracted from {finalUrl}.",
                            Confidence = isbnMatch ? 0.85 : (authorMatch && publisherMatch ? 0.82 : (publisherMatch ? 0.76 : (authorMatch ? 0.72 : 0.62)))
                        };
                        candidate.Details.Add($"Inquiry: {inquiry.Purpose}");
                        candidate.Details.Add($"Search engine: {inquiry.Engine}");
                        candidate.Details.Add($"Search query: {inquiry.Query}");
                        candidate.Details.Add($"Source URL: {finalUrl}");
                        candidate.Details.Add(isbnMatch
                            ? "The source page contains the requested title and ISBN evidence."
                            : publisherMatch
                                ? "The source page contains the requested title and publisher evidence."
                                : authorMatch
                                    ? "The source page contains the requested title and author evidence."
                                    : "The backup source page contains the requested title but lacks independent identifier corroboration; the candidate remains low-confidence and subject to the existing decision process.");
                        if (!string.IsNullOrWhiteSpace(story.StoryTitle))
                            candidate.Details.Add($"Opening-content story clue used for discovery: {story.StoryTitle} by {story.StoryAuthors}.");
                        candidate.Details.Add("Existing decision rules remain responsible for authorization; this consultant never applies a repair directly.");
                        candidates.Add(candidate);
                        Debug.WriteLine($"[WEB CONSULTANT] Candidate | Title='{title}' | Inquiry={inquiry.Purpose} | Engine={inquiry.Engine} | URL='{finalUrl}' | Characters={description.Length} | Confidence={candidate.Confidence:P0}");
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        Debug.WriteLine($"[WEB CONSULTANT STATUS] State=PageUnavailable | Route=SearchResultPage | URL={pageUri} | {ex.Message}");
                    }
                }

                // A usable candidate completes the goal. If the provider returned links but
                // none supplied a valid description, allow the one adjusted secondary inquiry.
                if (candidates.Count > 0) break;
                string nextStep = inquiry.Purpose switch
                {
                    "primary-1" => "one reformulated primary inquiry remains.",
                    "primary-2" => "primary attempts exhausted; two backup inquiries remain.",
                    "backup-1" => "one reformulated backup inquiry remains.",
                    _ => "all four inquiries exhausted; preserve the book as unresolved."
                };
                Debug.WriteLine($"[WEB CONSULTANT TRACE] No usable description from {inquiry.Purpose}; {nextStep}");
            }

            Debug.WriteLine($"[WEB CONSULTANT STATUS] State={(candidates.Count > 0 ? "CandidateFound" : "NoMatch")} | Title='{cleanTitle}' | Candidates={candidates.Count} | GeneralInquiryLimit={inquiries.Length}");
            Debug.WriteLine($"[WEB CONSULTANT TRACE] Title='{title}' | Candidates={candidates.Count}");
            return candidates;
        }

        private static IEnumerable<Uri> BuildDirectOfficialSourceUris(string title, string author, string publisher)
        {
            string slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (slug.Length == 0) yield break;

            // These are known, publicly reachable official catalog URL conventions.
            // The URL is only a discovery attempt; page identity and content are still validated.
            if (author.Contains("Mercedes Lackey", StringComparison.OrdinalIgnoreCase))
            {
                // Prefer the canonical apex host. The previous www-only attempt failed
                // on the user's machine with DNS NXDOMAIN even though the official page
                // exists. Keep the www variant as a bounded fallback, not a search query.
                if (Uri.TryCreate("https://mercedeslackey.com/p/" + slug, UriKind.Absolute, out Uri? canonicalAuthorUri))
                    yield return canonicalAuthorUri;
                if (Uri.TryCreate("https://www.mercedeslackey.com/p/" + slug, UriKind.Absolute, out Uri? wwwAuthorUri))
                    yield return wwwAuthorUri;
            }
            if (publisher.Contains("Baen", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate("https://www.baen.com/" + slug + ".html", UriKind.Absolute, out Uri? publisherUri))
                    yield return publisherUri;
            }
        }

        private static bool IsSameOfficialHostFamily(string candidateHost, string expectedHost)
        {
            // Permit only the exact trusted publisher/author domain with or without
            // its conventional www prefix. Do not permit arbitrary sibling subdomains.
            static string RootHost(string host) => host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? host.Substring(4)
                : host;

            return RootHost(candidateHost).Equals(RootHost(expectedHost), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSubstantiveDescription(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 120) return false;

            // Search/catalog meta descriptions are often teaser snippets ending in
            // "..." or "…". Do not promote those clipped snippets as complete book
            // summaries. ExtractDescription will continue to JSON-LD and visible
            // paragraph content; if those are also incomplete, the bounded inquiry
            // loop can try another source rather than asking the user to approve a cut-off blurb.
            if (Regex.IsMatch(value, @"(?:\.{3}|…|\.\.\.)\s*$", RegexOptions.CultureInvariant))
                return false;

            // A page can contain the correct book title and author while its Open Graph
            // description is actually Amazon/store boilerplate. Length and word-count checks
            // alone incorrectly promoted the Warriorborn promotional disclaimer to 85%.
            // Reject commerce, legal, account, and promotion language before a candidate is
            // created; confidence must never compensate for a description that is not prose
            // about the book itself.
            if (Regex.IsMatch(value,
                @"\b(add to cart|buy now|read online|download|all rights reserved|privacy policy|terms of use|terms and conditions|terms & conditions|sign up|log in|shipping|customer service|some promotions may be combined|not eligible to be combined|other offers|eligible to be combined|promotional offer|limited time offer|subscribe and save|click here|accept cookies|cookie policy|your privacy choices|manage cookies|redemption links|ebooks? can only be redeemed|cannot be resold|is a bestselling author|bestselling author and|martial arts enthusiast|resume includes|lives with (?:his|her|their) family|about the author|more titles and copies may be available|sign in to see the full collection|availability can change throughout|place a hold on the title|hold will be automatically filled|library(?:'s|’s) budget|available again)\b",
                RegexOptions.IgnoreCase))
                return false;

            // Common page furniture often consists of a long legal sentence rather than
            // a short keyword. Require several book-description cues when the text appears
            // commerce/legal oriented. This deliberately errs toward asking for another
            // source instead of presenting store boilerplate as a book synopsis.
            if (Regex.IsMatch(value,
                @"\b(terms|conditions|promotion|promotions|eligible|offer|offers|cookies|privacy|advertisement|advertising|bestselling author|martial arts enthusiast|lives with his family|lives with her family|lives with their family|resume includes)\b",
                RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(value,
                    @"\b(story|novel|book|character|characters|reader|readers|adventure|mystery|journey|discovers|must|returns|finds|struggles|quest|world)\b",
                    RegexOptions.IgnoreCase))
                return false;

            // Reject common non-synopsis text that can be long enough to pass the
            // length/word-count gate: author biographies, review/rating UI, and
            // short catalog hooks. Keep this content-level check independent of
            // source confidence; a high confidence score cannot make page furniture
            // into a book synopsis.
            if (Regex.IsMatch(value,
                @"\b(customer reviews?|including product star ratings|help customers|star ratings|write a review|review this product|rate this book|your recently viewed items|frequently bought together|about the author|author biography|biography of the author|is the acclaimed author|acclaimed author of|author of over \d+|has written over \d+|has published over \d+|was born in \d{4}|grew up in|lives in [A-Z]|was raised in|her first novel|his first novel|first published in \d{4})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return false;

            // A first-sentence hook is not a complete description even when the
            // source supplies no ellipsis. Require a minimally coherent synopsis
            // signal for unusually short single-sentence candidates.
            if (value.Length < 180 &&
                !Regex.IsMatch(value,
                    @"\b(when|after|before|as|while|but|however|must|tries to|finds himself|finds herself|discovers|struggles|sets out|is forced to|must face|journey|quest|mystery|adventure|story of|novel about)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return false;

            return Regex.Matches(value, @"[A-Za-z]{3,}").Count >= 18;
        }

        private static bool IsRelevantSearchResultTitle(string resultTitle, string expectedTitle)
        {
            string result = CleanText(resultTitle);
            string expected = CleanText(expectedTitle);
            if (result.Length == 0 || expected.Length == 0) return false;
            // For one-token titles, substring matching is too permissive: "Goodbye" would
            // match "Gone Before Goodbye". Require the result heading to begin with the exact
            // title token, allowing source branding after it (for example "Goodbye | Fandom").
            string[] expectedTokens = Regex.Split(expected.ToLowerInvariant(), @"[^a-z0-9]+")
                .Where(t => t.Length >= 2).Distinct().ToArray();
            string normalizedResult = Regex.Replace(result.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
            if (expectedTokens.Length == 1)
            {
                return normalizedResult.StartsWith(expectedTokens[0] + " ", StringComparison.Ordinal) ||
                    string.Equals(normalizedResult, expectedTokens[0], StringComparison.Ordinal);
            }
            if (result.Contains(expected, StringComparison.OrdinalIgnoreCase)) return true;

            // Avoid obvious lexical collisions such as "Chrome Circle" returning the Chrome browser.
            // Require every meaningful title token to occur in the result heading, not merely one word.
            string[] tokens = Regex.Split(expected.ToLowerInvariant(), @"[^a-z0-9]+");
            tokens = tokens.Where(t => t.Length >= 3).Distinct().ToArray();
            if (tokens.Length == 0) return false;
            return tokens.All(token => Regex.IsMatch(normalizedResult, $@"(?<![a-z0-9]){Regex.Escape(token)}(?![a-z0-9])"));
        }

        private static bool IsUsableStoryClue(string title, string authors)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(authors)) return false;
            string t = CleanText(title);
            string a = CleanText(authors);
            if (t.Length < 3 || t.Length > 70 || a.Length < 3 || a.Length > 100) return false;
            // Reject production credits and back-matter headings before they become search clues.
            // These often match a generic "Title by Author" pattern but are not story titles.
            if (Regex.IsMatch(t,
                @"^(also|other books? by|other works? by|more books? by|about the author|cover design|cover art|cover illustration|cover artist|interior illustrations?|illustrations?|illustrated by|map(s)? by|artwork by|photo(graphy)? by|editorial staff|contributors?)\b",
                RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(t, @"copyright|\bISBN\b|\bSE\s*#?\d|\bSERRAted Edge\b|\.epub|\bby\b.*\bby\b", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(t, @"\b(electronic version|title page|table of contents|contents|dedication|copyright page|publisher information|about the author)\b", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(a, @"\b(baen books?|electronic version|publisher|editorial staff|unknown)\b", RegexOptions.IgnoreCase)) return false;
            if (t.Contains(" - ", StringComparison.Ordinal) || t.Contains("Free Stories", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static (string StoryTitle, string StoryAuthors) ExtractStoryClue(string openingContent)
        {
            if (string.IsNullOrWhiteSpace(openingContent)) return (string.Empty, string.Empty);

            // E_EpubContentResource supplies readable text, not HTML. Normalize both actual
            // newlines and common separator patterns, while avoiding treating the entire EPUB
            // opening as one line. Candidate lines are discovery clues only, never book identity.
            string text = WebUtility.HtmlDecode(openingContent)
                .Replace("\r\n", "\n").Replace('\r', '\n');
            text = Regex.Replace(text, @"[ \t]+", " ");
            string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(CleanText).Where(line => line.Length > 0).ToArray();

            // A few EPUBs flatten all inline text to spaces. In that case split only around
            // likely table-of-contents separators; do not split ordinary prose indiscriminately.
            if (lines.Length <= 2)
            {
                lines = Regex.Split(text, @"\s{2,}|(?<=\.)\s+(?=[A-Z][^.!?]{2,65}\s+by\s+[A-Z])")
                    .Select(CleanText).Where(line => line.Length > 0).ToArray();
            }

            List<string> candidateLines = new();
            for (int i = 0; i < lines.Length && candidateLines.Count < 500; i++)
            {
                candidateLines.Add(lines[i]);
                // Some EPUBs put the title and the byline in separate block elements.
                if (i + 1 < lines.Length &&
                    Regex.IsMatch(lines[i + 1], @"^by\s+[A-Z]", RegexOptions.None))
                    candidateLines.Add(lines[i] + " " + lines[i + 1]);
            }

            foreach (string candidateLine in candidateLines.Take(500))
            {
                if (candidateLine.Length > 240 ||
                    Regex.IsMatch(candidateLine, @"\b(copyright|all rights reserved|ISBN|SERRAted Edge|free stories 2020)\b", RegexOptions.IgnoreCase))
                {
                    // Do not discard every line containing a copyright phrase if a clean
                    // title/byline appears before it; simply do not use the line as a clue.
                    if (Regex.IsMatch(candidateLine, @"\b(copyright|all rights reserved|ISBN)\b", RegexOptions.IgnoreCase))
                        continue;
                }

                Match credit = Regex.Match(candidateLine,
                    @"^(?<title>[A-Z][A-Za-z0-9'’:,!?&. -]{2,70}?)\s+by\s+(?<authors>[A-Z][A-Za-z'’.-]+(?:\s+[A-Z][A-Za-z'’.-]+)?(?:\s+(?:and|&)\s+[A-Z][A-Za-z'’.-]+(?:\s+[A-Z][A-Za-z'’.-]+)?){0,2})\s*[.!]?$",
                    RegexOptions.None);
                if (!credit.Success) continue;

                string storyTitle = CleanText(credit.Groups["title"].Value).Trim(' ', '.', ',', ';', ':', '"', '\'');
                string storyAuthors = CleanText(credit.Groups["authors"].Value).Trim(' ', '.', ',', ';', ':', '"', '\'');
                if (IsUsableStoryClue(storyTitle, storyAuthors))
                {
                    Debug.WriteLine($"[WEB CONSULTANT TRACE] Story clue extracted | Title='{storyTitle}' | Authors='{storyAuthors}'");
                    return (storyTitle, storyAuthors);
                }
            }

            Debug.WriteLine($"[WEB CONSULTANT TRACE] No conservative story-title/byline pattern found | OpeningChars={openingContent.Length} | LinesExamined={Math.Min(candidateLines.Count, 500)}; continuing with title/publisher/ISBN queries.");
            return (string.Empty, string.Empty);
        }

        private static string GetSearchHtml(Uri initialUri, string engine, CancellationToken cancellationToken)
        {
            Uri current = initialUri;
            for (int hop = 0; hop <= MaxRedirects; hop++)
            {
                using HttpResponseMessage response = SearchClient.GetAsync(current, cancellationToken).GetAwaiter().GetResult();
                Debug.WriteLine($"[WEB CONSULTANT TRACE] Search HTTP | Engine={engine} | Hop={hop} | URL={current} | Status={(int)response.StatusCode} | Redirect={(response.Headers.Location?.ToString() ?? "(none)")}");
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399 && response.Headers.Location != null)
                {
                    Uri next = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
                    Debug.WriteLine($"[WEB CONSULTANT TRACE] Redirect | Engine={engine} | FromHost={current.Host} | ToHost={next.Host}");
                    // The prior failure named this internal hostname. Do not follow it blindly; try the next public endpoint.
                    if (next.Host.Equals("internal-search.duckduckgo.com", StringComparison.OrdinalIgnoreCase))
                        throw new HttpRequestException($"DuckDuckGo redirected to internal-search.duckduckgo.com; stopping this redirect chain and trying another public endpoint. Redirect={next}");
                    current = next;
                    continue;
                }
                if ((int)response.StatusCode == 429)
                    Debug.WriteLine($"[WEB CONSULTANT STATUS] State=RateLimited | Engine={engine} | URL={current} | RetryAfter={response.Headers.RetryAfter?.Delta?.ToString() ?? "(unspecified)"}");
                response.EnsureSuccessStatusCode();
                string html = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                DiagnoseSearchBody(engine, current, response, html);
                return html;
            }
            throw new HttpRequestException($"Search redirect limit ({MaxRedirects}) exceeded for {initialUri}");
        }


        // Diagnostic only: this does not alter parsing, candidate filtering, or repair decisions.
        // It distinguishes a successful HTTP exchange from a response that actually contains search results.
        private static void DiagnoseSearchBody(string engine, Uri requestUri, HttpResponseMessage response, string html)
        {
            string title = string.Empty;
            Match titleMatch = Regex.Match(html, "<title\\b[^>]*>(?<value>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success) title = CleanText(WebUtility.HtmlDecode(TagRegex.Replace(titleMatch.Groups["value"].Value, " ")));

            int ddgMarkers = Regex.Matches(html, "\\bresult__a\\b", RegexOptions.IgnoreCase).Count;
            int bingBlocks = Regex.Matches(html, "class=[\"']b_algo[\"']", RegexOptions.IgnoreCase).Count;
            int bingHeadings = Regex.Matches(html, "<h2\\b", RegexOptions.IgnoreCase).Count;
            bool challenge = IsChallengeResponse(html);
            string visibleSample = CleanText(WebUtility.HtmlDecode(TagRegex.Replace(
                Regex.Replace(html, "<(script|style|noscript)\\b[^>]*>.*?</\\1>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline), " ")));
            if (visibleSample.Length > 320) visibleSample = visibleSample.Substring(0, 320);

            Debug.WriteLine($"[WEB CONSULTANT DIAGNOSTIC] BODY | Engine={engine} | RequestUri={requestUri} | FinalUri={response.RequestMessage?.RequestUri} | Status={(int)response.StatusCode} | ContentType={response.Content.Headers.ContentType?.ToString() ?? "(none)"} | BytesAsChars={html.Length} | PageTitle='{title}' | DDGResultMarkers={ddgMarkers} | BingOrganicBlocks={bingBlocks} | BingH2Headings={bingHeadings} | ChallengeLikeText={challenge} | VisibleSample='{visibleSample}'");

            Regex parser = engine.StartsWith("DuckDuckGo", StringComparison.OrdinalIgnoreCase)
                ? DuckDuckGoResultLinkRegex : BingResultLinkRegex;
            int shown = 0;
            foreach (Match match in parser.Matches(html))
            {
                if (shown >= 5) break;
                string resultTitle = CleanText(WebUtility.HtmlDecode(TagRegex.Replace(match.Groups["title"].Value, " ")));
                string resultUrl = WebUtility.HtmlDecode(match.Groups["url"].Value).Replace("&amp;", "&");
                string destination = UnwrapSearchResultUrl(resultUrl);
                string host = Uri.TryCreate(destination, UriKind.Absolute, out Uri? parsed) ? parsed.Host : "(unresolved)";
                Debug.WriteLine($"[WEB CONSULTANT DIAGNOSTIC] RAW RESULT | Engine={engine} | Index={shown + 1} | Title='{resultTitle}' | Host={host} | URL='{destination}'");
                shown++;
            }
            if (shown == 0)
                Debug.WriteLine($"[WEB CONSULTANT DIAGNOSTIC] NO RAW RESULT LINKS | Engine={engine} | This response is not evidence of usable search results, regardless of HTTP status.");
        }

        private static bool IsChallengeResponse(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return true;

            // Both engines may return HTTP 200/202 with an interstitial instead of results.
            // Match visible wording as well as common challenge markup identifiers.
            return Regex.IsMatch(html,
                "captcha|challenge-platform|unusual traffic|verify you are human|robot check|consent required|access denied|automated queries|select all squares|select all images|solve the challenge|please solve|one last step|human verification|security check|prove you are human|are you a robot|containing a duck",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private static string UnwrapSearchResultUrl(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;
            rawUrl = WebUtility.HtmlDecode(rawUrl.Trim()).Replace("&amp;", "&");
            // DuckDuckGo frequently emits protocol-relative redirect links (//duckduckgo.com/l/?uddg=...).
            // Normalize before parsing so these valid links are not discarded as invalid destinations.
            if (rawUrl.StartsWith("//", StringComparison.Ordinal)) rawUrl = "https:" + rawUrl;
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri)) return rawUrl;

            // DuckDuckGo HTML/Lite commonly wrap destination URLs as /l/?uddg=<escaped-url>.
            // Resolve this before IsSearchOrSocialHost checks the hostname, otherwise every
            // wrapped result is discarded as a DuckDuckGo link even though parsing succeeded.
            if (uri.Host.EndsWith("duckduckgo.com", StringComparison.OrdinalIgnoreCase) &&
                (uri.AbsolutePath.Equals("/l/", StringComparison.OrdinalIgnoreCase) ||
                 uri.AbsolutePath.Equals("/l", StringComparison.OrdinalIgnoreCase)))
            {
                string? target = GetQueryParameter(uri, "uddg");
                if (IsHttpUrl(target)) return target!;
            }

            // Bing may wrap destination URLs in /ck/a?u=a1<base64>.
            if (uri.Host.EndsWith("bing.com", StringComparison.OrdinalIgnoreCase) &&
                uri.AbsolutePath.Contains("/ck/a", StringComparison.OrdinalIgnoreCase))
            {
                string? encoded = GetQueryParameter(uri, "u");
                if (!string.IsNullOrWhiteSpace(encoded) && encoded.StartsWith("a1", StringComparison.Ordinal) && encoded.Length > 2)
                {
                    try
                    {
                        string base64 = encoded.Substring(2).Replace('-', '+').Replace('_', '/');
                        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
                        string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        if (IsHttpUrl(decoded)) return decoded;
                    }
                    catch (FormatException ex)
                    {
                        Debug.WriteLine($"[WEB CONSULTANT TRACE] Bing wrapped URL could not be decoded safely: {ex.Message}");
                    }
                }
            }
            return rawUrl;
        }

        private static string? GetQueryParameter(Uri uri, string name)
        {
            foreach (string part in uri.Query.TrimStart('?').Split('&'))
            {
                string[] pair = part.Split(new[] { '=' }, 2);
                if (pair.Length == 2 && pair[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(pair[1].Replace("+", "%20"));
            }
            return null;
        }

        private static bool IsHttpUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static string ExtractDescription(string html)
        {
            foreach (Match meta in MetaRegex.Matches(html))
            {
                Match content = ContentRegex.Match(meta.Value);
                if (!content.Success) continue;
                string value = CleanDescriptionCandidate(WebUtility.HtmlDecode(content.Groups["value"].Value));
                if (IsSubstantiveDescription(value)) return value;
            }

            Match jsonLd = Regex.Match(html,
                "\\\"description\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"\\\\])+)\\\"",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (jsonLd.Success)
            {
                string value = CleanDescriptionCandidate(WebUtility.HtmlDecode(jsonLd.Groups["value"].Value
                    .Replace("\\n", " ").Replace("\\r", " ").Replace("\\\"", "\"")));
                if (IsSubstantiveDescription(value)) return value;
            }
            // Some official pages place a synopsis in visible paragraph content, not metadata tags.
            // Extract a single substantive paragraph instead of flattening navigation and contents.
            MatchCollection paragraphs = Regex.Matches(html,
                "<p\\b[^>]*>(?<body>.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            List<string> usableParagraphs = new();
            foreach (Match paragraph in paragraphs)
            {
                string value = CleanDescriptionCandidate(CleanHtml(paragraph.Groups["body"].Value));
                if (value.Length < 65 || value.Length > 4000) continue;
                if (Regex.IsMatch(value,
                    @"\b(add to cart|buy now|read online|download|all rights reserved|privacy policy|terms of use|sign up|log in|shipping|customer service)\b",
                    RegexOptions.IgnoreCase)) continue;
                if (usableParagraphs.Contains(value, StringComparer.OrdinalIgnoreCase)) continue;
                usableParagraphs.Add(value);
            }

            // Official author/publisher pages often split the synopsis across several short
            // paragraphs. Try the first coherent paragraph group rather than rejecting every
            // paragraph individually or selecting a random-length navigation/review block.
            for (int start = 0; start < usableParagraphs.Count; start++)
            {
                List<string> group = new();
                int total = 0;
                for (int index = start; index < usableParagraphs.Count && group.Count < 8; index++)
                {
                    string paragraph = usableParagraphs[index];
                    if (total + paragraph.Length > 4000) break;
                    group.Add(paragraph);
                    total += paragraph.Length;
                    string combined = string.Join(" ", group);
                    if (IsSubstantiveDescription(combined)) return combined;
                }
            }
            return string.Empty;
        }

        private static string CleanDescriptionCandidate(string value)
        {
            value = CleanText(value);
            if (value.Length == 0) return value;

            // Publisher metadata sometimes appends legal notices and an entire contents
            // list to the actual synopsis. Keep the descriptive text before that boundary;
            // the contents remain available separately in local EPUB evidence.
            Match boundary = Regex.Match(value,
                @"\b(?:As is usual with such copyrighted material|contents may be copied and shared but|all commercial rights are reserved|all rights reserved|table of contents)\b",
                RegexOptions.IgnoreCase);
            if (boundary.Success && boundary.Index >= 100)
                value = value.Substring(0, boundary.Index).TrimEnd(' ', '.', ';', ':', '-', '—');

            return CleanText(value);
        }

        private static string CleanHtml(string html)
        {
            string withoutScripts = Regex.Replace(html, "<(script|style|noscript)\\b[^>]*>.*?</\\1>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return CleanText(WebUtility.HtmlDecode(TagRegex.Replace(withoutScripts, " ")));
        }
        // Search-only cleanup for catalog/download suffixes such as "(ca-1)".
        // The EPUB's observed title is never rewritten by this helper.
        private static string NormalizeResearchTitle(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value.Trim(), @"\s*\([a-z]{1,5}-\d+\)\s*$", string.Empty,
                RegexOptions.IgnoreCase).Trim();
        }

        private static string CleanText(string value) => Regex.Replace(value ?? string.Empty, "\\s+", " ").Trim();
        private static bool ContainsPhrase(string text, string phrase) => text.IndexOf(phrase.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        private static bool IsSearchOrSocialHost(string host) =>
            host.Contains("duckduckgo.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("google.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("bing.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("pinterest.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase);

        private static HttpClient CreateClient(bool allowRedirects)
        {
            HttpClientHandler handler = new() { AllowAutoRedirect = allowRedirects, MaxAutomaticRedirections = MaxRedirects };
            HttpClient client = new(handler) { Timeout = TimeSpan.FromSeconds(12) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/json;q=0.9,*/*;q=0.8");
            return client;
        }
    }
}
