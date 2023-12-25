using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Isa.Flow.VkCollector.Data;
using Isa.Flow.VkCollector.Entities;
using Isa.Flow.VkCollector.EventArgs;
using Isa.Flow.VkCollector.Exceptions;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using Isa.Flow.Interact;
using RabbitMQ.Client;
using Isa.Flow.Interact.VkCollector;
using Newtonsoft.Json;

namespace Isa.Flow.VkCollector
{
    public class VkCollector : BaseActor
    {
        private readonly IConfiguration _config;

        public VkCollector(IConfiguration config)
            : base(new ConnectionFactory() { Uri = new Uri(config.GetSection("RabbitMq")["Uri"]!) }, config["ActorId"])
        {
            _config = config;

            AddRpcHandler<StartVkCollectorRequest, VkCollectorResponse>(req =>
            {              
                if (AccessToken == null)
                {
                    using var httpClient = new HttpClient();
                    var response = httpClient.GetAsync($"https://oauth.vk.com/authorize?client_id={_config["ApplicationId"]}&display=page&redirect_uri={_config["RedirectUri"]}&scope=wall&response_type=code&v=5.131").Result;
                    response.EnsureSuccessStatusCode();
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    return new VkCollectorResponse { Response = responseBody };
                }

                _ = StartAsync();

                return new VkCollectorResponse();
            });

            AddRpcHandler<AccessTokenRequest, VkCollectorResponse>(req =>
            {
                if (req.Code != null)
                {
                    using var httpClient = new HttpClient();
                    var response = httpClient.GetAsync($"https://oauth.vk.com/access_token?client_id={_config["ApplicationId"]}&client_secret={_config["Secret"]}&redirect_uri={_config["RedirectUri"]}&code={req.Code}").Result;
                    response.EnsureSuccessStatusCode();

                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody)!;
                    AccessToken = jsonResponse.access_token;

                    _ = StartAsync();

                    return new VkCollectorResponse();
                }

                return new VkCollectorResponse { Response = "Code is null" };
            });

            AddRpcHandler<StopVkCollectorRequest, VkCollectorResponse>(req =>
            {
                _ = StopAsync();

                return new VkCollectorResponse();
            });

            AddRpcHandler<VkCollectorCurrentStateRequest, VkCollectorCurrentStateResponse>(req =>
            {
                return new VkCollectorCurrentStateResponse { IsStarted = IsStarted };
            });
        }

        protected bool IsStarted { get; private set; } = false;
        protected string? AccessToken { get; set; }
        protected VkApi? Api { get; set; }
        protected Exception? Exception { get; set; }
        protected Task? Task { get; set; }
        protected bool SingleChannelHandling { get; set; }
        protected CancellationTokenSource? CancellationTokenSource { get; set; }
        protected Channel? CurrentChannel { get; set; }

