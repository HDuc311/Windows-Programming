using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using Avalonia.Controls.Chrome;
using MangaReader.DomainCommon;

namespace MangaReader.MangaList;

public class Manga
{
    public Manga(string title, string description, string coverUrl, string lastChapter, string mangaUrl)
    {
        Title = title;
        Description = description;
        CoverUrl = coverUrl;
        LastChapter = lastChapter;
        MangaUrl = mangaUrl;
    }

    public string Title { get; }

    public string Description { get; }
    public string CoverUrl { get; }
    public string LastChapter { get; }
    public string MangaUrl { get; }
}

public class MangaList
{
    public int TotalMangaNumber { get;}
    public int TotalPageNumber { get;}
    public List<Manga> CurrentPage { get;}

    public MangaList(int totalMangaNumber, int totalPageNumber, List<Manga> currentPage)
    {
        TotalMangaNumber = totalMangaNumber;
        TotalPageNumber = totalPageNumber;
        CurrentPage = currentPage;
    }
    
}

public class Domain
{
    private readonly string baseUrl;
    private readonly Http http;

    public Domain(string baseUrl, Http http)
    {
        this.baseUrl = baseUrl;
        this.http = http;
    }

    private async Task<string> DownloadHtml(int page, string filterText)
    {
        if (page < 1) page = 1;
        string url;
        if (filterText == "")
        {
            url = $"{this.baseUrl}/filter?status=0&sort=updatedAt&page={page}";
        }
        else
        {
            var text = HttpUtility.HtmlDecode(filterText);
            url =$"{this.baseUrl}/tim-kiem?keyword={text}&page={page}";
        }
        Console.WriteLine($"Downloading page {page} from {url}");
        return await http.GetStringAsync(url);
    }

    private int ParseTotalMangaNumber(XmlDocument doc)
    {
        //hdcphu@ updated for https://apptruyen247.com
        var text = doc.DocumentElement!.FirstChild!.FirstChild!.InnerText.Trim();
        var number = text.Substring(7);
        return int.Parse(number);
    }

     // private int ParseTotalPageNumber(XmlDocument doc)
     // {
     //     var div = doc.DocumentElement!.ChildNodes[3]!;
     //     var span = div.LastChild!;
     //     if (span.Attributes!["class"]!.Value == "current_page")
     //         return int.Parse(span.InnerText);
     //     var href = span.FirstChild!.Attributes!["href"]!.Value;
     //     var openingParenthesisIndex = href.LastIndexOf('(');
     //     var number = href.Substring(openingParenthesisIndex + 1, href.Length - openingParenthesisIndex - 2);
     //     return int.Parse(number);
     // }
    private int FindTotalPageNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalPages") + 13);
        s = s.Substring(0, s.IndexOf(","));
        return int.Parse(s);
    }

    private int FindTotalMangaNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalDocs")+12);
        s = s.Substring(0, s.IndexOf("}"));
        return int.Parse(s);
    }
    private List<Manga> ParseMangaList(XmlDocument doc)
    {
        //hdcphu@ updated for https://apptruyen247.com
        var div = doc.DocumentElement!.FirstChild!;
        var nodes = div.ChildNodes;
        var mangaList = new List<Manga>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeF1 = nodes[i]!.FirstChild!;
            var nodeUrlInfo = nodeF1.FirstChild!;
            var nodeTitleInfo = nodeF1.ChildNodes[1]!;
            
            var title = Html.Decode(nodeTitleInfo.FirstChild!.InnerText.Trim());
            var description = Html.Decode(nodeTitleInfo.ChildNodes[1]!.InnerText.Trim());
            var lastChapter = nodeTitleInfo.ChildNodes[2]!.FirstChild != null ? Html.Decode(nodeTitleInfo.ChildNodes[2]!.FirstChild?.InnerText!.Trim()!): "";
            
            var coverUrl = baseUrl + nodeUrlInfo.FirstChild!.Attributes!["src"]!.Value;
            var mangaUrl = baseUrl + nodeUrlInfo.Attributes!["href"]!.Value;

            var manga = new Manga(title, description, coverUrl, lastChapter, mangaUrl);
            mangaList.Add(manga);
            Console.WriteLine($"{i} : Title: {title} Description: {description} Chapter Url: {lastChapter}{Environment.NewLine} CoverUrl ={coverUrl},{Environment.NewLine} MangaUrl = {mangaUrl}");
        }
        return mangaList;
    }

    private MangaList Parse(string html)
    {
        try
        {
            var totalPageNumber = FindTotalPageNumber(html);
            var totalMangaNumber = FindTotalMangaNumber(html);
            var doc = new XmlDocument();
            File.WriteAllText("docbefore.html",html);
            var xmlStartAt= html.IndexOf("<div class=\"grid grid-cols-1");
            html = html.Substring(xmlStartAt);
            html = html.Substring(0, html.IndexOf("<div class=\"mt-6\">"));
            doc.LoadXml("<root>" + html + "</root>");
            Console.WriteLine("Page loaded");
            Console.WriteLine($"Got {totalMangaNumber} manga(s) of {totalPageNumber} page");
            var page = ParseMangaList(doc);
            return new MangaList(totalMangaNumber, totalPageNumber, page);
             
        }
        catch (Exception e)
        {
            throw new ParseException();
        }
    }

    public async Task<MangaList> LoadMangaList(int page, string filterText = "")
    {
        var html = await DownloadHtml(page, filterText);
        return this.Parse(html);
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetBytesAsync(url, token);
    }
}