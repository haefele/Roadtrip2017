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
    
    return months > 0 
        ? $"Noch {months} Monate und {days} Tage bis zum Roadtrip!"
        : $"Noch {days} Tage bis zum Roadtrip!";
}

private static async Task<string> GetBody(TraceWriter log)
{    
    string city = GetRandomCity();
    string image  = await GetRandomImage(city);
    
    log.Info($"Image for {city} is: {image}");

    return $@"Guten Morgen!<br><br>
    
Heute gibt es ein Bild von <b>{city}!</b><br><br>
<img src=""{image}"" width=720>";
}

private static string GetStringSetting(string key)
{
    return ConfigurationManager.AppSettings[key];
}

private static int GetIntSetting(string key)
{
    return int.Parse(ConfigurationManager.AppSettings[key]);
}

private static string GetRandomCity()
{
    var cities = GetStringSetting("Cities").Split(';');
    var random = new Random();
    var cityIndex = random.Next(0, cities.Length);
    return cities[cityIndex];
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