        private async Task AuthorizeAsync(CancellationToken cancellationToken)
        {
            Api = new VkApi();

            await Api.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = AccessToken,
                ApplicationId = ulong.Parse(_config["ApplicationId"]!),
                Settings = Settings.All
            }, cancellationToken);
        }

        private async Task CreateQueryAsync(CancellationToken cancellationToken, int startChannelId = 0)
        {
            try
            {
                var random = new Random();
                var db = new DataContext(_config);

                bool includeStartChanId = true;

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var query = await GetChannelsAsync(db, cancellationToken);

                    if (ChannelFilterFunc != null)
                        query = query.Where(ChannelFilterFunc);

                    if (includeStartChanId)
                    {
                        includeStartChanId = false;
                        query = query.Where(s => s.Id >= startChannelId);
                    }

                    ChannelListStarted?.Invoke(this, new ChannelListStartedEventArgs { Count = query.Count() });

                    foreach (var channel in query)
                    {
                        await ChannelProcessingAsync(db, channel, cancellationToken);
                        await Task.Delay(random.Next(2000, 7000), cancellationToken);
                    }

                    ChannelListCompleted?.Invoke(this, new ChannelListCompletedEventArgs { Count = query.Count() });

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
            catch (Exception e)
            {
                Exception = e;
                _ = StopAsync();
            }
        }

        private async Task<IEnumerable<Channel>> GetChannelsAsync(DataContext db, CancellationToken cancellationToken)
        {
            List<SourceDao>? sources = null;
            int atempts = 0;

            do
            {
                try
                {
                    using var t = await db.Database.BeginTransactionAsync(cancellationToken);

                    sources = await db.Source!.OrderBy(s => s.Id)
                        .Where(s => s.Type == "vk")
                        .Where(s => s.Enabled)
                        .ToListAsync(cancellationToken);

                    await t.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    sources = null;
                    atempts++;
                    if (atempts >= 5)
                        throw;

                    await Task.Delay(TimeSpan.FromSeconds(100), cancellationToken);
                }
            }
            while (sources == null);

            return sources.Select(Channel.Map)!;
        }

        private async Task ChannelProcessingAsync(DataContext db, Channel channel, CancellationToken cancellationToken)
        {
            var regexLetters = new Regex("\\p{L}");

            ulong offset = 0;
            ulong pageSize = 100;
            int messageCount = 0;
            int count = 0;

            try
            {
                if (channel == null)
                    ChannelError?.Invoke(this, new ChannelErrorEventArgs { Channel = channel!, Exception = new Exception() });

                ChannelStarted?.Invoke(this, new ChannelStartedEventArgs { Channel = channel! });
                CurrentChannel = channel;

                Random random = new Random();
                WallGetObject? get = null;

                do
                {
                    get = Api?.Wall.Get(new WallGetParams { Domain = channel?.Site, Count = 100, Offset = offset });
                    PostsBatchRead?.Invoke(this, new PostsBatchReadEventArgs { Channel = CurrentChannel!, Count = get!.WallPosts.Count });
                    messageCount += get!.WallPosts.Count;

                    using (var trans = await db.Database.BeginTransactionAsync(cancellationToken))
                    {
                        foreach (var post in get.WallPosts)
                        {
                            if (post.Text == string.Empty || !regexLetters.Match(post.Text).Success)
                                continue;

                            if (cancellationToken.IsCancellationRequested)
                                break;

                            string truncatedMessage = post.Text;
                            if (post.Text.Length > 50)
                                truncatedMessage = post.Text[..50];

                            var index = truncatedMessage.LastIndexOfAny([',', '.', '!', '?', ':', ';', ' ']);
                            if (index >= 0)
                            {
                                truncatedMessage = truncatedMessage[..index];
                            }

                            await db.Article.AddAsync(new ArticleDao
                            {
                                Title = Regex.Replace(truncatedMessage, "[\\p{Z}\\p{Zs}\\p{Zp}]+", " "),
                                PubDate = (DateTime)post.Date!,
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                SourceId = channel!.Id,
                                Body = post.Text,
                                Link = $"{post.FromId}_{post.Id}",
                            }, cancellationToken);

                            var thisChannel = await db.Source.Where(s => s.Id == channel!.Id).FirstOrDefaultAsync(cancellationToken);
                            thisChannel!.Count++;
                            db.Update(thisChannel);
                        }

                        count += await db.SaveChangesAsync(cancellationToken);
                        await trans.CommitAsync(cancellationToken);
                    }

                    offset += pageSize;
                    await Task.Delay(random.Next(1000, 5000), cancellationToken);
                }
                while (get!.TotalCount > (ulong)messageCount);
            }
            catch (OperationCanceledException)
            {
                ;
            }
            catch (Exception ex)
            {
                ChannelError?.Invoke(this, new ChannelErrorEventArgs { Channel = channel, Exception = ex });
                return;
            }

            ChannelSuccess?.Invoke(this, new ChannelSuccessEventArg { Channel = channel!, Messages = messageCount });
        }

        public async Task StartAsync(int startChannelId = 0)
        {
            if (Task != null || SingleChannelHandling)
            {
                throw new AlreadyStartedException();
            }

            CancellationTokenSource = new CancellationTokenSource();

            try
            {
                await AuthorizeAsync(CancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                if (Api != null)
                {
                    Api.Dispose();
                    Api = null;
                }

                LoginError?.Invoke(this, ex);
                return;
            }

            Started?.Invoke(this, new System.EventArgs());
            IsStarted = true;

            Task = CreateQueryAsync(CancellationTokenSource.Token, startChannelId);
        }

        public async Task StopAsync()
        {
            if (Task == null)
            {
                return;
            }

            CancellationTokenSource?.Cancel();

            await Task;

            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;

            Stopped?.Invoke(this, new StoppedEventArgs { Channel = CurrentChannel!, Exception = Exception });

            Exception = null;
            CurrentChannel = null;
            Task = null;

            Api?.Dispose();
            Api = null;

            IsStarted = false;
        }

        public Func<Channel, bool>? ChannelFilterFunc { get; set; }

        public event EventHandler<ChannelListStartedEventArgs>? ChannelListStarted;

        public event EventHandler<ChannelListCompletedEventArgs>? ChannelListCompleted;

        public event EventHandler<Exception>? LoginError;

        public event EventHandler? Started;

        public event EventHandler<StoppedEventArgs>? Stopped;

        public event EventHandler<ChannelStartedEventArgs>? ChannelStarted;

        public event EventHandler<ChannelSuccessEventArg>? ChannelSuccess;

        public event EventHandler<ChannelErrorEventArgs>? ChannelError;

        public event EventHandler<PostsBatchReadEventArgs>? PostsBatchRead;
    }
}