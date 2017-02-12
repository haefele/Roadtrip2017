#r "Newtonsoft.Json"

using System;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task Run(TimerInfo timer, TraceWriter log)
{
    var client = new SmtpClient(GetStringSetting("SmtpHost"), GetIntSetting("SmtpPort"));
    client.Credentials = new NetworkCredential(GetStringSetting("SmtpUsername"), GetStringSetting("SmtpPassword"));
    
    var recipients = GetStringSetting("EmailRecipients").Split(';');
    string subject = GetSubject(log);
    string body = await GetBody(log);

    foreach (var rec in recipients) 
    {
        log.Info($"Sending email to {rec}.");

        var mail = new MailMessage(GetStringSetting("EmailSender"), rec);
        mail.IsBodyHtml = true;
        mail.Subject = subject;
        mail.Body = body;

        await client.SendMailAsync(mail);
    }    
}

private static string GetSubject(TraceWriter log) 
{
    DateTime roadtrip = new DateTime(2017, 5, 30);
    DateTime today = DateTime.Today;

    var until = roadtrip - today;
    var months = ((int)until.TotalDays / 30);
    var days = (int)until.TotalDays % 30;

    log.Info($"Roadtrip is in {months} months and {days} days.");
    
    return $"Noch {months} Monate und {days} Tage bis zum Roadtrip!";
}

private static async Task<string> GetBody(TraceWriter log)
{    
    string paris  = await GetRandomImage("Paris");
    log.Info($"Image for Paris is: {paris}");
    string london = await GetRandomImage("London");
    log.Info($"Image for London is: {london}");
    string brussel = await GetRandomImage("Brüssel");
    log.Info($"Image for Brussel is: {brussel}");

    return $@"<h2>Paris</h2>
<img src=""{paris}"" width=1280><br/>

<h2>London</h2>
<img src=""{london}"" width=1280><br/>

<h2>Brüssel</h2>
<img src=""{brussel}"" width=1280>";
}

private static string GetStringSetting(string key)
{
    return ConfigurationManager.AppSettings[key];
}

private static int GetIntSetting(string key)
{
    return int.Parse(ConfigurationManager.AppSettings[key]);
}

private static async Task<string> GetRandomImage(string city) 
{
    var client = new HttpClient();
    client.BaseAddress = new Uri("https://api.cognitive.microsoft.com/bing/v5.0/images/");
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", GetStringSetting("BingSearchKey"));

    var response = await client.GetAsync($"search?q={city}&count=500&offset=0&mkt=de-DE&safeSearch=Moderate&imageType=Photo");
    var responseString = await response.Content.ReadAsStringAsync();
    JObject responseJson = JObject.Parse(responseString);

    var imagesJson = responseJson.Value<JArray>("value");
    int imagesCount = imagesJson.Count;
    int randomImageIndex = new Random().Next(0, imagesCount);

    return imagesJson[randomImageIndex].Value<string>("contentUrl");
}