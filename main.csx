using System.Net.Http;
using System.Text.RegularExpressions;

// Prepare for building new generated portion of the README content
var contentStringBuilder = new StringBuilder();
contentStringBuilder.AppendLine();
contentStringBuilder.AppendLine("The rest of this README is auto-generated. Generated on: " + DateTime.Now.ToString("s"));
contentStringBuilder.AppendLine();

// Load existing manual and generated README content
var readmeContent = await File.ReadAllTextAsync("README.md");
var divideIndex = readmeContent.IndexOf("<!-- Auto-Generated: -->");
var manualContent = readmeContent.Substring(0, divideIndex);
var generatedContent = readmeContent.Substring(divideIndex);

// Remember existing blocks of each heading to be able to restore it
var existingContent = new Dictionary<string, List<string>>();

var lines = generatedContent.Split(Environment.NewLine);
string currentHeading = null;
foreach (var line in lines)
{
  if (line.StartsWith("#"))
  {
    currentHeading = line;
    continue;
  }

  if (currentHeading == null)
  {
    continue;
  }

  if (!existingContent.ContainsKey(currentHeading))
  {
    existingContent[currentHeading] = new List<string>();
  }

  existingContent[currentHeading].Add(line);
}

// Place debugger above and press F5 to inspect how existing content is preserved

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
    contentStringBuilder.AppendLine($"### Chrome {version}");
    contentStringBuilder.AppendLine();

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

      var heading = $"#### [{title}]({uri}#{id})";
      contentStringBuilder.AppendLine(heading);

      // Recover existing section content if we have any
      if (existingContent.ContainsKey(heading))
      {
        var content = string.Join(Environment.NewLine, existingContent[heading]);

        // Collapse trailing lines into the one we add below
        content = content.TrimEnd();

        if (!string.IsNullOrEmpty(content))
        {
          contentStringBuilder.AppendLine(content);
        }

        contentStringBuilder.AppendLine();
      }
      else
      {
        contentStringBuilder.AppendLine();
      }
    }
  }
}

// Replace the content in README.md
generatedContent = contentStringBuilder.ToString();
await File.WriteAllTextAsync("README.md", manualContent + "<!-- Auto-Generated: -->" + generatedContent);
