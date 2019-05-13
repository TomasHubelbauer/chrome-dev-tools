using System.Net.Http;
using System.Text.RegularExpressions;

var contentStringBuilder = new StringBuilder();
contentStringBuilder.AppendLine();
contentStringBuilder.AppendLine("The rest of this README is auto-generated. Generated on: " + DateTime.Now.ToString("s"));
contentStringBuilder.AppendLine();

using (var httpClient = new HttpClient())
{
  string capabilitiesPageSource;
  try
  {
    capabilitiesPageSource = await File.ReadAllTextAsync(nameof(capabilitiesPageSource) + ".html");
  }
  catch (Exception)
  {
    // Download https://developers.google.com/web/updates/capabilities
    var capabilitiesPageUri = new Uri("https://developers.google.com/web/updates/capabilities");
    capabilitiesPageSource = await httpClient.GetStringAsync(capabilitiesPageUri);
    await File.WriteAllTextAsync(nameof(capabilitiesPageSource) + ".html", capabilitiesPageSource);
  }

  // Parse out the menu items containing the label "DevTools"
  var menuItemRegex = new Regex(@"href=""(?<uri>[^""]*)""[^>]*>What's New In DevTools \(Chrome (?<version>\d+)\)</a>");
  foreach (var menuItemMatch in menuItemRegex.Matches(capabilitiesPageSource).Cast<Match>())
  {
    var uri = menuItemMatch.Groups["uri"].Value;
    var version = menuItemMatch.Groups["version"].Value;

    Console.WriteLine("Chrome " + version);
    contentStringBuilder.AppendLine($"- Chrome {version}");

    string versionPageSource;
    try
    {
      versionPageSource = await File.ReadAllTextAsync($"version{version}PageSource.html");
    }
    catch (Exception)
    {
      // Download version page
      var versionPageUri = new Uri(uri);
      versionPageSource = await httpClient.GetStringAsync(versionPageUri);
      await File.WriteAllTextAsync($"version{version}PageSource.html", versionPageSource);
    }

    // Parse out the headings
    var headingRegex = new Regex(@"<h2 id=""(?<id>[^""]*)"">(?<title>[^<]*)</h2>");
    foreach (var headingMatch in headingRegex.Matches(versionPageSource).Cast<Match>())
    {
      var id = headingMatch.Groups["id"].Value;
      var title = headingMatch.Groups["title"].Value;

      Console.WriteLine("\t" + title);
      contentStringBuilder.AppendLine($"\t- [{title}]({uri}#{id})");
    }
  }
}

// Replace the content in README.md
var readmeContent = await File.ReadAllTextAsync("README.md");
var manualContent = readmeContent.Substring(0, readmeContent.IndexOf("<!-- Auto-Generated: -->"));
var generatedContent = contentStringBuilder.ToString();
await File.WriteAllTextAsync("README.md", manualContent + "<!-- Auto-Generated: -->" + generatedContent);
