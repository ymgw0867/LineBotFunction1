using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace LineBotFunction1
{

    class ImagemapSampleApp : WebhookApplication
    {
        private LineMessagingClient MessagingClient { get; }

        private BlobStorage BlobStorage { get; }
        private TraceWriter Log { get; }

        public ImagemapSampleApp(LineMessagingClient lineMessagingClient, BlobStorage blobStorage, TraceWriter log)
        {
            MessagingClient = lineMessagingClient;
            BlobStorage = blobStorage;
            Log = log;
        }

        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            Log.WriteInfo($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}, MessageType:{ev.Message.Type}");

            switch (ev.Message.Type)
            {
                case EventMessageType.Image:
                    await ReplyImagemapAsync(ev.ReplyToken, ev.Message.Id, ev.Source.Type + "_" + ev.Source.Id);
                    break;
                case EventMessageType.Text:
                    await MessagingClient.ReplyMessageAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text);
                    break;
            }
        }

        private async Task ReplyImagemapAsync(string replyToken, string messageId, string blobDirectoryName)
        {
            var imageStream = await MessagingClient.GetContentStreamAsync(messageId);
            var image = Image.FromStream(imageStream);

            using (var g = Graphics.FromImage(image))
            {
                g.DrawLine(Pens.Red, image.Width / 2, 0, image.Width / 2, image.Height);
                g.DrawLine(Pens.Red, 0, image.Height / 2, image.Width, image.Height / 2);
            }

            var uri = await UploadImageAsync(1040);
            await UploadImageAsync(700);
            await UploadImageAsync(460);
            await UploadImageAsync(300);
            await UploadImageAsync(240);
            var imageSize = new ImagemapSize(1024, (int)(1040 * (double)image.Height / image.Width));
            var areaWidth = imageSize.Width / 2;
            var areaHeight = imageSize.Height / 2;
            var imagemapMessage = new ImagemapMessage(uri.ToString().Replace("/1040", ""),
                "Sample Imagemap",
                imageSize,
                new IImagemapAction[] {
                    new MessageImagemapAction(new ImagemapArea(0, 0, areaWidth,areaHeight),"Area Top-Left"),
                    new MessageImagemapAction(new ImagemapArea(areaWidth, 0, areaWidth,areaHeight),"Area Top-Right"),
                    new MessageImagemapAction(new ImagemapArea(0, areaHeight, areaWidth,areaHeight),"Area Bottom-Left"),
                    new MessageImagemapAction(new ImagemapArea(areaWidth, areaHeight, areaWidth,areaHeight),"Area Bottom-Right"),
                });

            await MessagingClient.ReplyMessageAsync(replyToken, new[] { imagemapMessage });

            async Task<Uri> UploadImageAsync(int baseSize)
            {
                var img = image.GetThumbnailImage(baseSize, image.Height * baseSize / image.Width, () => false, IntPtr.Zero);
                return await BlobStorage.UploadImageAsync(img, blobDirectoryName + "/" + messageId, baseSize.ToString());
            }
        }

    }
}
