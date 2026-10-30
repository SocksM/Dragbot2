using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Dragbot2.Resources.AppSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace Dragbot2.Controllers;

[ApiController]
[Route("Youtube/webhook")]
public partial class YoutubeWebhookController(
    ILogger<YoutubeWebhookController> logger,
    RestClient restClient,
    IOptions<YoutubeSettings> optYoutubeSettings
) : ControllerBase
{
    private YoutubeSettings YoutubeSettings => optYoutubeSettings.Value;

    [HttpGet]
    public IActionResult Get()
    {
        var challenge = Request.Query["hub.challenge"].ToString();

        if (string.IsNullOrEmpty(challenge))
            return BadRequest();

        return Ok(challenge);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] YoutubeFeed youtubeRequest)
    {
        logger.LogInformation("YouTube notification received for channel {channelId} with \"{videoTitle}\" as the title", youtubeRequest.Entry?.ChannelId, youtubeRequest.Entry?.Title);

        if (youtubeRequest.Entry == null)
        {
            logger.LogError("Entry was null in the YouTube notification");
            return NoContent();
        }

        if (youtubeRequest.Entry is not
            {
                Published: not null,
                Updated: not null,
                VideoId: not null,
                ChannelId: not null,
            })
        {
            logger.LogError(
                "Invalid YouTube notification. Published: {published}, Updated: {updated}, VideoId: {videoId}, ChannelId: {channelId}, Title: {videoTitle}",
                youtubeRequest.Entry.Published,
                youtubeRequest.Entry.Updated,
                youtubeRequest.Entry.VideoId,
                youtubeRequest.Entry.ChannelId,
                youtubeRequest.Entry.Title);

            return NoContent();
        }

        var subscriptionSettings = YoutubeSettings.Subscriptions.FirstOrDefault(subscription => subscription.ChannelId == youtubeRequest.Entry.ChannelId);
        if (subscriptionSettings == null)
        {
            logger.LogInformation("YouTube notification received was not in the tracked channels list");
            return NoContent();
        }

        var isNewUpload =
            youtubeRequest.Entry.Published >= DateTimeOffset.UtcNow.AddMinutes(-1) &&
            Math.Abs((youtubeRequest.Entry.Updated - youtubeRequest.Entry.Published).Value.TotalSeconds) < 60;

        if (!isNewUpload)
        {
            logger.LogInformation("YouTube notification received was not a new upload for channel {channelId} with \"{videoTitle}\" as the title", youtubeRequest.Entry.ChannelId, youtubeRequest.Entry.Title);
            return NoContent();
        }

        var matches = RolePingRegex().Matches(subscriptionSettings.MessageTemplate);
        var roles = matches.Select(match => (
                successfulParse: ulong.TryParse(match.Groups[1].Value, out ulong result),
                result
            ))
            .Where(tuple => tuple.successfulParse)
            .Select(tuple => tuple.result)
            .ToList();

        logger.LogInformation("Sending YouTube notification message in channel {channelId} for video {videoId}", subscriptionSettings.DiscordChannelId, youtubeRequest.Entry.VideoId);
        await restClient.SendMessageAsync(
            subscriptionSettings.DiscordChannelId,
            new MessageProperties
            {
                Content = subscriptionSettings.MessageTemplate
                    .Replace("{title}", youtubeRequest.Entry.Title ?? "Unknown Title")
                    .Replace("{url}", $"https://www.youtube.com/watch?v={youtubeRequest.Entry.VideoId}"),
                AllowedMentions = new AllowedMentionsProperties
                {
                    Everyone = subscriptionSettings.MessageTemplate.Contains("@everyone", StringComparison.InvariantCultureIgnoreCase),
                    AllowedRoles = roles,
                },
            });

        return NoContent();
    }

    [XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
    public sealed class YoutubeFeed
    {
        [XmlElement("entry", Namespace = "http://www.w3.org/2005/Atom")]
        public YoutubeEntry? Entry { get; set; }
    }

    public sealed class YoutubeEntry
    {
        [XmlElement("title", Namespace = "http://www.w3.org/2005/Atom")]
        public string? Title { get; set; }

        [XmlElement("published", Namespace = "http://www.w3.org/2005/Atom")]
        public DateTimeOffset? Published { get; set; }

        [XmlElement("updated", Namespace = "http://www.w3.org/2005/Atom")]
        public DateTimeOffset? Updated { get; set; }

        [XmlElement("videoId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
        public string? VideoId { get; set; }

        [XmlElement("channelId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
        public string? ChannelId { get; set; }
    }

    [GeneratedRegex(@"<@&(\d+)>")]
    private static partial Regex RolePingRegex();
}
